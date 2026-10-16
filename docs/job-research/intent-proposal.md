# Proposed job intents and coverage

**Superseded coverage assessment, 2026-10-08:** read the [resolved source audit](source-audit.md) for conclusions on every job below. The original draft is retained as the record of the questions investigated, not a current implementation backlog. Its “verify” wording and proposed priority order must not be treated as evidence that those features are missing. In particular, phase continuation, BRD party cleanse, SMN retargeted Rekindle, SCH self-spread and manual SGE Kardia targeting already exist. The recommendation to replace the variant categories is withdrawn pending the explicit burst-control and existing-support migration decisions in the audit. DNC needs setup mapping, not a new continuation algorithm.

Draft for review, 2026-10-07. Covers all 21 supported combat jobs. This proposes behavior and layout changes; it does not enable presets, change rotations or replace the agreed requirements in [ACCORD.md](../ACCORD.md). Evidence is the [job research](jobs.md), [public setup inventory](runtime-inventory.json) and linked local source. Patch limitations in [the research index](README.md) still apply.

## Recommended direction

Keep ST and AoE as the controls that carry out damage. A burst control expresses permission to start a burst sequence; subsequent damage presses supply its follow-through. Keep meaningful protection, recovery, movement, preparation and recipient-assignment decisions accessible. A separate gauge is not, by itself, a separate user intent.

Replace the broad **Auto job mechanics** concept with explicit ownership of its effects. Routine rotation maintenance belongs to the damage path. Burst commitments belong to burst delegation. Healing/protective support belongs to support/protection delegation or an explicit healing/protection control. Preparation and tactical movement remain deliberate. This is a proposed migration, not permission to turn every currently optional mechanic on in the baseline.

Recommend three user-facing delegation categories initially:

| Proposed variant | A delegates | M leaves to the user | Boundaries |
| --- | --- | --- | --- |
| Auto burst | Start the reviewed offensive commitment at a suitable opportunity. | Initiate that commitment through the burst control. | Both modes retain ordinary rotation and follow-through. Do not hold every resource for a two-minute button or force a phase when it is unavailable. |
| Auto protection | Use specifically reviewed preventive defenses under their existing or explicitly revised conditions. | Request protection through its control. | Manual protection remains available in A. Emergency invulnerability, knockback timing and deliberate repositioning are excluded by default. |
| Auto support | Apply specifically reviewed routine allied support during damage when its usefulness and recipient are established. | Request that support through healing/support controls or set its recipient explicitly. | Not a generic automatic-healing switch. No blanket GCD healing, resurrection, Rescue, fairy dismissal or persistent-recipient reassignment. |

These labels are provisional. Each effect must have one owner; for example, an AST mitigation card belongs to the card-support policy, not two competing variants. A variant may have no effect on a job. Preserve global choices on job changes and the agreed A/M meaning of enabled/disabled; do not invent a healer burst effect merely to fill the matrix.

Existing baseline offensive automation on healers is an explicit compatibility exception to the manual-burst pattern. Preserve it for now and document it. If manual healer offensive windows are wanted later, design their control and preset migration explicitly rather than silently changing the meaning of M.

## Interaction contract

- **One press requests one game action.** “Start burst” does not execute multiple actions at once. Subsequent user presses complete the sequence. Where starting needs prerequisites, the control can expose the next valid prerequisite; its state must be understandable. Do not introduce an indefinite queued burst request.
- **Initiation and follow-through are separate decisions.** Disabling automatic initiation must not strand a sequence the user already started. Changes should preserve upstream behavior when the corresponding Accord addition is disabled; a reviewed Accord profile may opt into the new follow-through behavior.
- **Damage target and support recipient are separate.** Resolve the ally before recipient-dependent checks, validate readiness for that recipient and execute on that same ally. Keep the selected enemy. A valid focus retains the agreed priority; do not secretly switch to another ally because a different action would then become eligible.
- **A support action needs a usefulness policy.** Healing requires an appropriate need; prevention may use incoming damage/expiry context rather than missing HP. An expiring card is not automatically useful on any target. Do not spend all support simply to empty resources.
- **ST and AoE must be reviewed independently.** Switching between them should continue a compatible active sequence. AoE intent does not authorize wasteful single-target burst against an arbitrary pack member; retain action-specific target and lifetime checks.
- **Manual controls stay when they still express a decision.** Delegation does not make planned defenses, channels, movement, preparation or recovery redundant. A multifunctional button cannot be removed until every function has another verified route.
- **Slots are not assigned here.** Existing logical positions remain the starting point. Each job below states possible changes; final regular/crossbar mappings and shared-slot protection follow after coverage validation. Clear-other-actions must never remove utility simply because a combat guide did not mention it.

