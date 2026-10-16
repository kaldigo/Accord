# Focused source investigations

Reviewed 2026-10-07 against the revisions and limitations in [the research index](README.md). Findings below do not establish effective live settings or in-game correctness.

## AST: cards with an enemy selected

**User observation:** the ST-heal button offers a support card with the tank selected, but remains a normal heal with the enemy selected. The desired behavior is to offer the same eligible card when retargeting resolves that same tank. This does not mean all cards should target tanks.

**Clarification, 2026-10-08:** selecting the tank made the card appear on the hotbar; selecting the enemy again made it revert. This is a reversible action-selection/display symptom before execution, not merely a failure to redirect a card after pressing it. The user has already supplied the target-switch observation; do not ask them to establish it again. What remains unknown is whether the resolver selected the same ally in both states and which eligibility/readiness condition changed.

### Confirmed source behavior

In [AST.cs](../../WrathCombo/Combos/PvE/AST/AST.cs), the advanced ST-healing path obtains `SimpleTarget.Stack.OneButtonHealLogic`, passes that ally into `GetMatchingConfigST`, checks its HP, then checks `ActionReady(spell)` before returning `spell.RetargetIfEnabled(actionID)`.

[AST_Helper.cs](../../WrathCombo/Combos/PvE/AST/AST_Helper.cs) applies card-specific checks. Bole and Arrow include the configurable tank-role restriction; Ewer and Spire do not use that same restriction. Availability, configured HP thresholds and optional weave conditions also affect selection. Therefore, describing this code as only testing the hard-selected target would be incorrect.

In [SimpleTarget.cs](../../WrathCombo/CustomCombo/SimpleTarget.cs), `OneButtonHealLogic` uses an auto-rotation healing recipient when present, otherwise the healing stack. The public setup's custom stack is focus, UI mouseover, soft target, friendly hard target, selected target's target, living tank, self. A valid full-HP focus can win; this stack is not simply “choose the tank who most needs this card.” Without the custom settings, the fallback behavior differs.

[HealRetargeting.cs](../../WrathCombo/Combos/PvE/ALL/HealRetargeting.cs) uses the healing resolver and shared retarget settings when redirecting the returned action. Having the job presets enabled does not prove the separate shared targeting setup has been applied.

The reviewed public pack uses HP thresholds of 70% for Bole, Arrow and Spire and 60% for Ewer. These can intentionally prevent card use on a healthy ally, even when a preventive effect would be useful. They are configuration evidence, not the user's measured live values.

### Readiness remains a concrete hypothesis

### Further source investigation, 2026-10-08

A read-only check of the locally saved Wrath configuration found the intended custom healing stack and healing retargeting enabled, with AST's advanced ST-healing/card options and thresholds consistent with the reviewed pack. No private configuration export is included here and no configuration was changed. This is a saved snapshot, not proof of the settings loaded during the reported event, but missing shared setup should no longer be the leading explanation.

The readiness path has now been traced beyond its immediate caller:

