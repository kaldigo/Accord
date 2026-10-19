Manual fallback preview (0.2.15, 2026-10-19): three-bar manual PvE layouts can be saved before logout and exact previous Accord layouts restored on login when compatible Wrath is available. See [manual fallback](manual-fallback.md). Local build/data/recovery checks are recorded there; native acceptance remains pending.

Identity decision (2026-10-18): the engine replaces stock Wrath using InternalName and assembly `WrathCombo`, display name `Wrath Combo`. This supersedes the provisional `WrathFork` installation identity below. Accord remains separate. Preserve existing `WrathCombo.*` IPC and the private `WrathFork.Settings.*` v2 API/contract identity for transaction compatibility. Current legacy fork settings take precedence over an older stock configuration in the one-time backed-up migration. Never allow legacy WrathFork to remain installed alongside the restored identity. See [identity migration](identity-migration.md).

# Accord: premise and direction

Current 0.2.14 local preview: compact PvE/PvP layouts no longer require Hotbar Bridge. Accord uses its own regular/controller layouts directly; Sprint adapts across all active regular/cross bars, including utility bars, in static and live modes. See [Bridge retirement and validation](bridge-retirement.md). Earlier Bridge requirements below are historical. Native acceptance remains pending.

Current implementation (2026-10-18): PvE choices are shared across PvE/PvP, with intent selectors, per-bar keyboard compaction and mode-aware shared Sprint. See [implementation and verification](job-research/pvp-implementation.md). Historical separate-PvP preference and layout descriptions below are superseded. Native acceptance remains pending.