## Coverage notation

**Existing** means source/setup evidence supports the route, not that it has been tested in game. **Gap** identifies a concrete missing or coupled route. **Verify** means the necessary path or symptom is not yet established. **Proposed** is a design choice. A retained function may be accessible through an existing selector; it does not necessarily require another button.

## Tanks

All four keep separate ST/AoE, burst initiation, movement, protection, invulnerability, stance, Provoke and Shirk intents. Interrupt/stun access must remain deliberate or reliably accessible through a reviewed selector. Do not remove planned protection when Auto protection is enabled.

### PLD — Paladin

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Fast Blade / Total Eclipse: damage | Maintain combo/procs and the appropriate active magic sequence. | Existing advanced paths. Keep logical slots 1/2. |
| Fight or Flight: burst | Begin the offensive window; damage presses handle eligible continuation. Auto burst delegates initiation. | Existing burst delta; verify manual handoff at lower levels. Slot 3 is a removal candidate only under delegation and complete coverage. |
| Bulwark / Divine Veil / Sheltron: protection | Separate personal, party and resolved-ally protection. Auto protection only delegates reviewed effects. | Existing selectors; verify Sheltron/Intervention recipient decisions. Keep slots 5/6/11. |
| Intervene, Hallowed Ground and tank controls | Movement, emergency immunity and enmity decisions stay deliberate. | Keep current roles. Cover/Clemency/Passage access requires an explicit route audit; do not treat a damage path as permission to heal or channel. |

**Change decision:** target consistency and complete support/utility access first; no new damage engine proposed. Cover and emergency healing are not Auto support defaults. If the current selector hides a required utility behind an unrelated condition, amend that selector or provide access before cleanup removes alternatives.

### WAR — Warrior

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Heavy Swing / Overpower: damage | Maintain effects, generate/spend resources and complete accepted burst follow-ups. | Existing; retain slots 1/2 and established resource policy. |
| Berserk/upgrade: burst | Start the burst phase; damage carries it through. | Existing Auto burst; slot 3 conditional removal candidate. |
| Thrill / Raw Intuition: protection and sustain | Choose self versus resolved-ally protection deliberately; allow reviewed self/party prevention through Auto protection. | Existing selectors; verify Nascent recipient and availability. Keep slots 5/6. |
| Onslaught, Holmgang, tank controls | Movement, invulnerability and enmity stay explicit. | Retain. Verify party protection access, including Shake It Off, before declaring the two defense controls complete. |

**Change decision:** no demonstrated need for another damage/phase button. Do not replace ally protection with a self-heal just because the enemy remains selected. Validate party-defense selection and avoid consuming defenses in unintended combinations.

### DRK — Dark Knight

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Hard Slash / Unleash: damage | Maintain Darkside and resource spending, then complete Delirium/Living Shadow-related follow-through. | Existing shared logic; keep meaningful defensive MP reserve. |
| Carve and Spit selector: burst / interrupt | Make burst intent comprehensible while preserving interrupt access. | Existing multifunctional slot 3 cannot disappear with Auto burst. Verify which action wins when both intents are eligible. |
| Dark Mind / TBN: protection | Personal/party prevention and selected-ally shielding remain distinct uses. | Existing selectors; verify Oblation/TBN retargeting and damage-type gates. |
| Shadowstride, Living Dead, tank controls | Deliberate movement, invulnerability and enmity. | Retain current roles. |

**Change decision:** assess whether interrupt priority makes the burst control ambiguous. If so, give interrupt a stable route before considering burst-slot removal. Auto support does not authorize spending reserved TBN MP on an arbitrary ally.

