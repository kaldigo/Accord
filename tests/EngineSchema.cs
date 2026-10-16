using System.Reflection;
using System.Runtime.Loader;
using Newtonsoft.Json.Linq;
using Accord;
using WrathFork.Contract;

internal static class EngineSchema
{
    internal static int Verify(string path, SetupPack pack)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var sdk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher/addon/Hooks/dev");
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            foreach (var root in new[] { dir, sdk })
                if (File.Exists(Path.Combine(root, name.Name + ".dll"))) return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(root, name.Name + ".dll"));
            return null;
        };
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path));
        var type = assembly.GetType("WrathCombo.Core.Configuration")!;
        var configuration = Activator.CreateInstance(type)!;
        assembly.GetType("WrathCombo.Services.Service")!.GetProperty("Configuration", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, configuration);
        assembly.GetType("WrathCombo.Window.Tabs.Debug")?.GetField("DebugConfig", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(null, true);
        var pluginType = assembly.GetType("WrathCombo.WrathCombo")!;
        var plugin = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(pluginType);
        pluginType.GetField("P", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, plugin);
        var searchField = pluginType.GetField("IPCSearch", BindingFlags.Instance | BindingFlags.NonPublic)!;
        searchField.SetValue(plugin, System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(searchField.FieldType));
        var user = assembly.GetType("WrathCombo.CustomComboNS.Functions.UserData")!;
        foreach (var t in assembly.GetTypes())
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                if (user.IsAssignableFrom(field.FieldType) && t.FullName != "WrathCombo.Combos.PvE.BST") _ = field.GetValue(null);
        var snapshot = JObject.FromObject(configuration);
        var ids = Enum.GetValues(assembly.GetType("WrathCombo.Combos.Preset")!).Cast<object>().Select(Convert.ToInt32).ToHashSet();
        var unknown = new SortedSet<string>();
        foreach (var job in pack.Data.Jobs)
            foreach (var map in JObject.Parse(job.Settings.ToJsonString()).Properties().Where(p => p.Name.StartsWith("Custom") && p.Name.EndsWith("V6")))
                foreach (var entry in ((JObject)map.Value).Properties())
                    if (snapshot[map.Name]?[entry.Name] is null) unknown.Add(map.Name + ":" + entry.Name);
        if (unknown.Count > 0) throw new Exception("Unregistered settings: " + string.Join(", ", unknown));
        var names = new JObject(Enum.GetValues(assembly.GetType("WrathCombo.Combos.Preset")!).Cast<object>().Select(p => new JProperty(p.ToString()!, Convert.ToInt32(p))));
        int count = 0;
        foreach (var job in pack.Data.Jobs)
            for (int mask = 0; mask < 16; mask++)
            {
                var choices = new AccordSettings();
                foreach (var variant in VariantDefinitions.All) choices.Variants[variant.Id] = (mask & (1 << (int)variant.Id)) != 0;
                var changes = JObject.Parse(pack.Resolve(job, choices).Settings.ToJsonString());
                var enable = changes["EnabledActionsV6"]!; changes.Remove("EnabledActionsV6");
                var shared = JObject.Parse(pack.Data.SharedSettings.ToJsonString()); shared.Remove("EnabledActionsV6");
                changes.Merge(shared, new Newtonsoft.Json.Linq.JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                SettingsPatch.NormalizePresetMaps(changes, names);
                var updated = SettingsPatch.Apply(snapshot, new JObject { ["Settings"] = changes, ["Enable"] = enable, ["Disable"] = new JArray(job.OwnedPresetIds) }, ids);
                Newtonsoft.Json.JsonConvert.PopulateObject(updated.ToString(), configuration,
                    new Newtonsoft.Json.JsonSerializerSettings { ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace });
                var roundtrip = JObject.FromObject(configuration);
                SettingsPatch.NormalizePresetMaps(roundtrip, names);
                var expected = (JObject)updated.DeepClone();
                SettingsPatch.NormalizePresetMaps(expected, names);
                foreach (var field in changes.Properties())
                    if (!JToken.DeepEquals(expected[field.Name], roundtrip[field.Name])) throw new Exception("Settings did not round-trip: " + job.Job + "/" + field.Name);
                count++;
            }
        return count;
    }
}
