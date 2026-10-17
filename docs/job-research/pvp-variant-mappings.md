# PvP intent and variant mappings

Reviewed 2026-10-17 against all 21 local PvP job implementations at upstream `5365eb1edc13654be945a624b2311a8b8541a599` and the official job actions captured in [the inventory](pvp-inventory.json). This records the source audit used for the now-implemented local PvP profiles. The per-job “split required” descriptions below record the original gap; completed changes and final access decisions are in [the implementation record](pvp-implementation.md). The accompanying [action ledger](pvp-action-ledger.json) accounts for every official action, including native transformations, common actions and role choices. Validation checks coverage and source hashes.

## Rules shared by every job

The four variants delegate **initiation**, not permission to complete an action already started. A means the appropriate combat/healing button can initiate the listed work. M exposes its initiator and keeps the continuation on that intent's automation button. A root named `BurstMode` remains enabled in both states. Never disable an entire damage selector to make burst manual.

The table's Burst column covers offensive cooldown initiation; Protection covers personal survival; Support covers healing or protection of allies; Mechanics covers a separate job system. A job may have no additional effect for a variant. Basic combos, proc consumption and completion are baseline work, not extra class-mechanic initiators. Dual-purpose actions have one owner, stated below. Preserve existing HP, charge, movement, immunity, range and timing settings; variant changes do not reset them.

Always retain a deliberate route for Guard, Purify, Recuperate, Standard-issue Elixir, Sprint, the selected role action, limit-break initiation, movement/escape, displacement/interrupt and deliberate ally selection. These are distinct intents. The role button represents the currently selected role action, not three simultaneous actions. Existing emergency replacement features remain separate policy; this pass does not make global protection or support presets follow a per-job checkbox.

Limit breaks remain deliberate initiators in these layouts. This does not remove Wrath's optional LB-on-combat-button features for independent Wrath users. When Accord eventually owns a PvP profile, it must leave those initiation options off, while retaining completion after a manual LB where supported. **No hands-free AutoActions settings or execution change is part of this work.**

Most jobs have one damage intent, not separate ST/AoE automation. Do not invent a second selector. NIN explicitly has both. BLM keeps elemental choice where its configured button option permits it. Healing, ground placement and selected-target actions remain separate where they express different intent from attacking.

Native combo bindings must use the game's combo command, not a constituent's non-assignable action ID or Wrath's displayed replacement. The ledger says which source control owns each native result. Its labels are semantic bindings; resolve actual command type/ID from the installed game sheets during Accord layout implementation. A custom ST placeholder changes which IDs `OneButtonRotationChecker` accepts; inspect that configuration when resolving the primary selector, without changing the global placeholder preference.

## Four-variant ownership

“None” means no separate automation to move under that checkbox. Common/role actions are covered above. The evidence sections below distinguish existing switches from required initiation-only switches.

