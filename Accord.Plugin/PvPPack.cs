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
    [JsonIgnore]
    public HashSet<uint> ActiveJobs { get; set; } = new();
    [JsonIgnore]
    public Dictionary<VariantId, bool> Variants { get; set; } = new();
    public Dictionary<uint, int> PreparedJobs { get; set; } = new();
    public HashSet<uint> PendingLayouts { get; set; } = new();
    public string ConfigurationId { get; set; } = "";
    [JsonIgnore] public bool LiveMode { get; set; }
    [JsonIgnore] public bool ClearOtherActions { get; set; }
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
    public int KeyboardBar { get; set; } = -1;
    public int Order { get; set; }
    public PvPControl[] Fallbacks { get; set; } = [];
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
    public SlotValue[] RetiredBindings { get; set; } = [];
}

internal sealed class PvPPack
{
    internal const int Revision = 2;
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
            flags["PvPIntentRoutes"] = true;
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
        if(delegated && control.Fallbacks.Length>0)
            delegated=control.Fallbacks.All(c=>Resolve(c,settings,snapshot)==default);
        if (delegated && control.RequiredPreset is { } preset)
            delegated = snapshot["Settings"]!["EnabledActionsV6"]!.Values<int>().Contains(snapshot["PresetIds"]!.Value<int>(preset));
        if (delegated && control.AnyPositive.Length>0)
            delegated=control.AnyPositive.Any(key=>snapshot["Settings"]!["CustomIntValuesV6"]!.Value<int?>(key)>0);
        if (delegated && control.AnyOption is { } array)
            delegated=snapshot["Settings"]!["CustomBoolArrayValuesV6"]?[array]?.Values<bool>().Any(b=>b) == true;
        if (delegated && control.TimingOption is { } timing)
            delegated=snapshot["Settings"]!["CustomIntValuesV6"]!.Value<int?>(timing) is 1 or 2;
        if (delegated) return default;
        if(control.Custom is "PvPIntent") return control.Binding;
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
    // Overflow must not make keyboard setup depend on an unshared controller page.
    // Keep existing pairs stable; only allocate new pairs where both endpoints are usable.
    internal static SlotMap[] AvailableExtras(PvPProfile job, IReadOnlyList<SlotMap> maps, Func<Position,bool> protectedSlot)
    {
        var occupied=maps.Where(m=>m.Applies(job.JobId)).Select(m=>m.Cross).ToHashSet();
        var result=new List<SlotMap>();
        foreach(var extra in Extras(job))
        {
            var existing=maps.SingleOrDefault(m=>m.Id==extra.Id);
            if(existing!=null){result.Add(existing);continue;}
            if(protectedSlot(extra.Regular))continue;
            var free=Enumerable.Range(11,7).SelectMany(bar=>Enumerable.Range(0,16).Select(slot=>new Position(bar,slot)))
                .Where(p=>!protectedSlot(p) && !occupied.Contains(p)).Select(p=>(Position?)p).FirstOrDefault();
            if(free is not { } cross)continue;
            extra.CrossSet=cross.Bar-9;extra.CrossSlot=cross.Slot+1;
            occupied.Add(cross);result.Add(extra);
        }
        return result.ToArray();
    }
    internal static SlotMap[] Pairs(PvPProfile job, IReadOnlyList<SlotMap> maps)
    {
        var overflow=Extras(job);
        return job.Controls.Select((_,i)=>maps.SingleOrDefault(m=>m.Applies(job.JobId) && m.Regular==Positions[i] && !m.SharedAcrossJobs)
            ?? (i>=12 ? overflow.Single(m=>m.Regular==Positions[i]) :
                new SlotMap{Id=$"pvp-keyboard-{job.JobId}-{i}",JobId=job.JobId,Profile="All",RegularBar=Positions[i].Bar+1,RegularSlot=Positions[i].Slot+1})).ToArray();
    }
    internal static Dictionary<string,Position> IntentRoutes(PvPProfile job,SlotMap[] maps,SlotValue[] values,Func<Position,bool> protectedSlot)
    {
        if(protectedSlot(new(0,0)) || protectedSlot(new(0,1)))throw new InvalidOperationException("ST/AoE need unshared job slots at bar 1 slots 1 and 2.");
        var result=new Dictionary<string,Position>();
        foreach(int bar in new[]{0,1})
        {
            var indices=Enumerable.Range(0,maps.Length).Where(i=>job.Controls[i].KeyboardBar==bar)
                .OrderBy(i=>values[i]==default?1:0).ThenBy(i=>job.Controls[i].Order).ToArray();
            var destinations=Enumerable.Range(0,bar==0?8:12).Select(s=>new Position(bar,s)).Where(p=>!protectedSlot(p)).ToArray();
            if(indices.Count(i=>values[i]!=default)>destinations.Length)throw new InvalidOperationException($"Not enough unshared slots on bar {bar+1} for {job.Job}.");
            for(int i=0;i<Math.Min(indices.Length,destinations.Length);i++)result[maps[indices[i]].Id]=destinations[i];
        }
        return result;
    }
}
