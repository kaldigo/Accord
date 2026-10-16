using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using WrathCombo.Core;
using WrathCombo.CustomComboNS;
using WrathFork.Contract;

namespace WrathCombo.Engine;

internal static class SettingsSchema
{
    private sealed record PresetInfo(Preset Preset, string InternalName, uint Job, Preset? Parent, Preset[] Conflicts);
    // Read metadata without constructing action-hook attributes or requiring a logged-in player.
    private static readonly PresetInfo[] Presets = typeof(Preset).GetFields(BindingFlags.Public | BindingFlags.Static).Select(field =>
    {
        var data = field.GetCustomAttributesData();
        var parent = data.FirstOrDefault(a => a.AttributeType.Name == "ParentComboAttribute");
        var conflicts = data.FirstOrDefault(a => a.AttributeType.Name == "ConflictingCombosAttribute");
        var job = data.FirstOrDefault(a => a.AttributeType.Name == "JobInfoAttribute");
        return new PresetInfo((Preset)field.GetValue(null)!, field.Name, job is null ? 0 : Convert.ToUInt32(job.ConstructorArguments[0].Value),
            parent is null ? null : (Preset)Convert.ToInt32(parent.ConstructorArguments[0].Value),
            conflicts is null ? [] : ((System.Collections.Generic.IEnumerable<CustomAttributeTypedArgument>)conflicts.ConstructorArguments[0].Value!).Select(v => (Preset)Convert.ToInt32(v.Value)).ToArray());
    }).ToArray();
    private static readonly System.Collections.Generic.Dictionary<Preset, PresetInfo> ById = Presets.ToDictionary(p => p.Preset);
    internal static JObject Describe(JObject snapshot)
    {
        var fields = new JObject();
        foreach (var name in SettingsPatch.Fields)
        {
            if (snapshot[name] is not { } value) continue;
            var descriptor = new JObject { ["Type"] = value.Type.ToString(), ["Shape"] = value.DeepClone() };
            var limits = typeof(Configuration).GetField(name)?.GetCustomAttribute<Attributes.Setting>();
            descriptor["Minimum"] = limits?.MinInt is int min ? min : limits?.MinFloat;
            descriptor["Maximum"] = limits?.MaxInt is int max ? max : limits?.MaxFloat;
            descriptor["RangePolicy"] = limits is null ? "Registered storage type; job-specific predicates retain engine semantics" : "Engine setting attribute bounds";
            fields[name] = descriptor;
        }
        return new JObject { ["SettingsSchemaVersion"] = 2, ["Fields"] = fields,
            ["TargetSelectors"] = new JArray(typeof(SimpleTarget).GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name)),
            ["Features"] = new JObject(Enum.GetValues<Feature>().Select(f => new JProperty(f.ToString(), new JObject {
                ["Default"] = FeatureDefaults.Enabled(f), ["Jobs"] = new JArray(Enumerable.Range(1,42).Where(j => FeatureScope.Supports((uint)j, f))) }))),
            ["Presets"] = new JArray(Presets.Select(p => new JObject {
                ["Id"] = (int)p.Preset, ["Name"] = p.InternalName, ["Job"] = p.Job,
                ["Parent"] = p.Parent is { } parent ? (int)parent : (int?)null, ["Conflicts"] = new JArray(p.Conflicts.Select(c => (int)c)) })),
            ["InactiveChildrenPolicy"] = "Retained as preferences; do not enable their parent implicitly",
            ["MaxOptionArrayLength"] = 256, ["MaxTargetStackLength"] = 64 };
    }

    internal static JArray Validate(JObject current, JObject updated)
    {
        foreach (string name in SettingsPatch.Fields)
        {
            if (JToken.DeepEquals(current[name], updated[name])) continue;
            var limits = typeof(Configuration).GetField(name)?.GetCustomAttribute<Attributes.Setting>();
            if (updated[name]?.Type is JTokenType.Integer or JTokenType.Float)
            {
                double value = updated[name]!.Value<double>();
                double? min = limits?.MinInt is int lo ? lo : limits?.MinFloat;
                double? max = limits?.MaxInt is int hi ? hi : limits?.MaxFloat;
                if (min.HasValue && value < min || max.HasValue && value > max) throw new ArgumentException("Setting outside engine bounds: " + name);
            }
        }
        foreach (string name in new[] { "CustomHealStack", "RaiseStack" })
            foreach (var target in updated[name]!.Values<string>())
                if (typeof(SimpleTarget).GetProperty(target!) is null) throw new ArgumentException("Unknown target selector: " + target);
        var enabled = updated["EnabledActionsV6"]!.Values<int>().ToHashSet();
        var prior = current["EnabledActionsV6"]!.Values<int>().ToHashSet();
        bool Active(Preset p, System.Collections.Generic.HashSet<int> set) => set.Contains((int)p) &&
            ((ById.TryGetValue(p, out var info) ? info.Parent : null) is not { } parent || Active(parent, set));
        var warnings = new JArray();
        foreach (var p in Presets.Where(p => enabled.Contains((int)p.Preset)))
        {
            if (!Active(p.Preset, enabled)) { warnings.Add("Inactive parent for " + p.InternalName); continue; }
            foreach (var other in p.Conflicts.Where(c => Active(c, enabled)))
                if (!Active(p.Preset, prior) || !Active(other, prior)) throw new ArgumentException("Conflicting presets: " + p.InternalName + " / " + other);
        }
        return warnings;
    }
}