| Job | Auto burst initiates | Auto protection initiates | Auto support initiates | Auto class mechanics initiates |
|---|---|---|---|---|
| PLD | Imperator | Holy Sheltron; self-heal Holy Spirit policy | None; Guardian stays selected-ally intent | None |
| WAR | Primal Rend, offensive Onslaught/Orogeny | Bloodwhetting | None | None |
| DRK | Shadowbringer | The Blackest Night self use, Salted Earth, Impalement | None; selected-ally Blackest Night remains accessible | None |
| GNB | Rough Divide's No Mercy entry, Gnashing Fang, Fated Circle, Blasting Zone | Heart of Corundum self use | None; selected-ally Corundum remains accessible | None |
| WHM | Seraph Strike, Afflatus Misery | None beyond common controls | Cure II/III and Aquaveil on Glare, with resolved ally | None |
| SCH | Chain Stratagem | None beyond common controls | Adloquium; Expedient's party utility | None; Biolysis maintenance is baseline |
| AST | Gravity II, Macrocosmos offensive entry | None beyond common controls | Aspected Benefic on Malefic | Minor Arcana draw; existing card play follows draw |
| SGE | Phlegma III | None beyond common controls | Pneuma; Kardia assignment/reminder | Eukrasia initiation |
| MNK | Rising Phoenix | Riddle of Earth | None | None; forms/procs are baseline |
| DRG | Geirskogul, High Jump | Horrid Roar; Chaotic Spring sustain | None | None |
| NIN | Bunshin, Dokumori on both selectors | Meisui selection during Three Mudra; Huton preference is separately preserved | None | Three Mudra entry on both selectors |
| SAM | Meikyo Shisui, Ogi Namikiri | Hissatsu: Chiten | None | None |
| RPR | Death Warrant, Plentiful Harvest | Arcane Crest | None | None; Enshroud is LB-initiated |
| VPR | Bloodcoil; offensive Uncoiled Fury use | Snake Scales is deliberate channel; no automatic start currently | None | Rattling Coil resource refresh |
| BRD | Harmonic Arrow spending | The Warden's Paean self-cleanse | None; selected-ally Paean remains accessible | Apex Arrow buff maintenance |
| MCH | Full Metal Field, Wildfire | None | None; turret placement remains deliberate | Analysis and the Drill/Bioblaster/Air Anchor/Chain Saw cycle |
| DNC | Starfall Dance | Fan Dance damage-reduction use | Curing Waltz | Honing Dance and partner reminder |
| BLM | Offensive Burst, offensive Elemental Weave, offensive Xenoglossy | Defensive Burst, ice Elemental Weave, defensive Xenoglossy | None | None; elemental progression/Paradox are baseline |
| SMN | Crimson Cyclone, Mountain Buster, Slipstream, Necrotize | Radiant Aegis | None | None; summons are deliberate LB choices |
| RDM | Embolden, Enchanted Riposte | Forte | None | None |
| PCT | Holy in White/Comet in Black spending | Tempera Coat | None; Star Prism is LB follow-through | Creature Motif, Living Muse, portraits, Subtractive Palette |

Tank Rampart options belong to Protection when that role action is selected. Offensive role replacements (Smite, Eagle Eye Shot, Diabrosis, Phantom Dart) can follow Burst, while retaining the selected-role button for deliberate use. Other role choices have no job-selector integration in the reviewed files: do not pretend that their existence in `RoleActions.cs` means they are automatically used.

## Per-job settings, follow-through and retained intent

Preset names below are exact identifiers from each linked source. “Split required” is an identified implementation requirement, not an unresolved question to send back for live testing. Until that narrow split exists, keep the indicated native control available; do not advertise a fully working manual variant or remove its button.

### Paladin

Source: [PLDPVP.cs](../../WrathCombo/Combos/PvP/PLDPVP.cs), `PLDPvP_Burst`, Fast Blade combo. Burst owns `PLDPvP_Imperator`; leave `PLDPvP_Confiteor` enabled in M as well as A. `PLDPvP_PhalanxCombo` follows Blade of Faith Ready and the previous blade action; it does not initiate Phalanx. Atonement/Supplication/Sepulchre are baseline ready-action returns.

Protection owns `PLDPvP_Sheltron`, `PLDPvP_HolySpirit`, `PLDPvP_Rampart`. Holy Spirit also supplies ranged damage, so retain its ranged/heal intent even when its self-heal branch is delegated. Keep Guardian (chosen ally), Intervene (movement), Shield Smite (stun) and Phalanx. Intervene/Melee and ShieldSmite are existing offensive integration options, not reasons to remove movement/stun controls. No new class-mechanic switch is needed. Imperator and self-only Sheltron controls can disappear in their respective A states; native Imperator/Phalanx transformations remain reachable.

### Warrior

Source: [WARPVP.cs](../../WrathCombo/Combos/PvP/WARPVP.cs), `WARPvP_BurstMode`, Heavy Swing combo. Protection owns `_Bloodwhetting` and `WARPvP_Rampart`; Burst owns `_PrimalRend`, `_Onslaught`, `_Orogeny`. Retain Blota for pull timing, Onslaught for movement, Primal Scream for LB. `_InnerChaos` and `_PrimalScream` are continuation settings: the latter returns Primal Wrath only with Wrathfull, not the initial LB.

Split required: `_Bloodwhetting` also gates Nascent Chaos follow-through, and `_PrimalRend` gates both Rend and Ruination. M must suppress the first action only, leaving Chaotic Cyclone/Ruination on the combat button. Preserve Blota timing and native Fell Cleave. Until split, Bloodwhetting and Rend stay usable through their native transforming controls. No new resource-spending policy.

