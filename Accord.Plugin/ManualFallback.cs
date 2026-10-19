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

internal sealed class ManualFallbackRecord
{
    public int Version { get; set; } = 1;
    public string Character { get; set; } = "";
    public string Phase { get; set; } = "Preparing";
    public List<SlotEdit> Slots { get; set; } = new();
}

// A per-character write-ahead record survives crashes, plugin reloads and patch downtime.
// Before contains the exact prior assignments, including the third bar and controller pages.
internal sealed class ManualFallback
{
    private readonly ManualLayoutPack pack = new();
    private ActionCatalog? actions;
    private bool hold;
    internal bool ExitPending { get; private set; }
    internal string Status { get; private set; } = "Manual fallback is ready for normal logout.";
    private string Character => NativeHotbars.CharacterKey(Svc.PlayerState.ContentId);
    private string DirectoryName => Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "manual-fallback", Character);
    private string Journal => Path.Combine(DirectoryName, "pending.json");
    internal bool Pending => Player.Available && Svc.ClientState.IsLoggedIn && Svc.PlayerState.ContentId != 0 && File.Exists(Journal);
    internal bool Holding => hold || ExitPending;
    internal void Login() { hold = false; ExitPending = false; }

    internal unsafe void Prepare(IEnumerable<uint> selected, bool exiting = false)
    {
        uint current = (uint)Player.Job;
        bool pvp = SetupMode();
        HotbarLayoutStore.Ready(pvp);
        if (Svc.PlayerState.ContentId == 0) throw new InvalidOperationException("Character is not ready.");
        if (Pending)
        {
            var pending = ReadRecord(); // Never replace the original snapshot with the manual layout.
            if (pending.Phase != "Manual")
            {
                // A failed prepare may have rolled back to compressed bars. An interrupted
                // restore may be mixed. Finish preparing instead of claiming either is manual.
                var selectedIds = selected.ToHashSet();
                var targets = pack.Jobs.Where(j => selectedIds.Contains(j.JobId))
                    .SelectMany(j => new[] { j.JobId }.Concat(j.BaseClasses)).ToHashSet();
                var retry = new List<SlotEdit>();
                foreach (var slot in pending.Slots.Where(s => targets.Contains(s.Job)))
                {
                    if (NativeHotbars.Ready(current,pvp)->IsHotbarShared((uint)slot.Position.Bar))
                        throw new InvalidOperationException("Manual preparation paused: a destination is now game-shared.");
                    var value = Read(slot.Job,slot.Position,pvp);
                    if (value != slot.Before && value != slot.After)
                        throw new InvalidOperationException("Manual preparation paused: a destination was edited outside the saved layout.");
                    retry.Add(new(slot.Job,slot.Position,value,slot.After));
                }
                pending.Phase = "Preparing";
                JsonStore.Write(Journal,pending);
                Apply(retry,current,pvp);
                NativeHotbars.Flush(current,pvp);
                pending.Phase = "Manual";
                JsonStore.Write(Journal,pending);
            }
            hold = true; ExitPending = exiting;
            NativeHotbars.Flush(current, pvp);
            Status = "Manual hotbars retained; original Accord layout is still backed up.";
            return;
        }
        var ids = selected.ToHashSet();
        var jobs = pack.Jobs.Where(j => ids.Contains(j.JobId)).ToArray();
        if (jobs.Length == 0) { Status = "No selected combat jobs need a manual fallback."; return; }
        var module = NativeHotbars.Ready(current, pvp);
        foreach (var p in ManualLayoutPack.Positions)
            if (module->IsHotbarShared((uint)p.Bar))
                throw new InvalidOperationException($"Manual fallback needs job-specific bars 1–3 and controller pages 1–3. {p} is game-shared.");
        actions ??= new ActionCatalog(Svc.Data.GetExcelSheet<Sheets.Action>(ClientLanguage.English),
            Svc.Data.GetExcelSheet<Sheets.CraftAction>(ClientLanguage.English));
        var record = new ManualFallbackRecord { Character = Character };
        foreach (var job in jobs)
        {
            var layout = ManualLayoutPack.Create(job).ToDictionary(e => e.Key, e => Resolve(job.Job, e.Value));
            foreach (uint target in new[] { job.JobId }.Concat(job.BaseClasses))
                foreach (var (position, value) in layout)
                    record.Slots.Add(new(target, position, Read(target, position, pvp), value));
        }
        Validate(record);
        // Keep an immutable original even if a later recovery record is damaged.
        JsonStore.Write(Path.Combine(DirectoryName, "original-" + Guid.NewGuid().ToString("N") + ".json"), record);
        JsonStore.Write(Journal, record);
        hold = true; // Also suppress live repair if a failed rollback leaves a pending record.
        try
        {
            Apply(record.Slots, current, pvp);
            NativeHotbars.Flush(current, pvp);
            record.Phase = "Manual";
            JsonStore.Write(Journal, record);
            ExitPending = exiting;
            Status = $"Manual layouts saved for {jobs.Length} combat jobs. Normal Accord layouts are backed up.";
        }
        catch
        {
            // Preparing records allow recovery from either side of a partial transaction.
            Status = "Manual fallback failed. Recovery record retained; automatic layout changes are paused.";
            throw;
        }
    }

    internal unsafe void Restore(IEnumerable<uint> selected)
    {
        if (!Pending) return;
        bool pvp = SetupMode();
        uint current = (uint)Player.Job;
        HotbarLayoutStore.Ready(pvp);
        var record = ReadRecord();
        var ids = selected.ToHashSet();
        var targets = pack.Jobs.Where(j => ids.Contains(j.JobId))
            .SelectMany(j => new[] { j.JobId }.Concat(j.BaseClasses)).ToHashSet();
        var edits = new List<SlotEdit>();
        foreach (var slot in record.Slots.Where(s => targets.Contains(s.Job)))
        {
            if (NativeHotbars.Ready(current, pvp)->IsHotbarShared((uint)slot.Position.Bar))
                throw new InvalidOperationException("Manual fallback restore paused: a destination is now game-shared.");
            var value = Read(slot.Job, slot.Position, pvp);
            if (value != slot.Before && value != slot.After)
                throw new InvalidOperationException($"Manual fallback restore paused: {slot.Position} for class {slot.Job} was edited. Your edits and the original backup were retained.");
            if (value != slot.Before) edits.Add(new(slot.Job, slot.Position, value, slot.Before));
        }
        // Mark recovery before restoring: an interrupted restore can contain both snapshots.
        record.Phase = "Restoring";
        JsonStore.Write(Journal, record);
        Apply(edits, current, pvp);
        NativeHotbars.Flush(current, pvp);
        File.Move(Journal, Path.Combine(DirectoryName, "restored-" + Guid.NewGuid().ToString("N") + ".json"));
        hold = false; ExitPending = false;
        Status = "Normal Accord layouts restored. Deselected classes were left unchanged.";
    }

    private SlotValue Resolve(string job, string action) => action switch
    {
        "Empty" => default,
        "Sprint" => new(1, 3),
        "Limit Break" => new(10, 3), // GeneralAction.LimitBreak; upgrades with the party gauge.
        _ => actions!.ResolveSlot(job, action, ManualLayoutPack.AssignedId(job, action))
    };
    private static bool SetupMode() => FallbackMode.Current;
    private static SlotValue Read(uint job, Position p, bool pvp) => NativeHotbars.Read(job, p, live: !pvp, pvp: false);
    private static void Apply(IReadOnlyList<SlotEdit> edits, uint current, bool pvp)
    {
        foreach (var e in edits)
            if (Read(e.Job, e.Position, pvp) != e.Before) throw new InvalidOperationException("Hotbars changed during manual layout preparation.");
        HotbarTransaction.Apply(edits.Where(e => e.Before != e.After).ToArray(),
            (e, undo) => NativeHotbars.Write(current, e.Job, e.Position, undo ? e.Before : e.After, pvp: false, activePvP: pvp), null, null);
    }
    private ManualFallbackRecord ReadRecord()
    {
        var record = JsonSerializer.Deserialize<ManualFallbackRecord>(File.ReadAllText(Journal))
            ?? throw new InvalidDataException("Invalid manual fallback recovery record.");
        Validate(record);
        return record;
    }
    private void Validate(ManualFallbackRecord record)
    {
        var allowed = pack.Jobs.SelectMany(j => new[] { j.JobId }.Concat(j.BaseClasses)).ToHashSet();
        if (record.Version != 1 || record.Character != Character || record.Phase is not ("Preparing" or "Manual" or "Restoring") ||
            record.Slots.Count == 0 || record.Slots.Select(e => (e.Job, e.Position)).Distinct().Count() != record.Slots.Count ||
            record.Slots.Any(e => !allowed.Contains(e.Job) || !ManualLayoutPack.Positions.Contains(e.Position)))
            throw new InvalidDataException("Manual fallback recovery record is invalid or belongs to another character.");
    }
}
