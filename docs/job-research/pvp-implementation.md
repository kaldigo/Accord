# Accord PvP implementation

Implemented locally on 2026-10-17. This supersedes the implementation-pending language in the original research. Included in the 0.2.8 test release; in-game verification remains pending.

## Setup and ownership

Accord switches its setup page and A/M system-information entries with the game's actual hotbar mode. PvP has its own active-class selection (initially empty), four variants, live preference, cleanup preference, preparation history and backup. No PvE choices are imported into PvP. Deselecting a class leaves its existing settings and bars intact.

All 21 jobs have executable profiles in `Accord.Plugin/pvp-presets.json`. Every manual layout uses 12–14 job-owned controls including five common actions and the selected-role button. Jobs with more than 12 use additional regular bar 1 slots and controller crossbar 2 slots. Native combo bindings come from the installed game sheets (`PvPCombo`, command type 26); ordinary PvP actions use command type 1. Wrath's optional custom ST damage placeholder is respected without changing that global preference. NIN's Fuma AoE and WHM's Cure II selectors retain their native bindings, as required by their source entry checks.

Static variant changes apply behaviour only. Explicit setup applies initialized selected jobs; a never-visited PvP job is queued for one application after the game initializes it on a PvP visit. Live mode applies the current selected job on class/mode/variant changes and checks for drift every second. Combat/loading pauses layout writes without changing the live preference. Keyboard compaction stays within each bar and retains primary, healing, mobility and recovery positions; controller positions remain fixed.

Both PvE and PvP cleanup/restore protect Bridge-carried positions: selected whole bars (default 3–10), both endpoints of all shared pairs, and game-shared bars. PvP gets its own common-action and LB controls rather than overwriting carried PvE shared commands. Backup files identify character and mode and include routes; failed writes attempt rollback of every touched slot and route. Restore turns off that mode's live preference.

The updated Bridge is required. Its new `HotbarBridge.PvP.*` API publishes mappings, routes, protected positions and observed initialized jobs. External edits require a ready PvP context, stage extension pairs, and commit those pairs with routes. Uncommitted additions roll back when the edit closes. The existing PvE endpoints still reject PvP writes.

## Engine changes

Accord configures only scoped PvP presets, necessary continuation suboptions and granular fork features through the existing validated/journalled settings API. It does not modify `AutoActions`, the hands-free master toggle, runner, exclusions or execution. Personal HP thresholds, reserves, timing options and target stacks are retained. Global emergency-replacement presets are not repurposed as per-job variants.

Initiation-only gates preserve existing continuation conditions:

| Job | Narrow split |
|---|---|
| AST | Gravity initiation separated from its parent setting, keeping Double Gravity enabled after manual Gravity |
| WAR | Bloodwhetting entry separated from Nascent Chaos; Rend entry separated from Ruination |
| GNB | Gnashing Fang entry separated from Savage Claw/Wicked Talon and Continuation |
| DRG | Geirskogul entry separated from Starcross |
| SAM | Ogi entry gated; Kaeshi/Tendo/Zanshin remain existing follow-through |
| RPR | Death Warrant and Plentiful entry separated from existing expiry/pooling handling |
| DNC | Starfall, Fan and Honing entry gated independently; existing Acclaim channel guard retained |
| BLM | Burst, Elemental Weave and Xenoglossy have separate offensive/defensive entry gates |
| RDM | Embolden/Forte/Riposte entry separated from Prefulgence/Vice/melee/Scorch |
| PCT | Coat entry separated from Grassa expiry |

All entry gates default on, preserving unconfigured upstream behaviour. DNC's automatic partner and SGE's initial Kardia reminder optionally resolve a valid recipient and execute on that same recipient; these new recipient features default off and Accord enables them for managed PvP profiles. Direct partner/Kardia buttons remain for deliberate selection. No targetless Dancer preparation was added.

Primary selectors remain active for all variant combinations, including roots named BurstMode. Required continuation flags stay on. BLM defensive suboptions and RDM follow-through suboptions are owned intent switches; AST card-play selection covers both drawn card types. Independent LB initiation options stay off in managed profiles, with native LB controls retained. NIN's native LB button retains repeat access without adding new automatic LB logic.

## Access decisions after implementation checks

- Reaper keeps Death Warrant for deliberate Fate Sealed release outside Enshroud. The existing combat selector does not cover that release. Plentiful stays visible when its personal pooling policy is off.
- Dancer retains Honing Dance for early Ovation release, En Avant and partner choice. Curing Waltz stays visible if its personal HP threshold disables automatic use.
- Viper retains Snake Scales for deliberate channel/release. Rattling Coil stays visible if both of its automatic-use options are off.
- Machinist retains Wildfire for Detonator, turret placement and knockback. Full Metal Field stays visible if its timing choice is unset/unsupported.
- Other delegated HP-gated controls retain a manual fallback when their configured thresholds disable automatic initiation. BLM's mixed offensive/defensive controls, selected-ally protections, interrupts, movement and distinct LB choices remain accessible as documented in the ledger.

These fallbacks preserve access without inventing resource or HP policies. Auto selections still obey the user's Wrath thresholds/options; the UI states this explicitly.

## Validation and remaining acceptance

- Actual engine-schema tests cover all 336 PvP job/variant combinations, required parents/continuations, feature ownership, custom damage bindings, personal-setting preservation and unchanged `AutoActions`.
- Layout tests verify distinct destinations, common/role coverage, fixed controls, per-bar compaction and controller ownership.
- The 504-action semantic ledger supplies 8,064 action/state checks. Source provenance is separately pinned in `pvp-implementation-review.json`, including unchanged hands-free source hashes. The original upstream inventory hashes remain intact.
- Bridge tests cover mode isolation, staged-pair rollback, wrong-mode refusal, linked/shared-slot protection, initialization tracking, conflict pauses and automatic resumption. Existing PvE regression tests also pass.
- Accord and Bridge compile without warnings; Wrathful compiles with inherited warnings and no errors.

In-game acceptance is still needed for native combo display/pressing, retargeted execution, Honing channel presentation, first PvP visits, class/mode transitions and backup restoration. Automated checks establish settings/layout/source properties; they do not prove combat behaviour in a running client.
