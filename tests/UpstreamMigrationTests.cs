using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WrathCombo.Engine;

internal static class UpstreamMigrationTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value) { checks++; if (!value) throw new Exception("Identity migration protection failed."); }
        var directory = Path.Combine(Path.GetTempPath(), "wrath-migration-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "WrathFork.json");
        var target = Path.Combine(directory, "WrathCombo.json");
        var marker = target + ".wrathfork-migration.json";
        const string original = """
            {"$type":"WrathCombo.Core.Configuration, WrathFork", "Version":6,
             "EnabledActionsV6":[4022], "RotationConfig":{"Enabled":true},
             "CustomIntValuesV6":{"threshold":42}, "UnknownFutureSetting":"kept",
             "ForkConfigurationId":"preserved-id", "ForkOperationReceipts":{"_Sequence":19},
             "nested":{"$type":"System.Collections.Generic.List`1[[WrathCombo.Combos.Preset, WrathFork]], System.Private.CoreLib"},
             "text":"WrathFork"}
            """;
        try
        {
            File.WriteAllText(target, "old stock settings");
            Check(!ForkIdentityMigration.ImportOnce(target, _ => throw new Exception("Unexpected validation")));
            Check(File.ReadAllText(target) == "old stock settings");
            File.WriteAllText(source, original);
            Check(ForkIdentityMigration.ImportOnce(target, text => JObject.Parse(text)));
            var migrated = JObject.Parse(File.ReadAllText(target));
            Check((string?)migrated["$type"] == "WrathCombo.Core.Configuration, WrathCombo");
            Check(((string)migrated["nested"]!["$type"]!).Contains("Preset, WrathCombo]]"));
            Check((string?)migrated["text"] == "WrathFork" && (string?)migrated["UnknownFutureSetting"] == "kept");
            Check(migrated["CustomIntValuesV6"]!["threshold"]!.Value<int>() == 42);
            Check(migrated["RotationConfig"]!["Enabled"]!.Value<bool>());
            Check(migrated.Value<string>("ForkConfigurationId") == "preserved-id");
            Check(migrated["ForkOperationReceipts"]!.Value<int>("_Sequence") == 19);
            var backup = JObject.Parse(File.ReadAllText(marker)).Value<string>("Backup")!;
            Check(File.ReadAllText(Path.Combine(backup, "WrathFork.json")) == original);
            Check(File.ReadAllText(Path.Combine(backup, "WrathCombo.json")) == "old stock settings");
            Check(File.ReadAllText(source) == original);
            File.WriteAllText(target, "settings changed after migration");
            Check(!ForkIdentityMigration.ImportOnce(target, _ => throw new Exception("Reimport")));
            Check(File.ReadAllText(target) == "settings changed after migration");
            File.Delete(target);
            try { ForkIdentityMigration.ImportOnce(target, _ => { }); Check(false); }
            catch (InvalidDataException) { Check(!File.Exists(target)); }
            File.Delete(marker);
            File.WriteAllText(source, "broken json");
            try { ForkIdentityMigration.ImportOnce(target, _ => { }); Check(false); }
            catch (InvalidOperationException) { Check(!File.Exists(target) && !File.Exists(marker)); }
            File.WriteAllText(source, original);
            try { ForkIdentityMigration.ImportOnce(target, _ => throw new JsonSerializationException("Invalid field")); Check(false); }
            catch (InvalidOperationException) { Check(!File.Exists(target) && !File.Exists(marker)); }
            try { ForkIdentityMigration.ImportOnce(target, _ => File.WriteAllText(target, "concurrent settings")); Check(false); }
            catch (InvalidOperationException) { Check(File.ReadAllText(target) == "concurrent settings"); }
            Check(Directory.GetFiles(directory, "*.import-*").Length == 0);
            Check(File.ReadAllText(source) == original);
            File.Delete(target);
            Check(ForkIdentityMigration.ImportOnce(target, text => JObject.Parse(text)));
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
                .Replace(", WrathCombo", ", WrathFork");
            File.WriteAllText(Path.Combine(directory, "WrathFork.json"), typed);
            var target = Path.Combine(directory, "WrathCombo.json");
            ForkIdentityMigration.ImportOnce(target, text => JsonConvert.DeserializeObject(text, type,
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
