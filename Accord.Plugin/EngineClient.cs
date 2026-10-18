using System;
using System.Linq;
using System.IO;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WrathFork.Contract;

namespace Accord;

internal sealed class EngineClient : IDisposable
{
    private readonly ICallGateSubscriber<string, object> notifications = Svc.PluginInterface.GetIpcSubscriber<string, object>("WrathFork.Settings.Changed.v2");
    private readonly Action<string> onChanged;
    internal EngineClient(Action changed) { onChanged = _ => changed(); notifications.Subscribe(onChanged); }
    private string Journal => Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "engine-operation.pending.json");
    internal bool HasPending => File.Exists(Journal);
    internal JObject Discover() => Decode(Svc.PluginInterface.GetIpcSubscriber<string>("WrathFork.Settings.Discover.v2").InvokeFunc());
    internal JObject Validate(JObject patch) => Decode(Svc.PluginInterface.GetIpcSubscriber<string, string>("WrathFork.Settings.Validate.v2").InvokeFunc(patch.ToString(Formatting.None)));
    internal JObject Read()
    {
        if (!Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "WrathCombo" && p.IsLoaded))
            throw new InvalidOperationException("Load the Accord-compatible Wrath Combo build. Behaviour and live hotbar updates are unavailable.");
        var result = Decode(Svc.PluginInterface.GetIpcSubscriber<string>("WrathFork.Settings.Read.v2").InvokeFunc());
        if (result.Value<string>("Identity") != "WrathFork" || result.Value<int>("ApiVersion") != 2 || result.Value<int>("SettingsSchemaVersion") != 2 ||
            result.Value<string>("EngineVersion") != "1.0.4.27" ||
            Enum.GetNames<Feature>().Except(result["Features"]!.Values<string>()).Any())
            throw new InvalidOperationException("The fork API/settings capabilities do not match this reviewed setup.");
        return result;
    }

    internal void Apply(JObject snapshot, JObject patch, AccordSettings desired)
    {
        if (HasPending) throw new InvalidOperationException("A previous settings operation needs recovery before another update.");
        Validate(patch);
        var request = new JObject { ["Instance"] = snapshot["Instance"]!.DeepClone(),
            ["ConfigurationId"] = snapshot["ConfigurationId"]!.DeepClone(), ["OperationId"] = Guid.NewGuid().ToString(),
            ["ExpectedRevision"] = snapshot["Revision"]!.DeepClone(), ["ExpectedSequence"] = snapshot["OperationSequence"]!.DeepClone(), ["Patch"] = patch.DeepClone() };
        Directory.CreateDirectory(Svc.PluginInterface.GetPluginConfigDirectory());
        var journal = new JObject { ["Request"] = request, ["Desired"] = JObject.FromObject(desired) };
        var temporary = Journal + ".tmp";
        File.WriteAllText(temporary, journal.ToString());
        File.Move(temporary, Journal, true);
        Recover();
    }

    internal bool Recover()
    {
        if (!HasPending) return false;
        Read(); // Identity/capability check before replaying an interrupted operation.
        var journal = JObject.Parse(File.ReadAllText(Journal));
        var result = Decode(Svc.PluginInterface.GetIpcSubscriber<string, string>("WrathFork.Settings.Apply.v2")
            .InvokeFunc(journal["Request"]!.ToString(Formatting.None)));
        var desired = journal["Desired"]!.ToObject<AccordSettings>()!;
        desired.EngineConfigurationId = result.Value<string>("ConfigurationId")!;
        desired.LastEngineRevision = result["Receipt"]!.Value<string>("CommittedRevision")!;
        // Persist acknowledgement before removing the replay record. If saving fails, retry the same operation ID.
        Svc.PluginInterface.SavePluginConfig(desired);
        Plugin.Instance.Settings = desired;
        File.Delete(Journal);
        Plugin.Instance.MigrationPending = false;
        Plugin.Instance.RequiresMigration = false;
        return true;
    }

    internal void ReviewRejectedOperation()
    {
        if (!HasPending) return;
        // Keep the exact old request for diagnosis; explicitly abandon it without asserting new engine state.
        File.Move(Journal, Journal + ".abandoned-" + Guid.NewGuid().ToString("N"));
    }
    public void Dispose() => notifications.Unsubscribe(onChanged);

    private static JObject Decode(string value)
    {
        var result = JObject.Parse(value);
        if (result.Value<bool>("Success") != true)
            throw new InvalidOperationException("Fork rejected the request: " + result.Value<string>("Error"));
        return result;
    }
}