### Dark Knight

Source: [DRKPVP.cs](../../WrathCombo/Combos/PvP/DRKPVP.cs), `DRKPvP_Burst`, Hard Slash combo. Burst owns `_Shadowbringer`; Protection owns `_BlackestNight`, `_SaltedEarth`, `_Impalement`, `_Rampart`. Keep `_SaltAndDarkness` and `_Scorn` as continuation, including after manual Salted Earth/Eventide. Plunge remains movement; Blackest Night remains accessible for a chosen ally even if self shielding is automated.

Shadowbringer spends HP and uses its existing HP/Dark Arts condition; do not remove that threshold to spend faster. Blackest Night checks self status and does not establish selected-ally coverage. The source still names `Bloodspiller`/`Scorn`, while the official kit shows Scarlet Delirium/Comeuppance/Torcleaver and Disesteem; use native Hard Slash/Eventide transformations rather than inventing new direct bindings from those old names. Eventide remains deliberate.

### Gunbreaker

Source: [GNBPVP.cs](../../WrathCombo/Combos/PvP/GNBPVP.cs), `GNBPvP_Burst`, Keen Edge combo. Burst owns `_RoughDivide`, `_ST_GnashingFang`, `_FatedCircle`, `_BlastingZone`; Protection owns `_Corundum`, `_Rampart`. `_ST_Continuation` stays enabled in both burst states, covering all ready Continuation forms. Rough Divide still needs a movement control; Heart of Corundum still needs chosen-ally access. Relentless Rush/Terminal Trigger retain their channel/finish control.

Split required: `_ST_GnashingFang` tests `ActionReady(GnashingFang) || OriginalHook(GnashingFang) != GnashingFang`. Disabling it stops main-button chain continuation. The separate `GNBPvP_GnashingFang` selector adds weaves on the Gnashing button, which is a fallback, not the requested main-button follow-through. Split entry from continuation before hiding the manual chain. GNB has no extra class-mechanic effect.

### White Mage

Source: [WHMPVP.cs](../../WrathCombo/Combos/PvP/WHMPVP.cs), `WHMPvP_Burst` on Glare; `WHMPvP_Heals` on Cure II. Burst owns `_Seraph_Strike` and `_Afflatus_Misery`; Glare IV is already returned when ready regardless of those entry toggles. Support owns `_Burst_Heals`, preserving its Cure II/III/Aquaveil option array and retarget/HP settings. The damage heal block is excluded during Sacred Sight; retain Cure II as deliberate healing and Aquaveil for deliberate barrier/cleanse use.

Keep Miracle of Nature for interrupt timing and Seraph Strike for deliberate movement, plus Afflatus Purgation. `_AfflatusPurgation` is LB initiation and must not be mistaken for normal burst completion. `_Seraphstrike` is the separate Strike-to-Glare-IV convenience selector. The heal selector's Cure II fallback uses option index 2 for retargeting, not index 0: preserve this source fact and do not claim independent Cure II retarget control works merely from labels.

### Scholar

Source: [SCHPVP.cs](../../WrathCombo/Combos/PvP/SCHPVP.cs), `SCHPvP_Burst` on Broil; `SCHPvP_RetargetAdlo` on Adloquium. Burst owns `_ChainStratagem`. Support owns `_Adlo` and `_Expedient`; retain Adloquium for chosen-ally shielding, Expedient for movement and Deployment Tactics for deliberate ally/enemy spread. `_DeploymentTactics` on Broil only spreads enemy Biolysis when charges exceed one; it does not cover ally spread.

Biolysis remains baseline maintenance. Its branch explicitly allows use when Expedient integration is off, or Recitation is active, or Expedient is more than five seconds away. Do not impose a new stall when Support is M. Adlo checks HP and Catalyze on the resolved heal target and retargets that same target. Summon Seraph and Seraphism remain deliberate; their transformations/pet effects are native, not implemented by the Broil selector.

### Astrologian

