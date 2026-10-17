# PvP setup research and implementation boundary

Recorded 2026-10-17. The inventory and detailed branch review are complete; see [the four-variant mappings](pvp-variant-mappings.md) and [action-access ledger](pvp-action-ledger.json). Accord PvP layouts/settings are implemented locally for all 21 jobs. Included in the 0.2.8 test release. Build/schema/layout tests pass; in-game acceptance is still pending. See [implementation details](pvp-implementation.md).

## Agreed scope

- Build information for PvP layouts with Auto burst, Auto protection, Auto support and Auto class mechanics. Buttons continue to express intent; automation follows through on button presses.
- Hotbar Bridge must support PvP with mode-isolated state and safe persistence.
- Do not enable, extend or otherwise alter Wrath's hands-free auto rotation in PvP. Its existing restriction remains.
- Preserve upstream PvP behaviour unless a specific intent-routing gap requires a narrow change. Do not copy PvE settings, action IDs or assumptions into PvP.
- The hotbar-location editor is a separate future feature. Existing per-bar compaction and periodic live checks remain local unreleased work.

## Sources and completeness

The [official PvP guide](https://na.finalfantasyxiv.com/jobguide/pvp/) currently includes a 7.55 adjustment overview and a 7.5 banner; preserve these source labels rather than infer a new patch number. The inventory records official action names, categories, assignability annotations, URLs and page hashes for all 21 supported combat jobs. It also records every root selector declaration, preset toggle reference, setting key and direct entry-check line found in each pinned Wrath PvP job file. Inventory generation is repeatable with `tools/collect-pvp-research.py`.

The collector is syntactic evidence, not proof that an action is covered in every state. In particular, action IDs and actual native combo bindings must be resolved against the installed game sheets, and helper calls/conditional continuation must be traced before finalising variant ownership or removing a button. Numeric optimization and new rotation policies are not part of this pass.

## Layout implications already established

Many jobs expose one primary combat selector. A preset called BurstMode can be the entire damage rotation: switching it off is not a valid implementation of manual burst. Distinguish an initiation switch from the selector parent and retain required continuation. NIN has distinct ST/AoE selector paths; several healers and other jobs have additional heal, utility or alternate-action selectors.

Common coverage must include Standard-issue Elixir, Recuperate, Purify, Guard and Sprint, plus the current role action and each job's limit break. Whether an optional response is automated does not by itself remove its distinct deliberate use. Movement, crowd control, target-specific ally protection and limit-break timing require an explicit ownership decision. No automatic limit-break policy has been approved by this research.

Native PvP combo actions require particular care. Several component attacks are officially non-assignable; that does not mean the main combo button should disappear. Record the native combo command type/ID, not its transient displayed action. The existing PvE ActionCatalog excludes PvP actions and cannot be reused unchanged.

Global Wrath emergency Recuperate, Guard and Quick Purify are action replacements, not hands-free rotation. They have shared/movement exclusions, Guard-preservation logic, HP/MP/status conditions and special-state exclusions. They are candidate protection/support settings, not a licence to loosen their upstream conditions. Their shared scope also needs isolation from per-job selection/variant changes.

DNC illustrates why the PvE mapping cannot be copied: PvP exposes Honing Dance, Fan Dance and its own partner/recovery/limit-break mechanics, not the PvE Standard/Technical dance workflow. Its current main selector has optional Honing Dance, partner, Curing Waltz and dash handling, while Fan Dance and Starfall are embedded in the main path. A manual burst setting cannot be inferred merely from the parent name.

## All-job inventory

Root selectors and conditional toggles below are the source inventory. The linked detailed mappings now classify initiation, follow-through, retained intent and concrete setting gaps for every job; these counts are not button counts.

| Job | Root selectors | Conditional toggles | Official job and limit-break entries |
|---|---|---|---|
| PLD | PLDPvP_Burst | 9 | 17 |
| WAR | WARPvP_BurstMode | 8 | 14 |
| DRK | DRKPvP_Burst | 9 | 14 |
| GNB | GNBPvP_Burst, GNBPvP_GnashingFang | 7 | 19 |
| WHM | WHMPvP_Burst, WHMPvP_Heals, WHMPvP_Seraphstrike | 9 | 9 |
| SCH | SCHPvP_Burst, SCHPvP_RetargetAdlo | 6 | 11 |
| AST | ASTPvP_Burst, ASTPvP_Epicycle, ASTPvP_Heal | 9 | 13 |
| SGE | SGEPvP_BurstMode, SGEPvP_RetargetKardia | 8 | 11 |
| MNK | MNKPvP_Burst | 7 | 14 |
| DRG | DRGPvP_Burst, DRGPvP_BurstProtection | 9 | 15 |
| NIN | NINPvP_ST_BurstMode, NINPvP_AoE_BurstMode | 17 | 19 |
| SAM | SAMPvP_Burst | 6 | 16 |
| RPR | RPRPvP_Burst | 9 | 18 |
| VPR | VPRPvP_Burst, VPRPvP_SnakeScales_Feature | 6 | 29 |
| BRD | BRDPvP_BurstMode | 7 | 10 |
| MCH | MCHPvP_BurstMode | 11 | 14 |
| DNC | DNCPvP_BurstMode | 5 | 14 |
| BLM | BLMPvP_BurstMode, BLMPvP_Manipulation_Feature | 6 | 21 |
| SMN | SMNPvP_BurstMode | 9 | 18 |
| RDM | RDMPvP_BurstMode, RDMPvP_Dash_Feature | 7 | 14 |
| PCT | PCTPvP_Burst, PCTPvP_OneButtonMotifs | 10 | 26 |

See [pvp-inventory.json](pvp-inventory.json) for the actual action names, source locations, selectable setting keys and toggle names for each row.

## Bridge implementation and requested carry-over

Hotbar Bridge now has explicit PvP storage handling and separate mode-specific regular/controller maps, routes, baselines and restore backups. Entering/leaving PvP invalidates previews and preserves the live-sync preference. Old external-edit IPC keeps its PvE meaning and is rejected in PvP; a future Accord PvP installer will require explicit mode-aware IPC.

The user clarified that utility assignments **must carry across**: selected whole regular bars (initially 3-10), including natively unshared bars, plus every Bridge-shared pair's regular and controller endpoints. These are synchronized in both directions, including the inactive mode's saved storage. Whole unshared bars remain per-job; shared assignments also propagate across jobs. First alignment is previewed; subsequent changes use saved baselines, and ambiguous edits pause for a choice. Job-specific PvP controls outside this selection remain separate.

The native wrapper resolves saved PvP indexes through `GetPvPSavedHotbarIndexForClassJobId`, uses PvP-aware saves, and verifies the active mode before writes. It preserves raw command type/ID. Mode-tagged backups reject cross-mode restores while preserving legacy PvE backup support. Compilation and simulated tests are complete; in-game native storage acceptance remains pending. See the parent repository's `hotbar-sync/README.md` for usage and storage details.

## Accord implementation boundary

[Detailed mappings](pvp-variant-mappings.md) cover all four variants for all 21 jobs. The [ledger](pvp-action-ledger.json) contains 504 official action entries, including common actions, role choices, native transformations and pet effects. `tools/validate-pvp-mappings.py` checks all referenced presets, source hashes, action coverage and semantic access across all 16 variant combinations. It does not claim to simulate the game's rotation or validate executable layout bindings.

The review identifies exact initiation/continuation splits and two recipient gaps. These are implementation tasks, not open questions requiring encounter observations. No PvP rotation or hands-free AutoActions code has been changed. The next Accord implementation needs its separate PvP catalog/profile, native combo binding resolution, the documented narrow settings additions and mode-aware Bridge integration. Existing PvE preparation and global custom-button settings must remain intact.
