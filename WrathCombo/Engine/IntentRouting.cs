using System;
using System.Linq;
using ECommons.GameHelpers;
using WrathCombo.Combos.PvE;
using WrathCombo.Combos.PvE.ALL;
using WrathCombo.Core;
using WrathCombo.Services;
using WrathCombo.Extensions;
using WrathCombo.CustomComboNS;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;

namespace WrathCombo.Engine;

internal static class IntentRouting
{
    private static readonly IntentHandoff Burst = new();
    internal static void Reset() => Burst.Reset();

    private static uint IntentJob => Options.Job;
    internal static bool BurstFollowThrough => Options.Enabled(Feature.BurstContinuation) && Burst.Active(IntentJob, Environment.TickCount64);

    internal static void ObserveInput(uint original, uint replacement)
    {
        // Hypercharge can be routine heat spending. Only the manual burst button
        // makes that particular action a burst initiator when Barrel is unavailable.
        if (Options.Enabled(Feature.BurstContinuation) && IntentJob == 31 && original is MCH.Heatblast or MCH.BlazingShot && replacement == MCH.Hypercharge)
            Burst.ObserveInput(IntentJob, replacement, Environment.TickCount64);
    }

    internal static uint Protection(uint action)
    {
        if (!Player.Available || Player.IsDead ||
            !InCombat() || !HasBattleTarget() || !CanWeave() || InPvP()) return 0;
        // Let the DNC damage path finish the accepted channel before protection can cancel it.
        if ((uint)Player.Job == 38 && DNC.TryImprovisationFinish(out _)) return 0;
        // AoE already holds Flamethrower; injected protection must not bypass that hold.
        if ((uint)Player.Job == 31 && Options.Enabled(Feature.MchFlamethrowerContinuation) &&
            MCH.HoldingFlamethrower) return 0;
        bool generic = action is All.SingleTargetDPS or All.AoEDPS;
        bool aoe = action == All.AoEDPS || (uint)Player.Job switch
        {
            20 => action == MNK.ArmOfTheDestroyer || action == OriginalHook(MNK.ArmOfTheDestroyer),
            22 => action == DRG.DoomSpike || action == OriginalHook(DRG.DoomSpike),
            30 => action == NIN.DeathBlossom,
            34 => action is SAM.Fuga or SAM.Fuko,
            39 => action == RPR.SpinningScythe,
            41 => action is VPR.SteelMaw or VPR.ReavingMaw,
            25 => action is BLM.Blizzard2 or BLM.HighBlizzard2,
            27 => action == SMN.Outburst || action == OriginalHook(SMN.Outburst),
            35 => action is RDM.Scatter or RDM.Impact,
            42 => action == PCT.FireIIinRed,
            _ => false,
        };
        if ((uint)Player.Job == 30 && (NIN.MudraPhase || LocalPlayer.HasStatus(NIN.Buffs.TenChiJin))) return 0;
        if (aoe && GroupDamageIncoming())
        {
            if ((uint)Player.Job is 20 or 22 or 30 or 34 or 39 or 41 && Options.Enabled(Feature.MeleeFeintOnAoe) && RoleActions.Melee.CanFeint())
                return RoleActions.Melee.Feint;
            if ((uint)Player.Job is 25 or 27 or 35 or 42 && Options.Enabled(Feature.CasterAddleOnAoe) && RoleActions.Caster.CanAddle())
                return RoleActions.Caster.Addle;
            if (Options.Enabled(Feature.RdmBarrierOnAoe) && (uint)Player.Job == 35 && !JustUsed(RoleActions.Caster.Addle, 6) &&
                ActionReady(RDM.MagickBarrier) && !LocalPlayer.HasStatus(RDM.Buffs.MagickBarrier, true) &&
                NumberOfAlliesInRange(RDM.MagickBarrier) >= GetPartyMembers().Count * .75)
                return RDM.MagickBarrier;
        }
        uint rangedMitigation = (uint)Player.Job switch
        {
            23 when generic || action is BRD.HeavyShot or BRD.BurstShot or BRD.QuickNock or BRD.Ladonsbite => BRD.Troubadour,
            31 when generic || action is MCH.SplitShot or MCH.HeatedSplitShot or MCH.SpreadShot or MCH.Scattergun => MCH.Tactician,
            38 when generic || action is DNC.Cascade or DNC.Windmill => DNC.ShieldSamba,
            _ => 0,
        };
        // Dances are committed sequences: never replace their steps with protection.
        if ((uint)Player.Job == 38 && (LocalPlayer.HasStatus(DNC.Buffs.StandardStep) || LocalPlayer.HasStatus(DNC.Buffs.TechnicalStep)))
            return 0;
        bool rangedAoe = action == All.AoEDPS || action is BRD.QuickNock or BRD.Ladonsbite or MCH.SpreadShot or MCH.Scattergun or DNC.Windmill;
        if (rangedMitigation == MCH.Tactician && Options.Enabled(rangedAoe ? Feature.MchDismantleOnAoe : Feature.MchDismantleOnSt) && GroupDamageIncoming())
        {
            if (ActionReady(MCH.Dismantle) && !JustUsed(MCH.Tactician, 6) &&
                !CurrentTarget.HasStatus(MCH.Debuffs.Dismantled, true) && CurrentTarget.CanApplyStatus(MCH.Debuffs.Dismantled))
                return MCH.Dismantle;
            if (JustUsed(MCH.Dismantle, 6)) return 0;
        }
        if (Options.Enabled(rangedAoe ? Feature.RangedMitigationOnAoe : Feature.RangedMitigationOnSt) && rangedMitigation != 0 && GroupDamageIncoming() && ActionReady(rangedMitigation) &&
            NumberOfAlliesInRange(rangedMitigation) >= GetPartyMembers().Count * .75 &&
            !LocalPlayer.HasStatusEffects([BRD.Buffs.Troubadour, DNC.Buffs.ShieldSamba, MCH.Buffs.Tactician], true))
            return rangedMitigation;
        // Tanks already evaluate the reviewed automatic mitigation selectors in
        // both rotations. Their manual selectors intentionally have looser timing.
        return 0;
    }