Source: [ASTPVP.cs](../../WrathCombo/Combos/PvP/ASTPVP.cs), `ASTPvP_Burst` on Malefic; `_Heal` on Aspected Benefic. Mechanics owns `_Burst_DrawCard`; keep `_Burst_PlayCard` and its Lord/Lady selection as draw follow-through in both states. Unlike the PvE tank-card case, these are PvP Minor Arcana cards. Support owns `_Burst_Heal`; preserve resolved ally, Diurnal Benefic and HP checks. Healing Double Cast remains tied to the healing intent and existing setting.

Burst owns `_Burst_Gravity` and `_Burst_Macrocosmos`; keep `_Burst_DoubleGravity`, `_Burst_DoubleMalefic` and `_Burst_Oracle` for appropriate continuation. Retain Aspected Benefic, Double Cast for deliberate duplication, Macrocosmos/Microcosmos for healing release, Epicycle/Retrograde for movement and Celestial River. Do not enable `_Epicycle` as a pure movement control: it substitutes Minor Arcana and Macrocosmos before the move. Microcosmos has no explicit main-selector release branch. The damage heal block sits inside enemy-immunity gating; do not call it complete coverage for emergency healing.

### Sage

Source: [SGEPVP.cs](../../WrathCombo/Combos/PvP/SGEPVP.cs), `_BurstMode` on Dosis; `_RetargetKardia` on Kardia. Burst owns `_Phlegma`; keep `_Psyche` as its post-Phlegma continuation. Mechanics owns `_Eukrasia`; Dosis already follows Eukrasia without an initiation toggle. `_Toxikon2` is earned follow-through and `_Toxikon` is baseline ranged/debuff handling.

Support owns `_Pneuma` and `_KardiaReminder`, preserving HP settings; retain Pneuma's deliberate line-heal/barrier intent, Kardia recipient selection, Icarus movement and Mesotes placement. The first Kardia reminder returns bare Kardia when self Kardia is missing, whereas the later low-HP branch explicitly retargets. This is a concrete gap: resolve a recipient for the first return before considering the reminder a complete automatic assignment. No extra Protection effect is fabricated.

### Monk

Source: [MNKPVP.cs](../../WrathCombo/Combos/PvP/MNKPVP.cs), `_Burst` on Dragon Kick combo. Burst owns `_Burst_RisingPhoenix`; Protection owns `_Burst_RiddleOfEarth`. Form progression and Phantom Rush remain baseline. `_Burst_FiresReply` follows the offensive state and `_Burst_WindsReply` supplies ranged damage. Keep Thunderclap for ally/enemy movement, Wind's Reply for deliberate knockback/range, Riddle of Earth for early release and Meteodrive.

Earth's Reply expiry is outside the initiation option because of the actual expression's `||`: it still returns with Earth Resonance at two seconds even when Riddle entry is M. That is existing follow-through, not an unverified proposed fix. Do not move parentheses and accidentally disable it. `_Burst_Meteodrive` initiates LB and remains outside delegated burst in the proposed profile. Preserve Rising Phoenix charge/Phantom Rush alignment.

### Dragoon

Source: [DRGPVP.cs](../../WrathCombo/Combos/PvP/DRGPVP.cs), `_Burst` on Raiden Thrust combo. Burst owns `_Geirskogul` and `_HighJump`. Protection owns `_HorridRoar` and `_ChaoticSpringSustain`; Chaotic Spring execute is a separate offensive condition, not automatically owned by Protection. Keep `_Nastrond` and `_WyrmwindThrust` as follow-through. Retain Elusive Jump, ranged Wyrmwind access through its native transformation, deliberate Chaotic Spring sustain and Sky High/Sky Shatter.

Split required: `_Geirskogul` also gates Starcross Ready. M must preserve Starcross while suppressing Geirskogul initiation. `_BurstProtection` is evasive-burst policy and a replacement on Elusive Jump that can return action 26; it is not the Auto protection variant. Preserve the user's setting instead of equating names. High Jump's Life of the Dragon/cooldown checks and Nastrond's HP/duration thresholds stay intact.

### Ninja

Source: [NINPVP.cs](../../WrathCombo/Combos/PvP/NINPVP.cs), `_ST_BurstMode` on Spinning Edge combo and `_AoE_BurstMode` on Fuma Shuriken. Burst owns both `_Bunshin` and `_Dokumori`; Mechanics owns both `_ThreeMudra` entries, preserving `_ThreeMudraPool`. Both `_MudraMode` selectors remain active after manual Three Mudra, with their existing priority/option arrays. Protection controls both `_Meisui` choices; Huton is also an explicit array option, so do not silently rewrite the user's ninjutsu ordering.