1. AST resolves `healTarget` and uses it for the card's role and HP conditions.
2. `ActionReady(spell)` performs the cooldown/charge check, then native `GetActionStatus` without the resolved recipient. The [FFXIVClientStructs declaration](https://raw.githubusercontent.com/aers/FFXIVClientStructs/main/FFXIVClientStructs/FFXIV/Client/Game/ActionManager.cs) provides an explicit target-ID parameter; omitting it passes the native sentinel rather than AST's resolved ally.
3. The accepted nonzero status codes are casting and not-yet-ready, as confirmed against game-data rows [580](https://v2.xivapi.com/api/sheet/LogMessage/580?fields=Text) and [582](https://v2.xivapi.com/api/sheet/LogMessage/582?fields=Text). They are not an allowance for invalid-target errors.
4. `RetargetIfEnabled` registers the chosen action/recipient only after readiness succeeds. `ActionRetargeting.TryGetTargetFor` and the execution hook consume that registration later; they do not repair a candidate that readiness already rejected.
5. `CooldownData.RemainingCharges` also calls native status for a single-charge action, but independently accepts zero remaining cooldown. Thus that earlier call alone does not establish the cause; the final native-status gate still matters.

This establishes a **recipient-consistency defect in the selection contract**: native readiness is not explicitly checked against the ally whose eligibility was evaluated. It does not establish the exact native return code or resolved ally in the user's run. Since the Bole action data permits self/party targeting, native fallback behavior must not be guessed as proof that an enemy selection always fails.

**Concrete correction to prepare:** for the four utility cards when retargeting is enabled, resolve the recipient once, reject an absent/invalid recipient, check card availability and recipient eligibility, check native readiness with that recipient's explicit game-object ID, and register the returned card against the same captured recipient. Preserve charge/cooldown and artificial-cooldown checks, the existing accepted readiness statuses, role/HP/priority rules, and upstream behavior when the optional correction is disabled. Do not simply ignore all target errors or mutate the selected enemy. Limit the change to the affected card path initially; other callers of `ActionReady` are outside this correction.

The immediate next work is this targeted implementation/validation design, not asking the user to repeat the already established target-switch observation. A later in-game check would validate the correction and distinguish any remaining recipient-selection issue; it is not a prerequisite for identifying the source inconsistency.

[Action.cs](../../WrathCombo/CustomCombo/Functions/Action.cs) implements `ActionReady` using native `GetActionStatus` without explicitly passing the ally resolved by the AST selector. That creates a specific question: does the native readiness result differ with the selected enemy versus the intended ally for these actions?

This is a **candidate cause**, not a confirmed finding about the native API. A changed healing-stack recipient, missing shared setup, role restriction, HP threshold or weave condition could also explain the symptom. Do not patch readiness globally or bypass action validation based on this trace alone.

The eventual contract should resolve the recipient, use it for recipient-dependent eligibility/readiness and execute on that same recipient while preserving the selected enemy. Avoid separately resolving a different recipient between decision and execution.

### Separate, confirmed integration limitation

The advanced damage routes contain offensive card use, Draw, Lord and Earthly Star. Bole, Arrow, Ewer and Spire are handled through healing selection, rather than those damage routes. Thus an eligible utility card can remain unused while the user continues damage, independent of the enemy-target symptom. Draw overwrite settings also deserve review because unused utility and the next draw interact.

For each card, decide recipient, usefulness, expiry/next-draw policy and the variant that delegates support while doing damage. The agreed philosophy permits appropriate support integration; it does not authorize every damage press to run the whole healing selector. Preventive cards particularly need review of HP-only thresholds.

### Focused validation needed

Use the same card, same eligible ally and HP, same range and comparable weave state. Compare direct ally selection with enemy selection that resolves to that ally. Record the resolved recipient ID, preset/shared flags, role/HP/weave results, native action status, returned action and execution recipient. Repeat with a valid full-HP focus and without a valid focus. Confirm the offensive target is retained.

Then independently test whether the intended delegated-support route offers the card while pressing damage. Passing the target test does not prove damage-route integration, or vice versa.

## MCH: lower damage than BLM in solo old extremes

**Confirmed user context:** both jobs are level 90 in artifact gear; the content is level-60 extreme trials entered **unsynced**, solo; Machinist **Auto burst is enabled**. The user confirms equivalent gear and tried two different trials. MCH let the bosses progress further into mechanics, with an estimated clear-time difference of roughly 10–30 seconds, no more than about 30 seconds. It felt less punchy; this prompted the burst review. This is a clear-time/feel observation, not a measured DPS deficit or identified missing action. Assess the level-90 kit, not the level-60 synced kit. Exact trials, total durations and an action log were not provided.

Accept the user's equivalent-gear comparison as the reported context; do not keep requesting that they reconfirm it. Equivalent gear does not establish equal expected clear times across different jobs. Without total fight duration, the 10–30-second difference cannot be converted into a meaningful relative difference. Boss downtime, invulnerability and how much of a burst/resource cycle fits before death are possible contextual factors, not established causes. No evidence here establishes a MCH regression, and this observation need not block the automation design.

### Setup evidence

The current public Auto burst delta enables MCH preset IDs `8100`, `8108`, `8110`, `8300`, `8307`, including ST Wildfire and ST/AoE Barrel Stabilizer. The earlier Barrel Stabilizer setup omission has already been corrected in Job Setup 0.2.16. Check the installed pack and effective runtime state; do not report the old omission as an unfixed engine defect.

The scripted opener is off. Tool, Heat, Battery and follow-up handling already exist. Wildfire automation is in the ST route; manual Heat Blast access remains for deliberate/AoE burst. Auto burst being on does not remove the configured boss, HP, resource or alignment conditions.

### Concrete source gates

See [MCH.cs](../../WrathCombo/Combos/PvE/MCH/MCH.cs) and [MCH_Helper.cs](../../WrathCombo/Combos/PvE/MCH/MCH_Helper.cs), especially `AreHyperchargeToolsReady`, `ShouldUseHyperchargeST`/the Hypercharge decision helpers, `ShouldUseQueenST` and the Barrel/Wildfire checks. Helper names may change with upstream; inspect the conditions, not only the inventory labels.

- Hypercharge checks tool timing. The reviewed default tool cutoff is nine seconds; tool readiness, combo state and relevant buffs can defer entry. The public setup does not enable the options that relax the reviewed post-Wildfire/Excavator tool holds.
- The ST Hypercharge decision also considers Wildfire readiness/cooldown, high Heat and whether the target passes the configured boss policy. “Enough Heat” alone is not its entire condition.
- Wildfire requires a recent Hypercharge and other readiness/target checks. Inspect whether the burst pair actually occurs and how much of the damage window lands before invulnerability or death.
- ST Barrel Stabilizer includes alignment with Wildfire's remaining cooldown. Its boss/non-boss conditions differ from the AoE HP gate.
- Queen selection has battery and upcoming-generation conditions and avoids summoning under local Wildfire. At 100 Battery the resource test is satisfied, but other conditions still apply. Some lower-battery opportunities depend on imminent generation; non-boss settings use a fixed threshold. Short remaining fight time is therefore relevant to an observed late or absent robot.
- Reassemble also checks available tools/charges. Delaying it is not automatically an omission; count its actual use and opportunity cost before changing the policy.

These gates are plausible investigation points. Source existence does not show which one affected the user's run. No sustained-damage regression, lost Wildfire or idle Queen has yet been measured.

### Level and patch cautions

The [official MCH toolkit](https://na.finalfantasyxiv.com/jobguide/machinist/) lists Chain Saw at 90, Excavator at 96 and Full Metal Field at 100. The latter two cannot be expected in this level-90 comparison. Official action text includes higher-level trait effects, so do not read the level-100 Barrel/Chain Saw descriptions as granting every effect at level 90.

The [MCH guide](https://www.thebalanceffxiv.com/jobs/ranged/machinist/basic-guide/) is labelled 7.3. Its principles of productive tool usage, resource management and coherent burst are useful, but level-100 battery planning and exact sequences must not be copied unmodified into this test. The review does not infer that MCH should equal BLM in this content.

### Smallest useful next verification

Inspect effective preset/settings state, then capture a representative run: encounter and duration, gear including weapon/accessories, tool usage, Hypercharge/Wildfire timing and hits, Battery generation/Queen timing, resource waste, and target downtime. Use a comparable uninterrupted target window to distinguish general rotation failure from a short-fight/context effect. Compare MCH's actual actions with its own available kit before comparing its headline damage with BLM.

If the trace shows a configuration omission, fix that configuration. If it shows an intentional hold that performs poorly in this specific context, review the existing setting first. Only a demonstrated missing or incorrect condition justifies a narrow engine change. No blanket resource-spending change is proposed.
