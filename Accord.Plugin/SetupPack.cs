using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotbarTools;
using WrathFork.Contract;

namespace Accord;

/// <summary>The public, reviewed setup data. Never reads another plugin's configuration.</summary>
internal sealed class SetupPack
{
    internal PresetPack Data { get; }
    private readonly Dictionary<uint, JobPreset> jobsById = new();
    internal SetupPack()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Accord.presets.json")
                           ?? throw new InvalidDataException("Missing reviewed Accord setup pack.");
        Data = JsonSerializer.Deserialize<PresetPack>(stream) ?? throw new InvalidDataException("Invalid setup pack.");
        if (Data.SchemaVersion != 1 || Data.WrathVersion != "1.0.4.26")
            throw new InvalidDataException("The setup pack does not match the pinned engine.");
        // Dances are owned by class mechanics; Devilment is the burst initiator.
        var dancer = Data.Jobs.Single(j => j.Job == "DNC");
        dancer.AutoBurst = new PresetVariant();
        dancer.Settings["EnabledActionsV6"]!.AsArray().Add(4018);
        dancer.Settings["EnabledActionsV6"]!.AsArray().Add(4045);
        foreach (var job in Data.Jobs)
        {
            jobsById.Add(job.JobId, job);
            foreach (uint baseClass in job.BaseClasses) jobsById.Add(baseClass, job);
            if (job.Slots.Select(s => s.LogicalSlot).Distinct().Count() != job.Slots.Length)
                throw new InvalidDataException($"Duplicate layout controls for {job.Job}.");
            var enabled = job.Settings["EnabledActionsV6"]!.AsArray().Select(n => n!.GetValue<int>());
            if (enabled.Except(job.OwnedPresetIds).Any())
                throw new InvalidDataException($"Invalid preset ownership for {job.Job}.");
        }
    }

    internal JobPreset? Find(uint job) => jobsById.GetValueOrDefault(job);

    internal static Dictionary<Feature, bool> EngineFeatures(uint job, AccordSettings settings)
    {
        var flags = new Dictionary<Feature, bool>();
        if (job < 19) return flags;
        flags[Feature.BurstContinuation] = true;
        bool protection = settings.Enabled(VariantId.Protection);
        bool support = settings.Enabled(VariantId.Support);
        bool mechanics = settings.Enabled(VariantId.ClassMechanics);
        if (job is 20 or 22 or 30 or 34 or 39 or 41) flags[Feature.MeleeFeintOnAoe] = protection;
        if (job is 25 or 27 or 35 or 42) flags[Feature.CasterAddleOnAoe] = protection;
        if (job is 23 or 31 or 38)
        {
            flags[Feature.RangedMitigationOnSt] = protection;
            flags[Feature.RangedMitigationOnAoe] = protection;
        }
        switch (job)
        {
            case 23: flags[Feature.BardSongInitiation] = mechanics; flags[Feature.BardMinneOnAoe] = support; break;
            case 30: flags[Feature.TcjContinuation] = true; break;
            case 31: flags[Feature.MchDismantleOnSt] = protection; flags[Feature.MchDismantleOnAoe] = protection; break;
            case 33: flags[Feature.AstCardsOnStDamage] = true; flags[Feature.AstCardsOnAoeDamage] = true; break;
            case 35: flags[Feature.RdmBarrierOnAoe] = protection; break;
            case 38:
                flags[Feature.DncStandardInitiation] = mechanics; flags[Feature.DncTechnicalInitiation] = mechanics;
                flags[Feature.DncCuringWaltz] = support; break;
            case 39: flags[Feature.RprCrestOnAoe] = protection; break;
            case 40: flags[Feature.SageKardiaOnStDamage] = support; flags[Feature.SageKardiaOnAoeDamage] = support; break;
            case 42: flags[Feature.PctTemperaOnAoe] = protection; break;
        }
        return flags;
    }

    internal JobPreset Resolve(JobPreset job, AccordSettings settings)
    {
        var result = Variants.Resolve(job, new VariantSelection
        {
            AutoBurst = settings.Enabled(VariantId.Burst),
            AutoMitigation = settings.Enabled(VariantId.Protection),
            AutoMechanics = settings.Enabled(VariantId.ClassMechanics),
        });
        var enabled = result.Settings["EnabledActionsV6"]!.AsArray().Select(n => n!.GetValue<int>()).ToHashSet();
        foreach (var id in SupportPresets(job.JobId))
        {
            if (settings.Enabled(VariantId.Support)) enabled.Add(id);
            else enabled.Remove(id);
        }
        foreach (var id in ProtectionPresets(job.JobId))
        {
            if (settings.Enabled(VariantId.Protection)) enabled.Add(id);
            else enabled.Remove(id);
        }
        foreach (var id in BurstPresets(job.JobId))
        {
            if (settings.Enabled(VariantId.Burst)) enabled.Add(id);
            else enabled.Remove(id);
        }
        if (job.JobId == 33)
            foreach (int id in new[] { 1011, 1044 })
            {
                if (settings.Enabled(VariantId.ClassMechanics)) enabled.Add(id);
                else enabled.Remove(id);
            }
        if (job.Job == "BRD")
        {
            // One button remains for deliberate song initiation; the active song cycle continues.
            enabled.Add(3014);
            AddSlot(11, "The Wanderer's Minuet", "Start / change song");
            AddSlot(12, "The Warden's Paean", "Deliberate cleanse");
        }
        if (job.Job == "DNC")
        {
            AddSlot(11, "Standard Step", "Start a standard dance");
            AddSlot(12, "Devilment", "Burst initiation");
        }
        if (job.Job == "AST")
        {
            AddSlot(11, "Astral Draw", "Initiate a card draw; rotation handles the held cards");
            AddSlot(12, "Divination", "Burst initiation");
        }
        if (job.Job == "WHM") AddSlot(11, "Presence of Mind", "Burst initiation");
        if (job.Job == "SCH") AddSlot(11, "Chain Stratagem", "Burst initiation");
        if (job.Job == "PLD") AddSlot(12, "Cover", "Deliberately protect the selected ally");
        if (job.Job == "WHM") AddSlot(12, "Benediction", "Immediate targeted recovery");
        if (job.Job == "SMN") AddSlot(9, "Lux Solaris", "Deliberate party healing during the available phase");
        result.Settings["EnabledActionsV6"] = new JsonArray(enabled.Order().Select(n => (JsonNode?)JsonValue.Create(n)).ToArray());
        return result;

        void AddSlot(int logical, string action, string function)
        {
            if (result.Slots.Any(s => s.LogicalSlot == logical)) throw new InvalidDataException("Manual intent slot already occupied.");
            var map = Defaults.Maps().Single(m => m.Applies(job.JobId) && !m.SharedAcrossJobs && m.Id.EndsWith("-" + (logical - 1)));
            result.Slots = [..result.Slots, new PresetSlot { LogicalSlot = logical, RegularBar = map.RegularBar,
                RegularSlot = map.RegularSlot, CrossSlot = map.CrossSlot, Action = action, Function = function }];
        }
    }

    private static int[] SupportPresets(uint job) => job switch
    {
        23 => [3047, 3046, 3070], // Paean (both damage paths), Nature's Minne.
        27 => [17028, 17056, 17029, 17059], // Rekindle and the conditional Lux Solaris heal.
        _ => [],
    };

    internal static int[] BurstPresets(uint job) => job switch
    {
        24 => [19008, 19195],
        28 => [16003, 16054],
        33 => [1016, 1043],
        38 => [4019, 4046],
        _ => [],
    };

    internal static int[] ProtectionPresets(uint job) => job switch
    {
        19 => [11086, 11087, 11090, 11094, 11097, 11098, 11103],
        21 => [18139, 18140, 18144, 18145, 18146], // Shake/Equilibrium with the existing automatic guards.
        23 => [3069],
        25 => [2195],
        27 => [17070, 17071, 17082],
        31 => [8118, 8195],
        32 => [5308, 5311, 5312],
        35 => [13024, 13025],
        37 => [7700, 7701, 7711, 7704, 7705, 7709, 7712, 7713, 7714],
        38 => [4034],
        42 => [20070],
        _ => [],
    };

    // Only preset membership is changed after preparation. Variant-specific numeric
    // options are applied once during preparation, never on checkbox/DTR changes.
    internal HashSet<int> ControlledPresets(JobPreset job) => new[] { job.AutoBurst, job.AutoMitigation, job.AutoMechanics }
        .Where(v => v is not null).SelectMany(v => v!.Enable.Concat(v.Disable))
        .Concat(SupportPresets(job.JobId)).Concat(ProtectionPresets(job.JobId)).Concat(BurstPresets(job.JobId))
        .Concat(job.JobId == 33 ? new[] { 1011, 1044 } : Array.Empty<int>()).ToHashSet();

    internal void ApplyMembership(JobPreset job, AccordSettings settings, HashSet<int> enabled)
    {
        var resolved = Resolve(job, settings);
        var controlled = ControlledPresets(job);
        enabled.ExceptWith(controlled);
        enabled.UnionWith(resolved.Settings["EnabledActionsV6"]!.AsArray()
            .Select(n => n!.GetValue<int>()).Where(controlled.Contains));
    }

    internal static void ApplyFragment(Newtonsoft.Json.Linq.JObject destination, JsonObject fragment)
    {
        destination.Merge(Newtonsoft.Json.Linq.JObject.Parse(fragment.ToJsonString()),
            new Newtonsoft.Json.Linq.JsonMergeSettings { MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Replace });
    }
}
