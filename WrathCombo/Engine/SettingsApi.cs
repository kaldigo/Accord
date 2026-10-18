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
    private readonly ICallGateProvider<string> read = Svc.PluginInterface.GetIpcProvider<string>("WrathFork.Settings.Read.v2");
    private readonly ICallGateProvider<string, string> apply = Svc.PluginInterface.GetIpcProvider<string, string>("WrathFork.Settings.Apply.v2");
    private readonly ICallGateProvider<string> discover = Svc.PluginInterface.GetIpcProvider<string>("WrathFork.Settings.Discover.v2");
    private readonly ICallGateProvider<string, string> validate = Svc.PluginInterface.GetIpcProvider<string, string>("WrathFork.Settings.Validate.v2");
    private readonly ICallGateProvider<string, object> changed = Svc.PluginInterface.GetIpcProvider<string, object>("WrathFork.Settings.Changed.v2");
    private string observedRevision = "";
    private long nextObservation;
    private uint lastJob;
    private ulong lastCharacter;
    private bool conflict;
    internal bool HasConflict => conflict;
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
        discover.RegisterFunc(() => Guard(() => new JObject { ["Success"] = true, ["Schema"] = SettingsSchema.Describe(Snapshot()) }));
        validate.RegisterFunc(request => Guard(() => {
            var current = Snapshot();
            var updated = ValidatePatch(current, JObject.Parse(request));
            return new JObject { ["Success"] = true, ["Revision"] = Revision(current), ["Warnings"] = SettingsSchema.Validate(current, updated) };
        }));
        Notify("Ready");
    }

    private string Guard(Func<JObject> action)
    {
        try
        {
            if (!Svc.Framework.IsInFrameworkUpdateThread) throw new InvalidOperationException("Use the framework thread.");
            if (conflict) throw new InvalidOperationException("Legacy WrathFork is loaded. Remove it and reload Wrath Combo.");
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

    private static string Revision(JObject snapshot) => SettingsTransaction.Hash(snapshot);
    private JObject Envelope()
    {
        var snapshot = Snapshot();
        var legacy = new JObject(SettingsPatch.Fields.TakeWhile(n => n != "BlockSpellOnMove").Append("EnabledActionsV6")
            .Where(n => snapshot[n] is not null).Select(n => new JProperty(n, snapshot[n]!.DeepClone())));
        return new JObject { ["Success"] = true, ["Identity"] = "WrathFork", ["ApiVersion"] = 2, ["SettingsSchemaVersion"] = 2,
            ["EngineVersion"] = UpstreamVersion.Text, ["Instance"] = instance, ["Revision"] = Revision(snapshot),
            ["LegacyRevisionV1"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(legacy.ToString(Formatting.None)))),
            ["ConfigurationId"] = Service.Configuration.ForkConfigurationId,
            ["Effective"] = Effective(), ["LeaseCount"] = WrathCombo.P!.IPC.Leasing.Registrations.Count,
            ["OperationSequence"] = Service.Configuration.ForkOperationReceipts.Value<long?>("_Sequence") ?? 0,
            ["ReceiptRetention"] = 256, ["Features"] = new JArray(Enum.GetNames<Feature>()), ["Settings"] = snapshot,
            ["PresetIds"] = new JObject(Enum.GetValues<Preset>().Select(p => new JProperty(p.ToString(), (int)p))),
            ["KnownPresets"] = new JArray(Enum.GetValues<Preset>().Select(p => (int)p)) };
    }

    private JObject Apply(JObject request)
    {
        if (WrathCombo.P!.IPC.Leasing.Registrations.Count != 0)
            throw new InvalidOperationException("Release existing engine leases before applying persistent settings.");
        var receipt = SettingsTransaction.Execute(request, instance, Service.Configuration.ForkConfigurationId, Snapshot(),
            Service.Configuration.ForkOperationReceipts, patch => ValidatePatch(Snapshot(), patch), Persist);
        var result = Envelope();
        result["Receipt"] = receipt;
        if (!receipt.Value<bool>("Replay")) Notify("Changed");
        return result;
    }

    private JObject ValidatePatch(JObject current, JObject patch)
    {
        var updated = SettingsPatch.Apply(current, patch, Enum.GetValues<Preset>().Select(p => (int)p).ToHashSet());
        SettingsSchema.Validate(current, updated);
        return updated;
    }

    private void Persist(JObject updated, JObject receipts)
    {
        var before = JObject.FromObject(Service.Configuration);
        var directory = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "configuration-backups");
        SettingsTransaction.PersistWithRollback(updated, receipts, before, Populate, Refresh,
            () => Svc.PluginInterface.SavePluginConfig(Service.Configuration), () =>
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json"), before.ToString());
            });
    }

    private static void Populate(JObject value) => JsonConvert.PopulateObject(value.ToString(), Service.Configuration,
        new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
    private static void Refresh()
    {
        if (Player.Available) Service.ActionReplacer.UpdateFilteredCombos();
        WrathCombo.P!.ActionRetargeting.ClearCachedRetargets();
        if (Player.Available) WrathCombo.P.IPCSearch.UpdateActiveJobPresets();
    }

    private JObject Effective()
    {
        var overrides = new JObject();
        var effective = new JArray();
        foreach (var preset in Enum.GetValues<Preset>())
        {
            bool saved = Service.Configuration.EnabledActions.Contains(preset);
            bool value = Player.Available ? CustomComboFunctions.IsEnabled(preset) : saved;
            if (value) effective.Add((int)preset);
            var lease = WrathCombo.P!.UIHelper.PresetControlled(preset);
            if (value != saved || lease is not null)
                overrides[((int)preset).ToString()] = new JObject { ["Saved"] = saved, ["Effective"] = value,
                    ["Owner"] = lease?.controllers ?? "Engine continuation/visibility rules" };
        }
        return new JObject { ["Job"] = Options.Job, ["RuntimeAvailable"] = Player.Available,
            ["ActionReplacementEnabled"] = Service.Configuration.ActionChanging,
            ["EnabledPresets"] = effective, ["Overrides"] = overrides, ["BurstContinuation"] = IntentRouting.BurstFollowThrough,
            ["Features"] = new JObject(Enum.GetValues<Feature>().Where(f => FeatureScope.Supports(Options.Job, f))
                .Select(f => new JProperty(f.ToString(), Options.Enabled(f)))) };
    }

    private void Notify(string state)
    {
        try { changed.SendMessage(new JObject { ["State"] = state, ["Instance"] = instance,
            ["Revision"] = Revision(Snapshot()) }.ToString(Formatting.None)); }
        catch (Exception ex) { Svc.Log.Warning(ex, "Settings subscriber notification failed"); }
    }

    internal void Tick()
    {
        if (Svc.PluginInterface.InstalledPlugins.Any(p => p.InternalName == "WrathFork" && p.IsLoaded))
        {
            conflict = true;
            Service.Configuration.SetActionChanging(false);
        }
        uint job = Player.Available ? (uint)Player.Job : 0;
        ulong character = Svc.PlayerState.ContentId;
        if (job != lastJob || character != lastCharacter || !Player.Available || Player.IsDead || !Svc.ClientState.IsLoggedIn)
            IntentRouting.Reset();
        lastJob = job; lastCharacter = character;
        if (Environment.TickCount64 >= nextObservation)
        {
            nextObservation = Environment.TickCount64 + 1000;
            string revision = Revision(Snapshot());
            if (revision != observedRevision) { observedRevision = revision; Notify("Changed"); }
        }
    }

    public void Dispose() { Notify("Unloading"); read.UnregisterFunc(); apply.UnregisterFunc(); discover.UnregisterFunc(); validate.UnregisterFunc(); IntentRouting.Reset(); }
}
