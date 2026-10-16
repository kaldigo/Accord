# Resolved source audit

Product decision update, 2026-10-08: the user has now agreed four variants (burst, protection, support, class mechanics) and clarified that manual initiation signals intent while the remaining sequence runs through the appropriate automation button. These decisions supersede recommendations below to defer variant selection or choose between multi-press burst and handoff. The source findings remain valid; use [the current requirements](../ACCORD.md#agreed-four-variants) for implementation.

2026-10-08. This supersedes the speculative coverage questions and implementation priorities in [intent-proposal.md](intent-proposal.md). Scope: the gaps and verification requests raised there, using the pinned local engine plus the public setup pack. No game configuration or engine code was changed. Findings distinguish existing behavior, configuration/layout limitations, new product requirements and observed symptoms requiring live evidence.

## Conclusions that change the proposal

1. **Phase continuation is generally already implemented.** MNK, DRG, NIN, SAM, RPR and VPR use active statuses/gauges and independently enabled follow-up presets. Their baseline enables these. A separate phase button expresses manual initiation; its existence does not show an integration defect.
2. **DNC already separates start from completion through a detailed option.** The current pack chooses broader preset switching. Change the setup mapping before proposing an engine change.
3. **Several support features proposed for investigation are already enabled:** BRD party cleanse on ST/AoE, SMN retargeted Rekindle on ST/AoE, SCH self-shield/spread through Soil, and SGE manual Kardia retargeting through the heal stack. Their scope/recipient rules still matter, but these are not absent features.
4. **A burst selector is not necessarily a single initiation action.** BRD, DRK, GNB and RDM can need repeated presses to issue multiple buffs/resources. Their damage buttons do not automatically issue every disabled burst action after the first manual buff. If Accord promises one accepted initiation followed by damage-only presses, these are concrete integration requirements. They are not failures of the present multi-press controls.
5. **There are genuine narrower limitations:** AST utility cards have no advanced damage route; PLD Cover has no route in the reviewed layout; MCH's advanced Dismantle condition cannot initiate it from an absent debuff at the saved threshold; some manual protection controls are priority chains rather than direct access to a chosen defense.

Recommendation: retain the current variant categories until these behavior boundaries are agreed. The proposed three-category replacement was premature: it needs an explicit migration for existing baseline support and independent phase timing. No broad new Auto support layer is needed to recreate already enabled support.

## What was traced

For the entries below, inspection connected the invoking action, source selector, enabled baseline/variant presets, relevant detailed settings, phase/readiness conditions, recipient and returned action. Follow-up helper functions were read where the selector delegated the decision. The [runtime inventory](runtime-inventory.json) identifies the exact public baseline and variant deltas; it is not the user's effective live configuration.

“Covered” below means the source route and its configuration exist. This closes the earlier question about missing implementation. In-game regression testing is still appropriate after an actual change, but is not a reason to keep an answered source question open. Numerical optimization and all unrelated branches of these jobs are outside this audit.

## Tanks

### PLD

**Manual burst continuation is covered.** `TryOGCDAttacks` in [PLD_Helper.cs](../../WrathCombo/Combos/PvE/PLD/PLD_Helper.cs) independently enables Requiescat on ST/AoE and checks Fight or Flight cooldown above 50 seconds for entry; Blade of Honor has its own ready transformation. The GCD path tests Requiescat/Confiteor-ready state and selects the blade chain, with Holy Spirit/Circle handling below the Confiteor level. Those presets are in the baseline. Manually using Fight or Flight provides the state/cooldown conditions; automatic Fight or Flight need not be enabled for continuation.

**Ally protection is covered with a specific resolver.** [PLD.cs](../../WrathCombo/Combos/PvE/PLD/PLD.cs), `PLD_RetargetSheltron`, chooses UI mouseover, hard-selected party ally, then target's target only when the player does not have aggro. It returns Intervention when ready and applicable, otherwise the original self-defense. This is not the healer focus-first stack. That difference is established, not an unresolved question.

**Passage is accessible; Cover is not in the reviewed layout.** Divine Veil's enabled party selector prioritizes Reprisal, then Veil, then Passage if ready. Bulwark's mitigation list includes Clemency, gated by the player's HP and MP; this is not a general selected-ally emergency-heal control. Cover's standalone selector exists but is not enabled or assigned in the public slots. The pack therefore does not provide complete deliberate access to every PLD utility.

**Decision:** retain the present burst and defense routes. Treat direct Cover/ally-Clemency and immediate Passage timing as layout/access decisions if required, not missing automatic damage behavior. Do not claim the current priority chains provide arbitrary access to a chosen defense.

### WAR

**Burst continuation is covered.** [WAR_Helper.cs](../../WrathCombo/Combos/PvE/WAR/WAR_Helper.cs) separates Inner Release initiation flags from status-driven spending, Primal Rend, Wrath and Ruination. The baseline enables the applicable follow-ups on both paths. Higher-level actions are gated by learned action/status, rather than requested on every press.

**Nascent conversion and party protection are covered.** `WAR_RawIntuition_Targeting` in [WAR.cs](../../WrathCombo/Combos/PvE/WAR/WAR.cs) follows UI mouseover, hard party ally, then target's target when not holding aggro; it checks Nascent readiness and buff applicability. Thrill's mitigation selector traverses the configured priority array. Shake It Off is an enabled entry in that list, with party requirement/status checks in the helper. The separate Shake/Reprisal selector is not needed to make Shake reachable from Thrill.

**Decision:** no missing phase/party-protection integration demonstrated. Keep the current intents. A request for immediate Shake independently of earlier available mitigations would be a deliberate-access requirement.

### DRK

**The burst/interrupt priority is resolved.** `DRK_oGCDs` in [DRK.cs](../../WrathCombo/Combos/PvE/DRK/DRK.cs) checks Interject first, then Delirium, then Living Shadow, before the remaining configured actions. The baseline enables those first three features. Several burst presses may therefore be needed; pressing damage after only Delirium does not turn on the disabled Living Shadow initiation flag.

**Delirium continuation is covered.** [DRK_ActionLogic.cs](../../WrathCombo/Combos/PvE/DRK/DRK_ActionLogic.cs) selects enhanced Scarlet/Impalement actions from active Enhanced Delirium stacks, and lower-level Bloodspiller/Quietus from Delirium stacks. Baseline ST/AoE spender presets cover both cases.

**Targeted TBN is covered; Oblation is not equivalent.** TBN uses UI mouseover, hard party target and optional target's target when not holding aggro. The standalone Oblation retarget preset is not enabled in the baseline; the mitigation list's Oblation entry should not be described as granting the same targeted control. Dark Missionary is an enabled mitigation-list action, not missing merely because its own button is absent.

**Decision:** keep the multifunction burst button. If single-initiation burst is required, add narrowly scoped follow-through for the other burst commitment rather than removing the existing control. Keep the interrupt precedence explicit.

### GNB

**The manual sequence is resolved.** `GNB_NM_Features` in [GNB.cs](../../WrathCombo/Combos/PvE/GNB/GNB.cs) offers Bloodfest before falling back to No Mercy when its readiness and target checks pass. The pack enables `GNB_NM_Bloodfest` and sets `GNB_NM_Feature_Weave = 0`, so this substitution occurs in a weave window. With no eligible substitution it returns No Mercy. This is a multi-press preparation control, not a guaranteed one-press activation of both actions.

**Damage follow-ups are covered.** Advanced ST/AoE have independent cartridge, Reign and Continuation/brand presets in the baseline. [GNB_Helper.cs](../../WrathCombo/Combos/PvE/GNB/GNB_Helper.cs) checks readiness, range, recent No Mercy and expiry/overcap conditions. Automatic No Mercy/Bloodfest initiation is separately enabled by the burst variant.

**Protection is covered within its selector limits.** Heart of Stone/Corundum uses UI mouseover, hard party ally, then target's target when not holding aggro. Camouflage's priority selector includes Aurora and Heart of Light, both enabled. It does not give unconditional immediate access to either action, nor does the baseline enable the standalone Aurora retarget feature.

**Decision:** keep the current roles. Consolidating further is not necessary to complete cartridge sequences. A one-initiation burst contract needs deliberate treatment of Bloodfest/No Mercy ordering.

## Healers

### WHM

**Emergency healing is present.** `WHM_ST_Heals` in [WHM.cs](../../WrathCombo/Combos/PvE/WHM/WHM.cs) resolves `OneButtonHealLogic`, runs the configured priority list and retargets the selected heal. [WHM_Helper.cs](../../WrathCombo/Combos/PvE/WHM/WHM_Helper.cs) identifies Benediction as the first configurable heal entry; the public priority array ranks it first, threshold 20%, with no weave requirement. Earlier special-content/raidwide/cleanse/Lucid checks still exist, so this is priority-based emergency healing rather than an unconditional raw Benediction button.

**Decision:** close the generic missing-emergency-heal question. There is no reported WHM enemy-target symptom to investigate. The source uses the resolved ally for HP checks; any change to shared readiness/retargeting needs a WHM regression check, not a speculative WHM defect claim. Existing prevention access is a policy/layout choice, not grounds to add all healing to damage.

### SCH

**The shield-spread chain already exists and is configured.** `SCH_Mit_AoE` in [SCH.cs](../../WrathCombo/Combos/PvE/SCH/SCH.cs) on Sacred Soil prioritizes Soil if ready, then configured Fey Illumination, then Recitation when Deployment is ready, self-targeted Adloquium while Recitation is active, and self-targeted Deployment when Catalyze is present. The pack's `SCH_Mit_AoEOptions = [true,true,false,true]` enables the fairy and spread parts, excludes Expedient from this chain, and allows Seraph later. Succor fallback and Consolation also appear later in the selector.

This is **self-centered spread after earlier available actions**, not a free choice of shield recipient or immediate shield-preparation operation. The standalone Deployment/Recitation selectors exist, but their absence from the configured layout does not mean the Soil chain is missing.

**Pet availability is already checked.** The mitigation chain requires a present, non-busy fairy for Illumination/Seraph. Healing paths and Dissipation retain their own resource/pet conditions. Normal fairy preparation is enabled on both damage routes; the mechanics variant adds Dissipation on healing routes only. There is no evidence for moving dismissal into damage by default.

**Decision:** retain the implemented sequence. Add a distinct preparation control only if immediate or chosen-ally spread is a required player intent. That would be an access enhancement, not completion of an absent chain.

### AST

**The requested damage-route support is a real new route.** Advanced ST/AoE contains offensive card handling, not Bole/Arrow/Ewer/Spire selection. Their advanced selector remains ST healing. There is no setting found that simply enables those four utility cards in the advanced damage route.

**The target symptom is narrowed, not solved by guessing.** The ST-heal path passes its resolved ally into `GetMatchingConfigST`, uses that ally's HP and then calls readiness. Bole/Arrow have enabled tank restrictions and 70% thresholds in the pack; Ewer is 60%, Spire 70%; these card weave requirements are false. The priority array also allows higher-ranked eligible heals to win. Shared focus-first targeting can select a full-HP ally, and the custom shared stack has to be applied independently of the job presets.

`ActionReady` calls native status without an explicit resolved-ally argument and accepts statuses 0/582/580. Retarget registration happens after selection. No source evidence obtained establishes which status the native API returns in the user's failing case, so a readiness defect is not confirmed. The offensive-card quick-target selector concerns Play I/Balance/Spear, not proof of a utility-card hardtarget restriction.

**Decision:** implement no speculative global readiness fix. The support-routing requirement stands; first define useful card conditions separately from healing HP thresholds. The exact observed target-dependent failure needs a narrow paired capture: same card/ally/HP, resolved recipient, priority winner, native status and retarget registration with enemy versus ally selected. See [investigation](investigations.md).

### SGE

**Manual Kardia already follows the heal stack.** The baseline enables `SGE_Retarget` and `SGE_Retarget_Kardia`. `SGE_Retarget` in [SGE.cs](../../WrathCombo/Combos/PvE/SGE/SGE.cs) registers Kardia against `HealStack`; [SGE_Helper.cs](../../WrathCombo/Combos/PvE/SGE/SGE_Helper.cs) defines that as `SimpleTarget.Stack.AllyToHeal`. Therefore the existing manual Kardia button already uses the configured focus-first resolver.

**The automatic exception is narrower.** `UseKardia` uses a different `Target`: UI mouseover usable/in range, hard target usable/in range, then any tank. It only acts when the caster lacks Kardia. The relevant automatic ST damage/heal options are not enabled in the baseline. This is not an automatic reassignment currently happening on every damage press, and not a broken manual button.

**Other proposed support is already routed where appropriate.** Rhizomata is enabled on damage and healing paths. Zoe/Pneuma has its standalone selector enabled for deliberate use. ST healing resolves its recipient before selecting configured heals/shields.

**Decision:** retain manual Kardia and current replenishment. Only if automatic initial assignment is wanted do we need a targeted change to its resolver; do not describe this as necessary to make the present manual setup work.

## Melee jobs

### MNK

**Covered:** advanced ST/AoE independently gate Perfect Balance initiation, but always call `DoPerfectBalanceCombo` later. [MNK_Helper.cs](../../WrathCombo/Combos/PvE/MNK/MNK_Helper.cs) first requires active Perfect Balance, then selects the appropriate chakra-building actions. Masterful Blitz has separately enabled ST/AoE presets; the helper checks learned action, transformed Blitz and range. Manual Perfect Balance therefore has an existing damage-button continuation path.

**Preparation covered:** both damage routes enable Meditation and Form Shift. Helpers restrict Form Shift to out of combat with no relevant form/Perfect Balance state, and Meditation to low Chakra with no usable nearby damage target/out of combat and other burst-state protections.

**Decision:** retain the independent manual phase intent unless the user wants to delegate its timing. Removing the button does not repair a continuation gap. Brotherhood's selector can expose Riddle of Fire when the first buff is unavailable, so “manual burst” should not imply one press supplies every buff.

### DRG

**Covered:** ST/AoE separately enable Geirskogul initiation and Nastrond/Stardiver/follow-ups. [DRG_Helper.cs](../../WrathCombo/Combos/PvE/DRG/DRG_Helper.cs) requires Life/ready statuses and range/readiness for those follow-ups; it does not require the auto-Geirskogul option to have initiated that state. The baseline includes them.

**Manual burst is chained:** `DRG_BurstCDFeature` in [DRG.cs](../../WrathCombo/Combos/PvE/DRG/DRG.cs) returns Battle Litany from Lance Charge when Lance Charge is on cooldown and Litany is ready. A second burst press, rather than the first press alone, supplies that action when its damage-route initiation is off.

**Decision:** keep Geirskogul if manual Life timing is retained. Merging it with buffs is a new interface policy, not an engine-gap fix. Approach and backward jump continue to serve different intents.

### NIN

**Covered:** advanced ST/AoE calls `STTenChiJin`/`AoETenChiJin` before ordinary choices when the corresponding `TenChiJin_Auto` boolean is true; both are true in the pack. Helpers check active TCJ and progress through mudra state. Initiation uses separate presets. Manual Kassatsu is consumed by enabled Hyosho/Goka/ninjutsu routes using Kassatsu status; the initiation preset is not required for that follow-through. Mudra protection is also enabled.

**Manual burst is chained:** Hide's selector uses the saved Mug/Trick choices and Shadow Walker/Hidden state. With `TrickAfterMug = true`, it prioritizes Mug in combat before the eligible Trick action on a later press. TCJ's own selector additionally exposes Meisui after the TCJ sequence; disabling automatic Meisui does not make it magically fire from damage.

**Decision:** no new TCJ continuation implementation. Keep prepull Hide/Kassatsu functions. Removing the TCJ button requires explicitly delegating its initiation and Meisui or preserving their manual use elsewhere.

### SAM

**Covered:** advanced ST/AoE ends by checking active Meikyo and calling `DoMeikyoCombo` independently of the auto-Meikyo initiation preset. Iaijutsu, Tsubame, Ogi and related enabled follow-ups use their own resource/status conditions. Ikishoten can remain the manually used native action; damage follow-up presets are already present.

**Movement resolved:** `SAM_GyotenYaten` in [SAM.cs](../../WrathCombo/Combos/PvE/SAM/SAM.cs) changes Gyoten to Yaten in melee range when Kenki is at least 10; outside melee it remains Gyoten. This is deterministic range-based approach/disengage, not an unexplained random movement decision.

**Decision:** no continuation gap. Keep Meditate for its explicit downtime channel. Combining Meikyo initiation into a burst request is optional interface work and must not remove ordinary cadence control by accident.

### RPR

**Covered:** advanced ST/AoE calls `UseEnshroudWeaves` and `UseEnshroudComboGCD` with separately enabled Sacrificium/Lemure/Reaping/Communio options. [RPR_Helper.cs](../../WrathCombo/Combos/PvE/RPR/RPR_Helper.cs) starts those helpers with an active-Enshrouded check and uses Lemure/Void state and learned actions. The auto-Enshroud initiation flag does not gate the continuation helpers. Perfectio has its own readiness path.

**Decision:** manual Enshroud already hands off to damage. Retain or delegate that initiation according to desired control; do not add continuation code. Soulsow/Harvest Moon and movement return retain distinct uses.

### VPR

**Covered:** automatic Reawaken initiation is separate from `GenerationCombo`/`ReawakenCombo` and Legacy flags, all required continuation flags being enabled in the pack. [VPR_Helper.cs](../../WrathCombo/Combos/PvE/VPR/VPR_Helper.cs), `UseCombo`, selects the Reawaken combo from active Reawakened status. ST/AoE weave helpers similarly use phase state.

**Ranged spending already exists:** `UseUncoiledFury` explicitly returns true on ST when ready/in range, with coils available, outside melee and with a battle target. With no coils, the ST ranged-uptime branch can return Writhing Snap. Thus a separate ranged-spender button is not needed to solve the claimed out-of-range access question.

**Decision:** keep deliberate Reawaken timing or delegate it; no missing continuation/ranged route found. AoE does not use exactly the same ST out-of-melee override, so do not generalize this into a promise that every AoE press is a ranged filler.

## Physical ranged jobs

### BRD

**Party cleanse is already enabled on both routes.** `TryOGCDAttacks` in [BRD_Helper.cs](../../WrathCombo/Combos/PvE/BRD/BRD_Helper.cs) checks Paean readiness and weaving, handles the player's cleanse need first, then uses `WardenResolver` when the route's automatic party option is true. Both ST/AoE presets and both `Wardens_Auto` booleans are enabled. The resolver selects the first living, in-range, cleansable other party member in party-list order. This is a reactive party-list cleanse, not the focus-first healer stack or proactive debuff prevention.

**Manual burst is a repeated-press sequence.** `BRD_Buffs` in [BRD.cs](../../WrathCombo/Combos/PvE/BRD/BRD.cs) returns ready Raging Strikes, Battle Voice, Radiant Finale, then Barrage. When the damage-route buff presets are disabled, one initial press does not enable the remaining buffs there. Resonant/Encore and routine song/proc follow-ups have their separate baseline routes.

**Decision:** withdraw the proposed new party-cleanse integration. Preserve Peloton and manual protection. Proactive Paean on a chosen ally would need explicit access; it is not provided by the reactive damage selector. If Auto support is introduced, migrate this already-automatic behavior explicitly rather than silently switching it off in M.

### MCH

**Manual burst ordering is known.** `MCH_HeatblastGaussRicochet` in [MCH.cs](../../WrathCombo/Combos/PvE/MCH/MCH.cs) returns Barrel first when eligible, then Wildfire after recent Hypercharge, otherwise Hypercharge when not overheated, then the overheat attack. Those first three manual features are enabled. With automatic Wildfire off, returning to damage after just the first manual press is not equivalent to completing this burst sequence. ST/AoE already handle overheat damage; Wildfire is absent from the advanced AoE route.

**Dismantle has a concrete advanced-path defect.** Its advanced ST condition requires existing Dismantled remaining time **greater than** `MCH_DismantledDuration`. The public setting is zero. With no debuff, `RemainingTimeOrZero()` is zero, so `0 > 0` is false. This route cannot initiate Dismantle from that state. Simple mode uses a different absence/applicability check. The advanced Dismantle preset is not enabled by the reviewed baseline/variants, and the manual Dismantle/Tactician selector remains available; therefore this defect is not evidence for the reported damage issue.

**Damage diagnosis remains observational.** The tool, Wildfire/Hypercharge and Queen holds described in [investigations.md](investigations.md) are real conditions. The public pack enables burst correctly, including the prior Barrel correction. No source trace identifies which condition operated during the user's level-90 unsynced run or establishes an abnormal DPS result relative to BLM.

**Decision:** preserve the manual burst route; treat one-initiation handoff and AoE Wildfire as specific optional requirements. Correct Dismantle before relying on that automatic defense. Do not change resource policy to explain an unmeasured damage comparison.

### DNC

**Already solved in upstream configuration.** Keep `DNC_ST_Adv_TS`/`DNC_AoE_Adv_TS` enabled and set the respective `IncludeTS` to No for manual start with automatic steps/finish. Initiation checks IncludeTS plus learned/availability and timing conditions; completion checks active Technical state and completed steps without IncludeTS. The settings UI explicitly describes this mode.

**Current pack explains the different control contract.** It stores IncludeTS = Yes and enables/disables the wider presets with Auto burst, leaving the standalone Technical selector for manual steps/finish. Neither configuration implies uncontrolled restarts. The user's experience reports no such failure.

**Decision:** use the existing manual-start option if that is Accord's chosen behavior. The remaining engineering choice is how a global feature-only variant owns that initiation permission: a narrow Accord gate or an explicitly owned initiation option, without overwriting unrelated user settings. No new dance-completion algorithm is justified.

## Magical ranged jobs

### BLM

**Preparation and return coverage resolved.** `BLM_UmbralSoul` in [BLM.cs](../../WrathCombo/Combos/PvE/BLM/BLM.cs) changes Transpose to Umbral Soul when in ice and the action is learned. The enabled Aetherial Manipulation feature returns Between the Lines when it is ready, Ley Lines exists, the player is outside Circle of Power and stationary; otherwise it returns the original ally-movement action. These are explicit conditions, not an untraced return function.

**Decision:** keep both utility controls. Ley Lines remains useful for its location/relocation function under the existing layout. No new prep or return integration required by the earlier questions.

### SMN

**Rekindle is already automated and retargeted on ST/AoE.** [SMN_Helper.cs](../../WrathCombo/Combos/PvE/SMN/SMN_Helper.cs) requires a Phoenix demi, enabled demi attacks and the existing Searing alignment/remaining-time conditions. When Astral Flow is ready and Rekindle is enabled it uses target's party-member target, then a tank missing HP, lowest-HP ally missing HP, then self. Both route and retarget presets are enabled in the public pack.

**Manual targeting differs intentionally in source.** `SMN_Rekindle` in [SMN.cs](../../WrathCombo/Combos/PvE/SMN/SMN.cs) only retargets Astral Flow when its actual transformed action is Rekindle, prioritizing the hard-selected friendly party ally before target's target and fallbacks. Other phase forms retain their native behavior. This prevents that feature from treating an Ifrit movement action as an allied heal. Both advanced damage routes also return Carbuncle when `NeedToSummon` applies.

**Decision:** withdraw the proposed missing Rekindle integration. Retain Astral Flow for deliberate phase-dependent use; consider resolver consistency only as a specific product change. Existing automatic support must be accounted for if a new support variant is introduced.

### RDM

**Manual burst ordering resolved.** `RDM_EmboldenProtection` in [RDM.cs](../../WrathCombo/Combos/PvE/RDM/RDM.cs) offers Vice of Thorns first when available, then Manafication if enabled/ready and Embolden is already on cooldown or active, otherwise Embolden subject to its protection check. The pack enables that selector and Manafication option, while automatic damage-route burst initiation is separately variant-owned. Repeated burst presses may be required; ordinary melee and finisher handling is already enabled in ST/AoE.

**Movement resolved:** the enabled Displacement selector returns Corps-a-corps when a target exists at distance at least five and is within Corps range; otherwise it stays Displacement. The baseline damage presets enable Engagement and do not enable the automatic Corps movement choices. Vercure and the raise chain retain explicit controls.

**Decision:** no missing finisher/movement implementation shown. A single-initiation burst promise would need optional handoff for remaining burst actions, rather than assuming the manual selector is recursively executed through damage.

### PCT

**Preparation is already covered on both damage paths.** `TryDrawMotif` in [PCT_Helper.cs](../../WrathCombo/Combos/PvE/PCT/PCT_Helper.cs) checks separate motif-ready/learned states and enabled creature/weapon/landscape branches. The baseline enables prepull, no-target, motif and individual motif options on ST/AoE. It explicitly allows out-of-combat painting and in-combat no-target painting, as well as its cooldown/HP-based combat preparation.

**Tempera conversion is already in the delegated ST defense route.** The helper's protection block checks incoming group damage, weaving and recent Addle; it returns Coat when available, otherwise Grassa when learned, in party, enough allies are in range and Coat's status is active. The reviewed Auto mitigation flag enables this ST route. It is not only “automatic personal Coat”; its existing policy can spread the shield. AoE advanced has no corresponding Tempera enable in that helper.

**Decision:** close the missing-preparation/conversion question. Preserve manual Tempera and Hammer for deliberate use. Decide whether the existing ST conversion conditions are desired before changing protection ownership; do not add a duplicate conversion feature.

## What actually remains

### Source-supported work items

- AST optional utility-card selection during damage, with explicit card usefulness and recipient policy.
- DNC initiation-option ownership in the setup layer, using the already implemented completion path.
- Optional burst handoff for the jobs whose manual selector currently needs repeated presses. This requires a precise product choice: preserve the existing multi-press control, or hand later buff/resource actions to damage after an accepted initiation. Existing phase continuation should be reused.
- MCH advanced Dismantle absent-debuff condition, if that automation is to be enabled.
- Explicit utility/access decisions: PLD Cover and ally-Clemency; chosen-time/chosen-recipient shield spread; proactive Paean; direct defenses versus priority chains. These are layout choices supported by known source behavior, not unknown implementation.
- Targeting-policy decisions for existing non-healer resolvers and automatic SGE initial assignment. Manual SGE Kardia already uses the shared heal stack.

### Only the reported observations need new live evidence

1. **AST:** further investigation found the saved shared targeting setup enabled and established that the native readiness gate does not receive the ally used for eligibility. See the [targeted correction design](investigations.md#further-source-investigation-2026-10-08). Prepare that correction before requesting another reproduction. The exact native status/recipient during the reported event remains unobserved; subsequent live validation should test the correction, not re-establish the symptom.
2. **MCH:** was damage actually lost, and through which missed/late action in the level-90 unsynced fight? Effective configuration and a representative action trace are necessary; a BLM comparison alone cannot answer it.

Do not keep the other source questions open merely because all code changes eventually deserve regression testing. Final design still needs decisions, but the existence and conditional behavior of the routes above is now established from the inspected source/settings.
