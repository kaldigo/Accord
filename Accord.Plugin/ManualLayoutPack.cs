using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HotbarTools;

namespace Accord;

internal sealed class ManualJobLayout
{
    public uint JobId { get; set; }
    public string Job { get; set; } = "";
    public uint[] BaseClasses { get; set; } = [];
    public string Role { get; set; } = "";
    public string[] Actions { get; set; } = [];
    public string[] RoleActions { get; set; } = [];
    public string[] Omitted { get; set; } = [];
}

// Ordinary assigned actions only. No engine IPC, combos, macros or game-action execution.
internal sealed class ManualLayoutPack
{
    // Seraphism's replacement has the same English name but must not be assigned directly.
    internal static uint? AssignedId(string job, string name) => job == "SCH" && name == "Emergency Tactics" ? 3586u : null;
    internal ManualJobLayout[] Jobs { get; }
    internal ManualLayoutPack()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Accord.manual-layouts.json")
            ?? throw new InvalidDataException("Missing manual layouts.");
        Jobs = JsonSerializer.Deserialize<ManualJobLayout[]>(stream) ?? throw new InvalidDataException("Invalid manual layouts.");
        if (Jobs.Length != 21 || Jobs.Select(j => j.JobId).Distinct().Count() != 21)
            throw new InvalidDataException("Expected all 21 manual combat layouts.");
        foreach (var job in Jobs) Create(job); // Validate the entire pack before any native writes.
    }

    internal static readonly Position[] Positions = Enumerable.Range(0, 3)
        .SelectMany(bar => Enumerable.Range(0, 12).Select(slot => new Position(bar, slot)))
        .Concat(Enumerable.Range(10, 3).SelectMany(bar => Enumerable.Range(0, 16).Select(slot => new Position(bar, slot)))).ToArray();

    internal static Dictionary<Position, string> Create(ManualJobLayout job)
    {
        var result = Enumerable.Range(0, 3).SelectMany(bar => Enumerable.Range(0, 12)
            .Select(slot => new Position(bar, slot))).ToDictionary(p => p, _ => "Empty");
        var all = job.Actions.Concat(job.RoleActions).ToArray();
        if (all.Length > 34 || all.Any(string.IsNullOrWhiteSpace) || all.Contains("Empty") ||
            all.Distinct(StringComparer.OrdinalIgnoreCase).Count() != all.Length)
            throw new InvalidDataException($"Invalid three-bar manual layout for {job.Job}.");
        // Same role controls occupy the same locations across jobs. Primary rotation fills
        // the first bar; frequently used cooldowns/heals follow; overflow stays on bar three.
        var roleOrder = job.Role switch
        {
            "Tank" => new[] { "Rampart", "Reprisal", "Arm's Length", "Provoke", "Interject", "Low Blow", "Shirk" },
            "Healer" => ["Swiftcast", "Lucid Dreaming", "Esuna", "Surecast", "Rescue", "Repose"],
            "Melee" => ["Second Wind", "Bloodbath", "Feint", "Arm's Length", "True North", "Leg Sweep"],
            "Physical ranged" => ["Second Wind", "Head Graze", "Arm's Length", "Peloton", "Leg Graze", "Foot Graze"],
            "Caster" => ["Swiftcast", "Lucid Dreaming", "Addle", "Surecast", "Sleep"],
            _ => throw new InvalidDataException("Unknown manual role.")
        };
        if (job.RoleActions.Except(roleOrder).Any()) throw new InvalidDataException("Unknown role action.");
        for (int i = 0; i < roleOrder.Length; i++)
            if (job.RoleActions.Contains(roleOrder[i]))
                result[i < 5 ? new Position(1, 7 + i) : new Position(2, i - 5)] = roleOrder[i];
        result[new(2, 10)] = "Limit Break";
        result[new(2, 11)] = "Sprint";
        var available = result.Where(p => p.Value == "Empty").Select(p => p.Key).ToArray();
        if (job.Actions.Length > available.Length) throw new InvalidDataException("Manual layout exceeds three bars.");
        for (int i = 0; i < job.Actions.Length; i++) result[available[i]] = job.Actions[i];
        // Three controller pages mirror the regular rows, with the main rotation on face buttons.
        int[] crossSlots = [15, 14, 13, 11, 12, 10, 8, 9, 7, 6, 5, 4];
        foreach (var entry in result.ToArray()) result[new(entry.Key.Bar + 10, crossSlots[entry.Key.Slot])] = entry.Value;
        foreach (var p in Positions) result.TryAdd(p, "Empty");
        return result;
    }
}
