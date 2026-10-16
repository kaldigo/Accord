using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace WrathFork.Contract;

public static class SettingsMigration
{
    public static JObject Plan(JObject source, JObject snapshot)
    {
        if (source["Version"]?.Type != JTokenType.Integer || source.Value<int?>("Version") != 6 || source["EnabledActionsV6"] is not JArray oldEnabled)
            throw new ArgumentException("Expected a version 6 combined prototype configuration.");
        var current = (JObject)snapshot["Settings"]!;
        var known = snapshot["KnownPresets"]!.Values<int>().ToHashSet();
        var names = (JObject)snapshot["PresetIds"]!;
        var settings = new JObject();
        var omitted = new JArray();
        foreach (var entry in source.Properties())
        {
            if (entry.Name == "EnabledActionsV6") continue;
            if (!SettingsPatch.Fields.Contains(entry.Name) || current[entry.Name] is null)
            { omitted.Add(entry.Name); continue; }
            if (entry.Name.StartsWith("Custom") && entry.Name.EndsWith("V6"))
            {
                if (entry.Value is not JObject map) throw new ArgumentException("Invalid source map: " + entry.Name);
                var selected = new JObject();
                foreach (var value in map.Properties())
                    if (current[entry.Name]![value.Name] is not null) selected[value.Name] = value.Value.DeepClone();
                    else omitted.Add(entry.Name + "." + value.Name);
                settings[entry.Name] = selected;
            }
            else settings[entry.Name] = entry.Value.DeepClone();
        }
        foreach (string field in new[] { "AutoActions", "BurstPresets" })
            if (settings[field] is JObject map)
                foreach (var p in map.Properties().ToArray())
                    if (!(int.TryParse(p.Name, out int id) ? known.Contains(id) : names[p.Name] is not null))
                    { omitted.Add(field + "." + p.Name); p.Remove(); }
        SettingsPatch.NormalizePresetMaps(settings, names);
        var enabled = new JArray();
        foreach (var token in oldEnabled)
        {
            if (token.Type != JTokenType.Integer) throw new ArgumentException("Invalid source preset ID.");
            if (known.Contains(token.Value<int>())) enabled.Add(token.Value<int>()); else omitted.Add("EnabledActionsV6." + token);
        }
        var patch = new JObject { ["Settings"] = settings, ["Enable"] = enabled,
            ["Disable"] = new JArray(current["EnabledActionsV6"]!.Values<int>().Where(known.Contains)) };
        SettingsPatch.Apply(current, patch, known);
        return new JObject { ["Patch"] = patch, ["Omitted"] = omitted,
            ["SourceHash"] = SettingsTransaction.Hash(source), ["ExpectedRevision"] = snapshot["Revision"]!.DeepClone() };
    }
}
