using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;

namespace Accord;

internal static class AccordUi
{
    internal static void Draw()
    {
        var setup = SetupController.Instance;
        if (setup is null) return;
        ImGui.TextUnformatted("Accord — buttons express intent");
        ImGui.TextWrapped($"Current hotbars: {(SetupController.IsPvP ? "PvP" : "PvE")}. Active classes, variants and live mode are shared.");
        DrawClassSelection(setup);
        ImGui.Separator();
        ImGui.TextWrapped("Choose which decisions to delegate to your rotation presses. A = automatic; M = manual.");
        var job = Player.Available ? setup.Pack.Find((uint)Player.Job) : null;
        if (job is null) ImGui.TextWrapped("Switch to a supported class to see what each variant controls for that class.");
        foreach (var definition in VariantDefinitions.All)
        {
            bool enabled = setup.Settings.Enabled(definition.Id);
            if (ImGui.Checkbox(definition.Label, ref enabled)) setup.Run(() => setup.SetVariant(definition.Id, enabled));
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(definition.Description);
            if (job is not null)
            {
                ImGui.Indent();
                var pvp = SetupController.IsPvP ? setup.PvP.Pack.Find(job.JobId) : null;
                ImGui.TextWrapped($"{job.Job}: {(pvp is null ? JobIntents.Describe(job.JobId, definition.Id) : pvp.Descriptions.GetValueOrDefault(definition.Id, "No additional automation for this variant."))}");
                ImGui.Unindent();
            }
        }
        ImGui.Separator();
        bool cleanup = setup.Settings.ClearOtherActions;
        if (ImGui.Checkbox("Clear other actions", ref cleanup)) setup.Run(() => setup.SetCleanup(cleanup));
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Keep only the designed job layout. Preserves game-shared bars, utility bars, shared slots and existing Sprint buttons. Writes a recovery backup.");
        if (!setup.Settings.LiveMode && ImGui.Button("Set selected class hotbars")) setup.Run(setup.ApplyHotbars);
        if (ImGui.Button(setup.Settings.LiveMode ? "Disable live mode" : "Enable live mode")) setup.Run(() => setup.SetLive(!setup.Settings.LiveMode));
        ImGui.TextWrapped(setup.Settings.LiveMode
            ? "Live: refresh the current selected class on changes and check every second. Clear other actions also removes newly added leveling actions."
            : "Static: variants change behaviour. Use a setup button to update hotbars.");
        ImGui.TextWrapped("Keyboard actions close gaps within each bar; fixed controls and controller positions stay in place. Accord manages both regular and controller layouts. Sprint switches between PvE and PvP on all active hotbars, including utility bars.");
        ImGui.Separator();
        bool manualLogout = setup.Settings.ManualLogoutFallback;
        if (ImGui.Checkbox("Save manual hotbars before logout", ref manualLogout)) setup.Run(() => setup.SetManualFallback(manualLogout));
        ImGui.TextWrapped("Saves three-bar manual layouts for selected combat classes before normal logout. Restores your exact previous Accord layouts on login when compatible Wrath is available. Uses regular bars 1–3 and controller pages 1–3; other bars stay unchanged.");
        if (ImGui.Button("Prepare manual hotbars now")) setup.Run(setup.PrepareManualHotbars);
        ImGui.SameLine();
        if (ImGui.Button("Restore Accord hotbars")) setup.Run(setup.RestoreAccordHotbars);
        ImGui.TextWrapped(setup.ManualStatus);
        ImGui.Separator();
        ImGui.TextWrapped(setup.Status);
        if (setup.HasPendingOperation && ImGui.Button("Keep recovery record and review again")) setup.Run(setup.AbandonPendingOperation);
        if (ImGui.CollapsingHeader("Maintenance"))
        {
            ImGui.TextWrapped("Reapply Accord's default Wrath settings for selected classes. Backs up the previous settings and keeps your variants and hotbars.");
            if (ImGui.Button("Reset selected classes to Accord defaults")) setup.Run(setup.ResetSelectedSettings);
            if (!string.IsNullOrWhiteSpace(SetupController.IsPvP ? setup.PvP.Settings.LastHotbarBackup : setup.Settings.LastHotbarBackup))
            {
                ImGui.Separator();
                ImGui.TextWrapped("Restore the saved hotbar layout from before the last Accord hotbar operation, including live updates. Turns live mode off; Wrath settings and variants stay as they are. This is not necessarily your layout from before first using Accord.");
                if (ImGui.Button("Restore previous hotbar layout")) setup.Run(setup.Restore);
            }
            ImGui.Separator();
            ImGui.TextWrapped("Save Accord and Wrath settings and status to a local file for troubleshooting. Nothing is uploaded.");
            if (ImGui.Button("Save troubleshooting report")) setup.Run(setup.ExportDiagnostics);
            foreach (var backup in Directory.EnumerateFiles(Svc.PluginInterface.GetPluginConfigDirectory(), "combined-prototype-*.json").OrderByDescending(File.GetLastWriteTimeUtc))
                if (ImGui.Button("Review settings from old combined plugin: " + Path.GetFileName(backup)))
                {
                    Plugin.Instance.MigrationSource = backup;
                    Plugin.Instance.MigrationPending = true;
                    setup.Run(setup.PreviewMigration);
                }
        }
    }

