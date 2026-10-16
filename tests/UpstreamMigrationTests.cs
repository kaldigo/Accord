using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WrathCombo.Engine;

internal static class UpstreamMigrationTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value) { checks++; if (!value) throw new Exception("Upstream migration protection failed."); }
        var directory = Path.Combine(Path.GetTempPath(), "wrath-migration-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "WrathCombo.json");
        var target = Path.Combine(directory, "WrathFork.json");
        try
        {
            Check(!UpstreamSettingsMigration.ImportIfMissing(target, _ => throw new Exception("Unexpected validation")));
            const string original = """
                {"$type":"WrathCombo.Core.Configuration, WrathCombo", "Version":6,
                 "EnabledActionsV6":[4022], "RotationConfig":{"Enabled":true},
                 "CustomIntValuesV6":{"threshold":42}, "UnknownFutureSetting":"kept",
                 "nested":{"$type":"System.Collections.Generic.List`1[[WrathCombo.Combos.Preset, WrathCombo]], System.Private.CoreLib"},
                 "text":"WrathCombo"}
                """;
            File.WriteAllText(source, original);
            Check(UpstreamSettingsMigration.ImportIfMissing(target, text => JObject.Parse(text)));
            var migrated = JObject.Parse(File.ReadAllText(target));
            Check((string?)migrated["$type"] == "WrathCombo.Core.Configuration, WrathFork");
            Check(((string)migrated["nested"]!["$type"]!).Contains("Preset, WrathFork]]"));
            Check((string?)migrated["UnknownFutureSetting"] == "kept" && (string?)migrated["text"] == "WrathCombo");
            Check(migrated["CustomIntValuesV6"]!["threshold"]!.Value<int>() == 42);
            Check(migrated["RotationConfig"]!["Enabled"]!.Value<bool>());
            Check(File.ReadAllText(source) == original);
            foreach (var existing in new[] { File.ReadAllText(target), "{}", "", "broken json" })
            {
                File.WriteAllText(target, existing);
                Check(!UpstreamSettingsMigration.ImportIfMissing(target, _ => throw new Exception("Existing configuration touched")));
                Check(File.ReadAllText(target) == existing);
            }
            File.Delete(target);
            File.WriteAllText(source, "broken json");
            try { UpstreamSettingsMigration.ImportIfMissing(target, _ => { }); Check(false); }
            catch (InvalidOperationException) { Check(!File.Exists(target)); }
            File.WriteAllText(source, original);
            try { UpstreamSettingsMigration.ImportIfMissing(target, _ => throw new JsonSerializationException("Invalid field")); Check(false); }
            catch (InvalidOperationException) { Check(!File.Exists(target)); }
            try { UpstreamSettingsMigration.ImportIfMissing(target, _ => File.WriteAllText(target, "concurrent settings")); Check(false); }
            catch (InvalidOperationException) { Check(File.ReadAllText(target) == "concurrent settings"); }
            Check(Directory.GetFiles(directory, "*.import-*").Length == 0);
            Check(File.ReadAllText(source) == original);
        }
        finally { Directory.Delete(directory, true); }
        return checks;
    }

    internal static void VerifyModel(Type type)
    {
        var config = Activator.CreateInstance(type)!;
        var json = JObject.FromObject(config);
        var original = json.ToString();
        json["HideChildren"] = true;
        json["EnabledActionsV6"] = new JArray(4022);
        json["CustomIntValuesV6"] = new JObject { ["migration-test"] = 42 };
        json["RotationConfig"]!["Enabled"] = true;
        var imported = JsonConvert.DeserializeObject(json.ToString(), type, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None })!;
        var roundtrip = JObject.FromObject(imported);
        if (!roundtrip.Value<bool>("HideChildren") || !roundtrip["RotationConfig"]!.Value<bool>("Enabled") ||
            roundtrip["CustomIntValuesV6"]!.Value<int>("migration-test") != 42 || roundtrip["EnabledActionsV6"]![0]!.Value<int>() != 4022)
            throw new Exception("Real engine configuration lost upstream settings during import.");
        var directory = Path.Combine(Path.GetTempPath(), "wrath-model-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var typed = JsonConvert.SerializeObject(imported, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })
                .Replace(", WrathFork", ", WrathCombo");
            File.WriteAllText(Path.Combine(directory, "WrathCombo.json"), typed);
            var target = Path.Combine(directory, "WrathFork.json");
            UpstreamSettingsMigration.ImportIfMissing(target, text => JsonConvert.DeserializeObject(text, type,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None }));
            var loaded = JsonConvert.DeserializeObject(File.ReadAllText(target), type,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto })!;
            if (!JToken.DeepEquals(roundtrip, JObject.FromObject(loaded)))
                throw new Exception("Assembly identity migration changed configuration values.");
        }
        finally { Directory.Delete(directory, true); }
        JsonConvert.DeserializeObject(original, type);
    }
}