    internal static void ConfirmAction(uint action)
    {
        if (!Options.Enabled(Feature.BurstContinuation)) return;
        if (Burst.ConfirmInput(IntentJob, action, Environment.TickCount64)) return;
        bool initiator = IntentJob switch
        {
            19 => action == PLD.FightOrFlight,
            21 => action is WAR.Berserk or WAR.InnerRelease,
            32 => action is DRK.BloodWeapon or DRK.Delirium or DRK.LivingShadow,
            37 => action is GNB.NoMercy or GNB.Bloodfest,
            23 => action is BRD.RagingStrikes or BRD.BattleVoice or BRD.Barrage or BRD.RadiantFinale,
            31 => action is MCH.BarrelStabilizer or MCH.Wildfire,
            35 => action is RDM.Embolden or RDM.Manafication,
            20 => action is MNK.RiddleOfFire or MNK.Brotherhood,
            22 => action is DRG.LanceCharge or DRG.BattleLitany,
            30 => action is NIN.Mug or NIN.Dokumori or NIN.TrickAttack or NIN.KunaisBane,
            34 => action == SAM.Ikishoten,
            39 => action == RPR.ArcaneCircle,
            41 => action == VPR.SerpentsIre,
            42 => action == PCT.StarryMuse,
            27 => action == SMN.SearingLight,
            25 => action == BLM.LeyLines,
            _ => false,
        };
        if (initiator) Burst.Accept(IntentJob, Environment.TickCount64);
    }

    internal static bool? PresetOverride(Preset preset)
    {
        // Meisui consumes the result of an accepted TCJ, not a new mechanic.
        if (preset is Preset.NIN_ST_AdvancedMode_Meisui or Preset.NIN_AoE_AdvancedMode_Meisui &&
            Options.Enabled(Feature.TcjContinuation) &&
            (JustUsed(NIN.TCJSuiton, 20) || JustUsed(NIN.TCJHuton, 20)))
            return true;
        if (BurstPolicy.Allows(IntentJob, (int)preset) && BurstFollowThrough)
            return true;
        return null;
    }
}
