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
    BlmManawardOnAoe, WhmDivineCaressOnDamage,
    MnkMantraOnAoe, MchWildfireOnAoe, DncPartnerOnAoe,
    DncTargetlessPrepull, // Retired, inert compatibility value; never reuse this numeric ID.
    DncImprovisationFinish, MchFlamethrowerContinuation,
    MnkEarthOnAoe, MnkEarthReplyOnDamage, SamThirdEyeOnAoe,
    PvPWarBloodwhettingInitiation, PvPWarRendInitiation, PvPGnbGnashingInitiation, PvPDrgGeirskogulInitiation, PvPSamOgiInitiation, PvPRprPlentifulInitiation, PvPRprWarrantInitiation, PvPRdmEmboldenInitiation, PvPRdmForteInitiation, PvPRdmRiposteInitiation, PvPPctCoatInitiation, PvPDncStarfallInitiation, PvPDncFanInitiation, PvPDncHoningInitiation, PvPBlmOffensiveBurstInitiation, PvPBlmDefensiveBurstInitiation, PvPBlmOffensiveWeaveInitiation, PvPBlmDefensiveWeaveInitiation, PvPBlmOffensiveXenoInitiation, PvPBlmDefensiveXenoInitiation,
    PvPDncPartnerRecipient, PvPSgeKardiaRecipient,
    PvPAstGravityInitiation,
    PvPIntentRoutes,
    PvPBrdAllyCleanse, PvPVprScalesProtection,
}

public static class FeatureDefaults
{
    public static bool Enabled(Feature feature) => feature is Feature.DncStandardInitiation or
        Feature.DncTechnicalInitiation or Feature.BardSongInitiation or Feature.DncCuringWaltz or
        Feature.PvPAstGravityInitiation or Feature.PvPWarBloodwhettingInitiation or Feature.PvPWarRendInitiation or Feature.PvPGnbGnashingInitiation or Feature.PvPDrgGeirskogulInitiation or Feature.PvPSamOgiInitiation or Feature.PvPRprPlentifulInitiation or Feature.PvPRprWarrantInitiation or Feature.PvPRdmEmboldenInitiation or Feature.PvPRdmForteInitiation or Feature.PvPRdmRiposteInitiation or Feature.PvPPctCoatInitiation or Feature.PvPDncStarfallInitiation or Feature.PvPDncFanInitiation or Feature.PvPDncHoningInitiation or Feature.PvPBlmOffensiveBurstInitiation or Feature.PvPBlmDefensiveBurstInitiation or Feature.PvPBlmOffensiveWeaveInitiation or Feature.PvPBlmDefensiveWeaveInitiation or Feature.PvPBlmOffensiveXenoInitiation or Feature.PvPBlmDefensiveXenoInitiation;
}


public static class FeatureScope
{
    public static bool Supports(uint job, Feature feature) => feature switch
    {
        Feature.BurstContinuation => job is >= 19 and <= 42 && job is not 26 and not 29,
        Feature.BlmManawardOnAoe => job == 25,
        Feature.WhmDivineCaressOnDamage => job == 24,
        Feature.MnkMantraOnAoe or Feature.MnkEarthOnAoe or Feature.MnkEarthReplyOnDamage => job == 20,
        Feature.SamThirdEyeOnAoe => job == 34,
        Feature.MchWildfireOnAoe or Feature.MchFlamethrowerContinuation => job == 31,
        Feature.DncPartnerOnAoe or Feature.DncTargetlessPrepull or Feature.DncImprovisationFinish => job == 38,
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
        Feature.PvPWarBloodwhettingInitiation or Feature.PvPWarRendInitiation => job == 21,
        Feature.PvPGnbGnashingInitiation => job == 37,
        Feature.PvPDrgGeirskogulInitiation => job == 22,
        Feature.PvPSamOgiInitiation => job == 34,
        Feature.PvPRprPlentifulInitiation or Feature.PvPRprWarrantInitiation => job == 39,
        Feature.PvPRdmEmboldenInitiation or Feature.PvPRdmForteInitiation or Feature.PvPRdmRiposteInitiation => job == 35,
        Feature.PvPPctCoatInitiation => job == 42,
        Feature.PvPDncStarfallInitiation or Feature.PvPDncFanInitiation or Feature.PvPDncHoningInitiation => job == 38,
        Feature.PvPBlmOffensiveBurstInitiation or Feature.PvPBlmDefensiveBurstInitiation or Feature.PvPBlmOffensiveWeaveInitiation or Feature.PvPBlmDefensiveWeaveInitiation or Feature.PvPBlmOffensiveXenoInitiation or Feature.PvPBlmDefensiveXenoInitiation => job == 25,
        Feature.PvPDncPartnerRecipient => job == 38,
        Feature.PvPSgeKardiaRecipient => job == 40,
        Feature.PvPAstGravityInitiation => job == 33,
        Feature.PvPIntentRoutes => job is >=19 and <=42 && job is not 26 and not 29 and not 36,
        Feature.PvPBrdAllyCleanse => job==23,
        Feature.PvPVprScalesProtection => job==41,
        _ => false,
    };
}
