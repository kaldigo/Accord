using ECommons.GameHelpers;
using WrathCombo.Services;
using WrathFork.Contract;

namespace WrathCombo.Engine;

internal static class Options
{
    internal static uint Job => (uint)Player.Job switch
    {
        1 => 19, 2 => 20, 3 => 21, 4 => 22, 5 => 23, 6 => 24, 7 => 25, 26 => 27, 29 => 30,
        var job => job,
    };

    internal static bool Enabled(Feature feature) => Player.Available &&
        (Service.Configuration.ForkFeatures.TryGetValue(Job, out var job) && job.TryGetValue(feature, out bool value)
            ? value : FeatureDefaults.Enabled(feature));
}