Accord is to become a separate setup and hotbar plugin, working with a maintained fork of [Wrath Combo](https://github.com/PunishXIV/WrathCombo). The fork provides rotation functionality, granular settings and a configuration API. Accord owns the intent model, reviewed job mappings and variant choices, and applies the required settings through that API. Hotbar Bridge remains a separate synchronization plugin.

## Separation decision — 2026-10-16

The fork facilitates intent handling through configurable rotation functionality. **Accord decides which settings implement each intent/variant and sets them.** The engine must not know Accord's four variants, setup pack, managed-job registry or hotbar design. Successful-action tracking, sequence continuation, targeting and action selection remain engine responsibilities, enabled/configured through ordinary engine settings.

For example, the fork exposes automatic burst initiation and continuation after a confirmed manual initiation as independently configurable behaviour. Accord maps Auto burst A/M to the appropriate settings for each job. No `SetAccordVariant` engine API or Accord-aware rotation checks are intended.

This supersedes the earlier single-plugin architecture and the requirement to replace the fork's settings interface with Accord's UI. Accord retains its simplified UI; the fork's own interface is separate. The fork preserves the upstream UI and functionality, including auto rotation and its status icon. Only Accord integration and separate identity justify deviations; earlier combined-plugin removals are superseded. Final fork naming and repository allocation remain undecided.

The source now builds separate Accord and provisional WrathFork plugins (0.2.2). Accord configures the fork through versioned IPC. Local build/schema tests pass; native acceptance remains pending. Older 0.1.0 preview artifacts are the combined prototype. See [code separation assessment](separation.md) for actual dependencies, API requirements and migration order. Existing gameplay, targeting, variant and static/live hotbar requirements remain in force.

## Current upstream baseline

Updated on 2026-10-16 to Wrath **1.0.4.27**, upstream commit `5365eb1edc13654be945a624b2311a8b8541a599`. The upstream branch was merged with history preserved; its ECommons and PunishLib pins were adopted. Accord and WrathFork previews are now 0.2.2, with the setup pack and API compatibility checks pinned to 1.0.4.27. Both builds and 5,901 checks pass. Native acceptance remains pending.

## Starting point

Created 2026-10-07 from upstream commit `5d7652004b44ff25a0e9ddc34b868e487babda2c` (Fix SCH dots). The original checkout preserved upstream unchanged. The 2026-10-08 implementation adds an isolated Accord 0.1.0 development build, its setup interface and targeted intent-routing changes. See [implementation and validation](implementation.md) for precise coverage. It has been compiled and tested locally, not installed, published or validated in game. Existing HotbarTools releases and live game configuration have not been changed.

The repository is https://github.com/kaldigo/Accord. `origin` points there; `upstream` points to PunishXIV/WrathCombo. Preserve upstream history so fixes and changes can be reviewed and merged. The sibling HotbarTools repository remains independent.

## Product premise

The user configures their jobs through reviewed setups rather than Wrath's full settings UI. Accord provides global variant controls, a saved active-class selection and explicit selected-class hotbar setup, and optional live hotbar updates. It must package versioned settings and matching layouts. Live mode keeps selected layouts matched to behaviour; static mode deliberately leaves hotbars unchanged when variants change, so restoring a required manual button may require using a setup button.

Accord exposes only its simplified setup UI. The fork's configuration interface is a separate plugin concern, not an Advanced page inside Accord. Preserve engine configuration initialization independently of either UI. Read-only diagnostics, effective-setting exports and reset to reviewed settings remain appropriate.

Preserve Wrath's rotation engine, action hooks, targeting machinery, configuration semantics and timing behaviour. Do not extract a new library, rewrite the engine, simplify job logic or reorganize upstream files merely to make the code cleaner. Add our setup layer on top and change engine behaviour only for a concrete, documented requirement. Keep changes small and easy to compare with upstream.

## Agreed setup interface and hotbar modes

Finalised 2026-10-07, with four variant categories agreed 2026-10-08. These are the agreed requirements; current implementation and validation status is recorded separately. Per-job action ownership follows the automation philosophy below.

- Provide a saved **Active classes** selection with per-job checkboxes and Select all/Select none. **Set selected class hotbars** replaces current/all setup buttons. Base classes follow their job. New installations and upgrades without an explicit saved selection start with none selected; prepared-job history never opts classes in. Deselecting stops future Accord writes without undoing saved fork settings or hotbars. Shared engine defaults remain global; this selection does not disable the fork.
- Provide one global set of variant checkboxes. The checkboxes and system information controls represent the same choices, with no separate pending selection. Preserve those choices across class/job changes and apply their behaviour to the incoming job.
- **Clear other actions** uses the existing cleanup operation's scope: keep only the designed layout across the affected job-specific regular and cross hotbars, clearing intended empty spaces and other assignments. Preserve game-shared bars and Hotbar Bridge's configured shared slots using its actual map. This option applies to explicit setup and live hotbar updates. Retain the cleanup operation's protection checks and backups; do not guess shared positions when Bridge's map is unavailable.
- With **live mode disabled (static hotbars)**, changing a variant updates rotation behaviour only. Hotbars change only through an explicit setup operation. Switching a variant off can leave its manual action absent until the user sets the hotbars again; this is an accepted consequence of static mode.
- **Enable live mode** applies layouts to selected classes. Variant changes update settings and layouts for the selected classes. Class swaps refresh only an incoming selected class; unselected classes receive no Accord setup. Changing the selection applies settings immediately and hotbars only in live mode. Static mode retains existing hotbars. Variant system information entries are hidden on unselected classes.
- Disabling live mode leaves the current hotbars in place.
- Accord provides one individually clickable system information entry per variant category, each showing its icon and A/M. These control Accord's variants. The separate Wrath fork retains its own upstream auto-status icon, click controls and status text; the earlier combined-plugin instruction to replace it is superseded.
- The names and number of variants may change. Define the new setup controls and mode handling independently of the current mitigation/burst/job-mechanics trio, with stable variant identifiers and associated labels, icons and job-specific effects.

## Agreed automation philosophy: buttons express intent

Finalised 2026-10-07. **Buttons express intent; automation carries out that intent.** Do not make every button do everything. For each proposed integration, ask which intent the action fulfils and whether the user has expressed or delegated that intent.

The manual-burst baseline is the reference example: ST/AoE maintains the chosen damage rotation; the burst button means "I now want to burst"; subsequent ST/AoE presses carry out the appropriate follow-through. With automatic burst enabled, the user additionally delegates the initiation decision. Use this distinction between initiation and follow-through when reviewing other variants; a disabled variant does not remove all underlying automation.

Clarified 2026-10-08: preserve the surrounding rotation logic wherever possible. The main change is whether an initiating skill is selected through the automated intent button or has its own manual button. The manual initiator signals intent to start the sequence; its remaining handling belongs to the appropriate automation button. Manual burst must not require repeatedly visiting the burst button for each subsequent buff/resource action merely because upstream exposes a chained selector. Reuse existing status, cooldown, resource and follow-up logic; add only the narrow initiation/handoff handling needed. A press still performs at most one game action, and an unavailable/failed initiator must not be mistaken for successful activation. This does not authorize indefinite queued requests, automatic action execution without user presses, indiscriminate skill use, or a new rotation engine.

### Agreed four variants

Protection and support follow the same routing contract as burst and class mechanics. With a category automatic, its reviewed selector/actions run through the appropriate ST/AoE intent path. With it manual, its own button may initiate a sequence or run an automation selector. The mitigation button is an example of this general rule, not a limit on the actions covered by protection or support. Preserve action-specific usefulness, targeting and timing checks when moving the point of entry.

- **Auto burst:** the automation initiates the offensive commitment. With it off, the player uses the burst initiator and resumes ST/AoE for the remaining burst handling.
- **Auto protection:** reviewed protective actions are selected through the appropriate automation paths; manual protection access remains where deliberate timing is useful.
- **Auto support:** reviewed allied support is selected through appropriate automation paths, with existing targeting and usefulness rules preserved unless a specific change is required.
- **Auto class mechanics:** distinctive class systems are initiated/managed through the automation paths, such as Dancer's dances, AST cards and Bard songs. With it off, the necessary manual initiator/control remains while the appropriate automation handles the accepted sequence. Not every job has an additional effect: Gunbreaker does not acquire one merely to populate this category.

Each initiating action needs one explicit variant owner. Class systems may interact with burst, so the per-job mapping must specify the boundary without competing switches or accidentally disabling follow-through. AST cards belong to class mechanics, including healing/protection cards; their effects do not make them independently owned by support/protection. The precise dance/burst boundary is a mapping task, not permission to rewrite Dancer's existing sequence. Preserve global choices across jobs even when a category has no effect. Keep the variant definitions extensible despite these four agreed initial categories.

ST healing expresses intent to help the resolved ally, choosing an appropriate healing or support action. AoE healing addresses party healing needs. Mitigation expresses protection intent. Keep distinct controls where they represent meaningful timing, targeting or tactical decisions. These are functional intentions, not a requirement to impose identical buttons or sequences on every job.

### Integration and retargeting

- Review functionality stranded behind standalone selectors or only one rotation path. Reuse it in the appropriate existing intent path where that avoids an implementation-driven extra button. Do not merge distinct player decisions merely to reduce button count.
- Review healer job support that currently requires pressing a healing button even when its useful conditions arise during damage play. It may belong in ST/AoE damage when consistent with that intent or an explicitly enabled automation variant. This is not blanket permission for damage buttons to activate every heal or defensive. Decide action by action, including the conditions and variant that delegate its use.
- Retargeting must participate in action selection, not only redirect the action after selection. Resolve the intended ally before evaluating recipient-dependent HP, role, status and range conditions. Eligibility and execution must refer to the same resolved recipient; selecting an enemy must not suppress an otherwise eligible allied action solely because the enemy is the directly selected target. Preserve the offensive target when applying allied support.
- The user's AST example is a specific investigation case: the ST-healing button remains the normal heal with an enemy selected, but offers a support card when the tank is selected directly. If retargeting resolves that same eligible tank and the card's conditions are met, the card should also be offered through the retargeted ST-healing path. This concerns eligibility as well as execution; it does not establish that all cards should always target the tank or that the cause has already been verified in game.
- Preserve deliberate manual access where needed. Check ST and AoE coverage independently, along with healing/mitigation routes, prepull use, targeting, emergencies and timing, before removing a button.

### Settings, resources and layouts

The resource-holding review did not establish a need for a general spending-logic rewrite. Retain meaningful reserves, burst alignment, healing thresholds and content restrictions; review their configured values and enabled features first. Earlier suggestions to change emergency overrides or overcap logic were investigation options, not approved implementation requirements. A specific demonstrated gap can still justify a narrowly reviewed change.

The existing reviewed settings and hotbars are a useful starting point, not a frozen final design. Review and adjust them where necessary to express intent more clearly and support the agreed integrations and retargeting. Earlier decisions to retain buttons may reflect upstream integration limitations; reassess those reasons against actual coverage. Preserve established placement conventions and shared utilities unless a specific reviewed change calls for an adjustment. Final variant groupings should follow the intended behaviours, rather than constrain the review to the current three switches.

This section records the product direction. Consult [implementation and validation](implementation.md) for implemented changes and the boundary between local testing and in-game validation.

## Setup and automation constraints

The [resolved source audit](job-research/source-audit.md), dated 2026-10-08, supersedes the speculative implementation questions in the earlier [intent proposal](job-research/intent-proposal.md). It establishes existing phase continuation and support routes, records actual setup/access limitations, and separates the AST/MCH live observations from source-answerable questions. The four categories and manual-initiation contract above supersede the audit's earlier recommendation to defer those choices. Preserve its factual coverage findings; do not implement the original verification list as if every entry were missing functionality or authorization to remove controls.

The [job research reference](job-research/README.md) records the 2026-10-07 gameplay/toolkit pass and a separate Wrath/setup review for all 21 supported combat jobs, with deeper AST targeting/card and MCH level-90 investigations. It includes utility and preparation outside damage rotations. The [completed intent mapping](job-research/implemented-intents.md) records the resulting implementation decisions and supersedes the exploratory candidate lists. Observe the source-version and validation limits; native acceptance is tracked separately in [distribution](distribution.md).

- The existing reviewed source uses Auto mitigation, Auto burst and Auto job mechanics. These are migration evidence, not a fixed final set of Accord variants. Variant effects are job-specific; a switch may legitimately make no additional change.
- All-on means the maximum reviewed automation through the user's rotation presses, not hands-free action execution. Preserve manual functions that automation does not cover, including deliberate movement, targeting, emergency timing and situational utilities.
- Keep ST and AoE on separate buttons. Preserve approved controller positions, role consistency, ST/AoE/mobility positions and general tank/healer reserves. Optional keyboard compaction uses the selected variant and must preserve every remaining function.
- Use the existing reviewed layouts as the starting point for the intent-based review above rather than inventing a uniform eight-button claim. Approved job exceptions and the one-page twelve-slot noncombat layouts remain part of the source requirements. Do not add Sprint, Limit Break, Teleport or Return to job budgets; shared utility is separate.
- Preserve current/all hotbar application, macros, active-job status, the managed-job registry, backups, validation checks and restores behind the simplified interface. Do not require the previous separate preview/apply UI for each live transition. Class changes retain global choices, but status must reflect actual application and must not report a failed or pending transition as completed.
- Accord requests configuration changes through the fork's API. The fork validates, applies and persists its own settings and refreshes its caches. Accord persists only its own choices, setup metadata and hotbar state. Support base targeting/sliders as well as preset membership, without disabling the plugin or replacing a live external configuration file.
- Variant switches remain feature-only after preparation: they must not silently reset targeting, sliders or user choices exposed by Accord. Versioned base-setting migrations are separate and explicit.
- Current healer priority is eligible focus, UI/party-list mouseover, soft target, selected friendly target, selected target's friendly target, living tank, self. Exclude world/nameplate mouseover. A valid focus wins even at full HP; a fallback tank is not necessarily the main tank. Preserve documented resolver exceptions unless specifically changed.

## Added functionality

Review abilities that currently need a separate button because their automation exists only as a standalone selector or on one rotation path. Reuse the existing proven logic where practical, with a narrow integration change; do not assume returned actions are recursively processed by another replacement.

For each change, document the actual limitation, affected job/actions, ST and AoE behaviour, targeting and timing conditions, manual fallback, toggle ownership and hotbar effect. New automation must be optional. With the Accord addition disabled, preserve the upstream behaviour of the affected code path. Do not remove a manual control merely because some conditional automation exists.

Machinist's missing Barrel Stabilizer toggle was a setup omission, not an upstream limitation. Fixing such configuration mistakes does not justify changing rotation logic. Distinguish configuration omissions, unsupported integration, and intentional manual decisions in every review.

## Repository boundaries and upstream maintenance

HotbarTools contains Job Setup, Hotbar Bridge, compiled presets and tests. Use its reviewed variants and burst-coverage audit as migration evidence; do not copy personal Wrath configuration, exports, character data or secrets into this public fork. The private sibling `wrath/` working plans are reference material, not files to publish wholesale.

Keep Hotbar Bridge independent. Its current utility defaults map regular bar 10 and bar 9 slots 1–4 to cross hotbar 8; the four shared slots on the job crossbar remain protected. Accord should use Bridge's actual map rather than assume defaults.

Record each upstream merge and local engine patch. Review settings/preset-ID migrations as well as source conflicts, test supported switch combinations and hotbar consistency, and verify affected behaviour in game before declaring it validated. Do not publish upstream merges automatically merely because they compile. Preserve upstream licensing and dependency notices and clearly identify Accord as an independent fork.

## Historical combined-plugin implementation sequence

1. Establish and document this untouched upstream baseline.
2. Inventory startup/configuration/UI dependencies and the exact public preset data to import.
3. Add the Accord setup layer and validated in-memory configuration application without refactoring rotations.
4. Replace user-facing settings entry points with Accord's UI; preserve required initialization outside UI code.
5. Establish distinct plugin identity, configuration migration, updater feed and protection against loading alongside Wrath before distributing Accord.
6. Add missing automation one job/behaviour at a time, with disabled-path comparison and in-game checks.

The sequence above describes the combined prototype. The separation assessment now governs architectural work. A successful local build is not a validated release.


## Wrath Auto-Mode integration — 2026-10-16

For explicitly selected combat jobs, Accord enables the per-rotation Auto-Mode settings for its enabled reviewed ST/AoE damage and healing rotations, leaving unused alternative rotations off. This mapping is applied through the settings API on setup, selection, variants and selected-job migration, including already prepared jobs. It does not change the engine's master auto-rotation toggle or its global auto-rotation preferences. The fork's existing runner is restored under its own enable/pause/combat conditions and is blocked by an upstream-plugin conflict. Accord variants still describe intent delegation, independently of whether Wrath auto rotation is running.


## True-fork preservation clarification — 2026-10-16

Preserve Wrath functionality, settings and UI by default. Only change what Accord integration or separate plugin identity requires. Earlier combined-plugin removal decisions do not carry over. Wrath's own auto-status indicator coexists with Accord's variants, including left-click auto toggle, right-click configuration, active preset count, short text and locked/paused status. Upstream announcement/login behaviour and debug navigation are retained as well; they were not necessary removals.


## Command and interface correction

The fork retains `/wrath` and `/scombo`, upstream status/window names and inherited `WrathCombo.*`/`OnActionUsed` interfaces. Renaming these was an assistant assumption, not an agreed separation requirement. Separate internal packaging does not authorize changing Wrath's public controls. The new API for Accord is additive. Only changes needed for the requested Accord behaviour belong in Wrath.


## Class selector layout — 2026-10-16

Class selection appears first, above variants and hotbar controls. Use an always-visible, responsive multi-column grid of job icons: greyscale when unselected, full colour when selected, with job abbreviations and hover descriptions. Keep Select all/none and the selected count. Selection still defaults to none and retains the existing immediate settings/static/live behaviour. Wrath's own UI is unchanged.

Show **Set selected class hotbars** only while live mode is off. Live mode handles hotbar updates without this setup button.

Show the current class's explanation directly beneath each variant, labelled with the class abbreviation. Group settings reset, hotbar restore, local troubleshooting export and legacy settings import under **Maintenance**. Explain each operation's scope beside its clearly labelled control, including that hotbar restore disables live mode and restores the previous operation's layout rather than necessarily the pre-Accord layout.


## Distribution name � 2026-10-16

The user chose **Wrathful Combo** as the fork display name to distinguish it from upstream. Keep the existing internal identity `WrathFork`, configuration, `/wrath` and inherited IPC contracts. Publish Accord and Wrathful Combo alongside Hotbar Bridge in the existing HotbarTools custom repository feed; source repositories remain separate.

## Keyboard compaction - 2026-10-17

Compact keyboard layouts by default for both explicit setup and live updates. Reuse the established compaction rules: ST, AoE, actual mobility and tank/healer general reserves stay fixed; remaining actions fill available keyboard positions in order within their original bar. Never move an action between keyboard bars to fill a gap. Controller function positions and shared controls do not move. Persist per-job Bridge routes with the layout transaction and restore them with backups. Hotbar Bridge must be loaded so moved keyboard actions remain paired with their fixed controller controls. Without general cleanup, clear recognised moved/retired design actions from vacated keyboard slots but preserve unrelated assignments. Explicitly allowed extra job slots remain outside the original twelve-slot compaction region.

## Live hotbar drift checks - 2026-10-17

While live mode is enabled, inspect the current selected class once per second using the same layout and cleanup planner as application. Compare assigned command IDs and Bridge routes, not transient Wrath icons. The check is read-only: no backups, settings writes, Bridge external-edit resets or hotbar writes unless a correction is needed. Corrections use the existing backed-up transaction without reapplying rotation settings. Clear other actions controls removal of new leveling actions outside the design; shared game bars and Bridge slots remain protected. Static mode and unselected classes are not periodically repaired. Loading and unavailable dependencies defer correction until the next check.

## Future feature: editable hotbar locations - 2026-10-17

Requested: a simple way to choose hotbar locations for actions/functions in Accord. Record this for later; do not implement it as part of the current PvP investigation. Specific UI and placement rules are not yet agreed.

## PvP scope - 2026-10-17

Gather the job/action/settings information needed for PvP layouts and all four intent variants. Hotbar Bridge must support PvP, with PvE/PvP state and storage kept separate. Do not change Wrath hands-free auto rotation in PvP. Preserve upstream behaviour except for necessary intent handling. Evidence and implementation boundaries are recorded in [PvP research](job-research/pvp.md).

### PvP detailed mapping and Bridge carry-over clarification (2026-10-17)

The detailed 21-job PvP variant review and 504-entry action ledger are in docs/job-research/pvp-variant-mappings.md and pvp-action-ledger.json. These specify intended controls/settings and exact continuation gaps; they do not yet install Accord PvP layouts. The hands-free PvP restriction is unchanged. Bridge now supports PvP storage locally, with selected regular bars (initially 3-10) and all Bridge-shared endpoints linked across PvE/PvP. Non-shared whole bars retain per-job values; other job controls stay mode-specific. Protect these linked/shared positions when allocating future PvP layouts. Native validation remains pending.

## PvP implementation - 2026-10-17

The 21 PvP layouts and four-variant settings are now implemented locally in the separate Accord plugin. See [the implementation record](job-research/pvp-implementation.md) for exact engine splits, retained controls, protected shared/linked slots, mode-specific backups and validation. PvP selection starts empty; settings/layout choices are separate from PvE. Hands-free AutoActions and runner behaviour are unchanged. Included in the 0.2.8 test release with Hotbar Bridge 0.2.11. Native in-game acceptance remains pending.
