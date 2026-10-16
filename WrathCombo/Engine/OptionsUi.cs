using System;
using ECommons.GameHelpers;
using ECommons.DalamudServices;
using WrathCombo.Services;
using WrathFork.Contract;

namespace WrathCombo.Engine;

internal static class OptionsUi
{
    internal static void Draw()
    {
        if (!Player.Available) { ImGui.TextUnformatted("Select a job to configure engine options."); return; }
        ImGui.TextWrapped("Granular engine options for the current job. Accord may manage these settings through the API.");
        uint job = Options.Job;
        foreach (var feature in Enum.GetValues<Feature>())
        {
            bool enabled = Options.Enabled(feature);
            if (!ImGui.Checkbox(feature.ToString(), ref enabled)) continue;
            if (!Service.Configuration.ForkFeatures.TryGetValue(job, out var options))
                Service.Configuration.ForkFeatures[job] = options = new();
            options[feature] = enabled;
            Svc.PluginInterface.SavePluginConfig(Service.Configuration);
        }
    }
}