### GNB — Gunbreaker

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Keen Edge / Demon Slice: damage | Manage cartridges and perform sequence/Continuation follow-ups. | Existing advanced routes; no separate Gnashing Fang control proposed. |
| No Mercy: burst | Start the appropriate offensive preparation/window and return to damage. | Existing Auto burst includes Bloodfest; verify manual prerequisite and continuation ordering. Slot 3 conditional removal candidate. |
| Camouflage / Heart of Stone: protection | Personal mitigation/recovery versus targeted protection; preserve Aurora access. | Existing selectors; verify party mitigation and resolved-ally coverage. |
| Trajectory, Superbolide, tank controls | Deliberate movement, invulnerability and enmity. | Retain. |

**Change decision:** retain current intent structure. Audit targeted and party support rather than adding another cartridge-phase button. Preserve charge/reserve settings unless evidence justifies a reviewed adjustment.

## Healers

Keep ST damage, AoE damage, ST healing/support, party healing, planned mitigation, resurrection and movement/cast protection. ST healing means helping the resolved ally, including appropriate cards/support; it does not mean first selecting that ally manually. Do not automatically add every missing healer utility to every layout: assess existing access and record any intentionally omitted situational function.

### WHM — White Mage

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Stone / Holy: damage | Continue offensive rotation and existing offensive cooldown policy. | Existing; no new burst control. Do not automatically dump lilies to produce damage. |
| Cure / Medica: recovery | Resolve ally/party need and select the appropriate heal/cleanse; preserve emergency access. | Existing advanced healing. Verify emergency heal access and selected-enemy equivalence. |
| Asylum / Bell: planned party support | Placement/prevention and delayed healing/release remain intentional. | Keep slots 5/8. Auto protection owns only reviewed preventive effects; early release is not automatically delegated. |
| Rescue, Swiftcast/raise, Shift, Surecast | Recovery, repositioning and movement remain explicit. | Keep current roles. |

**Change decision:** no immediate Auto support effect recommended without an action-specific gap. Audit ally prevention access, including full-HP eligible allies, before extending damage routes. Existing healing and mitigation intent paths may already be the right home.

### SCH — Scholar

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Ruin / Art of War: damage and normal fairy preparation | Maintain damage/resource policy; ordinary pet preparation stays available. | Existing. No automatic Dissipation added to damage. |
| Physick / Succor: recovery | Appropriate ally/party heal or shield and required follow-up. | Existing. Verify pet-state-dependent actions and target consistency. |
| Sacred Soil: party prevention / prepared shield spread | Distinguish an immediate mitigation request from preparing and deploying a shield. | Existing multifunctional selector; verify Recitation/Deployment chain and recipient before treating it as one complete intent. |
| Dissipation / Expedient: deliberate resource trade / movement | Keep fairy dismissal deliberate by default; Expedient remains usable for movement even with delegated defense. | Keep slots 8/10. Existing conditional healing-route Dissipation is a migration decision, not silently moved into Auto support. |
| Rescue, raise, Surecast; fairy controls | Retain recovery and pet-management access as required. | Normal following remains the baseline. No new automatic pet placement policy. |

**Change decision:** propose a clearly identifiable shield-preparation operation if the current Soil selector cannot express it reliably. Do not add a generic “fairy burst” button. Audit existing fairy support for useful damage-route delegation action by action; reserves and dismissal tradeoffs stay explicit.

### AST — Astrologian

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Malefic / Gravity: damage with delegated support | Keep existing offensive cards/Draw/Star behavior; Auto support may add reviewed utility cards on eligible allies. | **Gap:** utility cards currently use healing routes. New optional integration must cover both ST and AoE. |
| Benefic: help resolved ally | Offer an eligible support card or heal with the enemy still selected. | Existing resolved-ally checks; **Verify:** reported readiness/retarget symptom. No hardtarget-only cause established. |
| Helios / Collective Unconscious | Party recovery versus deliberate prevention. | Retain; coordinate card-support and protection ownership so actions are not independently scheduled twice. |
| Synastry / Macrocosmos, raise, Lightspeed, Surecast | Linked healing, delayed recovery/release, resurrection and casting decisions. | Keep current roles; do not place all of these behind Auto support. |

