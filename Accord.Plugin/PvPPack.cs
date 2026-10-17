using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HotbarTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Accord;

public sealed class PvPSettings
{
    public HashSet<uint> ActiveJobs { get; set; } = new();
    public Dictionary<VariantId, bool> Variants { get; set; } = new();
    public Dictionary<uint, int> PreparedJobs { get; set; } = new();
    public HashSet<uint> PendingLayouts { get; set; } = new();
    public string ConfigurationId { get; set; } = "";
    public bool LiveMode { get; set; }
    public bool ClearOtherActions { get; set; }
    public string LastHotbarBackup { get; set; } = "";
    public bool Enabled(VariantId id) => Variants.GetValueOrDefault(id);
}

internal sealed class PvPControl
{
    public string Name { get; set; } = "";
    public SlotValue Binding { get; set; }
    public string Intent { get; set; } = "";
    public VariantId? Variant { get; set; }
    public string? RequiredPreset { get; set; }
    public string? Custom { get; set; }
    public string[] AnyPositive { get; set; } = [];
    public string? AnyOption { get; set; }
    public string? TimingOption { get; set; }
}

internal sealed class PvPProfile
{
    public uint JobId { get; set; }
    public string Job { get; set; } = "";
    public Dictionary<VariantId,string> Descriptions { get; set; } = new();
    public PvPControl[] Controls { get; set; } = [];
    public string[] Always { get; set; } = [];
    public string[] Manual { get; set; } = [];
    public Dictionary<VariantId, string[]> Variants { get; set; } = new();
    public Dictionary<string, VariantId> Features { get; set; } = new();
    public string[] RequiredBools { get; set; } = [];
    public Dictionary<string, int> RequiredInts { get; set; } = new();
    public string[] RecipientFeatures { get; set; } = [];
}

internal sealed class PvPPack
{
    internal const int Revision = 1;
    internal PvPProfile[] Jobs { get; }
    internal PvPPack()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Accord.pvp-presets.json")!;
        using var reader = new StreamReader(stream);
        Jobs = JsonConvert.DeserializeObject<PvPProfile[]>(reader.ReadToEnd())!;
        if (Jobs.Length != 21 || Jobs.Select(j => j.JobId).Distinct().Count() != 21)
            throw new InvalidDataException("Invalid PvP profiles.");
    }
    internal PvPProfile? Find(uint id) => Jobs.SingleOrDefault(j => j.JobId == id);
    internal JObject Patch(IEnumerable<PvPProfile> jobs, PvPSettings settings, JObject snapshot)
    {
        var enable = new HashSet<int>();
        var disable = new HashSet<int>();
        var features = new JObject();
        var bools = new JObject();
        var ints = new JObject();
        int Id(string name) => snapshot["PresetIds"]!.Value<int?>(name) ?? throw new InvalidDataException("Unavailable PvP setting: " + name);
        foreach (var job in jobs)
        {
            foreach (var name in job.Always) enable.Add(Id(name));
            foreach (var name in job.Manual) disable.Add(Id(name));
            foreach (var (variant, names) in job.Variants)
                foreach (var name in names) (settings.Enabled(variant) ? enable : disable).Add(Id(name));
            var flags = new JObject();
            foreach (var (feature, variant) in job.Features) flags[feature] = settings.Enabled(variant);
            foreach (var feature in job.RecipientFeatures) flags[feature] = true;
            features[job.JobId.ToString()] = flags;
            // These are required follow-through/branch switches, not personal timing or HP policy.
            foreach (var key in job.RequiredBools) bools[key] = true;
            foreach (var (key, value) in job.RequiredInts) ints[key] = value;
        }
        return new JObject { ["Settings"] = new JObject { ["ForkFeatures"] = features,
            ["CustomBoolValuesV6"] = bools, ["CustomIntValuesV6"] = ints },
            ["Enable"] = new JArray(enable.Order()), ["Disable"] = new JArray(disable.Order()) };
    }

    internal static SlotValue Resolve(PvPControl control, PvPSettings settings, JObject snapshot)
    {
        bool delegated = control.Variant is { } variant && settings.Enabled(variant);
        if (delegated && control.RequiredPreset is { } preset)
            delegated = snapshot["Settings"]!["EnabledActionsV6"]!.Values<int>().Contains(snapshot["PresetIds"]!.Value<int>(preset));
        if (delegated && control.AnyPositive.Length>0)
            delegated=control.AnyPositive.Any(key=>snapshot["Settings"]!["CustomIntValuesV6"]!.Value<int?>(key)>0);
        if (delegated && control.AnyOption is { } array)
            delegated=snapshot["Settings"]!["CustomBoolArrayValuesV6"]?[array]?.Values<bool>().Any(b=>b) == true;
        if (delegated && control.TimingOption is { } timing)
            delegated=snapshot["Settings"]!["CustomIntValuesV6"]!.Value<int?>(timing) is 1 or 2;
        if (delegated) return default;
        return control.Custom == "SingleTargetDPS" && snapshot["Settings"]!["CustomActionSettings"]?.Value<bool>("SingleTargetDPS") == true
            ? new SlotValue(1, 1_000_000) : control.Binding;
    }
}

