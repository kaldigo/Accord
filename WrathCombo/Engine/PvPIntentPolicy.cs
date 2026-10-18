using System.Collections.Generic;
using WrathCombo.Core;
using WrathFork.Contract;

namespace WrathCombo.Engine;

// Engine-owned action families. These are selection/continuation capabilities;
// clients decide which initiation switches to enable through the settings API.
internal static class PvPIntentPolicy
{
    static PvPIntentPolicy()
    {
        Jobs[23][2]=Jobs[23][2] with { Features=[Feature.PvPBrdAllyCleanse], Initiators=[29400] };
        Jobs[41][1]=Jobs[41][1] with { Features=[Feature.PvPVprScalesProtection] };
        Jobs[30][1]=Jobs[30][1] with { Initiators=[29507,29508,29512] };
    }
    internal sealed record Family(CombatRequest Kind, Preset[] Presets, Feature[] Features, uint[] Initiators);
    internal static readonly Dictionary<uint, Family[]> Jobs = new()
    {
        [19] = [new(CombatRequest.Offense, [Preset.PLDPvP_Imperator], [], [41431]), new(CombatRequest.Defense, [Preset.PLDPvP_Sheltron, Preset.PLDPvP_HolySpirit, Preset.PLDPvP_Rampart], [], [29067, 29062, 43244]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [21] = [new(CombatRequest.Offense, [Preset.WARPvP_BurstMode_Onslaught, Preset.WARPvP_BurstMode_Orogeny], [Feature.PvPWarRendInitiation], [29084, 29080, 29079]), new(CombatRequest.Defense, [Preset.WARPvP_Rampart], [Feature.PvPWarBloodwhettingInitiation], [29082, 43244]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [32] = [new(CombatRequest.Offense, [Preset.DRKPvP_Shadowbringer], [], [29091]), new(CombatRequest.Defense, [Preset.DRKPvP_BlackestNight, Preset.DRKPvP_SaltedEarth, Preset.DRKPvP_Impalement, Preset.DRKPvP_Rampart], [], [41438, 29094, 29093, 43244]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [37] = [new(CombatRequest.Offense, [Preset.GNBPvP_RoughDivide, Preset.GNBPvP_FatedCircle, Preset.GNBPvP_BlastingZone], [Feature.PvPGnbGnashingInitiation], [29102, 41511, 29128, 29123]), new(CombatRequest.Defense, [Preset.GNBPvP_Corundum, Preset.GNBPvP_Rampart], [], [41443, 43244]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [24] = [new(CombatRequest.Offense, [Preset.WHMPvP_Seraph_Strike, Preset.WHMPvP_Afflatus_Misery, Preset.WHMPvP_Diabrosis], [], [29226, 29229, 43257]), new(CombatRequest.Defense, [], [], []), new(CombatRequest.Support, [Preset.WHMPvP_Burst_Heals], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [28] = [new(CombatRequest.Offense, [Preset.SCHPvP_ChainStratagem, Preset.SCHPvP_Diabrosis], [], [29716, 43257]), new(CombatRequest.Defense, [], [], []), new(CombatRequest.Support, [Preset.SCHPvP_Adlo, Preset.SCHPvP_Expedient], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [33] = [new(CombatRequest.Offense, [Preset.ASTPvP_Burst_Macrocosmos, Preset.ASTPvP_Diabrosis], [Feature.PvPAstGravityInitiation], [29244, 43257]), new(CombatRequest.Defense, [], [], []), new(CombatRequest.Support, [Preset.ASTPvP_Burst_Heal], [], []), new(CombatRequest.Mechanic, [Preset.ASTPvP_Burst_DrawCard], [], [41503])],
        [40] = [new(CombatRequest.Offense, [Preset.SGEPvP_BurstMode_Phlegma, Preset.SGEPvP_Diabrosis], [], [29259, 43257]), new(CombatRequest.Defense, [], [], []), new(CombatRequest.Support, [Preset.SGEPvP_BurstMode_Pneuma, Preset.SGEPvP_BurstMode_KardiaReminder], [], []), new(CombatRequest.Mechanic, [Preset.SGEPvP_BurstMode_Eukrasia], [], [29258])],
        [20] = [new(CombatRequest.Offense, [Preset.MNKPvP_Burst_RisingPhoenix, Preset.MNKPvP_Smite], [], [29481, 43248]), new(CombatRequest.Defense, [Preset.MNKPvP_Burst_RiddleOfEarth], [], []), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [22] = [new(CombatRequest.Offense, [Preset.DRGPvP_HighJump, Preset.DRGPvP_Smite], [Feature.PvPDrgGeirskogulInitiation], [29491, 29493, 43248]), new(CombatRequest.Defense, [Preset.DRGPvP_HorridRoar, Preset.DRGPvP_ChaoticSpringSustain], [], [29496, 29490]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [30] = [new(CombatRequest.Offense, [Preset.NINPvP_ST_Bunshin, Preset.NINPvP_AoE_Bunshin, Preset.NINPvP_ST_Dokumori, Preset.NINPvP_AoE_Dokumori, Preset.NINPvP_Smite], [], [29511, 41451, 43248]), new(CombatRequest.Defense, [Preset.NINPvP_ST_Meisui, Preset.NINPvP_AoE_Meisui], [], [29507, 29510, 29512]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [Preset.NINPvP_ST_ThreeMudra, Preset.NINPvP_AoE_ThreeMudra], [], [29507])],
        [34] = [new(CombatRequest.Offense, [Preset.SAMPvP_Meikyo, Preset.SAMPvP_Smite], [Feature.PvPSamOgiInitiation], [29530, 29536, 43248]), new(CombatRequest.Defense, [Preset.SAMPvP_Chiten], [], [29533]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [39] = [new(CombatRequest.Offense, [Preset.RPRPvP_Smite], [Feature.PvPRprPlentifulInitiation, Feature.PvPRprWarrantInitiation], [29546, 29549, 43248]), new(CombatRequest.Defense, [Preset.RPRPvP_Burst_ArcaneCircle], [], [29552]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [41] = [new(CombatRequest.Offense, [Preset.VPRPvP_Bloodcoil, Preset.VPRPvP_UncoiledFury, Preset.VPRPvP_Smite], [], [39166, 39168, 43248]), new(CombatRequest.Defense, [], [], [39185]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [Preset.VPRPvP_RattlingCoil], [], [39189])],
        [23] = [new(CombatRequest.Offense, [Preset.BRDPvP_HarmonicArrow, Preset.BRDPvP_Eagle], [], [41464, 43251]), new(CombatRequest.Defense, [Preset.BRDPvP_Wardens], [], [29400]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [Preset.BRDPvP_ApexArrow], [], [29393])],
        [31] = [new(CombatRequest.Offense, [Preset.MCHPvP_BurstMode_FullMetalField, Preset.MCHPvP_BurstMode_Wildfire, Preset.MCHPvP_Eagle], [], [41469, 29409, 43251]), new(CombatRequest.Defense, [], [], []), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [Preset.MCHPvP_BurstMode_Analysis, Preset.MCHPvP_BurstMode_Drill, Preset.MCHPvP_BurstMode_BioBlaster, Preset.MCHPvP_BurstMode_AirAnchor, Preset.MCHPvP_BurstMode_ChainSaw], [], [29405, 29414, 29406, 29407, 29408])],
        [38] = [new(CombatRequest.Offense, [Preset.DNCPvP_Eagle], [Feature.PvPDncStarfallInitiation], [29421, 43251]), new(CombatRequest.Defense, [], [Feature.PvPDncFanInitiation], [29428]), new(CombatRequest.Support, [Preset.DNCPvP_BurstMode_CuringWaltz, Preset.DNCPvP_BurstMode_Partner], [], [29429, 29431]), new(CombatRequest.Mechanic, [], [Feature.PvPDncHoningInitiation], [29422])],
        [25] = [new(CombatRequest.Offense, [Preset.BLMPvP_PhantomDart], [Feature.PvPBlmOffensiveBurstInitiation, Feature.PvPBlmOffensiveWeaveInitiation, Feature.PvPBlmOffensiveXenoInitiation], [29657, 29658, 41475, 43291]), new(CombatRequest.Defense, [], [Feature.PvPBlmDefensiveBurstInitiation, Feature.PvPBlmDefensiveWeaveInitiation, Feature.PvPBlmDefensiveXenoInitiation], [29657, 29658, 41475]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [27] = [new(CombatRequest.Offense, [Preset.SMNPvP_BurstMode_CrimsonCyclone, Preset.SMNPvP_BurstMode_MountainBuster, Preset.SMNPvP_BurstMode_Slipstream, Preset.SMNPvP_BurstMode_Necrotize, Preset.SMNPvP_PhantomDart], [], [29669, 41483, 29667, 29671, 43291]), new(CombatRequest.Defense, [Preset.SMNPvP_BurstMode_RadiantAegis], [], [29670]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [35] = [new(CombatRequest.Offense, [Preset.RDMPvP_PhantomDart], [Feature.PvPRdmEmboldenInitiation, Feature.PvPRdmRiposteInitiation], [41488, 41494, 43291]), new(CombatRequest.Defense, [], [Feature.PvPRdmForteInitiation], [41496]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [], [], [])],
        [42] = [new(CombatRequest.Offense, [Preset.PCTPvP_HolyInWhite, Preset.PCTPvP_PhantomDart], [], [39198, 43291]), new(CombatRequest.Defense, [], [Feature.PvPPctCoatInitiation], [39211]), new(CombatRequest.Support, [], [], []), new(CombatRequest.Mechanic, [Preset.PCTPvP_CreatureMotif], [], [39204])],
    };
}
