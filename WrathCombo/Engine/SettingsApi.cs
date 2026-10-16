using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WrathCombo.Core;
using WrathCombo.CustomComboNS.Functions;
using WrathCombo.Services;
using WrathFork.Contract;

namespace WrathCombo.Engine;

internal sealed class SettingsApi : IDisposable
{
    private readonly ICallGateProvider<string> read = Svc.PluginInterface.GetIpcProvider<string>("WrathFork.Settings.Read.v1");
    private readonly ICallGateProvider<string, string> apply = Svc.PluginInterface.GetIpcProvider<string, string>("WrathFork.Settings.Apply.v1");
    private uint lastJob;
    private ulong lastCharacter;
    private bool conflict;
    private readonly string instance = Guid.NewGuid().ToString("N");

    internal SettingsApi()
    {
        // Populate the engine's registered typed settings, independent of opening a UI/job.
        foreach (var type in typeof(Configuration).Assembly.GetTypes())
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                // BST is not a supported setup job; its type initializer installs a live framework callback.
                if (type.FullName != "WrathCombo.Combos.PvE.BST" && typeof(UserData).IsAssignableFrom(field.FieldType)) _ = field.GetValue(null);
        read.RegisterFunc(() => Guard(() => Envelope()));
        apply.RegisterFunc(request => Guard(() => Apply(JObject.Parse(request))));
    }

    private string Guard(Func<JObject> action)
    {
        try
        {
            if (!Svc.Framework.IsInFrameworkUpdateThread) throw new InvalidOperationException("Use the framework thread.");
            if (conflict) throw new InvalidOperationException("Upstream Wrath is loaded. Disable it and reload WrathFork.");
            return action().ToString(Formatting.None);
        }
        catch (Exception ex) { return new JObject { ["Success"] = false, ["Error"] = ex.Message }.ToString(Formatting.None); }
    }

    private JObject Snapshot()
    {
        var all = JObject.FromObject(Service.Configuration);
        var result = new JObject();
        foreach (string name in SettingsPatch.Fields.Append("EnabledActionsV6"))
            if (all[name] is { } value) result[name] = value.DeepClone();
        foreach (string name in new[] { "AutoActions", "BurstPresets" })
            if (result[name] is JObject map)
                result[name] = new JObject(map.Properties().Select(p => new JProperty(((int)Enum.Parse<Preset>(p.Name)).ToString(), p.Value.DeepClone())));
        return result;
    }

    private static string Revision(JObject snapshot) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot.ToString(Formatting.None))));
    private JObject Envelope()
    {
        var snapshot = Snapshot();
        return new JObject { ["Success"] = true, ["Identity"] = "WrathFork", ["ApiVersion"] = 1, ["SettingsSchemaVersion"] = 1,
            ["EngineVersion"] = "1.0.4.27", ["Instance"] = instance, ["Revision"] = Revision(snapshot),
            ["ConfigurationId"] = Service.Configuration.ForkConfigurationId,
            ["Features"] = new JArray(Enum.GetNames<Feature>()), ["Settings"] = snapshot,
            ["PresetIds"] = new JObject(Enum.GetValues<Preset>().Select(p => new JProperty(p.ToString(), (int)p))),
            ["KnownPresets"] = new JArray(Enum.GetValues<Preset>().Select(p => (int)p)) };
    }

    private JObject Apply(JObject request)
    {
        if (WrathCombo.P!.IPC.Leasing.Registrations.Count != 0)
            throw new InvalidOperationException("Release existing engine leases before applying persistent settings.");
        var current = Snapshot();
        if (request.Value<string>("Instance") != instance || request.Value<string>("ExpectedRevision") != Revision(current))
            throw new InvalidOperationException("Engine state changed. Read it again before applying.");
        var updated = SettingsPatch.Apply(current, request["Patch"] as JObject ?? throw new ArgumentException("Missing patch."),
            Enum.GetValues<Preset>().Select(p => (int)p).ToHashSet());
        foreach (string name in new[] { "CustomHealStack", "RaiseStack" })
            foreach (var target in updated[name]!.Values<string>())
                if (typeof(global::WrathCombo.CustomComboNS.SimpleTarget).GetProperty(target!) is null)
                    throw new ArgumentException("Unknown target selector: " + target);
        if (JToken.DeepEquals(current, updated)) return Envelope();
        var before = JObject.FromObject(Service.Configuration);
        var directory = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "configuration-backups");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json"), before.ToString());
        try
        {
            Populate(updated);
            Refresh();
            Svc.PluginInterface.SavePluginConfig(Service.Configuration);
        }
        catch (Exception commitError)
        {
            try
            {
                Populate(before);
                Refresh();
                Svc.PluginInterface.SavePluginConfig(Service.Configuration);
            }
            catch (Exception rollbackError)
            {
                throw new AggregateException("Settings update failed and recovery could not be persisted. Use the fork configuration backup before resuming setup.", commitError, rollbackError);
            }
            throw;
        }
        return Envelope();
    }

    private static void Populate(JObject value) => JsonConvert.PopulateObject(value.ToString(), Service.Configuration,
        new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
    private static void Refresh()
    {
        if (Player.Available) Service.ActionReplacer.UpdateFilteredCombos();
        WrathCombo.P!.ActionRetargeting.ClearCachedRetargets();
        if (Player.Available) WrathCombo.P.IPCSearch.UpdateActiveJobPresets();
    }

    internal void Tick()
    {
        if (Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "WrathCombo" && p.IsLoaded))
        {
            conflict = true;
            Service.Configuration.SetActionChanging(false);
        }
        uint job = Player.Available ? (uint)Player.Job : 0;
        ulong character = Svc.PlayerState.ContentId;
        if (job != lastJob || character != lastCharacter || !Player.Available || Player.IsDead || !Svc.ClientState.IsLoggedIn)
            IntentRouting.Reset();
        lastJob = job; lastCharacter = character;
    }

    public void Dispose() { read.UnregisterFunc(); apply.UnregisterFunc(); IntentRouting.Reset(); }
}
