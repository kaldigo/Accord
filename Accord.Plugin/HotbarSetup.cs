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
    private HashSet<Position> ProtectedSlots() => HotbarLayoutStore.ProtectedSlots();

    internal sealed class Backup
    {
        public bool PvP { get; set; }
        public string Character { get; set; } = "";
        public List<SlotEdit> Edits { get; set; } = new();
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
        HotbarLayoutStore.Ready(false);
        var protectedSlots = ProtectedSlots();
        bool Protected(Position p) => protectedSlots.Contains(p) || NativeHotbars.Ready(current)->IsHotbarShared((uint)p.Bar);
        Notice = "";
        var extras = jobs.SelectMany(j => j.Preset.Slots.Where(s=>s.LogicalSlot>12)
            .Select(s => ExtraJobMappings.For(j.Job,s.LogicalSlot))).Where(m => !Protected(m.Cross)).ToArray();
        var maps = ExtraJobMappings.Merge(Defaults.Maps(), extras, new());
        Defaults.Validate(maps);
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
            var effectiveMaps = maps;
            bool existingSprint = ModeActions.HasSharedSprint(_=>true, p=>NativeHotbars.Read(job,p));
            designActions.Add(new SlotValue(1,3));
            foreach (var entry in PveLayoutPlan.Create(job, preset, effectiveMaps, Protected, sprint:!existingSprint))
            {
                var slot = entry.Slot;
                var value = slot is null ? default : slot.Action == "Sprint" ? new SlotValue(1,3) : actions.ResolveSlot(preset.Job, slot.Action);
                var retired = string.IsNullOrEmpty(slot?.RetiredAction) ? default : actions.ResolveSlot(preset.Job, slot.RetiredAction);
                Add(entry.Regular, value, retired, true);
                if (entry.Cross is { } cross) Add(cross, value, retired);
                else if (value != default) Notice = " Some controls are keyboard-only because their controller slots are shared or unavailable. Shared bars were preserved.";
            }
            // Clear recognised assignments at vacated destinations, including
            // positions used by an earlier compact layout or Sprint fallback.
            foreach(var p in Enumerable.Range(0,2).SelectMany(b=>Enumerable.Range(0,12).Select(s=>new Position(b,s))))
                if(!keep.Contains(p) && !Protected(p) && designActions.Contains(NativeHotbars.Read(job,p)))Add(p,default,default,true);
            if (cleanup)
                foreach (var edit in HotbarCleanup.Plan(job, keep, effectiveMaps,
                             bar => NativeHotbars.Ready(current)->IsHotbarShared((uint)bar), p => NativeHotbars.Read(job, p)))
                    if (!Protected(edit.Position) && !ModeActions.IsSprint(edit.Before)) edits.Add((job, edit.Position), edit);

            void Add(Position position, SlotValue value, SlotValue retired, bool keyboard = false)
            {
                if (!position.Valid || !keep.Add(position)) throw new InvalidDataException("Invalid or duplicate layout destination.");
                if (protectedSlots.Contains(position) || effectiveMaps.Any(m => m.SharedAcrossJobs && (m.Regular == position || m.Cross == position)))
                    throw new InvalidOperationException($"{position} overlaps a protected shared slot.");
                if (NativeHotbars.Ready(current)->IsHotbarShared((uint)position.Bar))
                    throw new InvalidOperationException($"{position} is a shared game hotbar. Choose job-specific bars first.");
                // With cleanup off, an unused design position is not permission
                // to erase an unrelated action. Recognised moved/retired design actions may be removed.
                var old = NativeHotbars.Read(job, position);
                if (value == default && ModeActions.IsSprint(old)) return;
                if (value == default && !cleanup && (retired == default || old != retired) &&
                    !(keyboard && designActions.Contains(old))) return;
                if (old != value) edits.Add((job, position), new(job, position, old, value));
            }
        }
        if (inspectOnly) return (null, edits.Count != 0);
        return (Commit(current, new Backup { Character = character, Edits = edits.Values.ToList() }), true);
    }

    private unsafe string Commit(uint current, Backup backup)
    {
        NativeHotbars.Ready(current);
        if (backup.PvP) throw new InvalidDataException("A PvP backup cannot be restored to PvE hotbars.");
        if (backup.Character != NativeHotbars.CharacterKey(Svc.PlayerState.ContentId))
            throw new InvalidOperationException("Character changed during hotbar setup.");
        if (backup.Edits.Select(e => (e.Job, e.Position)).Distinct().Count() != backup.Edits.Count)
            throw new InvalidDataException("Duplicate backup destinations.");
        var map = Defaults.Maps();
        HotbarLayoutStore.Ready(false);
        var protectedSlots = ProtectedSlots();
        foreach (var edit in backup.Edits)
        {
            if (protectedSlots.Contains(edit.Position) || NativeHotbars.Ready(current)->IsHotbarShared((uint)edit.Position.Bar) ||
                map?.Any(m => m.SharedAcrossJobs && (m.Regular == edit.Position || m.Cross == edit.Position)) == true)
                throw new InvalidOperationException("A backup destination is now shared. Restore was blocked to protect shared controls.");
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
                null, null);
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
        HotbarLayoutStore.Ready(false);
        var undo = new Backup
        {
            Character = backup.Character,
            Edits = backup.Edits.Select(e => new SlotEdit(e.Job, e.Position, NativeHotbars.Read(e.Job, e.Position), e.Before)).ToList(),
        };
        return Commit(current, undo);
    }
}