Keep Shukuchi, Fuma for ranged/AoE intent, and the transformed ninjutsu source controls when manual choice is required. ST always has Three Mudra -> Forked Raiju fallback; AoE returns Fuma unmodified when MudraMode is off. Those are different behaviours, not equivalent manual-mechanics settings. ST LB option combines first Seiton Tenchu and Unsealed repeat; AoE only tests LB readiness. Split required if main-button LB repeat is desired without automatic first LB. Until then retain Seiton Tenchu's transforming button. Assassinate, Zesho and Raiju continuation are baseline.

### Samurai

Source: [SAMPVP.cs](../../WrathCombo/Combos/PvP/SAMPVP.cs), `_Burst` on Yukikaze combo. Burst owns `_Meikyo` and Ogi entry; Protection owns `_Chiten`. Zanshin, Tendo/Kaeshi and Kaiten combo forms follow through independently of the Chiten/Meikyo entry flags. Retain Soten movement, Mineuchi interrupt and Zantetsuken. Respect moving/bind/priority-weaponskill checks and the existing Soten reserve.

Split required: Ogi Namikiri initiation is unconditional in the stationary, valid-target branch. Add an entry-only gate before offering M burst, leaving Kaeshi Namikiri above it intact. The Mineuchi condition contains `!HasBattleTarget()` in source; do not assume reliable automated interrupt coverage and remove Mineuchi. LB's Kuzushi/invulnerability logic remains upstream; its initiation option is not enabled by the proposed Burst checkbox.

### Reaper

Source: [RPRPVP.cs](../../WrathCombo/Combos/PvP/RPRPVP.cs), `_Burst` on Slice combo. Burst owns `_Burst_DeathWarrant` and Plentiful entry; Protection owns `_Burst_ArcaneCircle` (despite its name it returns Arcane Crest). Keep `_Burst_Enshrouded`, `_Enshrouded_Communio`, `_Enshrouded_DeathWarrant` completion logic; manual Tenebrae Lemurum starts the state. Reaping, Executioner's Guillotine and Perfectio already have state-based returns. Preserve Communio's stationary/expiry handling.

Split required: `_Burst_ImmortalPooling` is a policy option with a Plentiful return inside it, not an initiation toggle. Separate Plentiful entry and Death Warrant entry from Fate Sealed completion; the expiry `||` in the Enshrouded Death Warrant branch can fire even with its entry flag off. Retain Harvest Moon for ranged intent, Death Warrant for deliberate Fate Sealed release outside Enshroud, Plentiful when personal pooling policy disables automatic use, Hell's Ingress/Regress, and the LB control. Grim Swathe/Guillotine remain baseline resource generation/completion.

### Viper

Source: [VPRPVP.cs](../../WrathCombo/Combos/PvP/VPRPVP.cs), `_Burst` on Steel Fangs and Generation combos. Burst owns `_Bloodcoil` and offensive `_UncoiledFury`; Mechanics owns `_RattlingCoil` with its existing refill/reset options. Bloodcoil's Sanguine Feast/Ouroboros returns are explicitly outside its initiation flag. Serpent's Tail and Generation/Legacy follow-through are baseline. Keep the ranged Uncoiled Fury control, Slither, Snake Scales and World-swallower.

`_Backlash` is a completion option, not automatic Snake Scales initiation. `_SnakeScales_Feature` refreshes via Rattling Coil only when Scales is on cooldown and not Backlash. Therefore Protection A currently adds no channel start; never remove Scales on the claim that it is automated. Preserve deliberate channel duration/Backlash timing. Rattling Coil's defensive reset and resource refill share one action owned by Mechanics; do not let two variants overwrite the same flag.

### Bard

Source: [BRDPVP.cs](../../WrathCombo/Combos/PvP/BRDPVP.cs), `_BurstMode` on Powerful Shot. Mechanics owns `_ApexArrow` to maintain the PvP buff, with `_BlastArrow` as continuation. Burst owns `_HarmonicArrow`; keep charge/execute thresholds and Frontline March requirement. Protection owns `_Wardens` self-cleanse. Keep Paean for a chosen ally, Silent Nocturne for interrupt, Repelling Shot for displacement and Final Fantasia. `_EncoreOfLight` follows a ready LB finisher; it does not start the LB.

