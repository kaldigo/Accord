using Accord;
using Newtonsoft.Json.Linq;

// Independently reviewed prerequisites of the retained intent buttons and accepted
// sequences. Resolve names against the built engine, not the setup's numeric IDs.
// This checks configuration access; it does not simulate combat or native targeting.
internal static class ActionAccess
{
    private static readonly Dictionary<string, string[]> Required = new()
    {
        ["PLD"] = ["ST_AdvancedMode_Requiescat", "ST_AdvancedMode_Confiteor", "ST_AdvancedMode_BladeOfHonor", "AoE_AdvancedMode_Requiescat", "AoE_AdvancedMode_Confiteor", "AoE_AdvancedMode_BladeOfHonor", "RetargetSheltron", "BlockForWings"],
        ["WAR"] = ["ST_FellCleave", "ST_PrimalRend", "ST_PrimalWrath", "ST_PrimalRuination", "AoE_Decimate", "AoE_PrimalRend", "AoE_PrimalWrath", "AoE_PrimalRuination", "RawIntuition_Targeting"],
        ["DRK"] = ["ST_Spenders", "ST_Sp_ScarletChain", "ST_CD_Disesteem", "AoE_Spenders", "AoE_Sp_ImpalementChain", "AoE_CD_Disesteem", "Retarget_TBN"],
        ["GNB"] = ["ST_Continuation", "ST_GnashingFang", "ST_Reign", "ST_SonicBreak", "AoE_Reign", "AoE_FatedBrand", "AoE_SonicBreak", "HoC_Features"],
        ["WHM"] = ["ST_MainCombo_GlareIV", "AoE_DPS_GlareIV", "STHeals", "AoEHeals", "Raise", "ThinAirRaise", "Re_Asylum"],
        ["SCH"] = ["ST_ADV_DPS_BanefulImpact", "AoE_ADV_DPS_BanefulImpact", "ST_Heal", "AoE_Heal", "ST_Heal_Aetherflow", "AoE_Heal_Aetherflow", "ST_Heal_Aetherpact", "AoE_Heal_Consolation", "Raise"],
        ["AST"] = ["DPS_Oracle", "AOE_Oracle", "DPS_AutoPlay", "AOE_AutoPlay", "ST_Heals_Arrow", "ST_Heals_Bole", "ST_Heals_Spire", "ST_Heals_Ewer", "AoE_Heals_Horoscope", "AoE_Heals_HoroscopeHeal", "AoE_Heals_StellarDetonation", "Retargets", "Raise_Alternative"],
        ["SGE"] = ["ST_Advanced_Heal", "AoE_Advanced_Heal", "ST_Adv_Heal_EDiagnosis", "AoE_Adv_Heal_EPrognosis", "AoE_Adv_Heal_Philosophia", "ZoePneuma", "Retarget", "Retarget_Kardia", "Raise"],
        ["MNK"] = ["STUseMasterfulBlitz", "AoEUseMasterfulBlitz", "STUseFiresReply", "AoEUseFiresReply", "STUseWindsReply", "AoEUseWindsReply", "STUseFormShift", "AoEUseFormShift", "STUseMeditation", "AoEUseMeditation"],
        ["DRG"] = ["ST_Damage", "ST_Nastrond", "ST_Stardiver", "ST_Starcross", "ST_RiseOfTheDragon", "AoE_Damage", "AoE_Nastrond", "AoE_Stardiver", "AoE_Starcross", "AoE_RiseOfTheDragon"],
        ["NIN"] = ["ST_AdvancedMode_Ninjitsus", "AoE_AdvancedMode_Ninjitsus", "ST_AdvancedMode_Ninjitsus_Hyosho", "AoE_AdvancedMode_Ninjitsus_Goka", "ST_AdvancedMode_TenriJindo", "AoE_AdvancedMode_TenriJindo", "HideMug"],
        ["SAM"] = ["ST_Adv_OgiNamikiri", "AoE_Adv_OgiNamikiri", "ST_Adv_Zanshin", "AoE_Adv_Zanshin", "ST_Adv_Iaijutsu", "ST_Adv_Tsubame", "AoE_Adv_TenkaGoken", "GyotenYaten"],
        ["RPR"] = ["ST_Reaping", "ST_Lemure", "ST_Sacrificium", "ST_Communio", "ST_Perfectio", "AoE_Reaping", "AoE_Lemure", "AoE_Sacrificium", "AoE_Communio", "AoE_Perfectio", "Regress"],
        ["VPR"] = ["ST_GenerationCombo", "ST_LegacyWeaves", "ST_SerpentsTail", "AoE_ReawakenCombo", "AoE_SerpentsTail", "ST_UncoiledFuryCombo", "AoE_UncoiledFuryCombo"],
        ["BRD"] = ["Adv_Song", "AoE_Adv_Songs", "Adv_BuffsResonant", "Adv_BuffsEncore", "AoE_BuffsResonant", "AoE_BuffsEncore"],
        ["MCH"] = ["ST_Adv_Stabilizer_FullMetalField", "AoE_Adv_Stabilizer_FullMetalField", "ST_Adv_Hypercharge", "AoE_Adv_Hypercharge", "ST_Adv_Heatblast", "ST_Adv_GaussRicochet", "AoE_Adv_GaussRicochet"],
        ["DNC"] = ["ST_Adv_SS", "ST_Adv_SS_Prepull", "ST_Adv_FM", "ST_Adv_TS", "ST_Adv_Tillana", "ST_Adv_LD", "ST_Adv_DawnDance", "AoE_Adv_SS", "AoE_Adv_FM", "AoE_Adv_TS", "AoE_Adv_Tillana", "AoE_Adv_LD", "AoE_Adv_DawnDance"],
        ["BLM"] = ["ST_FlareStar", "ST_Despair", "ST_Transpose", "AoE_Transpose", "UmbralSoul", "Aetherial_Manipulation"],
        ["SMN"] = ["ST_Advanced_Combo_DemiSummons_Attacks", "AoE_Advanced_Combo_DemiSummons_Attacks", "ST_Advanced_Combo_Egi_AstralFlow", "AoE_Advanced_Combo_Egi_AstralFlow", "ST_Advanced_Combo_SearingFlash", "AoE_Advanced_Combo_SearingFlash", "ST_Advanced_Combo_DemiSummons_Rekindle_Retarget", "AoE_Advanced_Combo_DemiSummons_Rekindle_Retarget", "Raise"],
        ["RDM"] = ["ST_HolyFlare", "AoE_HolyFlare", "ST_MeleeCombo", "AoE_MeleeCombo", "ST_ViceOfThorns", "AoE_ViceOfThorns", "ST_Prefulgence", "AoE_Prefulgence", "CorpsDisplacement", "Raise"],
        ["PCT"] = ["ST_AdvancedMode_PrePullMotifs", "AoE_AdvancedMode_PrePullMotifs", "ST_AdvancedMode_NoTargetMotifs", "AoE_AdvancedMode_NoTargetMotifs", "ST_AdvancedMode_HammerStampCombo", "AoE_AdvancedMode_HammerStampCombo", "ST_AdvancedMode_StarPrism", "AoE_AdvancedMode_StarPrism", "ST_AdvancedMode_RainbowDrip", "AoE_AdvancedMode_RainbowDrip"],
    };

    internal static int Verify(string job, JObject settings, JObject names)
    {
        if (!Required.TryGetValue(job, out var suffixes)) return 0;
        var enabled = settings["EnabledActionsV6"]!.Values<int>().ToHashSet();
        foreach (string suffix in suffixes)
        {
            string name = job + "_" + suffix;
            int id = names[name]?.Value<int>() ?? throw new Exception("Unknown reviewed prerequisite: " + name);
            if (!enabled.Contains(id)) throw new Exception("Lost accepted sequence or retained control: " + name);
        }
        if (job == "NIN")
            foreach (string route in new[] { "ST", "AoE" })
                if (settings["CustomBoolValuesV6"]?[$"NIN_{route}_AdvancedMode_TenChiJin_Auto"]?.Value<bool>() != true)
                    throw new Exception("Manual TCJ cannot complete on " + route);
        if (job == "BRD")
            foreach (string route in new[] { "ST", "AoE" })
                if (settings["CustomBoolValuesV6"]?[$"BRD_{route}_Wardens_Auto"]?.Value<bool>() != true)
                    throw new Exception("Automatic Paean cannot resolve party recipients on " + route);
        return suffixes.Length;
    }
}