**Change decision:** first integration candidate. Auto support owns Bole/Arrow/Ewer/Spire policy, including recipient, usefulness, remaining duration and next-Draw interaction. M keeps those cards available through ST support; damage does not independently initiate them. Review preventive HP thresholds separately from the targeting defect investigation. See [the detailed trace](investigations.md#ast-cards-with-an-enemy-selected).

### SGE — Sage

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Dosis / Dyskrasia: damage | Damage continues healing the assigned Kardia recipient; ordinary resource behavior follows reviewed settings. | Existing; assignment is not reselected on each damage press. |
| Diagnosis / Prognosis: recovery and shielding | Select appropriate recovery/prevention on resolved ally or party. | Existing; verify readiness against the intended ally and access before damage lands. |
| Holos / Pneuma: prevention / deliberate recovery | Keep protection and line-heal intent available, including reviewed preparation such as Zoe. | Existing selectors; retain slots 5/8. No unsolicited Pneuma from damage. |
| Kardia: persistent recipient assignment | User explicitly assigns/reassigns the supported ally using consistent targeting rules. | **Gap relative to agreed target policy:** existing automatic resolver does not implement focus override. Keep slot 7. |
| Raise, Icarus, Surecast | Recovery and movement. | Retain. |

**Change decision:** fix or deliberately preserve/document the Kardia resolver exception before offering automatic assignment. Initial Auto support should not silently move Kardia. Review routine resource replenishment separately; unused Addersgall alone is not a reason to heal a full-HP ally.

## Melee damage jobs

All keep separate ST/AoE, burst commitment, movement, party protection and personal/knockback utility. Positionals and relevant preparation must remain supported. Proposed folding of mechanic buttons below requires separating routine use from discretionary burst; it must not hold ordinary rotation actions indefinitely when Auto burst is M.

### MNK — Monk

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Bootshine / Arm of the Destroyer | Routine forms/resources/preparation and completion of an active Perfect Balance/Blitz sequence. | Existing advanced paths; verify initiation-off still permits full follow-through. |
| Brotherhood and currently Perfect Balance | Burst requests the reviewed offensive commitment. Routine Perfect Balance scheduling and an active Blitz should not depend on an unrelated “mechanics” switch. | **Proposed:** fold redundant phase initiation into the appropriate offensive policy. Slot 6 remains until a manual burst route covers its deliberate function. |
| Thunderclap, Feint, Arm's Length, Riddle of Earth | Movement, party protection, knockback and personal/recovery intent. | Retain; Auto protection cannot replace manual availability. |

**Change decision:** trace Perfect Balance initiation versus continuation and distinguish ordinary use from burst alignment. Do not simply move all Perfect Balance use behind the burst switch. Verify Form Shift/Meditation access before removing controls.

### DRG — Dragoon

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| True Thrust / Doom Spike | Routine combos, appropriate jumps and active Life follow-through. | Existing; retain movement-related safeguards. |
| Lance Charge and currently Geirskogul | Burst expresses an offensive window; normal Life cycling follows its own cadence where appropriate. | **Proposed:** retire the extra Life button if deliberate timing and follow-through are covered. Existing slots 3/6 remain until verified. |
| Winged Glide / Elusive Jump; defenses | Approach and disengage remain separately meaningful. | Keep slots 4/8 and protection access. |

**Change decision:** the split between buff and Life selectors may be implementation-driven. Trace both ST/AoE and timing before merging; do not delay every Life window until a party burst request.

### NIN — Ninja

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Spinning Edge / Death Blossom | Normal ninjutsu/resources and uninterrupted completion of an accepted special sequence. | Existing; mudra integrity is a hard acceptance condition. |
| Hide selector / Kassatsu / Ten Chi Jin | Burst routes appropriate offensive commitments; preparation remains available outside combat. | **Proposed:** remove in-combat-only extra initiation where burst plus damage can cover it. Slots 3/6 retain prepull functions; slot 8 only removable after TCJ/Meisui handoff is verified. |
| Shukuchi and defenses | Deliberate movement and protection. | Retain. |

**Change decision:** separate prepull from combat permission before migrating Auto mechanics. Do not make a burst press unexpectedly begin an incompatible mudra sequence or interrupt an active one.

### SAM — Samurai

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Hakaze / Fuga | Routine Sen/Kenki management, effects, Iaijutsu and accepted follow-ups. | Existing; retain resource and target-lifetime policies. |
| Ikishoten and currently Meikyo | Burst initiates a suitable offensive commitment; routine Meikyo use remains distinguished from holding burst. | **Proposed:** fold the extra phase control only after manual initiation and follow-through are complete. Slot 6 is a candidate, not approved removal. |
| Gyoten selector / Meditate; defenses | Approach/disengage, downtime channel and protection remain deliberate. | Keep Meditate slot 9; verify movement direction is predictable. |

**Change decision:** no general Sen reset or resource-spending rewrite. Check all relevant level-dependent follow-ups when splitting initiation from continuation.

### RPR — Reaper

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Slice / Spinning Scythe | Maintain damage and complete active Enshroud/Communio and applicable follow-ups. | Existing; verify continuation when automatic phase initiation is off. |
| Arcane Circle and currently Enshroud | Burst initiates the appropriate commitment; routine Shroud management must not wait indefinitely for it. | **Proposed:** fold slot 6 after the resource-phase policy is explicit and both routes work. |
| Ingress/return, Soulsow/Harvest Moon; defenses | Movement/return and preparation/ranged opportunity stay deliberate. | Retain slots 4/9 and defenses even if some preparation is automatic. |

**Change decision:** classify Enshroud initiation by purpose, not just preset name. Keep the phase control while an independent deliberate timing use remains uncovered.

### VPR — Viper

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Steel Fangs / Steel Maw | Routine resource management, twinblade and active Reawaken/Legacy follow-through. | Existing selectors; verify both routes and level gating. |
| Serpent's Ire and currently Reawaken | Burst delegates the discretionary offensive commitment; normal gauge spending follows reviewed policy. | **Proposed:** slot 6 consolidation only after manual burst and ordinary resource-phase behavior are distinct. |
| Slither, protection and recovery | Keep recipient-aware movement and immediate personal recovery. | Retain. Uncoiled Fury may need deliberate ranged-spending access if the damage route cannot express that need. |

**Change decision:** do not add a ranged button automatically; first test whether out-of-range damage presses already supply the useful action. Preserve a manual route if they do not.

## Physical ranged jobs

Peloton explicitly means travel faster and stays accessible for all three jobs. Automatic damage is not a substitute. Party protection remains manually available.

### BRD — Bard

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Heavy Shot / Quick Nock | Songs, DoTs, procs and gauge maintain the rotation. | Existing; no separate song intent proposed for normal play. |
| Barrage selector: burst | Initiate the offensive buff sequence and return to damage. | Existing Auto burst; verify complete manual handoff. Slot 3 conditional removal candidate. |
| Troubadour / Nature's Minne | Party mitigation versus party healing amplification. | Retain slots 5/6. Minne is party-wide in the researched toolkit, not a targeted heal. |
| Repelling Shot, Arm's Length, Peloton; Paean access | Disengage, knockback, travel and eligible cleanse/prevention. | Keep current utility. **Verify:** Paean has an accessible route with correct recipient selection. |

**Change decision:** Auto support may delegate a specifically reviewed Paean policy; it must not bury manual cleanse access or treat it as generic HP healing. Give Minne one owner, preferably protection/support according to its final policy, and migrate its existing Auto mitigation effect explicitly.

### MCH — Machinist

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Split Shot / Spread Shot | Tools, ordinary Heat/Battery handling and active burst follow-through. | Existing; retain reviewed hold settings pending the level-90 investigation. |
| Heat Blast selector: burst | Express burst initiation while keeping ST-specific Wildfire and AoE behavior honest. | **Gap in delegated coverage:** Wildfire automatic route is ST-only. Keep slot 3; do not claim Auto burst covers every manual use. |
| Dismantle selector: protection | Distinguish enemy debuff and party mitigation as appropriate. | Keep slot 5; verify the reported Dismantle initiation condition before extending delegation. |
| Flamethrower, Arm's Length, Peloton | Deliberate stationary channel, knockback and travel. | Retain slots 6/7/8. Leave movement position 4 empty. |

**Change decision:** trace the manual burst selector first, then decide whether an AoE damage route should support an appropriate single-target Wildfire opportunity. Do not force Wildfire onto every pack. Diagnose the observed damage using effective settings and an action trace; no speculative resource rewrite. See [MCH investigation](investigations.md#mch-lower-damage-than-blm-in-solo-old-extremes).

### DNC — Dancer

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Cascade / Windmill | Normal damage, Standard maintenance and completion of a dance already initiated. | **Existing:** keep the Technical preset enabled and use IncludeTS = No for manual initiation with automated completion. Both advanced routes implement this distinction. |
| Technical Step: burst | Start Technical; subsequent ST/AoE presses take steps and finish using the existing timing policy. Auto burst additionally delegates starting it. | **Setup migration:** use the existing initiation setting rather than assume new continuation logic is required. Reconcile switching with Accord's feature-only variant contract. |
| Shield Samba / Curing Waltz / Improvisation | Protection, recovery and deliberate channel/release remain distinct. | Retain slots 5/6/7; no generic damage-route healing or channel initiation. |
| Partner assignment, En Avant, Peloton | Persistent recipient, movement and travel. | Preserve explicit reassignment access even if setup suggests a partner. |

**Correction, 2026-10-08:** the earlier proposal inspected the preset gate but missed the separate initiation option. [DNC.cs](../../WrathCombo/Combos/PvE/DNC/DNC.cs) additionally requires `IncludeTS == IncludeStep.Yes` to initiate Technical, while active dance completion does not require that option. [DNC_Config.cs](../../WrathCombo/Combos/PvE/DNC/DNC_Config.cs) explicitly offers manual Technical initiation with automated steps/finish. Starting also checks availability/cooldown and other conditions; there is no established repeated-start defect. The user mains DNC and reports never observing the suspected behavior. The public pack currently sets both IncludeTS options to Yes and switches the broader presets, explaining its documented manual-button completion workflow. Review how to expose the existing separation through Accord variants without resetting unrelated settings; do not start by altering the rotation engine. Runtime checks, if settings are changed, are regression checks for that migration, not investigation of an observed dance failure.

## Magical ranged jobs

Keep ST/AoE and meaningful cast/movement, protection and recovery intents. Automatic burst does not eliminate location, return, phase-specific or resurrection uses of existing controls.

### BLM — Black Mage

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Blizzard / Blizzard II | Fire/ice, MP and damage-resource rotation. | Existing. Ordinary phase management is baseline, not another intent. |
| Ley Lines | Commit to a casting location/window, with relocation still available. | Auto burst delegates initial use; retain slot 3 for relocation. |
| Aetherial Manipulation / Transpose | Chosen-ally movement/return and downtime preparation. | Retain slots 4/8; verify return selection and Umbral Soul access. |
| Addle, Manaward, Surecast | Party/personal protection and knockback timing. | Retain. |

**Change decision:** keep the current structure. Review whether moving/casting support is actually available when needed, without creating a second rotation control. Do not infer a potency target from the user's MCH comparison.

### SMN — Summoner

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Ruin / Outburst | Demi/elemental progression and appropriate damage follow-ups; pet preparation remains reachable. | Existing helper-based paths; inspect each phase separately. |
| Searing Light | Begin the offensive buff window; damage continues its sequence. | Existing Auto burst; slot 3 conditional removal candidate. |
| Astral Flow | Preserve deliberate movement and targeted recovery where phase makes them available. | Retain slot 4; damage coverage alone does not cover Rekindle or movement intent. |
| Addle, Radiant Aegis, Surecast, Swiftcast/raise | Protection and resurrection remain explicit. | Retain. |

**Change decision:** consider Auto support for eligible phase-specific recovery only after tracing recipient selection and existing damage-route use. It must not unexpectedly activate a movement form of Astral Flow. No automatic raise added.

### RDM — Red Mage

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Jolt / Scatter | Mana balance, ordinary melee entry and completed finishers under reviewed conditions. | Existing. No separate finisher button proposed. |
| Embolden selector | Begin the offensive commitment; damage handles eligible Manafication/sequence follow-through. | Existing Auto burst; verify manual ordering before slot 3 removal in A. |
| Displacement selector / Vercure | Deliberate approach/disengage versus recovery/Dualcast preparation. | Keep slots 4/6; verify movement is predictable rather than forced by damage. |
| Magick Barrier, Surecast, Swiftcast/raise | Protection and recovery chain. | Retain; no automatic resurrection or routine damage-path Vercure. |

**Change decision:** current structure largely matches intent. Targeted healing/raise and movement-selector checks matter more than merging another rotation button.

### PCT — Pictomancer

| Control/intent | Proposed follow-through and delegation | Coverage and layout consequence |
| --- | --- | --- |
| Fire in Red / Fire II in Red | Normal palette/paint, motif preparation and eligible follow-through. | Existing; verify out-of-combat/prepull preparation and required motif access. |
| Scenic Muse | Commit to the offensive window, then continue damage. | Existing Auto burst; slot 3 conditional removal candidate. |
| Hammer Stamp / Tempera Coat | Deliberate mobile damage spending versus personal/party shielding. | Retain slots 8/6; neither is redundant simply because a damage route can use one form. |
| Smudge, Addle, Surecast | Movement and protection. | Retain. |

**Change decision:** verify motif preparation coverage and the shield-conversion route. Auto protection must not consume the deliberate party-barrier opportunity without its reviewed conditions; no new motif “rotation button” proposed.

## Migration and implementation order

| Priority | Concrete work | Completion evidence |
| --- | --- | --- |
| 1 | AST target-aware eligibility investigation, separate from optional utility-card routing. | Same resolved ally is eligible and receives the same action with ally/enemy selected; offensive target retained. Card-routing tests cover ST/AoE and A/M independently. |
| 2 | DNC setup migration using existing IncludeTS options and enabled completion presets. | Both damage routes retain existing manual-start/automatic-completion behavior; variant switching changes initiation permission without resetting unrelated options. No new engine behavior presumed necessary. |
| 3 | Audit existing standalone manual burst selectors and the melee phase controls. | For each job, record prerequisite/start/follow-through and ordinary resource behavior. Remove a phase slot only where its deliberate uses are covered. |
| 4 | Healer support/targeting and multifunction utility audit. | SCH shield preparation/pet effects, SGE Kardia, SMN phase recovery and BRD Paean have explicit recipients, ownership and manual access. Missing access is recorded before layout cleanup. |
| Parallel diagnostic priority | MCH level-90 unsynced comparison. | Effective configuration plus representative action trace separates setup, timing/context and actual code defect. This does not block the intent design. |

For every migrated effect, record old baseline/variant ownership, proposed owner, whether existing settings suffice, whether new optional code is required, and ST/AoE/manual access results. Preserve detailed targeting/sliders through feature-only switches. Do not bulk-enable all old Auto mechanics presets to make the checkbox disappear.

Before final layouts, account for each existing slot function and each required utility: retained; moved with a verified equivalent; conditional on variant; or deliberately excluded with an explicit reason. Apply the agreed static/live behavior and actual Bridge shared-slot map. Never infer permission to clear all non-inventoried actions from this proposal.

## Acceptance criteria for a job

1. Manual initiation followed by ST and AoE works at representative supported levels; unavailable actions do not block the route.
2. Variant A delegates only named effects; M stops new delegated initiation while preserving accepted follow-through. Existing healer offensive-baseline exceptions are documented.
3. Protection/support resolve and validate their actual recipient; range, dead targets, full-HP focus, solo play and no eligible ally are handled without changing the offensive target.
4. Movement, channels, preparation, recovery and multifunction controls retain deliberate access. Switching damage mode does not corrupt an active sequence.
5. Disabled Accord additions preserve upstream behavior. Source review and automated checks are followed by focused in-game validation before coverage is declared complete.
6. Layout changes are justified by verified function coverage, not by a desired button count. No final slot removal is approved by this draft alone.
