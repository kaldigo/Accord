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


public static class FeatureScope
{
    public static bool Supports(uint job, Feature feature) => feature switch
    {
        Feature.BurstContinuation => job is >= 19 and <= 42 && job is not 26 and not 29,
        Feature.TcjContinuation => job == 30,
        Feature.DncStandardInitiation or Feature.DncTechnicalInitiation or Feature.DncCuringWaltz => job == 38,
        Feature.BardSongInitiation or Feature.BardMinneOnAoe => job == 23,
        Feature.AstCardsOnStDamage or Feature.AstCardsOnAoeDamage => job == 33,
        Feature.SageKardiaOnStDamage or Feature.SageKardiaOnAoeDamage => job == 40,
        Feature.PctTemperaOnAoe => job == 42,
        Feature.RprCrestOnAoe => job == 39,
        Feature.MeleeFeintOnAoe => job is 20 or 22 or 30 or 34 or 39 or 41,
        Feature.CasterAddleOnAoe => job is 25 or 27 or 35 or 42,
        Feature.RdmBarrierOnAoe => job == 35,
        Feature.RangedMitigationOnSt or Feature.RangedMitigationOnAoe => job is 23 or 31 or 38,
        Feature.MchDismantleOnSt or Feature.MchDismantleOnAoe => job == 31,
        _ => false,
    };
}