PvP has no three-song PvE loop to manufacture. Pitch Perfect remains the native Powerful Shot transformation. Harmonic can be omitted in A because its offensive spending is delegated; Apex can be omitted with Mechanics A, while its Blast continuation remains enabled in M. Paean's automation checks only self soft CC and is inside target-immunity gating, so it cannot replace the ally-cleanse control.

### Machinist

Source: [MCHPVP.cs](../../WrathCombo/Combos/PvP/MCHPVP.cs), `_BurstMode` on Blast Charge. Burst owns `_FullMetalField` and `_Wildfire`; Mechanics owns `_Analysis`, `_Drill`, `_BioBlaster`, `_AirAnchor`, `_ChainSaw`. Preserve `_AltAnalysis`, `_AltDrill` and `MCHPvP_FMFOption`; these select timing, not a variant. Blazing Shot is native Blast Charge progression.

The tool cycle has a deliberate controller (Drill's transforming button) in M; each phase is the next tool choice, not follow-through from an unrelated Wildfire press. Analysis can remain separately accessible to choose an enhancement. Keep Scattergun knockback, Bishop Autoturret/Aether Mortar placement and Marksman's Spite. Keep Wildfire for early Detonator intent: there is no explicit Detonator return in the main selector. No automatic turret placement or new pooling policy is implied. `_MarksmanSpite` is first LB, not burst follow-through.

### Dancer

Source: [DNCPVP.cs](../../WrathCombo/Combos/PvP/DNCPVP.cs), `_BurstMode` on Cascade family. Mechanics owns `_HoningDance` and `_Partner`; Support owns `_CuringWaltz`. Honing Dance is a channel/Acclaim workflow, not Standard/Technical steps. Keep En Avant for movement, Closed Position for partner choice and Contradance for charm timing. Partner automation currently returns bare Closed Position and does not resolve an ally; keep direct access until a recipient is resolved consistently.

Split required: Starfall Dance (Burst) and Fan Dance (Protection, because it supplies mitigation) are embedded without initiation toggles. Honing's Acclaim<4 `Cease` guard is inside its enabled branch: preserve the channel guard even when the initial channel was started manually. Honing Ovation early release remains a distinct manual control through Honing's native button. Saber Dance/Dance of the Dawn/proc combos remain baseline. This adds no targetless preparation and changes no current DNC behaviour in this research pass.

### Black Mage

Source: [BLMPVP.cs](../../WrathCombo/Combos/PvP/BLMPVP.cs), `_BurstMode` on the configured Fire/Blizzard set. Keep Fire/Blizzard elemental choice, Paradox baseline use, Aetherial Manipulation, Lethargy interrupt/control and Soul Resonance. `_ElementalStar` is LB follow-through; retain it independently of first-LB intent.

Split required: `_Burst`, `_ElementalWeave` and `_Xenoglossy` each combine offensive and defensive branches. Their existing suboptions enable defensive branches but do not disable offensive branches. Burst A and Protection M (and the reverse) therefore cannot be represented by flipping a single existing parent. Add per-branch entry gates with the original branch conditions unchanged. Keep the direct controls until that is implemented. Preserve movement threshold, button option, elemental states and target/HP policies; no new mana/element rotation is proposed.

### Summoner

Source: [SMNPvP.cs](../../WrathCombo/Combos/PvP/SMNPvP.cs), `_BurstMode` on Ruin III. Burst owns `_CrimsonCyclone`, `_MountainBuster`, `_Slipstream`, `_Necrotize`; Protection owns `_RadiantAegis`. Keep `_CrimsonStrike`, `_DeathFlare` and `_BrandofPurgatory` as continuation after manual entry; Further Ruin consumes Ruin IV independently. Preserve both Summon Bahamut and Summon Phoenix: they are different LB intents with native pet effects, not one interchangeable button.

Retain Crimson Cyclone for deliberate movement, Mountain Buster for deliberate close-range control and Radiant Aegis for a chosen ally. Offensive Mountain Buster has mitigation too, but Burst remains its single owner. Ruin's native Astral Impulse/Fountain of Fire transformations handle summon-state main attacks. No automatic summon initiation is added. Radiant Aegis's existing main branch is under enemy-immunity and weave checks; that is not complete ally shielding coverage.

### Red Mage

Source: [RDMPVP.cs](../../WrathCombo/Combos/PvP/RDMPVP.cs), `_BurstMode` on Jolt III. Burst owns `_Embolden`, `_Riposte`; Protection owns `_Forte`. Retain Corps-a-corps and Displacement for movement, Resolution for silence/bind timing and Southern Cross for LB targeting. Jolt/Grand Impact remain baseline. Preserve dash Purify convenience independently of the four variants.

Split required: `_Embolden` plus its suboption gates Prefulgence as well as initiation; `_Forte` plus its suboption gates Vice of Thorns; `_Riposte` gates the entire melee chain and Scorch. M must suppress only the first action and keep the remaining state-driven branches. Retain native transformed controls until these splits exist. The existing melee entry depends on range, Corps availability and No Guard rules; preserve them instead of creating a new burst sequence.

### Pictomancer

Source: [PCTPVP.cs](../../WrathCombo/Combos/PvP/PCTPVP.cs), `_Burst` on Fire in Red; `_OneButtonMotifs` on Living Muse. Mechanics owns `_CreatureMotif` entry and `_SubtractivePalette`; `_LivingMuse` and `_MogOfTheAges` remain follow-through after a manual motif as well as automatic drawing; Burst owns `_HolyInWhite`; Protection owns `_TemperaCoat`. Keep Smudge, deliberate Tempera Grassa release and Advent of Chocobastion. `_StarPrism` and `_StarPrismOneButtonMotifs` are LB follow-through. `_BurstControl` is an HP policy, not Manual burst; preserve it.

Mechanics M exposes Creature Motif as the drawing intent, then leaves muse and portrait use on the combat selector under existing target/state conditions. The Living Muse motif selector is also a native-access fallback, but repeatedly pressing it is not a substitute for combat-button follow-through. Keep Subtractive Palette as a separate stance intent when M, and preserve movement/Quick Sketch policy. Split required for Tempera Coat: its current option gates both initial shield and expiring Grassa return. Retain expiry completion after manual shield. Do not add new out-of-combat painting or force the existing stationary condition off.

## Implementation checklist derived from the review

1. Separate PvP Accord selection, variants, preparation state and action catalog. Preserve PvE presets and global custom-button settings. Resolve native combo commands from sheets, including inherited transformed actions and pet effects.
2. Add only the initiation/continuation splits identified for AST (Gravity parent/Double Gravity continuation), WAR, GNB, DRG, SAM, RPR, DNC, BLM, RDM and PCT. NIN needs a split only for desired LB repeat on the main selector; direct LB remains complete access without it. Retarget fixes for DNC partner and SGE first Kardia are specific recipient gaps, not a rewrite of healer target policy.
3. Keep the primary selector and required continuation in all 16 variant combinations. Apply the ledger's direct controls, then remove only a delegated initiator with complete appropriate-selector coverage. Preserve deliberate targeting/channel/positioning intents and shared-slot ownership.
4. Test mixed variants as well as all A/all M. In particular BLM offensive/defensive independence, WAR manual Bloodwhetting follow-through, GNB manual Gnashing, RDM manual Embolden/Forte/melee, DNC manually started Honing and targetless non-initiation, PCT manual Coat expiry, and NIN ST/AoE differences have concrete source-defined expectations.

These are implementation requirements now established by the audit. No live encounter is needed to discover whether the existing branches are gated together. Native hotbar binding, target execution and channel presentation still require in-game acceptance after implementation; compilation cannot prove those.

## Interaction with Bridge-shared slots

Bridge carries selected whole bars (initial selection 3–10) and configured shared pairs across modes, as requested. Accord PvP job layouts must protect those positions. In particular, a job-specific PvP limit break must occupy a job-owned slot, not overwrite a Bridge-shared PvE limit-break assignment. Carrying a raw shared command does not convert a PvE action into a PvP equivalent. Both regular and controller endpoints of shared pairs remain reserved.
