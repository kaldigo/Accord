# Job gameplay and automation review

Coverage update, 2026-10-08: the [resolved source audit](source-audit.md) answers the original per-job review questions below. Its source/settings conclusions supersede preliminary “verify/investigate” language in this overview.

Reviewed 2026-10-07. Read [scope and evidence rules](README.md) first. These are candidate intents, not approved new buttons or variant names. Gameplay paragraphs are short guide syntheses; utility comes from the official toolkit. Source-fit paragraphs describe inspected selector structure and the public setup review, not in-game validation.

## PLD — Paladin

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/paladin/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/tanks/paladin/basic-guide/) (labelled patch 7.4, updated 17 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Maintain the physical combo and proc follow-ups, then commit damage to Fight or Flight and the magic sequence. Oath is protection capacity, not an offensive gauge.

Candidate intents: ST damage; AoE damage; burst initiation; personal protection; party protection; protect/heal a chosen ally; invulnerability; movement; enmity control.

Utility and deliberate access: Sheltron/Intervention, Cover and Clemency carry different recipient and cost decisions. Passage of Arms involves positioning. Include stance, Shirk, Provoke, interrupt and stun.

**Pass 2 — Wrath/setup fit**

Advanced ST/AoE, mitigation and separate targeted support selectors exist. Auto burst adds Fight or Flight; targeted Oath support remains separate.

Review decision: Check whether the support intent covers the desired ally without a target change. Do not absorb Clemency or Cover into damage simply because they are available.

[Source directory](../../WrathCombo/Combos/PvE/PLD/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## WAR — Warrior

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/warrior/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/tanks/warrior/basic-guide/) (labelled patch 7.4, updated 27 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Maintain Surging Tempest, manage Beast gauge and Infuriate, and follow through on Inner Release. Damage and self-sustain interact, but ally protection is a distinct choice.

Candidate intents: ST damage; AoE damage; burst initiation; self/ally sustain; party protection; invulnerability; movement; enmity control.

Utility and deliberate access: Nascent Flash needs an ally; Bloodwhetting serves self-sustain. Shake It Off interacts with existing defensive effects. Preserve stance, swap, interrupt and stun access.

**Pass 2 — Wrath/setup fit**

Advanced damage routes coexist with Raw Intuition/Nascent targeting, mitigation, party protection and invulnerability selectors.

Review decision: Review recipient selection and deliberate defensive combinations. Resource holds alone are not evidence of missing automation.

[Source directory](../../WrathCombo/Combos/PvE/WAR/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## DRK — Dark Knight

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/darkknight/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/tanks/dark-knight/basic-guide/) (labelled patch 7.5, updated 14 Aug, 2026).

**Pass 1 — gameplay and toolkit**

Maintain Darkside while managing MP and Blood. Delirium and Living Shadow structure burst. Spending MP on damage competes with retaining access to The Blackest Night.

Candidate intents: ST damage; AoE damage; burst initiation; self/ally shielding; general or magical protection; invulnerability; movement; enmity control.

Utility and deliberate access: The Blackest Night and Oblation can protect an ally. Dark Mind and Dark Missionary have damage-type considerations. Living Dead is an emergency/timing decision.

**Pass 2 — Wrath/setup fit**

Damage uses shared action logic; mitigation and targeted TBN/Oblation selectors remain. The current Carve and Spit control also preserves interrupt access.

Review decision: Separate MP reserve policy from a routing omission. Keep the interrupt function until it has a verified replacement route.

[Source directory](../../WrathCombo/Combos/PvE/DRK/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## GNB — Gunbreaker

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/gunbreaker/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/tanks/gunbreaker/basic-guide/) (labelled patch 7.4, updated 17 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Build cartridges, spend them through the appropriate single-target or area sequence, and follow Continuation actions. No Mercy and Bloodfest structure burst and weaving is constrained.

Candidate intents: ST damage; AoE damage; burst initiation; self/ally protection and recovery; party protection; invulnerability; movement; enmity control.

Utility and deliberate access: Aurora and Heart of Corundum need recipient decisions. Trajectory is movement. Superbolide and tank swapping require deliberate timing.

**Pass 2 — Wrath/setup fit**

Advanced routes already handle cartridge sequences and follow-ups. Separate mitigation, Aurora and Heart of Corundum targeting are present; auto burst supplies No Mercy/Bloodfest.

Review decision: Do not duplicate cartridge-combo buttons solely because standalone selectors exist. Verify follow-through and charge retention at the actual level.

[Source directory](../../WrathCombo/Combos/PvE/GNB/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## WHM — White Mage

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/whitemage/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/healers/white-mage/basic-guide/) (labelled patch 7.3, updated 7 Aug, 2025).

**Pass 1 — gameplay and toolkit**

Maintain damage while planning healing around lilies, free healing tools and Blood Lily recovery. Lily use has both healing and damage-cycle implications; MP and movement also affect choices.

Candidate intents: ST damage; AoE damage; burst where applicable; heal an ally; heal party; protect ally/party; emergency recovery; raise; cleanse; movement.

Utility and deliberate access: Raise, Esuna, Rescue and Aetherial Shift express independent utility. Planned mitigation is useful before HP falls; healing thresholds cannot describe all prevention.

**Pass 2 — Wrath/setup fit**

Advanced damage/healing routes, ST/AoE mitigation and retargeting selectors exist. The current offensive baseline is already automatic; the burst variant adds no separate change.

Review decision: Review which preventive support is stranded during damage play. Keep emergency heals and recovery explicit; do not make damage a general heal selector.

[Source directory](../../WrathCombo/Combos/PvE/WHM/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## SCH — Scholar

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/scholar/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/healers/scholar/basic-guide/) (labelled patch 7.4, updated 27 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Balance Aetherflow healing against Energy Drain, using fairy tools and planned shields. Recitation, Deployment and pet state create combinations that cannot be judged by current HP alone.

Candidate intents: ST damage; AoE damage; heal ally/party; prepare and deploy shields; party protection; emergency recovery; raise; cleanse; party movement; fairy management.

Utility and deliberate access: Expedient provides movement as well as mitigation. Summon, placement/following and fairy availability matter. Dissipation temporarily trades away the fairy.

**Pass 2 — Wrath/setup fit**

Separate Recitation, Deployment, fairy, Dissipation and Aetherflow selectors accompany advanced damage/heals. Current auto mechanics adds conditional Dissipation to healing routes.

Review decision: Distinguish deliberate shield preparation from routine follow-through. Audit pet-state effects before routing Dissipation through damage; no such change is approved.

[Source directory](../../WrathCombo/Combos/PvE/SCH/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## AST — Astrologian

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/astrologian/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/healers/astrologian/basic-guide/) (labelled patch 7.3, updated 7 Aug, 2025).

**Pass 1 — gameplay and toolkit**

Maintain damage while planning heals and card usage. Offensive cards and healing/mitigation cards have different recipients and timing. Earthly Star, Neutral Sect and Macrocosmos reward anticipation.

Candidate intents: ST damage; AoE damage; offensive support; targeted support; party healing/protection; planned delayed recovery; raise; cleanse; movement/casting support.

Utility and deliberate access: Card eligibility is action-specific. Synastry and Macrocosmos preserve deliberate timing. A mitigation card may be useful before healing is required.

**Pass 2 — Wrath/setup fit**

Damage routes use offensive cards, Draw, Lord and Star. Utility cards are selected through healing. ST healing already evaluates a resolved ally; see the detailed targeting investigation.

Review decision: Prioritize card routing and target-aware readiness. Define which variant delegates useful utility-card use during damage, without sending every heal through ST/AoE.

[Source directory](../../WrathCombo/Combos/PvE/AST/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## SGE — Sage

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/sage/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/healers/sage/basic-guide/) (labelled patch 7.3, updated 7 Aug, 2025).

**Pass 1 — gameplay and toolkit**

Kardia turns damage into healing for its chosen recipient. Addersgall supports healing and MP management; shields, mitigation, Addersting and movement tools have different purposes.

Candidate intents: ST damage; AoE damage; assign passive healing recipient; heal ally/party; prevention; planned large recovery; raise; cleanse; movement.

Utility and deliberate access: Kardia assignment is a persistent recipient decision. Icarus can target ally or enemy. Zoe/Pneuma and shields may be deliberate preparation rather than reactive healing.

**Pass 2 — Wrath/setup fit**

Advanced damage/heal routes coexist with Kardia, Rhizomata, Zoe/Pneuma and mitigation selectors. Manual Kardia retargeting already uses the shared healing stack. The optional automatic initial-assignment path uses a different resolver and is disabled in the baseline.

Review decision: Keep manual Kardia and deliberate Pneuma. Only automatic initial assignment needs a resolver-policy change if it is introduced; Rhizomata is already enabled on damage and healing paths.

[Source directory](../../WrathCombo/Combos/PvE/SGE/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## MNK — Monk

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/monk/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/melee/monk/basic-guide/) (labelled patch 7.4, updated 26 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Follow form and Fury requirements, spending Chakra and building the appropriate Nadi/Blitz through Perfect Balance. Brotherhood and Riddle of Fire define damage windows.

Candidate intents: ST damage; AoE damage; burst initiation; mechanic-phase initiation/follow-through; personal/party protection; movement; preparation.

Utility and deliberate access: Form Shift and Meditation support preparation/downtime. Thunderclap needs a recipient and may be used defensively. Positionals and True North remain situational.

**Pass 2 — Wrath/setup fit**

Advanced damage, Beast Chakra/Blitz, Perfect Balance and burst selectors exist. The current job-mechanics switch delegates Perfect Balance initiation on ST/AoE.

Review decision: Check that manual initiation flows back into both damage routes. Preparation access must survive any removal of standalone mechanic buttons.

[Source directory](../../WrathCombo/Combos/PvE/MNK/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## DRG — Dragoon

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/dragoon/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/melee/dragoon/basic-guide/) (labelled patch 7.5, updated 27 Jun, 2026).

**Pass 1 — gameplay and toolkit**

Alternate the damage combos while maintaining their effects, then weave jumps and Life of the Dragon actions into buff windows. Positionals and animation/movement consequences matter.

Candidate intents: ST damage; AoE damage; burst initiation; Life phase initiation/follow-through; protection; targeted movement; disengage.

Utility and deliberate access: Elusive Jump is not interchangeable with a targeted gap closer. Preserve positional assistance and the ability to avoid forced movement at an unsafe time.

**Pass 2 — Wrath/setup fit**

Burst cooldown and Geirskogul/Life handling are separately switchable in the existing setup; advanced ST/AoE follows through.

Review decision: Decide whether the separate Life initiation represents a useful player intent or only a historical variant split. Test both paths before changing the slot.

[Source directory](../../WrathCombo/Combos/PvE/DRG/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## NIN — Ninja

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/ninja/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/melee/ninja/basic-guide/) (labelled patch 7.05, updated 11 Aug, 2024).

**Pass 1 — gameplay and toolkit**

Sequence mudras without interruption, manage Ninki and Kazematoi, and prepare the attacks that support personal and party burst windows. Kassatsu and Ten Chi Jin change action sequencing.

Candidate intents: ST damage; AoE damage; burst initiation; special-sequence initiation/follow-through; preparation; protection; ground movement.

Utility and deliberate access: Hide resets mudras out of combat; prepull Suiton/Kassatsu matters. Shukuchi is deliberate ground movement. Sequence integrity is essential.

**Pass 2 — Wrath/setup fit**

Advanced routes and standalone mudra/Kassatsu/TCJ selectors coexist. Current auto mechanics adds Kassatsu, TCJ and Meisui; Hide and prepull Kassatsu remain accessible.

Review decision: Do not remove prepull controls on the strength of in-combat coverage. Review interruptions and lower-level mudra availability separately.

[Source directory](../../WrathCombo/Combos/PvE/NIN/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## SAM — Samurai

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/samurai/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/melee/samurai/basic-guide/) (labelled patch 7.4, updated 5 Aug, 2025).

**Pass 1 — gameplay and toolkit**

Build Sen and Kenki, maintain combo effects, and execute Iaijutsu and follow-ups. Meikyo and Ikishoten shape resource and burst timing; target lifetime affects damage-over-time value.

Candidate intents: ST damage; AoE damage; burst initiation; mechanic-phase initiation/follow-through; protection; approach; disengage; downtime preparation.

Utility and deliberate access: Meditate and Sen conversion can matter during downtime. Gyoten and Yaten express different movement intents and spend resources.

**Pass 2 — Wrath/setup fit**

Advanced damage coexists with Meikyo/Sen, Ikishoten, Iaijutsu/Ogi and movement selectors. Current mechanics delegates Meikyo on both damage routes.

Review decision: Verify that a manual burst press starts the intended sequence at each level. Keep movement and downtime decisions distinct from damage optimization.

[Source directory](../../WrathCombo/Combos/PvE/SAM/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## RPR — Reaper

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/reaper/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/melee/reaper/basic-guide/) (labelled patch 7.5, updated 5 Aug, 2026).

**Pass 1 — gameplay and toolkit**

Maintain Death’s Design while generating and spending Soul and Shroud. Enshroud has a sequence to complete; Arcane Circle and its follow-up structure burst.

Candidate intents: ST damage; AoE damage; burst initiation; Enshroud initiation/follow-through; protection; movement and return; preparation/ranged fallback.

Utility and deliberate access: Soulsow prepares Harvest Moon outside combat/during downtime. Ingress/Egress and Regress form a movement-and-return decision.

**Pass 2 — Wrath/setup fit**

Advanced routes coexist with Soulsow, Regress, Arcane Circle/Plentiful Harvest, Enshroud and Communio selectors. Mechanics delegates Enshroud.

Review decision: Preserve prep and return access. A retained Enshroud button may become unnecessary only after initiation and all follow-through are covered at relevant levels.

[Source directory](../../WrathCombo/Combos/PvE/RPR/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## VPR — Viper

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/viper/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/melee/viper/basic-guide/) (labelled patch 7.55, updated 5 Aug, 2026).

**Pass 1 — gameplay and toolkit**

Maintain combo effects, execute twinblade follow-ups, and manage coils and Serpent Offerings. Reawaken changes the sequence; Serpent’s Ire supplies burst resources.

Candidate intents: ST damage; AoE damage; burst initiation; Reawaken follow-through; ranged fallback; protection; targeted movement.

Utility and deliberate access: Uncoiled Fury offers ranged flexibility. Slither can use a chosen ally or enemy. Positionals and stored resources affect movement planning.

**Pass 2 — Wrath/setup fit**

Advanced routes coexist with Vicewinder/Vicepit, Uncoiled Fury, Reawaken/Legacy and Slither selectors.

Review decision: Classify a separate ranged spender as deliberate mobility access versus redundant rotation access. Do not equate every resource phase with a new intent button.

[Source directory](../../WrathCombo/Combos/PvE/VPR/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## BRD — Bard

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/bard/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/ranged/bard/basic-guide/) (labelled patch 7.4, updated 27 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Maintain the song cycle and damage-over-time effects while responding to procs and managing gauge. Align offensive buffs and avoid wasting proc/resource opportunities.

Candidate intents: ST damage; AoE damage; burst initiation; party protection; party healing amplification; targeted cleanse/prevention; movement; out-of-combat party speed.

Utility and deliberate access: Peloton is a travel intent. The Warden’s Paean can remove/prevent an eligible debuff; Nature’s Minne supports healing. Repelling Shot is positional movement.

**Pass 2 — Wrath/setup fit**

Advanced damage, buff, song and DoT selectors exist. Songs already cycle through ST/AoE; auto burst delegates the offensive buff group.

Review decision: Reactive party Paean is already enabled on ST/AoE, using living, cleansable, in-range party-list selection. Proactive prevention remains a separate deliberate-access question. Retain Peloton and party-wide Minne access.

[Source directory](../../WrathCombo/Combos/PvE/BRD/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## MCH — Machinist

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/machinist/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/ranged/machinist/basic-guide/) (labelled patch 7.3, updated 5 Aug, 2025).

**Pass 1 — gameplay and toolkit**

Keep tool usage productive while managing Heat, Battery and charge-based attacks. Hypercharge and Wildfire need coherent timing; robot use depends on resources and the available damage window.

Candidate intents: ST damage; AoE damage; burst initiation/follow-through; party protection; deliberate channel; out-of-combat party speed.

Utility and deliberate access: Peloton remains useful outside combat. Flamethrower is a channel. Dismantle and Tactician have different protection targets and timing.

**Pass 2 — Wrath/setup fit**

Tools, Heat and Battery are in advanced routes. Wildfire is ST-only in the reviewed automation; manual Heat Blast access remains useful for AoE burst. See the level-90 comparison investigation.

Review decision: Review effective burst presets and short-fight holds before changing the engine. The earlier Barrel Stabilizer setup omission is already corrected in the public pack.

[Source directory](../../WrathCombo/Combos/PvE/MCH/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## DNC — Dancer

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/dancer/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/ranged/dancer/basic-guide/) (labelled patch 7.40, updated 16 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Maintain Standard effects and handle procs/resources around Technical windows. Starting a dance commits to its steps and finish; partnering is persistent support assignment.

Candidate intents: ST damage; AoE damage; burst/dance initiation and completion; partner assignment; party protection; healing; channelled support; movement; travel speed.

Utility and deliberate access: Peloton, En Avant, Curing Waltz and Improvisation serve utility. Closed Position requires a recipient decision. Channel duration and finish timing matter.

**Pass 2 — Wrath/setup fit**

Advanced damage and standalone dance/partner selectors coexist. Current manual baseline requires pressing Technical Step through its steps and finish before returning to damage.

Review decision (corrected 2026-10-08): Wrath already supports manual Technical initiation with ST/AoE completion through its separate IncludeTS = No setting while the relevant preset stays enabled. The current public pack switches the broader presets instead. Review the setup/variant mapping, not a presumed missing continuation feature; retain deliberate initiation access in manual mode.

[Source directory](../../WrathCombo/Combos/PvE/DNC/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## BLM — Black Mage

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/blackmage/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/casters/black-mage/basic-guide/) (labelled patch 7.4, updated 19 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Cycle fire and ice around MP while maintaining damage and using instant casts/resources for movement. Ley Lines placement and cast planning affect output independently of burst buffs.

Candidate intents: ST damage; AoE damage; Ley Lines commitment; downtime preparation; instant-cast/movement support; targeted movement and return; protection.

Utility and deliberate access: Transpose and Umbral Soul matter during downtime. Aetherial Manipulation uses an ally; Retrace/return effects make the Ley Lines control multifunctional.

**Pass 2 — Wrath/setup fit**

Advanced damage, manipulation, Umbral Soul and Ley Lines-related selectors exist. Auto burst adds Ley Lines; its manual control remains for positioning/return.

Review decision: Do not use its damage as a numeric MCH correctness benchmark without equal context. Retain repositioning even if placement becomes automatic.

[Source directory](../../WrathCombo/Combos/PvE/BLM/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## SMN — Summoner

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/summoner/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/casters/summoner/basic-guide/) (labelled patch 7.55, updated 31 Jul, 2026).

**Pass 1 — gameplay and toolkit**

Sequence demi and elemental phases, use their follow-ups, and align Searing Light. Element choice and cast/movement constraints affect how the sequence fits the encounter.

Candidate intents: ST damage; AoE damage; burst initiation; elemental/demi follow-through; deliberate movement; protection; targeted recovery; raise; pet preparation.

Utility and deliberate access: Summon Carbuncle is preparation. Astral Flow can mean different actions across phases, including movement and targeted Rekindle. Raise is independent recovery.

**Pass 2 — Wrath/setup fit**

Advanced routes delegate much of the sequence to helpers; standalone Searing, demi/elemental, Carbuncle, Rekindle and raise selectors remain.

Review decision: ST/AoE already retarget Rekindle during eligible Phoenix windows; the manual Astral Flow selector separately supports chosen-ally Rekindle only when transformed into that action. Retain it for deliberate phase-dependent use; no missing Rekindle integration is established.

[Source directory](../../WrathCombo/Combos/PvE/SMN/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## RDM — Red Mage

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/redmage/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/casters/red-mage/basic-guide/) (labelled patch 7.4, updated 27 Dec, 2025).

**Pass 1 — gameplay and toolkit**

Balance Black and White Mana, use Dualcast, then complete melee and magical finishers. Embolden and Manafication affect burst timing, while instant casts also support recovery.

Candidate intents: ST damage; AoE damage; burst initiation; melee/finisher follow-through; approach/disengage; protection; targeted heal; raise.

Utility and deliberate access: Vercure and Verraise retain recovery purpose. Engagement and Displacement have different movement consequences. Magick Barrier is party support.

**Pass 2 — Wrath/setup fit**

Advanced routes handle mana and finishers alongside standalone melee, movement, protection and recovery selectors. Auto burst adds Embolden/Manafication.

Review decision: Preserve recipient selection and useful recovery chains. A damage weave must not unexpectedly replace an intended disengage or commit unsafe movement.

[Source directory](../../WrathCombo/Combos/PvE/RDM/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).

## PCT — Pictomancer

Sources: [official toolkit](https://na.finalfantasyxiv.com/jobguide/pictomancer/); [The Balance guide](https://www.thebalanceffxiv.com/jobs/casters/pictomancer/basic-guide/) (labelled patch 7.4, updated 10 Aug, 2025).

**Pass 1 — gameplay and toolkit**

Prepare motifs, spend palette/paint, and sequence creature, weapon and landscape effects. Burst preparation and downtime painting differ from immediately spending available resources.

Candidate intents: ST damage; AoE damage; burst initiation; preparation; mobile damage; personal protection; party shielding; movement.

Utility and deliberate access: Motifs have downtime/prepull value. Hammer resources support movement. Tempera’s personal and party functions should remain distinguishable.

**Pass 2 — Wrath/setup fit**

Advanced logic and helpers handle motifs, palette and hammer follow-ups. Current auto burst adds Starry Muse; Hammer and Tempera retain deliberate access.

Review decision: Both damage routes already enable prepull and no-target motif preparation. The delegated ST Tempera route already includes conditional Grassa conversion. Retain manual Tempera/Hammer for deliberate decisions rather than add duplicate preparation or conversion logic.

[Source directory](../../WrathCombo/Combos/PvE/PCT/). Exact baseline presets, variant changes, existing slots and selector entry points are in [runtime-inventory.json](runtime-inventory.json).
