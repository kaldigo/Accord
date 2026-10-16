using System;
using System.Linq;
using ECommons.GameHelpers;

namespace Accord;

internal static class AccordUi
{
    internal static void Draw()
    {
        var setup = SetupController.Instance;
        if (setup is null) return;
        ImGui.TextUnformatted("Accord — buttons express intent");
        ImGui.TextWrapped("Choose which decisions to delegate to your rotation presses. A = automatic; M = manual.");
        foreach (var definition in VariantDefinitions.All)
        {
            bool enabled = setup.Settings.Enabled(definition.Id);
            if (ImGui.Checkbox(definition.Label, ref enabled)) setup.Run(() => setup.SetVariant(definition.Id, enabled));
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(definition.Description);
        }
        ImGui.Separator();
        bool cleanup = setup.Settings.ClearOtherActions;
        if (ImGui.Checkbox("Clear other actions", ref cleanup)) setup.Run(() => setup.SetCleanup(cleanup));
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Keep only the designed job layout. Preserves game-shared bars and Bridge shared slots. Requires Hotbar Bridge; writes a recovery backup.");
        if (ImGui.Button("Set current class hotbars")) setup.Run(() => setup.ApplyHotbars(false));
        ImGui.SameLine();
        if (ImGui.Button("Set all classes hotbars")) setup.Run(() => setup.ApplyHotbars(true));
        if (ImGui.Button(setup.Settings.LiveMode ? "Disable live mode" : "Enable live mode")) setup.Run(() => setup.SetLive(!setup.Settings.LiveMode));
        ImGui.TextWrapped(setup.Settings.LiveMode
            ? "Live: refresh the current class on variant and class changes."
            : "Static: variants change behaviour. Use a setup button to update hotbars.");
        ImGui.Separator();
        ImGui.TextWrapped(setup.Status);
        var job = Player.Available ? setup.Pack.Find((uint)Player.Job) : null;
        if (job is not null && ImGui.CollapsingHeader("Current class intents"))
            foreach (var definition in VariantDefinitions.All)
                ImGui.TextWrapped($"{definition.Label}: {JobIntents.Describe(job.JobId, definition.Id)}");
        if (!string.IsNullOrWhiteSpace(setup.Settings.LastHotbarBackup) && ImGui.Button("Restore last hotbar backup")) setup.Run(setup.Restore);
        if (ImGui.CollapsingHeader("Reviewed settings and diagnostics"))
        {
            ImGui.TextWrapped("Variant changes preserve detailed settings. Reset reapplies the reviewed settings for the current class and backs up the previous configuration.");
            if (ImGui.Button("Reset current class settings")) setup.Run(setup.ResetCurrentSettings);
            if (ImGui.Button("Export local diagnostics")) setup.Run(setup.ExportDiagnostics);
        }
    }

    internal static void Command(string args)
    {
        var setup = SetupController.Instance!;
        var parts = args.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) { Plugin.Instance.IsOpen = true; return; }
        if (Plugin.Instance.MigrationPending) throw new InvalidOperationException("Open /accord to complete separate setup first.");
        switch (parts[0])
        {
            case "current": setup.ApplyHotbars(false); return;
            case "all": setup.ApplyHotbars(true); return;
            case "live": setup.SetLive(Value(setup.Settings.LiveMode)); return;
            case "restore": setup.Restore(); return;
        }
        var id = parts[0] switch
        {
            "burst" => VariantId.Burst, "protection" => VariantId.Protection,
            "support" => VariantId.Support, "mechanics" => VariantId.ClassMechanics,
            _ => throw new ArgumentException("Use /accord current, all, live, burst, protection, support or mechanics."),
        };
        setup.SetVariant(id, Value(setup.Settings.Enabled(id)));
        bool Value(bool previous) => parts.ElementAtOrDefault(1) switch
        {
            "on" => true, "off" => false, "toggle" => !previous,
            _ => throw new ArgumentException("Specify on, off or toggle."),
        };
    }
}
