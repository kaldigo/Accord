using System.Collections.Generic;

namespace Accord;

public enum VariantId { Burst, Protection, Support, ClassMechanics }

public sealed class AccordSettings : Dalamud.Configuration.IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string EngineConfigurationId { get; set; } = "";
    public string LastEngineRevision { get; set; } = "";
    public Dictionary<VariantId, bool> Variants { get; set; } = new();
    // No classes are managed until explicitly selected, including upgrades.
    public HashSet<uint>? ActiveJobs { get; set; } = new();
    public Dictionary<uint, int> PreparedJobs { get; set; } = new();
    public bool SharedSettingsPrepared { get; set; }
    public bool LiveMode { get; set; }
    public bool ClearOtherActions { get; set; }
    public string LastHotbarBackup { get; set; } = "";
    public bool Enabled(VariantId id) => Variants.GetValueOrDefault(id);
}

internal sealed record VariantDefinition(VariantId Id, string Label, string Icon, string Description);

internal static class VariantDefinitions
{
    internal static readonly VariantDefinition[] All =
    [
        new(VariantId.Burst, "Auto burst", "SwordUnsheathed", "Delegate burst initiation; ST/AoE carries out the accepted sequence."),
        new(VariantId.Protection, "Auto protection", "Tank", "Use reviewed protection through rotation presses; retain deliberate defensive controls."),
        new(VariantId.Support, "Auto support", "Healer", "Use reviewed allied support through appropriate intent paths."),
        new(VariantId.ClassMechanics, "Auto class mechanics", "GoldStar", "Delegate job mechanics such as dances, cards and songs. Effects depend on the job."),
    ];
}
