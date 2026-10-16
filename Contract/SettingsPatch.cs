using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace WrathFork.Contract;

/// <summary>Wire-level validation. No plugin instances or rotation implementation cross IPC.</summary>
public static class SettingsPatch
{
    public static readonly string[] Fields = ["AutoActions", "BurstPresets", "CustomFloatValuesV6",
        "CustomIntValuesV6", "CustomIntArrayValuesV6", "CustomBoolValuesV6", "CustomBoolArrayValuesV6",
        "CustomHealStack", "RaiseStack", "RetargetHealingActionsToStack", "UseCustomHealStack",
        "UseFieldMouseoverOverridesInDefaultHealStack", "UseFocusTargetOverrideInDefaultHealStack",
        "UseLowestHPOverrideInDefaultHealStack", "UseUIMouseoverOverridesInDefaultHealStack",
        "DancerDanceCompatActionIDs", "ForkFeatures", "BlockSpellOnMove", "ActionChanging",
        "QueueAdjust", "QueueAdjustThreshold", "OverwriteQueue", "Throttle", "PenaltyPause", "MovementLeeway",
        "OpenerTimeout", "MeleeOffset", "InterruptDelay", "MaximumWeavesPerWindow", "UseExperimentalHP",
        "AddOutOfPartyNPCsToRetargeting", "CustomActionSettings", "ActiveBLUSpells", "IgnoredNPCs"];

    public static void NormalizePresetMaps(JObject settings, JObject presetIds)
    {
        foreach (string name in new[] { "AutoActions", "BurstPresets" })
            if (settings[name] is JObject map)
                settings[name] = new JObject(map.Properties().Select(p => new JProperty(
                    int.TryParse(p.Name, out _) ? p.Name : presetIds.Value<int?>(p.Name)?.ToString() ?? throw new ArgumentException("Unknown preset: " + p.Name), p.Value.DeepClone())));
    }

