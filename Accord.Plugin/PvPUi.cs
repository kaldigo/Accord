using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;

namespace Accord;

internal static class PvPUi
{
    internal static void Draw(SetupController setup)
    {
        ImGui.TextUnformatted("Accord PvP - buttons express intent");
        DrawClassSelection(setup);
        ImGui.Separator();
        ImGui.TextWrapped("Choose which decisions to delegate to your rotation presses. A = automatic; M = manual.");
        var job = Player.Available ? setup.PvP.Pack.Find((uint)Player.Job) : null;
        if (job is null) ImGui.TextWrapped("Switch to a supported class to see what each variant controls for that class.");
        foreach (var definition in VariantDefinitions.All)
        {
            bool enabled = setup.PvP.Settings.Enabled(definition.Id);
            if (ImGui.Checkbox(definition.Label, ref enabled)) setup.Run(() => setup.SetVariant(definition.Id, enabled));
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(definition.Description);
            if (job is not null)
            {
                ImGui.Indent();
                ImGui.TextWrapped($"{job.Job}: {Describe(job, definition.Id)}");
                ImGui.Unindent();
            }
        }
        ImGui.TextWrapped("Wrath thresholds and optional timing policies still apply. Where these disable an action, its manual control stays available.");
        ImGui.Separator();
        bool cleanup = setup.PvP.Settings.ClearOtherActions;
        if (ImGui.Checkbox("Clear other actions", ref cleanup)) setup.Run(() => setup.SetCleanup(cleanup));
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Keep only the designed job layout. Preserves game-shared bars, Bridge shared slots and bars linked across PvE/PvP. Requires Hotbar Bridge; writes a recovery backup.");
        if (!setup.PvP.Settings.LiveMode && ImGui.Button("Set selected PvP class hotbars")) setup.Run(setup.ApplyHotbars);
        if (ImGui.Button(setup.PvP.Settings.LiveMode ? "Disable live mode" : "Enable live mode")) setup.Run(() => setup.SetLive(!setup.PvP.Settings.LiveMode));
        ImGui.TextWrapped(setup.PvP.Settings.LiveMode
            ? "Live: refresh the current selected class on changes and check every second. Updates pause during combat and loading, then resume."
            : "Static: variants change behaviour. Use a setup button to update hotbars.");
        ImGui.TextWrapped("Keyboard actions close gaps within each bar; fixed controls and controller positions stay in place. Hotbar Bridge is required.");
        ImGui.Separator();
        ImGui.TextWrapped("PvP recovery, role actions and limit breaks have their own job slots. Linked bars and shared slots keep their carried assignments.");
        ImGui.TextWrapped(setup.Status);
        if (setup.HasPendingOperation && ImGui.Button("Keep recovery record and review again")) setup.Run(setup.AbandonPendingOperation);
        if (ImGui.CollapsingHeader("Maintenance"))
        {
            ImGui.TextWrapped("Reapply PvP intent settings for selected classes. Keeps personal HP/charge thresholds, variants and hotbars. Hands-free auto rotation is unchanged.");
            if (ImGui.Button("Reapply selected classes' PvP intent settings")) setup.Run(setup.ResetSelectedSettings);
            if (!string.IsNullOrWhiteSpace(setup.PvP.Settings.LastHotbarBackup))
            {
                ImGui.Separator();
                ImGui.TextWrapped("Restore the saved hotbar layout from before the last Accord hotbar operation, including live updates. Turns live mode off; Wrath settings and variants stay as they are. This is not necessarily your layout from before first using Accord.");
                if (ImGui.Button("Restore previous hotbar layout")) setup.Run(setup.Restore);
            }
            ImGui.Separator();
            ImGui.TextWrapped("Save Accord and Wrath settings and status to a local file for troubleshooting. Nothing is uploaded.");
            if (ImGui.Button("Save troubleshooting report")) setup.Run(setup.ExportDiagnostics);

        }
    }

    private static void DrawClassSelection(SetupController setup)
    {
        ImGui.TextUnformatted("Active classes");
        ImGui.TextWrapped("Click a class to enable or disable Accord. PvP selection is separate from PvE and starts with none selected.");
        if (ImGui.Button("Select all")) setup.Run(() => setup.SetActiveJobs(setup.PvP.Pack.Jobs.Select(j => j.JobId)));
        ImGui.SameLine();
        if (ImGui.Button("Select none")) setup.Run(() => setup.SetActiveJobs(Array.Empty<uint>()));

        float scale = ImGuiHelpers.GlobalScale;
        int columns = Math.Clamp((int)(ImGui.GetContentRegionAvail().X / (72 * scale)), 2, 8);
        if (ImGui.BeginTable("AccordClasses", columns, ImGuiTableFlags.SizingStretchSame))
        {
            foreach (var candidate in setup.PvP.Pack.Jobs)
            {
                ImGui.TableNextColumn();
                bool active = setup.PvP.Active(candidate.JobId);
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
                    var selected = setup.PvP.Settings.ActiveJobs.ToHashSet();
                    if (active) selected.Remove(candidate.JobId); else selected.Add(candidate.JobId);
                    setup.SetActiveJobs(selected);
                });
                ImGui.TextUnformatted(candidate.Job);
            }
            ImGui.EndTable();
        }
        ImGui.TextUnformatted($"Accord active for {setup.PvP.Settings.ActiveJobs.Count} classes.");
    }

    private static string Describe(PvPProfile job, VariantId variant)
    {
        return job.Descriptions.GetValueOrDefault(variant) ?? "No additional automation for this variant.";
    }
}
