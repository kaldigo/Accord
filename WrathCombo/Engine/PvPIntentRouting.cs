using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using WrathCombo.Combos.PvE;
using WrathCombo.Core;
using WrathCombo.Native;
using WrathCombo.Extensions;
using WrathFork.Contract;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;

namespace WrathCombo.Engine;

internal static class PvPIntentRouting
{
    internal const uint Offense=1_000_010, Defense=1_000_011, Support=1_000_012, Mechanic=1_000_013;
    internal const uint Release=1_000_014;
    private static readonly PvPRequestState Requests=new();
    private static uint evaluating;
    private static (uint Job,ulong Character,ulong Target,long At) healingRecipient;
    internal static bool Enabled => InPvP() && Options.Enabled(Feature.PvPIntentRoutes);
    internal static bool Active => Enabled && evaluating!=0;
    internal static bool Area => Active && evaluating==All.AoEDPS;
    internal static uint ReleaseAction(uint input)
    {
        if(input!=Release || !Enabled)return 0;
        uint source=Options.Job switch {31=>29409,38=>29422,39=>29549,_=>0};
        if(source==0)return All.Cease;
        uint result=OriginalHook(source);
        return result!=source ? result:All.Cease;
    }
    internal static bool Requested(CombatRequest kind) => Active &&
        (Kind(evaluating)==kind || Requests.Active(kind,Environment.TickCount64));
    private static CombatRequest? Kind(uint action) => action is >=Offense and <=Mechanic ? (CombatRequest)(action-Offense) : null;
    private static PvPIntentPolicy.Family[] Families => PvPIntentPolicy.Jobs.GetValueOrDefault(Options.Job) ?? [];
    internal static void Reset() { Requests.Reset(); evaluating=0; healingRecipient=default; }
    internal static void Tick() => CheckContext();
    internal static void ConfirmRecipient(uint action,ulong target)
    {
        if(Enabled && action==29243)healingRecipient=(Options.Job,Svc.PlayerState.ContentId,target,Environment.TickCount64);
    }
    internal static Dalamud.Game.ClientState.Objects.Types.IGameObject? HealingRecipient =>
        healingRecipient.Job==Options.Job && healingRecipient.Character==Svc.PlayerState.ContentId &&
        Environment.TickCount64-healingRecipient.At is >=0 and <15000
            ? Svc.Objects.FirstOrDefault(o=>o.GameObjectId==healingRecipient.Target):null;
    private static void CheckContext() => Requests.Context(Options.Job,Svc.PlayerState.ContentId,
        Enabled && Player.Available && !Player.IsDead && Svc.ClientState.IsLoggedIn);
    internal static IDisposable Enter(uint action)
    {
        CheckContext();
        var previous=evaluating;
        evaluating=Enabled && (action is All.SingleTargetDPS or All.AoEDPS || Kind(action)!=null) ? action:0;
        return new Scope(()=>evaluating=previous);
    }
    private sealed class Scope(Action exit):IDisposable { public void Dispose()=>exit(); }
    internal static bool? Matches(uint action,CustomActionType type)
    {
        if(!Active || action!=evaluating)return null;
        return type==(Options.Job==30 && Area ? CustomActionType.AoEDPS:CustomActionType.SingleTargetDPS);
    }
    private static bool? Allows(PvPIntentPolicy.Family? family)
    {
        if(!Active || family is null)return null;
        if(Kind(evaluating) is {} requested)return family.Kind==requested;
        // Cards, dances, Eukrasia, coils and paint already expose their active
        // results. Do not turn their next initiation back on after a manual start.
        if(family.Kind==CombatRequest.Mechanic && Options.Job!=31)return null;
        if(family.Kind==CombatRequest.Offense && Options.Job is not (21 or 37 or 24 or 33 or 22 or 30 or 34 or 39 or 31 or 25 or 27 or 35))return null;
        return Requests.Active(family.Kind,Environment.TickCount64) ? true:null;
    }
    internal static bool? PresetOverride(Preset preset) => !Active ? null : Allows(Families.FirstOrDefault(f=>f.Presets.Contains(preset)));
    internal static bool? FeatureOverride(Feature feature) => feature==Feature.PvPIntentRoutes || !Active ? null :
        Allows(Families.FirstOrDefault(f=>f.Features.Contains(feature)));
    internal static uint Filter(uint action)
    {
        if(!Active || Kind(evaluating) is not {} requested)return action;
        if(action==All.Cease)return action;
        var family=Families.First(f=>f.Kind==requested);
        // Do not turn a manual selector into a fallback damage button or another intent.
        if(family.Initiators.Any(a=>a==action || OriginalHook(a)==action))return SelfTarget(action,requested);
        // An explicit press can choose an otherwise reserved action. Readiness,
        // native transformation and target validity still apply; saved automatic
        // HP/timing policies are never rewritten by this manual choice.
        foreach(var source in family.Initiators)
        {
            if(Options.Job==25 && source==41475)
            {
                bool fire=LocalPlayer.HasStatus(Combos.PvP.BLMPvP.Buffs.AstralFire1) || LocalPlayer.HasStatus(Combos.PvP.BLMPvP.Buffs.AstralFire2) || LocalPlayer.HasStatus(Combos.PvP.BLMPvP.Buffs.AstralFire3);
                bool ice=LocalPlayer.HasStatus(Combos.PvP.BLMPvP.Buffs.UmbralIce1) || LocalPlayer.HasStatus(Combos.PvP.BLMPvP.Buffs.UmbralIce2) || LocalPlayer.HasStatus(Combos.PvP.BLMPvP.Buffs.UmbralIce3);
                if(requested==CombatRequest.Offense ? !fire:!ice)continue;
            }
            uint candidate=OriginalHook(source);
            bool self=requested==CombatRequest.Defense && candidate is 29093 or 41443 or 29670 or 29400;
            var target=self?LocalPlayer:CurrentTarget;
            if(ActionReadyForTarget(candidate,target?.GameObjectId ?? 0xE0000000) && InActionRange(candidate,target))
                return SelfTarget(candidate,requested);
        }
        return All.Cease;
    }
    private static uint SelfTarget(uint action,CombatRequest kind) => kind==CombatRequest.Defense && action is 29093 or 41443 or 29670 or 29400
        ? action.Retarget(evaluating,LocalPlayer):action;
    internal static void Observe(uint original,uint replacement)
    {
        CheckContext();
        if(Enabled && Kind(original) is {} kind)Requests.Observe(kind,replacement,Environment.TickCount64);
    }
    internal static void Confirm(uint action)
    {
        CheckContext();
        if(!Enabled)return;
        Requests.Confirm(action,Environment.TickCount64);
        if(Options.Job==30 && action is 29508 or 29512)Requests.Complete(CombatRequest.Defense);
        if(Options.Job==31 && action==29408)Requests.Complete(CombatRequest.Mechanic);
        if(Options.Job==33 && action==29253)Requests.Complete(CombatRequest.Offense);
        if(Options.Job==42 && action is 39209 or 39782)Requests.Complete(CombatRequest.Mechanic);
    }
}
