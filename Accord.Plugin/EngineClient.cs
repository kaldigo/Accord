using System;
using System.Linq;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WrathFork.Contract;

namespace Accord;

internal sealed class EngineClient
{
    internal JObject Read()
    {
        if (!Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "WrathFork" && p.IsLoaded))
            throw new InvalidOperationException("Load the compatible WrathFork engine. Behaviour and live hotbar updates are unavailable.");
        var result = Decode(Svc.PluginInterface.GetIpcSubscriber<string>("WrathFork.Settings.Read.v1").InvokeFunc());
        if (result.Value<string>("Identity") != "WrathFork" || result.Value<int>("ApiVersion") != 1 || result.Value<int>("SettingsSchemaVersion") != 1 ||
            result.Value<string>("EngineVersion") != "1.0.4.26" ||
            Enum.GetNames<Feature>().Except(result["Features"]!.Values<string>()).Any())
            throw new InvalidOperationException("The fork API/settings capabilities do not match this reviewed setup.");
        return result;
    }

    internal JObject Apply(JObject snapshot, JObject patch)
    {
        var request = new JObject { ["Instance"] = snapshot["Instance"]!.DeepClone(),
            ["ExpectedRevision"] = snapshot["Revision"]!.DeepClone(), ["Patch"] = patch };
        return Decode(Svc.PluginInterface.GetIpcSubscriber<string, string>("WrathFork.Settings.Apply.v1")
            .InvokeFunc(request.ToString(Formatting.None)));
    }

    private static JObject Decode(string value)
    {
        var result = JObject.Parse(value);
        if (result.Value<bool>("Success") != true)
            throw new InvalidOperationException("Fork rejected the request: " + result.Value<string>("Error"));
        return result;
    }
}
