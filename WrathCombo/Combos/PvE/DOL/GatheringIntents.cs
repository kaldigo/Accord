using Dalamud.Game.ClientState.Conditions;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using WrathCombo.CustomComboNS;

namespace WrathCombo.Combos.PvE;

internal partial class DOL
{
    // These selectors only change the action offered by a button. They never execute
    // actions or infer GP-spending preferences from an unavailable action.
    internal static uint GatheringIntent(uint action, bool gathering, bool collectable) =>
        collectable ? action switch
        {
            4073 or 272 => 22182, 4087 or 273 => 22186, // Yield / Scour
            241 => 22184, 224 => 22188, // Node yield / Meticulous
            295 => 22183, 294 => 22187, // Chance / Brazen
            25589 => 21205, 25590 => 21206, // Boon chance / Focus
            21203 => 34871, 21204 => 34872, // Boon yield / Priming
            4589 => 22185, 4590 => 22189, // Crystal buff / Scrutiny
            280 => 240, 282 => 815, // Crystal yield / Collect
            _ => action
        } : !gathering ? action switch
        {
            4072 => Prospect, 4086 => Triangulate,
            4081 => TruthOfMountains, 4095 => TruthOfForests,
            235 => LayOfTheLand, 218 => ArborCall,
            237 => LayOfTheLand2, 220 => ArborCall2,
            239 => 303, 222 => 304, // Low-cost node yield / Sneak
            _ => action
        } : action;

    internal static bool LineOut(FishingState state) => state is
        FishingState.CastingOut or FishingState.LineInWater or FishingState.Bite or
        FishingState.Hooking or FishingState.AmbitiousLure or FishingState.ModestLure;

    internal static uint FishingIntent(uint action, bool diving, bool lineOut, bool freeMooch) =>
        diving ? action switch
        {
            Cast => Gig, 288 => 7903, // Bait / Fathom
            MoochII => SharkEyeII, 26805 => SharkEye, // Mooch / searching
            37045 => 7911, // Store swimbait / Truth of Oceans
            SurfaceSlap => VeteranTrade, PrizeCatch => NaturesBounty,
            Snagging => Salvage, CastLight => ElectricCurrent,
            FishEyes => VitalSight, Chum => BaitedBreath,
            _ => action
        } : lineOut ? action switch
        {
            Cast => Hook,
            4102 => 4179, 4106 => 4103, // Patience I/II / weak/strong hooksets
            PrizeCatch => 269, 37046 => 27523, // Large catch / multi-catch choices
            SurfaceSlap => 37595, 4596 => 37594, // Catch selection / lure choices
            _ => action
        } : action == MoochII && freeMooch ? Mooch : action;

    internal class DOL_ContextTools : CustomCombo
    {
        protected internal override Preset Preset => Preset.DOL_ContextTools;
        protected override unsafe uint Invoke(uint actionID)
        {
            bool gathering = HasCondition(ConditionFlag.Gathering);
            bool collectable = gathering && GenericHelpers.TryGetAddonByName<AtkUnitBase>("GatheringMasterpiece", out var addon) && addon->IsVisible;
            return GatheringIntent(actionID, gathering, collectable);
        }
    }

    internal class FSH_ContextTools : CustomCombo
    {
        protected internal override Preset Preset => Preset.FSH_ContextTools;
        protected override unsafe uint Invoke(uint actionID)
        {
            bool diving = HasCondition(ConditionFlag.Diving);
            var framework = EventFramework.Instance();
            var fishing = framework == null ? null : framework->EventHandlerModule.FishingEventHandler;
            bool lineOut = fishing != null && LineOut(fishing->State);
            // Exact native success only: no paid fallback when free Mooch is usable.
            bool freeMooch = !diving && !lineOut && actionID == MoochII &&
                ActionManager.Instance()->GetActionStatus(ActionType.Action, Mooch) == 0;
            return FishingIntent(actionID, diving, lineOut, freeMooch);
        }
    }
}
