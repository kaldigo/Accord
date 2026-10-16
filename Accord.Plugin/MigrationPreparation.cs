using System.Linq;
using Newtonsoft.Json.Linq;

namespace Accord;

internal static class MigrationPreparation
{
    internal static (JObject Plan, AccordSettings Choices) Build(SetupPack pack, JObject source, JObject snapshot, AccordSettings fallback)
    {
        var plan = WrathFork.Contract.SettingsMigration.Plan(source, snapshot);
        var desired = source["Accord"]?.ToObject<AccordSettings>() ?? fallback;
        desired.LiveMode = false;
        desired.ActiveJobs = pack.SelectedJobs(desired).Select(j => j.JobId).ToHashSet();
        desired.PreparedJobs.Clear();
        desired.SharedSettingsPrepared = true;
        var patch = (JObject)plan["Patch"]!;
        var settings = (JObject)patch["Settings"]!;
        WrathFork.Contract.SettingsPatch.NormalizePresetMaps(settings, (JObject)snapshot["PresetIds"]!);
        var features = settings["ForkFeatures"] as JObject ?? new JObject();
        var enabled = patch["Enable"]!.Values<int>().ToHashSet();
        var disabled = patch["Disable"]!.Values<int>().ToHashSet();
        foreach (var job in pack.Data.Jobs)
        {
            // Mark imported details as prepared so the first class change cannot overwrite them with a baseline.
            desired.PreparedJobs[job.JobId] = job.SetupRevision;
            if (!pack.IsActive(job.JobId, desired)) continue;
            if (settings["AutoActions"] is not JObject) settings["AutoActions"] = new JObject();
            ((JObject)settings["AutoActions"]!).Merge(pack.AutoModes(job, desired, (JObject)snapshot["PresetIds"]!));
            features[job.JobId.ToString()] = JObject.FromObject(SetupPack.EngineFeatures(job.JobId, desired));
            var controlled = pack.ControlledPresets(job);
            enabled.ExceptWith(controlled);
            disabled.UnionWith(controlled);
            enabled.UnionWith(pack.Resolve(job, desired).Settings["EnabledActionsV6"]!.AsArray()
                .Select(n => n!.GetValue<int>()).Where(controlled.Contains));
        }
        settings["ForkFeatures"] = features;
        patch["Enable"] = new JArray(enabled.Order());
        patch["Disable"] = new JArray(disabled.Order());
        return (plan, desired);
    }
}