    private static void DrawClassSelection(SetupController setup)
    {
        ImGui.TextUnformatted("Active classes");
        ImGui.TextWrapped("Click a class to enable or disable Accord. Base classes follow their job.");
        if (ImGui.Button("Select all")) setup.Run(() => setup.SetActiveJobs(setup.Pack.Data.Jobs.Select(j => j.JobId)));
        ImGui.SameLine();
        if (ImGui.Button("Select none")) setup.Run(() => setup.SetActiveJobs(Array.Empty<uint>()));

        float scale = ImGuiHelpers.GlobalScale;
        int columns = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / (72 * scale)), 2, 8);
        if (ImGui.BeginTable("AccordClasses", columns, ImGuiTableFlags.SizingStretchSame))
        {
            foreach (var candidate in setup.Pack.Data.Jobs)
            {
                ImGui.TableNextColumn();
                bool active = setup.Pack.IsActive(candidate.JobId, setup.Settings);
                var icon = ClassIcons.Get(candidate.JobId, active);
                ImGui.PushStyleColor(ImGuiCol.Button, active
                    ? ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderActive]
                    : new Vector4(0.15f, 0.15f, 0.15f, 0.4f));
                ImGui.PushID((int)candidate.JobId);
                bool clicked = icon is not null
                    ? ImGui.ImageButton(icon.Handle, new Vector2(40 * scale))
                    : ImGui.Button($"{candidate.Job}##class-{candidate.JobId}", new Vector2(48 * scale));
                ImGui.PopID();
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"{candidate.Job}: {(active ? "selected — click to disable Accord" : "not selected — click to enable Accord")}\nDeselecting retains existing settings and hotbars.");
                if (clicked) setup.Run(() =>
                {
                    var selected = setup.Pack.SelectedJobs(setup.Settings).Select(j => j.JobId).ToHashSet();
                    if (active) selected.Remove(candidate.JobId); else selected.Add(candidate.JobId);
                    setup.SetActiveJobs(selected);
                });
                ImGui.TextUnformatted(candidate.Job);
            }
            ImGui.EndTable();
        }
        ImGui.TextUnformatted($"Accord active for {setup.Pack.SelectedJobs(setup.Settings).Length} classes.");
    }

    internal static void Command(string args)
    {
        var setup = SetupController.Instance!;
        var parts = args.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) { Plugin.Instance.IsOpen = true; return; }
        if (Plugin.Instance.MigrationPending) throw new InvalidOperationException("Open /accord to complete separate setup first.");
        switch (parts[0])
        {
            case "selected": setup.ApplyHotbars(); return;
            case "live": setup.SetLive(Value(setup.Live)); return;
            case "restore": setup.Restore(); return;
            case "manual": setup.PrepareManualHotbars(); return;
            case "normal": setup.RestoreAccordHotbars(); return;
        }
        var id = parts[0] switch
        {
            "burst" => VariantId.Burst, "protection" => VariantId.Protection,
            "support" => VariantId.Support, "mechanics" => VariantId.ClassMechanics,
            _ => throw new ArgumentException("Use /accord manual, normal, selected, restore, live, burst, protection, support or mechanics."),
        };
        setup.SetVariant(id, Value(setup.VariantEnabled(id)));
        bool Value(bool previous) => parts.ElementAtOrDefault(1) switch
        {
            "on" => true, "off" => false, "toggle" => !previous,
            _ => throw new ArgumentException("Specify on, off or toggle."),
        };
    }
}
