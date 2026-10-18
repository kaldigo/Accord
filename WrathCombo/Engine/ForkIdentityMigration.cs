using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace WrathCombo.Engine;

internal static class ForkIdentityMigration
{
    private static bool Exists(string path)
    {
        try { File.GetAttributes(path); return true; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    internal static bool ImportOnce(string destination, Action<string> validate)
    {
        var marker = destination + ".wrathfork-migration.json";
        if (Exists(marker))
        {
            if (JObject.Parse(File.ReadAllText(marker)).Value<int>("Version") != 1 || !Exists(destination))
                throw new InvalidDataException("Wrath identity migration record is invalid or its configuration is missing. Restore the backup before loading.");
            return false;
        }
        var directory = Path.GetDirectoryName(destination)!;
        var source = Path.Combine(directory, "WrathFork.json");
        if (!Exists(source)) return false;
        try
        {
            var original = File.ReadAllText(source);
            var previous = Exists(destination) ? File.ReadAllText(destination) : null;
            var json = JObject.Parse(original);
            foreach (var property in json.Descendants().OfType<JProperty>().Where(p => p.Name == "$type"))
                if (property.Value.Type == JTokenType.String)
                    property.Value = Regex.Replace((string)property.Value!, @",\s*WrathFork(?=,|\]|$)", ", WrathCombo");
            var text = json.ToString();
            validate(text);
            // Abort if anything writes either configuration during validation.
            if (File.ReadAllText(source) != original ||
                (Exists(destination) ? File.ReadAllText(destination) : null) != previous)
                throw new IOException("Wrath configuration changed during migration.");
            var backup = Path.Combine(directory, "wrath-identity-backups", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backup);
            File.Copy(source, Path.Combine(backup, "WrathFork.json"));
            if (previous is not null) File.Copy(destination, Path.Combine(backup, "WrathCombo.json"));
            var temporary = destination + ".import-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporary, text);
                File.Move(temporary, destination, true);
                File.WriteAllText(marker, new JObject { ["Version"] = 1, ["Backup"] = backup }.ToString());
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not migrate WrathFork settings. No rotation hooks were started. Resolve the migration error before loading Wrath Combo.", ex);
        }
    }
}
