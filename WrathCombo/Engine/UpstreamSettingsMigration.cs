using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace WrathCombo.Engine;

internal static class UpstreamSettingsMigration
{
    // Check attributes rather than File.Exists: an access error must not look like a new install.
    private static bool Exists(string path)
    {
        try { File.GetAttributes(path); return true; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    internal static bool ImportIfMissing(string destination, Action<string> validate)
    {
        if (Exists(destination)) return false;
        var source = Path.Combine(Path.GetDirectoryName(destination)!, "WrathCombo.json");
        if (!Exists(source)) return false;

        try
        {
            var json = JObject.Parse(File.ReadAllText(source));
            // Keep all settings, including fields unknown to this fork. Only assembly identity changes.
            foreach (var property in json.Descendants().OfType<JProperty>().Where(p => p.Name == "$type"))
                if (property.Value.Type == JTokenType.String)
                    property.Value = Regex.Replace((string)property.Value!, @",\s*WrathCombo(?=,|\]|$)", ", WrathFork");
            var text = json.ToString();
            validate(text);
            // A concurrent config creation must win. Never overwrite, including on a retry.
            var temporary = destination + ".import-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporary, text);
                File.Move(temporary, destination, false);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not import upstream Wrath settings. The upstream file has not been changed. " +
                "Resolve the import error before loading Wrathful Combo again.", ex);
        }
    }
}
