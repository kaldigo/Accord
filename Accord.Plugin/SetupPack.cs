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
        if (Data.SchemaVersion != 1 || Data.WrathVersion != "1.0.4.27")
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

    internal JobPreset[] SelectedJobs(AccordSettings settings)
    {
        var ids = (settings.ActiveJobs ?? new HashSet<uint>())
            .Select(id => Find(id)?.JobId).Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        return Data.Jobs.Where(job => ids.Contains(job.JobId)).ToArray();
    }

    internal bool IsActive(uint job, AccordSettings settings) =>
        SelectedJobs(settings).Any(selected => selected.JobId == Find(job)?.JobId);

    // Auto-Mode eligibility is distinct from the engine's master auto-rotation toggle.
    internal Newtonsoft.Json.Linq.JObject AutoModes(JobPreset job, AccordSettings settings, Newtonsoft.Json.Linq.JObject presetIds)
    {
        var resolved = Resolve(job, settings);
        var enabled = resolved.Settings["EnabledActionsV6"]!.AsArray().Select(n => n!.GetValue<int>()).ToHashSet();
        var result = new Newtonsoft.Json.Linq.JObject();
        if (resolved.Settings["AutoActions"] is not JsonObject configured) return result;
        foreach (var entry in configured)
        {
            int id = int.TryParse(entry.Key, out var numeric) ? numeric
                : presetIds[entry.Key]?.ToObject<int>() ?? throw new InvalidDataException("Unknown auto-mode preset: " + entry.Key);
            result[id.ToString()] = enabled.Contains(id);
        }
        return result;
    }

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
            case 20: flags[Feature.MnkMantraOnAoe] = support; flags[Feature.MnkEarthOnAoe] = protection; flags[Feature.MnkEarthReplyOnDamage] = support; break;
            case 34: flags[Feature.SamThirdEyeOnAoe] = protection; break;
            case 23: flags[Feature.BardSongInitiation] = mechanics; flags[Feature.BardMinneOnAoe] = support; break;
            case 24: flags[Feature.WhmDivineCaressOnDamage] = protection; break;
            case 25: flags[Feature.BlmManawardOnAoe] = protection; break;
            case 30: flags[Feature.TcjContinuation] = true; break;
            case 31: flags[Feature.MchFlamethrowerContinuation] = true; flags[Feature.MchWildfireOnAoe] = settings.Enabled(VariantId.Burst); flags[Feature.MchDismantleOnSt] = protection; flags[Feature.MchDismantleOnAoe] = protection; break;
            case 33: flags[Feature.AstCardsOnStDamage] = true; flags[Feature.AstCardsOnAoeDamage] = true; break;
            case 35: flags[Feature.RdmBarrierOnAoe] = protection; break;
            case 38:
                flags[Feature.DncStandardInitiation] = mechanics; flags[Feature.DncTechnicalInitiation] = mechanics;
                flags[Feature.DncCuringWaltz] = support; flags[Feature.DncPartnerOnAoe] = support;
                flags[Feature.DncTargetlessPrepull] = false; // Retired: no added idle dance initiation.
                flags[Feature.DncImprovisationFinish] = true; break;
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
        if (job.JobId == 31)
        {
            if (settings.Enabled(VariantId.ClassMechanics)) enabled.Add(8305);
            else enabled.Remove(8305);
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
        if (job.JobId == 30)
        {
            enabled.ExceptWith(new[] { 10036, 10040 }); // TCJ starts TCJ; damage completes the accepted sequence.
            AddSlot(9, "Meisui", "Deliberately convert Shadow Walker into resources");
        }
        if (job.JobId == 32)
        {
            enabled.Remove(5121); // Interrupt is distinct from the burst request.
            AddSlot(12, "Interject", "Interrupt the selected enemy");
        }
        if (job.JobId == 20) AddSlot(9, "Mantra", "Boost incoming party healing");
        if (job.JobId == 38)
        {
            AddSlot(9, "Closed Position", "Choose or change a dance partner");
            // The distinct manual control must honour the player's selected ally.
            // Automatic damage paths keep their own partner resolver.
            enabled.Remove(4175);
        }
        if (settings.Enabled(VariantId.Burst))
        {
            if (job.JobId is 24 or 28) ReplaceSlot(11, "Empty", "Burst initiation is delegated to ST/AoE");
            if (job.JobId is 33 or 38) ReplaceSlot(12, "Empty", "Burst initiation is delegated to ST/AoE");
            if (job.JobId == 31) ReplaceSlot(3, "Empty", "Burst and overheat are handled by ST/AoE");
            if (job.JobId == 32) ReplaceSlot(3, "Empty", "Burst is handled by ST/AoE; interrupt has its own control");
        }
        if (settings.Enabled(VariantId.ClassMechanics))
        {
            if (job.JobId == 31) ReplaceSlot(6, "Empty", "Conditional Flamethrower initiation and channel holding are handled by AoE");
            if (job.JobId is 23 or 33) ReplaceSlot(11, "Empty", "Song or draw initiation is delegated to ST/AoE");
            if (job.JobId == 38)
            {
                ReplaceSlot(3, "Empty", "Technical dance initiation is delegated to ST/AoE");
                ReplaceSlot(11, "Empty", "Standard dance follows upstream initiation and completion through ST/AoE");
            }
        }
        if (settings.Enabled(VariantId.Support))
        {
            if (job.JobId == 38)
            {
                ReplaceSlot(7, "Empty", "Improvisation and its finish are handled by ST/AoE");
                ReplaceSlot(9, "Empty", "Dance partner selection is delegated to ST/AoE");
            }
            if (job.JobId == 20) ReplaceSlot(9, "Empty", "Mantra is handled by ST/AoE");
            if (job.JobId == 23) ReplaceSlot(6, "Empty", "Nature's Minne is handled by ST/AoE");
        }
        bool protection = settings.Enabled(VariantId.Protection);
        foreach (int id in ManualProtectionPresets(job.JobId))
        {
            if (protection) enabled.Remove(id);
            else if (job.Settings["EnabledActionsV6"]!.AsArray().Any(n => n!.GetValue<int>() == id)) enabled.Add(id);
        }
        var manualProtection = !protection && job.JobId is 19 or 21 or 32 or 37 or 40
            ? result.Slots.Where(s => s.LogicalSlot == 5 || (job.JobId == 19 && s.LogicalSlot == 6))
                .Select(s => (s.Action, s.Function)).ToArray() : [];
        if (protection || job.JobId is 19 or 21 or 32 or 37 or 28 or 40)
        {
            switch (job.JobId)
            {
                case 19:
                    ReplaceSlot(5, "Clemency", "Deliberate targeted healing");
                    ReplaceSlot(6, "Passage of Arms", "Channel party protection");
                    AddSlot(13, "Arm's Length", "Deliberate knockback immunity"); break;
                case 21:
                    ReplaceSlot(5, "Arm's Length", "Deliberate knockback immunity");
                    AddSlot(11, "Equilibrium", "Immediate self recovery"); break;
                case 32:
                    ReplaceSlot(5, "Arm's Length", "Deliberate knockback immunity");
                    AddSlot(11, "Oblation", "Deliberate targeted ally protection"); break;
                case 37:
                    ReplaceSlot(5, "Arm's Length", "Deliberate knockback immunity");
                    AddSlot(11, "Aurora", "Deliberate targeted ally recovery"); break;
                case 24: ReplaceSlot(5, "Asylum", "Deliberate field placement"); break;
                case 28:
                    ReplaceSlot(5, "Sacred Soil", "Deliberate field placement");
                    AddSlot(12, "Deployment Tactics", "Prepare and spread an ally shield"); break;
                case 33: ReplaceSlot(5, "Collective Unconscious", "Channel party protection"); break;
                case 40: ReplaceSlot(5, "Panhaima", "Prepare shielding for repeated hits"); break;
                default:
                    if (job.JobId >= 19) ReplaceSlot(5, "Empty", "Protection is handled by ST/AoE");
                    if (job.JobId == 25) ReplaceSlot(6, "Empty", "Manaward is handled by ST/AoE");
                    if (job.JobId == 34) ReplaceSlot(8, "Empty", "Third Eye / Tengentsu is handled by ST/AoE");
                    if (job.JobId == 39) ReplaceSlot(8, "Empty", "Arcane Crest is handled by ST/AoE");
                    break;
            }
        }
        foreach (var (action, function) in manualProtection)
        {
            if (result.Slots.Any(s => s.Action == action)) continue;
            var spare = result.Slots.FirstOrDefault(s => s.RegularBar == 2 && s.Action == "Empty");
            if (spare is not null) { spare.Action = action; spare.Function = function; }
            else AddSlot(Math.Max(13, result.Slots.Max(s => s.LogicalSlot) + 1), action, function);
        }
        // Imported variant blanks also retire their known baseline action, without
        // granting permission to clear unrelated player assignments.
        foreach (var slot in result.Slots.Where(s => s.Action == "Empty" && s.RetiredAction.Length == 0))
            slot.RetiredAction = job.Slots.SingleOrDefault(s => s.LogicalSlot == slot.LogicalSlot)?.Action ?? "";
        result.Settings["EnabledActionsV6"] = new JsonArray(enabled.Order().Select(n => (JsonNode?)JsonValue.Create(n)).ToArray());
        return result;

        void ReplaceSlot(int logical, string action, string function)
        {
            var slot = result.Slots.Single(s => s.LogicalSlot == logical);
            if(action!=slot.Action && slot.Action!="Empty")slot.RetiredAction=slot.Action;
            if (action == "Empty") slot.RetiredAction = slot.Action != "Empty" ? slot.Action : job.Slots.SingleOrDefault(s => s.LogicalSlot == logical)?.Action ?? "";
            slot.Action = action; slot.Function = function;
        }

        void AddSlot(int logical, string action, string function)
        {
            if (result.Slots.Any(s => s.LogicalSlot == logical)) throw new InvalidDataException("Manual intent slot already occupied.");
            var map = logical >= 13 ? ExtraJobMappings.For(job.JobId, logical) : Defaults.Maps().Single(m => m.Applies(job.JobId) && !m.SharedAcrossJobs && m.Id.EndsWith("-" + (logical - 1)));
            result.Slots = [..result.Slots, new PresetSlot { LogicalSlot = logical, RegularBar = map.RegularBar,
                RegularSlot = map.RegularSlot, CrossSlot = map.CrossSlot, Action = action, Function = function }];
        }
    }

    internal static int[] SupportPresets(uint job) => job switch
    {
        20 => [9097], // Mantra: same party-healing intent on both damage paths.
        38 => [4012, 4032, 4033, 4041, 4022, 4049], // Partner selection and Improvisation on both damage paths.
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
        20 => [9096],
        34 => [15094],
        19 => [11086, 11087, 11090, 11092, 11094, 11097, 11098, 11103],
        21 => [18134, 18136, 18139, 18140, 18144, 18145, 18146, 18150], // Shake/Equilibrium with the existing automatic guards.
        23 => [3069],
        25 => [2195],
        28 => [16034, 16035, 16073, 16077, 16081],
        33 => [1078],
        27 => [17070, 17071, 17082],
        31 => [8118, 8195],
        32 => [5306, 5308, 5311, 5312, 5317],
        35 => [13024, 13025],
        37 => [7700, 7701, 7711, 7704, 7705, 7707, 7709, 7712, 7713, 7714, 7720],
        38 => [4034],
        42 => [20070],
        _ => [],
    };

    internal static int[] ManualProtectionPresets(uint job) => job switch
    {
        19 => [11047, 11048, 11049, 11050, 11051, 11052, 11053, 11054, 11055, 11057, 11063, 11064],
        21 => [18045, 18046, 18047, 18048, 18049, 18050, 18051, 18052, 18053, 18054],
        32 => [5090, 5091, 5092, 5093, 5094, 5095, 5096, 5097, 5098, 5099],
        37 => [7074, 7075, 7076, 7077, 7078, 7079, 7080, 7081, 7082, 7083],
        24 => [19040], 28 => [16082], 33 => [1095], 40 => [14082],
        _ => [],
    };

    // Only preset membership is changed after preparation. Variant-specific numeric
    // options are applied once during preparation, never on checkbox/DTR changes.
    internal HashSet<int> ControlledPresets(JobPreset job) => new[] { job.AutoBurst, job.AutoMitigation, job.AutoMechanics }
        .Where(v => v is not null).SelectMany(v => v!.Enable.Concat(v.Disable))
        .Concat(ManualProtectionPresets(job.JobId)).Concat(SupportPresets(job.JobId)).Concat(ProtectionPresets(job.JobId)).Concat(BurstPresets(job.JobId))
        .Concat(job.JobId == 30 ? new[] { 10036, 10040 } : job.JobId == 32 ? new[] { 5121 } : Array.Empty<int>())
        .Concat(job.JobId == 38 ? new[] { 4175 } : Array.Empty<int>())
        .Concat(job.JobId == 31 ? new[] { 8305 } : Array.Empty<int>())
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