    public static JObject Apply(JObject current, JObject patch, ISet<int> knownPresets)
    {
        if (patch.Properties().Any(p => p.Name is not ("Settings" or "Enable" or "Disable")))
            throw new ArgumentException("Unknown patch member.");
        var result = (JObject)current.DeepClone();
        if (patch["Settings"] is not JObject changes) throw new ArgumentException("Settings must be an object.");
        foreach (var p in changes.Properties())
        {
            if (!Fields.Contains(p.Name)) throw new ArgumentException("Unsupported setting: " + p.Name);
            if (p.Name == "ForkFeatures")
            {
                if (p.Value is not JObject jobs) throw new ArgumentException("Invalid feature map.");
                foreach (var job in jobs.Properties())
                {
                    if (!uint.TryParse(job.Name, out uint id) || id is < 1 or > 42 || job.Value is not JObject features)
                        throw new ArgumentException("Invalid feature job.");
                    foreach (var feature in features.Properties())
                        if (!Enum.TryParse<Feature>(feature.Name, out var f) || !Enum.IsDefined(f) || feature.Name != f.ToString() || !FeatureScope.Supports(id, f) || feature.Value.Type != JTokenType.Boolean)
                            throw new ArgumentException("Invalid engine feature: " + feature.Name);
                }
            }
            else if (p.Name == "CustomActionSettings")
            {
                if (p.Value is not JObject options || current[p.Name] is not JObject schema) throw new ArgumentException("Invalid custom action settings.");
                foreach (var option in options.Properties())
                    if (schema[option.Name]?.Type != JTokenType.Boolean || option.Value.Type != JTokenType.Boolean) throw new ArgumentException("Invalid custom action option.");
            }
            else if (p.Name == "ActiveBLUSpells")
            {
                if (p.Value is not JArray spells || spells.Count > 256 || spells.Any(t => t.Type != JTokenType.Integer || t.Value<long>() < 0 || t.Value<long>() > uint.MaxValue)) throw new ArgumentException("Invalid BLU spell list.");
            }
            else if (p.Name == "IgnoredNPCs")
            {
                if (p.Value is not JObject npcs || npcs.Count > 10000 || npcs.Properties().Any(e => !uint.TryParse(e.Name, out _) || e.Value.Type != JTokenType.Integer || e.Value.Value<long>() < 0 || e.Value.Value<long>() > uint.MaxValue)) throw new ArgumentException("Invalid ignored NPC map.");
            }
            else if (p.Name is "AutoActions" or "BurstPresets")
            {
                if (p.Value is not JObject flags) throw new ArgumentException("Invalid preset map.");
                foreach (var flag in flags.Properties())
                    if (!int.TryParse(flag.Name, out int id) || !knownPresets.Contains(id) || flag.Value.Type != JTokenType.Boolean)
                        throw new ArgumentException("Unknown preset or invalid preset value: " + flag.Name);
            }
            else if (p.Name.StartsWith("Custom") && p.Name.EndsWith("V6"))
            {
                if (p.Value is not JObject values || current[p.Name] is not JObject schema)
                    throw new ArgumentException("Invalid custom setting map.");
                foreach (var entry in values.Properties())
                {
                    if (schema[entry.Name] is not { } template) throw new ArgumentException("Unknown setting: " + entry.Name);
                    if (p.Name is "CustomIntArrayValuesV6" or "CustomBoolArrayValuesV6")
                    {
                        if (entry.Value is not JArray items || items.Count > 256) throw new ArgumentException("Invalid option array.");
                        foreach (var item in items) ValidateValue(item, p.Name == "CustomIntArrayValuesV6" ? new JValue(0) : new JValue(false), entry.Name);
                    }
                    else ValidateValue(entry.Value, template, entry.Name);
                }
            }
            else if (p.Name is "CustomHealStack" or "RaiseStack")
            {
                if (p.Value is not JArray stack || stack.Count > 64 || stack.Any(t => t.Type != JTokenType.String || string.IsNullOrWhiteSpace(t.Value<string>())))
                    throw new ArgumentException("Invalid target stack.");
            }
            else
                ValidateValue(p.Value, current[p.Name] ?? throw new ArgumentException("Unavailable setting: " + p.Name), p.Name);
        }
        result.Merge(changes, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
        // Normalise to the engine's float storage before computing a transaction revision.
        foreach (var field in current.Properties())
        {
            if (field.Value.Type == JTokenType.Float && result[field.Name] is { } scalar)
                result[field.Name] = new JValue(scalar.Value<float>());
            if (field.Name == "CustomFloatValuesV6" && result[field.Name] is JObject floats)
                foreach (var value in floats.Properties().ToArray()) value.Value = new JValue(value.Value.Value<float>());
        }
        var enabled = (current["EnabledActionsV6"] ?? new JArray()).Values<int>().ToHashSet();
        foreach (string operation in new[] { "Disable", "Enable" })
        {
            if (patch[operation] is not JArray ids) throw new ArgumentException(operation + " must be an array.");
            foreach (var token in ids)
            {
                if (token.Type != JTokenType.Integer || !knownPresets.Contains(token.Value<int>()))
                    throw new ArgumentException("Unknown preset ID.");
                if (operation == "Enable") enabled.Add(token.Value<int>()); else enabled.Remove(token.Value<int>());
            }
        }
        result["EnabledActionsV6"] = new JArray(enabled.Order());
        return result;
    }

    private static void ValidateValue(JToken value, JToken template, string name)
    {
        if (template is JArray array)
        {
            if (value is not JArray values || values.Count > 256) throw new ArgumentException("Invalid array: " + name);
            if (array.Count == 0 && values.Count != 0) throw new ArgumentException("No array schema for: " + name);
            foreach (var item in values) ValidateValue(item, array[0]!, name);
            // Fixed-size options and priority lists must not create out-of-range indexing.
            if (array.Count != values.Count) throw new ArgumentException("Array length mismatch: " + name);
        }
        else if (template.Type == JTokenType.Float && value.Type is JTokenType.Integer or JTokenType.Float)
        {
            if ((!double.IsFinite(value.Value<double>()) || Math.Abs(value.Value<double>()) > float.MaxValue)) throw new ArgumentException("Invalid number: " + name);
        }
        else if (template.Type != value.Type || value.Type is JTokenType.Null or JTokenType.Object)
            throw new ArgumentException("Invalid setting type: " + name);
        if (value.Type == JTokenType.Integer && (value.Value<long>() < int.MinValue || value.Value<long>() > (name == "DancerDanceCompatActionIDs" ? uint.MaxValue : int.MaxValue)))
            throw new ArgumentException("Integer out of bounds: " + name);
    }
}
