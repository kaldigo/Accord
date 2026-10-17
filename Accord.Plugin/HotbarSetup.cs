using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using HotbarTools;
using Sheets = Lumina.Excel.Sheets;

namespace Accord;

internal sealed class HotbarSetup
{
    internal string Notice { get; private set; } = "";
    private ActionCatalog? actions;
    private bool BridgeLoaded => Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "HotbarBridge" && p.IsLoaded);
    private string ReadBridge(string name) => Svc.PluginInterface.GetIpcSubscriber<string>("HotbarBridge." + name).InvokeFunc();
    private void SetRoutes(Dictionary<uint, Dictionary<string, Position>> routes)
    {
        if (!Svc.PluginInterface.GetIpcSubscriber<string, bool>("HotbarBridge.SetJobRoutes").InvokeFunc(JsonSerializer.Serialize(routes)))
            throw new InvalidOperationException("Hotbar Bridge rejected layout routes.");
    }

    private HashSet<Position> ProtectedSlots() => JsonSerializer.Deserialize<HashSet<Position>>(ReadBridge("PvP.GetProtectedSlots"))
        ?? throw new InvalidDataException("Update Hotbar Bridge to protect bars carried between PvE and PvP.");

    internal sealed class Backup
    {
        public bool PvP { get; set; }
        public string Character { get; set; } = "";
        public List<SlotEdit> Edits { get; set; } = new();
        public Dictionary<uint, Dictionary<string, Position>>? Routes { get; set; }
    }

    internal string Apply(IEnumerable<(uint Job, JobPreset Preset)> jobs, bool cleanup) =>
        PlanAndApply(jobs, cleanup, false).Backup!;

    internal bool NeedsRefresh(uint job, JobPreset preset, bool cleanup) =>
        PlanAndApply([(job, preset)], cleanup, true).Changed;

    private unsafe (string? Backup, bool Changed) PlanAndApply(IEnumerable<(uint Job, JobPreset Preset)> jobs, bool cleanup, bool inspectOnly)
    {
        jobs = jobs.ToArray();
        uint current = (uint)Player.Job;
        var module = NativeHotbars.Ready(current);
        string character = NativeHotbars.CharacterKey(Svc.PlayerState.ContentId);
        bool bridge = BridgeLoaded;
        if (!bridge)
            throw new InvalidOperationException("Load Hotbar Bridge before applying compact hotbars, so controller routes and shared slots can be preserved.");
        if (!inspectOnly && bridge && !Svc.PluginInterface.GetIpcSubscriber<bool>("HotbarBridge.BeginExternalEdit").InvokeFunc())
            throw new InvalidOperationException("Hotbar Bridge is busy. Try again.");
        try
        {
            var protectedSlots = ProtectedSlots();
            bool Protected(Position p) => protectedSlots.Contains(p) || NativeHotbars.Ready(current)->IsHotbarShared((uint)p.Bar);
            Notice = "";
            var extras = jobs.Where(j => j.Preset.Slots.Any(s => s.LogicalSlot > 12))
                .Select(j => ExtraJobMappings.For(j.Job)).Where(m => !Protected(m.Cross)).ToArray();
            if (!inspectOnly && bridge && extras.Length > 0 &&
                !Svc.PluginInterface.GetIpcSubscriber<string, bool>("HotbarBridge.EnsureJobMappings").InvokeFunc(JsonSerializer.Serialize(extras)))
                throw new InvalidOperationException("Update Hotbar Bridge to support the extra Paladin control.");
            List<SlotMap>? maps = bridge ? JsonSerializer.Deserialize<List<SlotMap>>(ReadBridge("GetMapping")) : null;
            if (bridge && maps is null) throw new InvalidDataException("Bridge mapping is unavailable.");
            if (maps is not null) Defaults.Validate(maps);
            if (inspectOnly && extras.Any(extra => maps!.All(map => map.Id != extra.Id || map.JobId != extra.JobId)))
                return (null, true);
            var oldRoutes = bridge ? JsonSerializer.Deserialize<Dictionary<uint, Dictionary<string, Position>>>(ReadBridge("GetJobRoutes"))
                ?? throw new InvalidDataException("Bridge routes are unavailable.") : null;
            var routes = bridge ? new Dictionary<uint, Dictionary<string, Position>>() : null;
            var beforeRoutes = bridge ? new Dictionary<uint, Dictionary<string, Position>>() : null;
            var edits = new Dictionary<(uint, Position), SlotEdit>();
            actions ??= new ActionCatalog(Svc.Data.GetExcelSheet<Sheets.Action>(ClientLanguage.English),
                Svc.Data.GetExcelSheet<Sheets.CraftAction>(ClientLanguage.English));
            foreach (var (job, preset) in jobs)
            {
                var keep = new HashSet<Position>();
                // Vacated keyboard positions may contain actions moved by this or a prior variant.
                // Clear only recognised design actions when general cleanup is disabled.
                var designActions = preset.Slots.SelectMany(s => new[] { s.Action, s.RetiredAction })
                    .Where(a => !string.IsNullOrEmpty(a) && a != "Empty")
                    .Select(a => actions.ResolveSlot(preset.Job, a)).ToHashSet();
                var effectiveMaps = maps ?? ExtraJobMappings.Merge(Defaults.Maps(), extras, new());
                if (routes is not null)
                {
                    beforeRoutes![job] = oldRoutes!.GetValueOrDefault(job) ?? new();
                    routes[job] = new();
                }
                foreach (var entry in PveLayoutPlan.Create(job, preset, effectiveMaps, Protected))
                {
                    var slot = entry.Slot;
                    var value = slot is null ? default : actions.ResolveSlot(preset.Job, slot.Action);
                    if (routes is not null && entry.Registered) routes[job][entry.Map.Id] = entry.Regular;
                    var retired = string.IsNullOrEmpty(slot?.RetiredAction) ? default : actions.ResolveSlot(preset.Job, slot.RetiredAction);
                    Add(entry.Regular, value, retired, true);
                    if (entry.Cross is { } cross) Add(cross, value, retired);
                    else if (value != default) Notice = " Some controls are keyboard-only because their controller slots are shared or unavailable. Shared bars were preserved.";
                }
                if (routes is not null) LayoutRoutes.Effective(effectiveMaps.Where(m => m.Applies(job)), routes[job]);
                if (cleanup)
                    foreach (var edit in HotbarCleanup.Plan(job, keep, effectiveMaps,
                                 bar => NativeHotbars.Ready(current)->IsHotbarShared((uint)bar), p => NativeHotbars.Read(job, p)))
                        if (!protectedSlots.Contains(edit.Position)) edits.Add((job, edit.Position), edit);

                void Add(Position position, SlotValue value, SlotValue retired, bool keyboard = false)
                {
                    if (!position.Valid || !keep.Add(position)) throw new InvalidDataException("Invalid or duplicate layout destination.");
                    if (protectedSlots.Contains(position) || effectiveMaps.Any(m => m.SharedAcrossJobs && (m.Regular == position || m.Cross == position)))
                        throw new InvalidOperationException($"{position} overlaps a protected shared Bridge slot.");
                    if (NativeHotbars.Ready(current)->IsHotbarShared((uint)position.Bar))
                        throw new InvalidOperationException($"{position} is a shared game hotbar. Choose job-specific bars first.");
                    // With cleanup off, an unused design position is not permission
                    // to erase an unrelated action. Recognised moved/retired design actions may be removed.
                    var old = NativeHotbars.Read(job, position);
                    if (value == default && !cleanup && (retired == default || old != retired) &&
                        !(keyboard && designActions.Contains(old))) return;
                    if (old != value) edits.Add((job, position), new(job, position, old, value));
                }
            }
            if (inspectOnly) return (null, HotbarRefreshPolicy.NeedsApply(edits.Count, routes!, oldRoutes!));
            return (Commit(current, new Backup { Character = character, Edits = edits.Values.ToList(), Routes = beforeRoutes }, routes), true);
        }
        finally
        {
            if (!inspectOnly && bridge) Svc.PluginInterface.GetIpcSubscriber<bool>("HotbarBridge.EndExternalEdit").InvokeFunc();
        }
    }

    private unsafe string Commit(uint current, Backup backup, Dictionary<uint, Dictionary<string, Position>>? routes)
    {
        NativeHotbars.Ready(current);
        if (backup.PvP) throw new InvalidDataException("A PvP backup cannot be restored to PvE hotbars.");
        if (backup.Character != NativeHotbars.CharacterKey(Svc.PlayerState.ContentId))
            throw new InvalidOperationException("Character changed during hotbar setup.");
        if (backup.Edits.Select(e => (e.Job, e.Position)).Distinct().Count() != backup.Edits.Count)
            throw new InvalidDataException("Duplicate backup destinations.");
        List<SlotMap>? map = BridgeLoaded ? JsonSerializer.Deserialize<List<SlotMap>>(ReadBridge("GetMapping")) : null;
        var protectedSlots = ProtectedSlots();
        foreach (var edit in backup.Edits)
        {
            if (protectedSlots.Contains(edit.Position) || NativeHotbars.Ready(current)->IsHotbarShared((uint)edit.Position.Bar) ||
                map?.Any(m => m.SharedAcrossJobs && (m.Regular == edit.Position || m.Cross == edit.Position)) == true)
                throw new InvalidOperationException("A backup destination is now shared. Restore was blocked to protect shared controls.");
        }
        if (routes is not null)
        {
            if (map is null) throw new InvalidOperationException("Bridge mapping is unavailable.");
            foreach (var (job, route) in routes) LayoutRoutes.Effective(map.Where(m => m.Applies(job)), route);
        }
        var directory = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "hotbar-backups", backup.Character);
        var path = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-ffff}-{Guid.NewGuid():N}.json");
        foreach (var edit in backup.Edits)
            if (NativeHotbars.Read(edit.Job, edit.Position) != edit.Before) throw new InvalidOperationException("Hotbars changed while preparing the operation.");
        JsonStore.Write(path, backup);
        try
        {
            HotbarTransaction.Apply(backup.Edits,
                (edit, undo) => NativeHotbars.Write(current, edit.Job, edit.Position, undo ? edit.Before : edit.After),
                routes is null ? null : () => SetRoutes(routes),
                backup.Routes is null ? null : () => SetRoutes(backup.Routes));
            return path;
        }
        catch (Exception original)
        {
            throw new InvalidOperationException($"Hotbar setup failed. Recovery backup: {path}", original);
        }
    }

    internal string Restore(string path)
    {
        uint current = (uint)Player.Job;
        var backup = JsonSerializer.Deserialize<Backup>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid Accord hotbar backup.");
        if (backup.PvP) throw new InvalidDataException("A PvP backup cannot be restored to PvE hotbars.");
        if (backup.Character != NativeHotbars.CharacterKey(Svc.PlayerState.ContentId))
            throw new InvalidOperationException("This backup belongs to another character.");
        if (backup.Routes is not null && !BridgeLoaded) throw new InvalidOperationException("Load Bridge to restore this backup's routes.");
        bool bridge = BridgeLoaded;
        if (bridge && !Svc.PluginInterface.GetIpcSubscriber<bool>("HotbarBridge.BeginExternalEdit").InvokeFunc())
            throw new InvalidOperationException("Hotbar Bridge is busy.");
        try
        {
            var oldRoutes = bridge ? JsonSerializer.Deserialize<Dictionary<uint, Dictionary<string, Position>>>(ReadBridge("GetJobRoutes"))! : null;
            var undo = new Backup
            {
                Character = backup.Character,
                Edits = backup.Edits.Select(e => new SlotEdit(e.Job, e.Position, NativeHotbars.Read(e.Job, e.Position), e.Before)).ToList(),
                Routes = backup.Routes?.Keys.ToDictionary(j => j, j => oldRoutes!.GetValueOrDefault(j) ?? new()),
            };
            return Commit(current, undo, backup.Routes);
        }
        finally
        {
            if (bridge) Svc.PluginInterface.GetIpcSubscriber<bool>("HotbarBridge.EndExternalEdit").InvokeFunc();
        }
    }
}
