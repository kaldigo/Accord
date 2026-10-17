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
        UpstreamMigrationTests.VerifyModel(type);
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
                count += ActionAccess.Verify(job.Job, changes, names);
                changes["AutoActions"] = pack.AutoModes(job, choices, names);
                var enable = changes["EnabledActionsV6"]!; changes.Remove("EnabledActionsV6");
                var shared = JObject.Parse(pack.Data.SharedSettings.ToJsonString()); shared.Remove("EnabledActionsV6");
                changes.Merge(shared, new Newtonsoft.Json.Linq.JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                SettingsPatch.NormalizePresetMaps(changes, names);
                var updated = SettingsPatch.Apply(snapshot, new JObject { ["Settings"] = changes, ["Enable"] = enable, ["Disable"] = new JArray(job.OwnedPresetIds) }, ids);
                assembly.GetType("WrathCombo.Engine.SettingsSchema")!.GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] {snapshot, updated});
                Newtonsoft.Json.JsonConvert.PopulateObject(updated.ToString(), configuration,
                    new Newtonsoft.Json.JsonSerializerSettings { ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace });
                var roundtrip = JObject.FromObject(configuration);
                SettingsPatch.NormalizePresetMaps(roundtrip, names);
                var expected = (JObject)updated.DeepClone();
                SettingsPatch.NormalizePresetMaps(expected, names);
                foreach (var field in changes.Properties())
                    if (!JToken.DeepEquals(expected[field.Name], roundtrip[field.Name])) throw new Exception("Settings did not round-trip: " + job.Job + "/" + field.Name);
                foreach (var mode in ((JObject)changes["AutoActions"]!).Properties())
                {
                    var id = int.Parse(mode.Name);
                    if (mode.Value.Value<bool>() != updated["EnabledActionsV6"]!.Values<int>().Contains(id)) throw new Exception("Auto-Mode did not follow the selected rotation.");
                    var metadata = assembly.GetType("WrathCombo.Combos.Preset")!;
                    var member = metadata.GetField(Enum.GetName(metadata, id)!)!;
                    if (!member.GetCustomAttributesData().Any(a => a.AttributeType.Name == "AutoActionAttribute")) throw new Exception("Auto-Mode preset lacks runner metadata: " + mode.Name);
                }
                count++;
            }
        var schemaType = assembly.GetType("WrathCombo.Engine.SettingsSchema")!;
        var describe = schemaType.GetMethod("Describe", BindingFlags.NonPublic | BindingFlags.Static)!;
        var validate = schemaType.GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static)!;
        var description = (JObject)describe.Invoke(null, new object[] { snapshot })!;
        if (description["Fields"]!["Throttle"]!.Value<int>("Maximum") != 500) throw new Exception("Discovery lost engine range metadata.");
        count++;
        var outOfRange = (JObject)snapshot.DeepClone(); outOfRange["Throttle"] = 501;
        try { validate.Invoke(null, new object[] { snapshot, outOfRange }); throw new Exception("Out-of-range throttle accepted."); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { count++; }
        var brokenTarget = (JObject)snapshot.DeepClone(); brokenTarget["CustomHealStack"] = new JArray("InventedAllySelector");
        try { validate.Invoke(null, new object[] { snapshot, brokenTarget }); throw new Exception("Unknown target selector accepted."); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { count++; }
        var presets = description["Presets"]!.OfType<JObject>().ToDictionary(p => p.Value<int>("Id"));
        var conflict = presets.Values.First(p => p["Parent"]!.Type == JTokenType.Null && p["Conflicts"]!.Values<int>().Any(id => presets[id]["Parent"]!.Type == JTokenType.Null));
        int other = conflict["Conflicts"]!.Values<int>().First(id => presets[id]["Parent"]!.Type == JTokenType.Null);
        var conflicting = (JObject)snapshot.DeepClone(); conflicting["EnabledActionsV6"] = new JArray(conflict.Value<int>("Id"), other);
        try { validate.Invoke(null, new object[] { snapshot, conflicting }); throw new Exception("Conflicting root presets accepted."); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { count++; }
        // Simulate importing personal combined settings with every global variant selection.
        for (int mask = 0; mask < 16; mask++)
        {
            var choices = new AccordSettings { ActiveJobs = pack.Data.Jobs.Select(j => j.JobId).ToHashSet(), LiveMode = true, LastHotbarBackup = "retained-backup.json" };
            foreach (var variant in VariantDefinitions.All) choices.Variants[variant.Id] = (mask & (1 << (int)variant.Id)) != 0;
            var source = (JObject)snapshot.DeepClone();
            source["Version"] = 6; source["Accord"] = JObject.FromObject(choices);
            var envelope = new JObject { ["Settings"] = snapshot.DeepClone(), ["KnownPresets"] = new JArray(ids), ["PresetIds"] = names.DeepClone(), ["Revision"] = SettingsTransaction.Hash(snapshot) };
            var (plan, importedChoices) = MigrationPreparation.Build(pack, source, envelope, new AccordSettings());
            var imported = SettingsPatch.Apply(snapshot, (JObject)plan["Patch"]!, ids);
            assembly.GetType("WrathCombo.Engine.SettingsSchema")!.GetMethod("Validate", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] {snapshot, imported});
            if (importedChoices.LiveMode || importedChoices.LastHotbarBackup != "retained-backup.json" || !pack.Data.Jobs.All(j => importedChoices.PreparedJobs[j.JobId] == j.SetupRevision)) throw new Exception("Migration lost recovery metadata or would reset imported details.");
            foreach (var field in SettingsPatch.Fields.Where(f => f.StartsWith("Custom") && f.EndsWith("V6")))
                if (!JToken.DeepEquals(snapshot[field], imported[field])) throw new Exception("Migration reset personalised settings: " + field);
            count++;
        }
        foreach (var selection in new[] { new HashSet<uint>(), new HashSet<uint> { 38 } })
        {
            var choices = new AccordSettings { ActiveJobs = selection };
            var source = (JObject)snapshot.DeepClone();
            source["Version"] = 6;
            source["Accord"] = JObject.FromObject(choices);
            var envelope = new JObject { ["Settings"] = snapshot, ["KnownPresets"] = new JArray(ids), ["PresetIds"] = names, ["Revision"] = SettingsTransaction.Hash(snapshot) };
            var (plan, importedChoices) = MigrationPreparation.Build(pack, source, envelope, new AccordSettings());
            var original = SettingsMigration.Plan(source, envelope);
            var originalSettings = SettingsPatch.Apply(snapshot, (JObject)original["Patch"]!, ids);
            var imported = SettingsPatch.Apply(snapshot, (JObject)plan["Patch"]!, ids);
            if (!importedChoices.ActiveJobs!.SetEquals(selection)) throw new Exception("Import expanded the active selection from prepared history.");
            foreach (var job in pack.Data.Jobs.Where(j => !selection.Contains(j.JobId)))
            {
                foreach (var mode in pack.AutoModes(job, choices, names).Properties())
                    if (!JToken.DeepEquals(imported["AutoActions"]?[mode.Name], originalSettings["AutoActions"]?[mode.Name])) throw new Exception("Import changed an inactive job's Auto-Mode.");
                if (!JToken.DeepEquals(imported["ForkFeatures"]?[job.JobId.ToString()], originalSettings["ForkFeatures"]?[job.JobId.ToString()])) throw new Exception("Import remapped an inactive job's features.");
                if (!imported["EnabledActionsV6"]!.Values<int>().Intersect(job.OwnedPresetIds).ToHashSet().SetEquals(originalSettings["EnabledActionsV6"]!.Values<int>().Intersect(job.OwnedPresetIds))) throw new Exception("Import remapped an inactive job's presets.");
            }
            count++;
        }
        var notice = assembly.GetType("WrathCombo.Window.MajorChangesWindow")!;
        var upstreamVersion = (Version)assembly.GetType("WrathCombo.Engine.UpstreamVersion")!.GetField("Current", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        if (upstreamVersion.ToString() != pack.Data.WrathVersion) throw new Exception("Notice version differs from the inherited engine baseline.");
        if (!(bool)notice.GetField("DoesVersionHaveChange", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!) throw new Exception("Fork package version suppressed the upstream migration notice.");
        var freshConfiguration = Activator.CreateInstance(configuration.GetType())!;
        if (!upstreamVersion.Equals(configuration.GetType().GetField("HideMajorChangesForVersion")!.GetValue(freshConfiguration))) throw new Exception("New config notice default uses fork package version.");
        count += 3;
        if ((string?)pluginType.GetField("Command", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue() != "/wrath") throw new Exception("Fork changed the upstream /wrath command.");
        if ((string?)pluginType.GetField("OldCommand", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue() != "/scombo") throw new Exception("Fork removed the upstream /scombo alias.");
        count += 2;
        count += PvPTests.Run(snapshot,names,ids,(before,after)=>{
            var warnings=(JArray)validate.Invoke(null,new object[]{before,after})!;
            if(warnings.Values<string>().Any(w=>w!.Contains("PvP")))throw new Exception("Inactive PvP parent: "+warnings);
        });
        return count;
    }
}
