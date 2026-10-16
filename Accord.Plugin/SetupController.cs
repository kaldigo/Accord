using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using HotbarTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



namespace Accord;

internal sealed class SetupController : IDisposable
{
    internal static SetupController? Instance { get; private set; }
    internal SetupPack Pack { get; } = new();
    private readonly HotbarSetup hotbars = new();
    private readonly List<IDtrBarEntry> entries = new();
    private uint currentJob;
    private ulong character;
    internal string Status { get; private set; } = "Waiting for a character.";
    internal AccordSettings Settings => Plugin.Instance.Settings;
    private readonly EngineClient engine = new();
    private long nextPoll;
    internal static bool Enabled(VariantId id) => Instance?.Settings.Enabled(id) ?? false;

    internal SetupController()
    {
        Instance = this;
        foreach (var definition in VariantDefinitions.All)
        {
            var entry = Svc.DtrBar.Get("Accord " + definition.Label);
            entry.OnClick = _ => Run(() => SetVariant(definition.Id, !Settings.Enabled(definition.Id)));
            entry.Tooltip = definition.Label + ": click to switch A/M.\n" + definition.Description;
            entries.Add(entry);
        }
        UpdateEntries();
    }

    internal void Run(Action operation)
    {
        if (!Svc.Framework.IsInFrameworkUpdateThread)
        {
            _ = Svc.Framework.RunOnFrameworkThread(() => Run(operation));
            return;
        }
        try { operation(); }
        catch (Exception ex)
        {
            if (Status != ex.Message) Svc.Log.Error(ex, "Accord setup operation failed");
            Status = ex.Message;
        }
    }

    internal void Tick()
    {
        if (!Player.Available || !Svc.ClientState.IsLoggedIn)
        {
            currentJob = 0;
            character = 0;

            return;
        }

        if (Environment.TickCount64 < nextPoll) return;
        nextPoll = Environment.TickCount64 + 1000;
        JObject snapshot;
        try { snapshot = engine.Read(); }
        catch { currentJob = 0; throw; }
        if (Settings.EngineConfigurationId == snapshot.Value<string>("ConfigurationId") &&
            Settings.LastEngineRevision.Length != 0 && Settings.LastEngineRevision != snapshot.Value<string>("Revision"))
        {
            Status = "Engine settings changed outside Accord. Use Set current class hotbars or change a variant to reapply the reviewed mapping. Live updates paused.";
            return;
        }
        if (Settings.EngineConfigurationId != snapshot.Value<string>("ConfigurationId")) currentJob = 0;
        uint job = (uint)Player.Job;
        ulong contentId = Svc.PlayerState.ContentId;
        if (job == currentJob && contentId == character) return;
        if (contentId == 0 || Pack.Find(job) is null) return;
        // Wait for the game to finish activating this job before consuming the transition.
        unsafe { NativeHotbars.Ready(job); }
        Prepare([Pack.Find(job)!]);
        if (Settings.LiveMode) ApplyHotbars(false);
        else Status = $"{Pack.Find(job)!.Job}: behaviour ready. Static hotbars unchanged.";
        currentJob = job;
        character = contentId;
    }

    private void Mutate(Action mutation)
    {
        var before = JsonConvert.SerializeObject(Settings);
        try { mutation(); Svc.PluginInterface.SavePluginConfig(Settings); }
        catch { Plugin.Instance.Settings = JsonConvert.DeserializeObject<AccordSettings>(before)!; throw; }
        UpdateEntries();
    }

    private void Prepare(IEnumerable<JobPreset> jobs, bool reset = false) => ApplySettings(jobs, CloneSettings(), reset);
    private AccordSettings CloneSettings() => JsonConvert.DeserializeObject<AccordSettings>(JsonConvert.SerializeObject(Settings))!;

