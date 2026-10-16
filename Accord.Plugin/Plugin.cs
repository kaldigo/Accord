global using Dalamud.Bindings.ImGui;
using System;
using System.IO;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Accord;

public sealed class Plugin : IDalamudPlugin
{
    internal static Plugin Instance = null!;
    internal bool IsOpen;
    internal bool MigrationPending;
    internal bool RequiresMigration;
    internal string MigrationSource = "";
    internal AccordSettings Settings;
    private readonly SetupController setup;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        ECommonsMain.Init(pluginInterface, this, Module.DalamudReflector);
        // Preserve the combined source and require a reviewed API import before replacing it.
        var file = pluginInterface.ConfigFile;
        if (file.Exists && JObject.Parse(File.ReadAllText(file.FullName))["EnabledActionsV6"] is not null)
        {
            string backup = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "combined-prototype-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json");
            Directory.CreateDirectory(pluginInterface.GetPluginConfigDirectory());
            File.Copy(file.FullName, backup, false);
            var combined = JObject.Parse(File.ReadAllText(file.FullName));
            Settings = combined["Accord"]?.ToObject<AccordSettings>() ?? new AccordSettings();
            MigrationSource = backup;
            Settings.PreparedJobs.Clear();
            Settings.SharedSettingsPrepared = false;
            Settings.EngineConfigurationId = "";
            Settings.LastEngineRevision = "";
            Settings.LiveMode = false;
            MigrationPending = true;
            RequiresMigration = true;
            IsOpen = true;
        }
        else Settings = pluginInterface.GetPluginConfig() as AccordSettings ?? new AccordSettings();
        setup = new SetupController();
        Svc.Commands.AddHandler("/accord", new Dalamud.Game.Command.CommandInfo((_, args) => setup.Run(() => AccordUi.Command(args))) { HelpMessage = "Open Accord setup and hotbar controls." });
        pluginInterface.UiBuilder.Draw += Draw;
        pluginInterface.UiBuilder.OpenConfigUi += Open;
        pluginInterface.UiBuilder.OpenMainUi += Open;
        Svc.Framework.Update += Update;
    }

    private void Open() => IsOpen = true;
    private void Update(IFramework _) => setup.Run(setup.Tick);
    private void Draw()
    {
        if (!IsOpen) return;
        if (ImGui.Begin("Accord###Accord", ref IsOpen))
        {
            if (MigrationPending)
            {
                ImGui.TextWrapped(setup.MigrationSummary);
                ImGui.TextWrapped(setup.Status);
                if (ImGui.Button("Preview personal settings import")) setup.Run(setup.PreviewMigration);
                if (setup.MigrationReady && ImGui.Button("Import these personal settings")) setup.Run(setup.ImportMigration);
                if (!RequiresMigration && !setup.HasPendingOperation && ImGui.Button("Cancel import")) MigrationPending = false;
                if (setup.HasPendingOperation && ImGui.Button("Keep recovery record and review again")) setup.Run(setup.AbandonPendingOperation);
            }
            else AccordUi.Draw();
        }
        ImGui.End();
    }
    public void Dispose()
    {
        Svc.Framework.Update -= Update;
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= Open;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= Open;
        Svc.Commands.RemoveHandler("/accord");
        setup.Dispose();
        ECommonsMain.Dispose();
    }
}