// Controller positions remain stable across variants; keyboard gaps close within each bar.
internal static class PvPLayout
{
    internal static readonly Position[] Positions = [new(0,0),new(0,1),new(0,2),new(0,3),new(1,0),new(1,1),
        new(1,2),new(1,3),new(1,11),new(1,10),new(1,4),new(1,5),new(0,4),new(0,5),new(0,6),new(0,7),new(1,6),new(1,7),new(1,8),new(1,9)];
    internal static SlotMap[] Extras(PvPProfile job) => Enumerable.Range(12, Math.Max(0,job.Controls.Length-12)).Select(i => new SlotMap {
        Id=$"pvp-{job.JobId}-{i}", Label=$"{job.Job} PvP: {job.Controls[i].Name}", JobId=job.JobId, Profile="All",
        RegularBar=Positions[i].Bar+1, RegularSlot=Positions[i].Slot+1, CrossSet=2, CrossSlot=i-11 }).ToArray();
    internal static HashSet<int> Fixed(PvPProfile job) => job.Controls.Select((c,i)=>(c,i)).Where(x=>x.i==0 ||
        x.c.Name is "Fuma Shuriken" or "Blizzard" or "Cure II" or "Aspected Benefic" or "Adloquium" ||
        x.c.Binding.Id is 29055 or 29711 or 29056 or 29054 or 29057 or 43259 ||
        x.c.Intent.Contains("move", StringComparison.OrdinalIgnoreCase) || x.c.Intent.Contains("escape", StringComparison.OrdinalIgnoreCase))
        .Select(x=>x.i).ToHashSet();
    internal static Dictionary<string, Position> Routes(SlotMap[] maps, SlotValue[] values, ISet<int>? fixedSlots=null)
    {
        if (maps.Length != values.Length) throw new InvalidDataException("Incomplete PvP layout.");
        var result = new Dictionary<string, Position>();
        fixedSlots ??= new HashSet<int>{0};
        foreach(var i in fixedSlots) result[maps[i].Id]=maps[i].Regular;
        foreach (var group in Enumerable.Range(0,maps.Length).Where(i=>!fixedSlots.Contains(i)).GroupBy(i=>maps[i].Regular.Bar))
        {
            var destinations = group.Select(i=>maps[i].Regular).OrderBy(p=>p.Slot).ToArray();
            // Damage stays in its original slot. Other active actions precede empty positions.
            var order = group.OrderBy(i=>i==0 ? 0 : values[i]==default ? 2 : 1).ThenBy(i=>maps[i].Regular.Slot).ToArray();
            for (int k=0;k<order.Length;k++) result[maps[order[k]].Id]=destinations[k];
        }
        return result;
    }
}
