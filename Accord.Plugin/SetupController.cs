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
    private readonly LiveUpdateState updates = new();
    internal string Status { get; private set; } = "Waiting for a character.";
    internal AccordSettings Settings => Plugin.Instance.Settings;
    private readonly EngineClient engine;
    private bool disposed;
    internal static bool Enabled(VariantId id) => Instance?.Settings.Enabled(id) ?? false;

    internal SetupController()
    {
        Instance = this;
        engine = new EngineClient(updates.Wake);
        foreach (var definition in VariantDefinitions.All)
        {
            var entry = Svc.DtrBar.Get("Accord " + definition.Label);
            entry.Shown = false;
            entry.OnClick = _ => Run(() => SetVariant(definition.Id, !Settings.Enabled(definition.Id)));
            entry.Tooltip = definition.Label + ": click to switch A/M.\n" + definition.Description;
            entries.Add(entry);
        }
        Settings.ActiveJobs ??= Pack.SelectedJobs(Settings).Select(j => j.JobId).ToHashSet();
        // Plugin construction may run off the framework thread. Tick updates
        // visibility and text once player state can safely be read.
    }

    internal void Run(Action operation)
    {
        if (disposed) return;
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
        uint observed = Player.Available ? (uint)Player.Job : 0;
        ulong observedId = Player.Available && Svc.ClientState.IsLoggedIn ? Svc.PlayerState.ContentId : 0;
        if (!updates.Poll(Environment.TickCount64, observed, observedId)) return;
        try { TickReady(); }
        finally { updates.DeferRetry(Environment.TickCount64); }
    }

    private void TickReady()
    {
        if (engine.HasPending)
        {
            if (engine.Recover()) { updates.Invalidate(); UpdateEntries(); Status = "Settings operation recovered. Layout updates will resume after state verification."; }
        }
        if (Plugin.Instance.MigrationPending) return;
        UpdateEntries();
        if (!Player.Available || !Svc.ClientState.IsLoggedIn)
        {
            updates.Invalidate();

            return;
        }


        if (!Pack.IsActive((uint)Player.Job, Settings))
        {
            updates.Invalidate();
            Status = "Accord is inactive for this class. Choose active classes in /accord.";
            return;
        }

        JObject snapshot;
        try { snapshot = engine.Read(); }
        catch { updates.Invalidate(); throw; }
        // Re-read current settings on each transition. ApplySettings changes only Accord-owned
        // switches for prepared jobs; unrelated personal edits must not freeze live layouts.
        if (Settings.EngineConfigurationId != snapshot.Value<string>("ConfigurationId")) updates.Invalidate();
        uint job = (uint)Player.Job;
        ulong contentId = Svc.PlayerState.ContentId;
        if (updates.Applied) return;
        if (contentId == 0 || Pack.Find(job) is null) return;
        // Wait for the game to finish activating this job before consuming the transition.
        unsafe { NativeHotbars.Ready(job); }
        updates.Apply(job, contentId, () =>
        {
            if (Settings.LiveMode) ApplyCurrentHotbars();
            else
            {
                Prepare([Pack.Find(job)!]);
                Status = $"{Pack.Find(job)!.Job}: behaviour ready. Static hotbars unchanged.";
            }
        });
    }

    private void Mutate(Action mutation)
    {
        if (engine.HasPending) throw new InvalidOperationException("Resolve the pending settings operation before changing Accord choices.");
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
        var managedJobs = jobs.Where(j => Pack.IsActive(j.JobId, desired)).ToArray();
        desired.ActiveJobs = Pack.SelectedJobs(desired).Select(j => j.JobId).ToHashSet();
        if (managedJobs.Length == 0)
        {
            Mutate(() => Plugin.Instance.Settings = desired);
            return;
        }
        var snapshot = engine.Read();
        if (desired.EngineConfigurationId != snapshot.Value<string>("ConfigurationId"))
        {
            desired.PreparedJobs.Clear();
            desired.SharedSettingsPrepared = false;
        }
        var changes = new JObject();
        var enable = new HashSet<int>();
        var disable = new HashSet<int>();
        foreach (var job in managedJobs)
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
            WrathFork.Contract.SettingsPatch.NormalizePresetMaps(changes, (JObject)snapshot["PresetIds"]!);
            if (changes["AutoActions"] is not JObject) changes["AutoActions"] = new JObject();
            ((JObject)changes["AutoActions"]!).Merge(Pack.AutoModes(job, desired, (JObject)snapshot["PresetIds"]!));
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
        engine.Apply(snapshot, patch, desired);
        UpdateEntries();
    }

    private JObject? migrationSnapshot;
    private JObject? migrationPatch;
    private AccordSettings? migrationChoices;
    internal string MigrationSummary { get; private set; } = "Preview the import to review its contents.";
    internal bool MigrationReady => migrationPatch is not null;

    internal void PreviewMigration()
    {
        var sourcePath = Plugin.Instance.MigrationSource;
        if (string.IsNullOrWhiteSpace(sourcePath)) throw new InvalidOperationException("No combined prototype backup is selected.");
        migrationPatch = null;
        var source = JObject.Parse(File.ReadAllText(sourcePath));
        var snapshot = engine.Read();
        var (plan, desired) = MigrationPreparation.Build(Pack, source, snapshot, CloneSettings());
        var patch = (JObject)plan["Patch"]!;
        var validation = engine.Validate(patch);
        plan["Validation"] = validation;
        plan["SourceFile"] = sourcePath;
        var report = Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "settings-migration-preview.json");
        File.WriteAllText(report, plan.ToString());
        migrationSnapshot = snapshot;
        migrationPatch = patch;
        migrationChoices = desired;
        MigrationSummary = $"Import from {Path.GetFileName(sourcePath)}. Personal job values and targeting settings are retained; intent-controlled switches follow your saved variants. Live mode stays off and hotbars are untouched. {plan["Omitted"]!.Count()} unsupported, retired or non-engine fields remain in the backup. Review report: {report}";
        Status = "Migration preview ready; no settings changed.";
    }

    internal void ImportMigration()
    {
        if (migrationPatch is null || migrationSnapshot is null || migrationChoices is null) throw new InvalidOperationException("Preview the migration first.");
        engine.Apply(migrationSnapshot, migrationPatch, migrationChoices);
        Plugin.Instance.MigrationPending = false;
        migrationPatch = null;
        UpdateEntries();
        Status = "Personal settings imported and backed up. Live mode remains off; hotbars unchanged.";
    }

    internal bool HasPendingOperation => engine.HasPending;
    internal void AbandonPendingOperation() { engine.ReviewRejectedOperation(); migrationPatch = null; Status = "Previous request retained for diagnosis. Review settings before reapplying."; }

    internal void SetVariant(VariantId id, bool enabled)
    {
        var desired = CloneSettings();
        desired.Variants[id] = enabled;
        ApplySettings(Pack.SelectedJobs(desired), desired);
        if (Settings.LiveMode) RequestCurrentHotbars();
        else Status = "Variant saved. Static hotbars unchanged; use setup to restore any needed manual buttons.";
    }

    internal void SetLive(bool live)
    {
        if (live && Pack.SelectedJobs(Settings).Length == 0)
            throw new InvalidOperationException("Select at least one active class first.");
        Mutate(() => Settings.LiveMode = live);
        if (live) RequestCurrentHotbars();
        else Status = "Static mode enabled. Current hotbars retained.";
    }

    internal void SetCleanup(bool value)
    {
        Mutate(() => Settings.ClearOtherActions = value);
        if (Settings.LiveMode) RequestCurrentHotbars();
    }

    private void RequestCurrentHotbars()
    {
        updates.Request();
        if (Settings.LiveMode) Status = "Live layout pending for the current active class; it will retry when hotbars are ready.";
    }

    internal void SetActiveJobs(IEnumerable<uint> jobs)
    {
        var desired = CloneSettings();
        desired.ActiveJobs = jobs.Select(id => Pack.Find(id)?.JobId).Where(id => id.HasValue)
            .Select(id => id!.Value).ToHashSet();
        ApplySettings(Pack.SelectedJobs(desired), desired);
        updates.Invalidate();
        if (Settings.LiveMode) RequestCurrentHotbars();
        else Status = "Active classes saved. Existing hotbars retained; deselected classes will receive no further Accord updates.";
    }

    internal void ApplyHotbars() => ApplyHotbars(Pack.SelectedJobs(Settings), null);
    private void ApplyCurrentHotbars()
    {
        uint job = (uint)Player.Job;
        if (Pack.IsActive(job, Settings)) ApplyHotbars([Pack.Find(job)!], job);
    }

    private unsafe void ApplyHotbars(JobPreset[] selected, uint? currentClass)
    {
        if (selected.Length == 0) throw new InvalidOperationException("Select at least one active class first.");
        if (!Player.Available || Svc.PlayerState.ContentId == 0) throw new InvalidOperationException("Log in before applying hotbars.");
        uint job = (uint)Player.Job;
        NativeHotbars.Ready(job);
        Prepare(selected);
        var layouts = selected.SelectMany(p => (currentClass is null ? new[] { p.JobId }.Concat(p.BaseClasses) : [currentClass.Value])
            .Select(id => (id, Pack.Resolve(p, Settings))));
        var backup = hotbars.Apply(layouts, Settings.ClearOtherActions);
        try { Mutate(() => Settings.LastHotbarBackup = backup); }
        catch { hotbars.Restore(backup); throw; }
        Status = $"Hotbars for {selected.Length} selected class(es) applied and backed up.";
    }

    internal void Restore()
    {
        if (string.IsNullOrWhiteSpace(Settings.LastHotbarBackup)) throw new InvalidOperationException("No hotbar backup has been recorded.");
        var undo = hotbars.Restore(Settings.LastHotbarBackup);
        try { Mutate(() => { Settings.LastHotbarBackup = undo; Settings.LiveMode = false; }); }
        catch { hotbars.Restore(undo); throw; }
        Status = "Hotbars restored. Live mode disabled so the restored layout is retained.";
    }

    internal void ResetSelectedSettings()
    {
        var selected = Pack.SelectedJobs(Settings);
        if (selected.Length == 0) throw new InvalidOperationException("Select at least one active class first.");
        Prepare(selected, reset: true);
        Status = "Selected classes: reviewed settings restored. Previous settings backed up; variants and hotbars retained.";
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
            Schema = engine.Discover(),
        }, Formatting.Indented));
        Status = "Diagnostics saved to " + path;
    }

    private void UpdateEntries()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].Shown = Player.Available && Pack.IsActive((uint)Player.Job, Settings);
            var definition = VariantDefinitions.All[i];
            entries[i].Text = new SeStringBuilder().AddIcon(Enum.Parse<BitmapFontIcon>(definition.Icon))
                .AddText(" " + (Settings.Enabled(definition.Id) ? "A" : "M")).Build();
        }
    }

    public void Dispose()
    {
        disposed = true;
        engine.Dispose();
        foreach (var definition in VariantDefinitions.All) Svc.DtrBar.Remove("Accord " + definition.Label);
        Instance = null;
    }
}
