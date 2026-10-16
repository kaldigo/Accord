namespace WrathFork.Contract;

// Engine capabilities, not client variants. Append entries; never renumber.
public enum Feature
{
    BurstContinuation, TcjContinuation,
    DncStandardInitiation, DncTechnicalInitiation, BardSongInitiation, DncCuringWaltz,
    AstCardsOnStDamage, AstCardsOnAoeDamage, SageKardiaOnStDamage, SageKardiaOnAoeDamage,
    BardMinneOnAoe, PctTemperaOnAoe, RprCrestOnAoe,
    MeleeFeintOnAoe, CasterAddleOnAoe, RdmBarrierOnAoe,
    RangedMitigationOnSt, RangedMitigationOnAoe, MchDismantleOnSt, MchDismantleOnAoe,
}

public static class FeatureDefaults
{
    public static bool Enabled(Feature feature) => feature is Feature.DncStandardInitiation or
        Feature.DncTechnicalInitiation or Feature.BardSongInitiation or Feature.DncCuringWaltz;
}