    private void ApplySettings(IEnumerable<JobPreset> jobs, AccordSettings desired, bool reset = false)
    {
        if (Plugin.Instance.MigrationPending) throw new InvalidOperationException("Open /accord to complete separate setup first.");
        var snapshot = engine.Read();
        if (desired.EngineConfigurationId != snapshot.Value<string>("ConfigurationId"))
        {
            desired.PreparedJobs.Clear();
            desired.SharedSettingsPrepared = false;
        }
        var changes = new JObject();
        var enable = new HashSet<int>();
        var disable = new HashSet<int>();
        foreach (var job in jobs)
        {
            var resolved = Pack.Resolve(job, desired);
            bool baseline = reset || desired.PreparedJobs.GetValueOrDefault(job.JobId) != job.SetupRevision;
            var selected = resolved.Settings["EnabledActionsV6"]!.AsArray().Select(n => n!.GetValue<int>()).ToHashSet();
            if (baseline)
            {
                var fragment = JObject.Parse(resolved.Settings.ToJsonString());
                fragment.Remove("EnabledActionsV6");
                changes.Merge(fragment, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                disable.UnionWith(job.OwnedPresetIds);
                enable.UnionWith(selected);
                desired.PreparedJobs[job.JobId] = job.SetupRevision;
            }
            else
            {
                var controlled = Pack.ControlledPresets(job);
                disable.UnionWith(controlled);
                enable.UnionWith(selected.Where(controlled.Contains));
            }
            if (changes["ForkFeatures"] is not JObject) changes["ForkFeatures"] = new JObject();
            changes["ForkFeatures"]![job.JobId.ToString()] = JObject.FromObject(SetupPack.EngineFeatures(job.JobId, desired));
        }
        if (!desired.SharedSettingsPrepared)
        {
            var shared = JObject.Parse(Pack.Data.SharedSettings.ToJsonString());
            if (shared["EnabledActionsV6"] is JArray sharedIds) enable.UnionWith(sharedIds.Values<int>());
            shared.Remove("EnabledActionsV6");
            changes.Merge(shared, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            desired.SharedSettingsPrepared = true;
        }
        WrathFork.Contract.SettingsPatch.NormalizePresetMaps(changes, (JObject)snapshot["PresetIds"]!);
        var patch = new JObject { ["Settings"] = changes, ["Enable"] = new JArray(enable.Order()), ["Disable"] = new JArray(disable.Order()) };
        // Validate locally using the engine's published setting shape, then let the engine validate again.
        WrathFork.Contract.SettingsPatch.Apply((JObject)snapshot["Settings"]!, patch, snapshot["KnownPresets"]!.Values<int>().ToHashSet());
        var committed = engine.Apply(snapshot, patch);
        desired.EngineConfigurationId = committed.Value<string>("ConfigurationId")!;
        desired.LastEngineRevision = committed.Value<string>("Revision")!;
        Plugin.Instance.Settings = desired;
        UpdateEntries();
        try { Svc.PluginInterface.SavePluginConfig(desired); }
        catch (Exception ex) { throw new InvalidOperationException("Engine settings committed, but Accord could not save its choices. Reapply setup before relying on a reload.", ex); }
    }

    internal void SetVariant(VariantId id, bool enabled)
    {
        var desired = CloneSettings();
        desired.Variants[id] = enabled;
        ApplySettings(Pack.Data.Jobs.Where(j => Settings.PreparedJobs.ContainsKey(j.JobId) || Player.Available && Pack.Find((uint)Player.Job) == j), desired);
        if (Settings.LiveMode)
        {
            try { ApplyHotbars(false); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Behaviour saved; live layout failed. Retry Set current class hotbars. " + ex.Message, ex);
            }
        }
        else Status = "Variant saved. Static hotbars unchanged; use setup to restore any needed manual buttons.";
    }

    internal void SetLive(bool live)
    {
        if (live) ApplyHotbars(false);
        Mutate(() => Settings.LiveMode = live);
        Status = live ? "Live mode enabled for the current class; it will refresh on class changes." : "Static mode enabled. Current hotbars retained.";
    }

    internal void SetCleanup(bool value) => Mutate(() => Settings.ClearOtherActions = value);

    internal unsafe void ApplyHotbars(bool all)
    {
        if (!Player.Available || Svc.PlayerState.ContentId == 0) throw new InvalidOperationException("Log in before applying hotbars.");
        uint job = (uint)Player.Job;
        NativeHotbars.Ready(job);
        var selected = all ? Pack.Data.Jobs.ToArray() : [Pack.Find(job) ?? throw new InvalidOperationException("Current class is not supported.")];
        Prepare(selected);
        var layouts = selected.SelectMany(p => (all ? new[] { p.JobId }.Concat(p.BaseClasses) : [job])
            .Select(id => (id, Pack.Resolve(p, Settings))));
        var backup = hotbars.Apply(layouts, Settings.ClearOtherActions);
        try { Mutate(() => Settings.LastHotbarBackup = backup); }
        catch { hotbars.Restore(backup); throw; }
        Status = all ? "All supported class hotbars applied and backed up." : $"{selected[0].Job} hotbars applied and backed up.";
    }

    internal void Restore()
    {
        if (string.IsNullOrWhiteSpace(Settings.LastHotbarBackup)) throw new InvalidOperationException("No hotbar backup has been recorded.");
        var undo = hotbars.Restore(Settings.LastHotbarBackup);
        try { Mutate(() => { Settings.LastHotbarBackup = undo; Settings.LiveMode = false; }); }
        catch { hotbars.Restore(undo); throw; }
        Status = "Hotbars restored. Live mode disabled so the restored layout is retained.";
    }

    internal void ResetCurrentSettings()
    {
        var job = Pack.Find((uint)Player.Job) ?? throw new InvalidOperationException("Select a supported class first.");
        Prepare([job], reset: true);
        Status = $"{job.Job}: reviewed settings restored. Previous settings backed up; variants and hotbars retained.";
    }

    internal void ExportDiagnostics()
    {
        var path = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "accord-diagnostics.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(new
        {
            EngineVersion = Pack.Data.WrathVersion,
            Status,
            CurrentJob = (uint)Player.Job,
            Settings,
            // This is a private local export, never a file in the source repository.
            Engine = engine.Read(),
        }, Formatting.Indented));
        Status = "Diagnostics saved to " + path;
    }

    private void UpdateEntries()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var definition = VariantDefinitions.All[i];
            entries[i].Text = new SeStringBuilder().AddIcon(Enum.Parse<BitmapFontIcon>(definition.Icon))
                .AddText(" " + (Settings.Enabled(definition.Id) ? "A" : "M")).Build();
        }
    }

    public void Dispose()
    {
        foreach (var definition in VariantDefinitions.All) Svc.DtrBar.Remove("Accord " + definition.Label);
        Instance = null;
    }
}
