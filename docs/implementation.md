# Accord 0.1.0 development implementation

All-variant source recheck (2026-10-17): [current matrix, settings prerequisites and source findings](job-research/all-variant-audit.md) supersedes earlier layout-retention and completion claims below.

Protection correction (2026-10-16): [current routing and action-access audit](job-research/protection-routing.md) supersedes the general-selector retention described below. Implemented locally; native acceptance remains pending.

**Architecture update, 2026-10-16:** this log describes the combined prototype. The agreed direction is now a separate Accord plugin configuring a Wrath fork through its API. The fork supplies granular functionality/settings; Accord owns intent/variant mappings and sets those settings. The source split and local 0.2.0 packages are now implemented; native acceptance remains pending. See [separation assessment](separation.md); earlier completion claims apply only to the combined implementation.

2026-10-08. Based on the pinned Wrath 1.0.4.26 engine and the public HotbarTools Job Setup pack. This is a local development build, not a published or in-game-validated release.

## Setup and ownership

- Independent `Accord.dll`, manifest/internal name `Accord`, configuration and `/accord` command. Debug/Release outputs use isolated repository directories. The upstream update/MOTD entry point and writable Wrath IPC registration are not exposed. `tools/package-release.ps1` prepares a validated preview ZIP, testing-only feed and checksum. Nothing is installed or published.
- Loading refuses an already loaded Wrath Combo. If Wrath is enabled later, Accord stops action replacement and reports the conflict; disable Wrath and reload Accord before continuing. This does not unload another plugin automatically.
- Accord replaces the main configuration window. Old sidebar/tab rendering and command registration are removed; retained upstream source is not an accessible Advanced interface. Preset metadata, setting sanitisation and upstream configuration migrations remain available outside drawing the old UI. Migrations run before preparation, not on a delayed login callback that could erase a reviewed setup.
- Four global variants share one persisted selection across checkbox, macro and DTR entry. DTR uses native sword/tank/healer/star icons and A/M text. The inherited hands-free runner was initially disconnected; the later explicit auto-mode request below supersedes this.
- Current/all-class hotbar setup covers 21 combat jobs, 11 noncombat jobs and reviewed base-class inheritance. Static switches update behaviour only. Enabling live mode applies the current class; variant/class changes refresh the current layout. Failed applications report failure and allow explicit retry instead of claiming the layout caught up.
- Preparation applies versioned baseline settings inside Accord. Subsequent switches touch reviewed preset membership only. Resetting a class to reviewed settings is a separate control and creates a configuration backup. Local diagnostics are exported to Accord's private configuration directory.
- Cleanup uses the actual Bridge map, including disabled shared pairs, and preserves native shared bars. Cleanup requires Bridge loaded. Without cleanup, unused design positions retain unrelated assignments. Regular and cross destinations are validated together under Bridge's external-edit lock. Compaction/custom bar relocation is not exposed in this initial interface; the reviewed regular positions are used.
- Writes have before-state checks, character-specific backups, read-back verification and reverse rollback. Route updates participate in rollback. Restore refuses another character's backup or destinations that have since become shared, and disables live mode on success. A failed rollback keeps the recovery backup path in the error.

## Rotation changes

| Area | Implemented routing | Manual access and limits |
| --- | --- | --- |
| Burst | Explicit continuation policy: 20 seconds for MNK/DRG/BRD/NIN/DRK/RDM/GNB; 30 for MCH. Single-initiator jobs use native statuses/gauges without a timer. Confirmed effects activate handoff; follow-ups cannot extend it. | Only reviewed buff/debuff leaves and required buff parents are admitted. Manual order gaps for BRD, MNK and NIN are fixed. Rotation parents and unrelated mechanics cannot be enabled by handoff. Existing timing/resource/target checks remain. |
| WHM/SCH/AST burst | Presence of Mind, Chain Stratagem and Divination initiation follow Auto burst independently of their proc follow-ups. | Added Presence of Mind, Chain Stratagem and Divination controls in unused reviewed positions. SGE has no new burst effect. |
| DNC | Class mechanics owns Standard/Technical initiation, including Finishing Move's new Standard initiation. Completion presets stay enabled in manual mode. Auto burst owns Devilment. | Standard Step and Devilment added; Technical Step retained. Active dances continue through damage presses. Protection cannot interrupt dance steps. Existing flourish/proc/finisher handling is retained. |
| AST cards | Class mechanics controls drawing. Already-held cards are follow-through of a draw, including a manual draw. Eligible Bole/Arrow/Ewer/Spire are considered in both advanced damage paths using the existing ST-heal priority, HP, role and weave checks. | Astral Draw added. ST-healing card readiness checks and execution use the same resolved recipient, including range; enemy selection remains intact. With retargeting off, native target checks remain in use. This does not route the whole healing selector into damage. |
| BRD songs | Class mechanics delegates starting a song cycle. An active song cycle continues through ST/AoE, including when initiation is manual. | One-button song initiation is present. Mode M does not cancel an already running song cycle. |
| Protection | Tank automatic counterparts include conditional party defenses. Feint/Addle cover AoE, as do RDM Barrier, RPR Crest and PCT Tempera. Ranged defenses cover ST/AoE; Dismantle's absent-debuff eligibility is corrected for prepared Accord jobs. Baseline delegated defenses now obey the protection variant. | Manual selectors retain deliberate entry conditions; they are not dumped into damage. Existing content/HP/threat/resource checks remain. Invulnerability, targeted protection, knockback timing and channeling retain dedicated controls. |
| Support | BRD Paean/Minne, SMN Rekindle/Lux Solaris, conditional DNC Curing Waltz, and initial SGE Kardia assignment have support ownership on ST/AoE. Kardia captures one healing-stack recipient for readiness/range/execution and never moves an existing assignment. | Manual access retained, with Lux Solaris added. AST cards belong to mechanics. Healing selectors are distinct intents and continue in M; damage does not run the whole healing selector. |
| MCH handoff | The AoE path can perform the existing manual selector's Wildfire follow-through after confirmed burst initiation and Hypercharge. Routine automatic heat spending does not itself signal burst. | A manual Heat Blast/Blazing Shot input that resolves to Hypercharge is remembered only for matching action confirmation within two seconds; an input alone never enables burst. Barrel Stabilizer/Wildfire confirmation also hands off. Heat Blast remains available for deliberate initiation. This does not establish or claim a fix for the unsynced BLM clear-time comparison. |
| Other mechanics | Reviewed MNK/DRG/NIN/SAM/RPR/VPR/SCH mechanic deltas and accepted-phase continuation remain. Meisui continues after confirmed TCJ Suiton/Huton with manual initiation. | No additional mechanic effect is invented for GNB or jobs whose delta is empty. Movement, recovery, prepull and noncombat utility are retained. |

The embedded source pack is kept recognisable. `SetupPack` applies ownership/layout adaptations; historical three-variant descriptions are migration evidence. The UI uses new per-job descriptions. Cover, Benediction and Lux Solaris join the added manual initiators in unused positions. See the [complete 21-job mapping and selector decisions](job-research/implemented-intents.md).

## Commands

`/accord` opens setup. `/accord current` and `/accord all` apply hotbars. `/accord live on|off|toggle` changes hotbar mode. `/accord burst|protection|support|mechanics on|off|toggle` changes the shared selection. `/accord restore` restores the latest recorded hotbar backup for the current character.

## Validation

- `dotnet build WrathCombo/WrathCombo.csproj -c Release`: successful with the same 71 warnings as the untouched upstream build; zero errors.
- `dotnet run --project tests/Accord.Tests.csproj -c Release`: 4,766 passing assertions across all 512 combinations; four-category descriptions for all 21 combat jobs; explicit ownership and switch-order independence; preservation of unrelated membership; layout budgets, base classes and manual initiators; per-job bounded/confirmed handoff; shared-slot cleanup; and injected partial-write/route/rollback failures. The final packaging run passed these checks and built with zero errors and 71 existing warnings.
- Distribution validates identity/API, dependencies and absence of an upstream plugin, nested archive or private configuration, then includes licences and SHA-256. Output isolation fixes an inherited build-directory artifact that otherwise included an old Wrath ZIP.
- Added action names were checked against the previously collected official job action inventory.
- No installed plugin, live configuration or hotbar was modified by this implementation session.

Remaining validation requires the game: actual ImGui/DTR rendering; native hotbar I/O and Bridge refresh after class/variant changes; AST readiness/execution with an enemy selected and an eligible resolved ally; and confirmed manual burst continuation under real action timing, particularly MCH. Unit tests and compilation cannot establish these native outcomes. There is no claim of measured damage improvement.

## Upstream maintenance

Local runtime changes are in `WrathCombo/Accord/` plus narrow integration points in startup, action readiness/replacement/watching, preset evaluation and the documented affected job paths. The public hotbar model/catalog/cleanup/native helpers were copied from the sibling HotbarTools `common/` directory on 2026-10-08; they are owned by this independent checkout, not linked to the parent repository at build time. Preserve both upstream and imported-source provenance when updating them.


## Separation validation — 2026-10-16

WrathFork and independent Accord Release builds succeed against the installed Dalamud API 15 SDK. The fork emits 72 existing-style deprecated API/nullability/unused-member warnings; Accord emits none. `tools/package-release.ps1` succeeds and creates both ZIPs, distinct testing feeds and checksums under `artifacts/v0.2.0-preview/`.

5,851 assertions/check cases pass: previous mapping/handoff/hotbar recovery cases, new granular feature ownership and rejected-patch preservation checks, and 512 setup/variant cases against actual compiled fork setting registrations with configuration serialization round trips. The offline schema host excludes unsupported BST, suppresses persistence and never constructs live plugin hooks. Package validation checks required dependencies, separate identities and absence of cross-plugin engine assemblies or private configuration.

Source checks confirm no engine references to Accord variants/prepared-job state and no engine assembly/configuration references in the client. The parent repository and dependency submodule checkouts remain unchanged. No game execution, IPC transport integration, live migration, installation or publication performed. See the current limitations and native acceptance list in separation.md and distribution.md.


## Upstream 1.0.4.27 merge - 2026-10-16

Merged upstream/main at `5365eb1edc13654be945a624b2311a8b8541a599` (tag 1.0.4.27). The preceding separation was checkpointed as `b831b3b26`; upstream ancestry and our engine patches are preserved. ECommons follows upstream's `26d30b9ec14c3d3c18e335a9e4c7a5421dd134db`, PunishLib `96c0cefa8ebf948b3d3d71d85994c61dc8430b5c`.

Conflict review: retained fork identity/version, kept upstream auto-rotation DTR/MOTD/login-reset wiring disabled, and adopted DNC cleanup while preserving granular initiation and support switches. Automatic MCH/NIN/action-observation merges were inspected against upstream.

Included upstream changes: MCH opener removes its former target requirement check; NIN checks learned Kassatsu upgrades at synced levels; SAM FRU opener sequence fixes; WHM Temperance/Divine Caress split-action checks; ground-action readiness guard; PvP custom-action coverage and other upstream fixes/cleanup. DNC changes are source cleanup, not a new dance-continuation change. No MCH damage-improvement claim follows from this merge.

API/client/setup compatibility pins now require 1.0.4.27. Both plugin versions are 0.2.1. Packaging succeeds in artifacts/v0.2.1-preview: 5,851 checks pass including all 512 compiled-engine schema/configuration round trips. Full fork build has 79 warnings and no errors; client has no warnings/errors. Upstream-added trailing whitespace is retained to avoid unrelated source churn. No installation, live migration or publication performed; native acceptance remains pending. Earlier research sections retain their historical 1.0.4.26 context.


## Separation completion - preview 0.2.2

Implemented API v2 and personalised settings migration as described in settings-api.md. Added discovery/effective-state/events, persistent receipts and a monotonic operation sequence, pending client journals, schema bounds/conflicts/scope checks and previewed migration through the API. Detailed values and recovery paths survive migration and later class changes; unsupported fields are reported and retained in source backups. No engine settings file is written by Accord.

5,901 checks pass with real compiled-engine validation for all 512 job/variant combinations plus all 16 migration variant combinations, injected persistence/rollback failures, lost acknowledgements, stale/session/configuration guards and expired replay protection. Fork builds with existing warnings (72 on this full build); client builds without warnings. Local 0.2.2 packages are produced; no native test, installation, live migration or publication performed.


## Active-class selection — 2026-10-16

Replaced current/all setup with a persisted active-job selection, select-all/none controls, selected-class hotbar setup and selected-class settings reset. Variants and live bulk updates affect selected jobs; class transitions only prepare/update an active incoming class. Base classes inherit their job selection. DTR variant entries hide on inactive classes. `/accord selected` replaces the old current/all commands.

New installations and upgrades without a saved selection start empty. Prepared-job history never enables classes; explicit saved selections are retained. Combined imports freeze selection before marking imported personal settings prepared, and only remap intent switches for selected jobs. Imported personal engine settings remain a full explicit import. Deselection stops subsequent Accord management without undoing previously saved settings or hotbars, and does not disable the independent engine. Shared engine defaults are still global.

Validation: both plugin builds and 5,909 assertions passed, including 512 job/variant combinations, selected/base-class membership, serialization, empty selection overriding prepared history, and migration preserving inactive job features/presets. Local 0.2.2 preview artifacts rebuilt; nothing installed or published. Native UI/class-swap/hotbar acceptance remains pending.


### Empty default and Auto-Mode follow-up

Missing/null active selections now mean none on both new installs and upgrades. Explicit saved selections survive. Accord derives per-rotation AutoActions from enabled reviewed rotations using the engine's published preset IDs, including on feature-only updates and selected-job migration. The master auto toggle is unchanged. Restored the fork's existing framework runner, guarded against conflicting upstream Wrath loading.

Validation: both plugins build; 5,910 assertions pass. All 512 job/variant combinations check Auto-Mode membership against enabled presets and compiled AutoAction metadata; inactive-job migration retains its Auto-Mode settings. Native auto-rotation execution remains unverified. Nothing installed or published.


### Standalone fork restoration audit

Restored full fork UI navigation, commands under `/wrath`, job-following navigation, conflict checks, optional opener DTR, inherited leased IPC under `WrathCombo.*` and action-request initialization. Removed the prototype forced-off master-auto setting on startup. Kept the explicitly removed auto-status DTR and upstream announcements out. See [audit and validation limits](fork-restoration-audit.md). Both Release builds and 5,910 regression assertions pass; native acceptance pending.


### True-fork correction

Restored Wrath's auto-status DTR (upstream entry name), preserving upstream left/right click actions and status formatting. Also restored the unnecessarily excluded login/MOTD, major-change window and debug navigation registrations. The true-fork preservation rule supersedes old combined-plugin removals; Accord's own variants remain separate. Build/regression validation does not verify native DTR rendering or clicks.


### Complete upstream difference review

Reviewed the remaining upstream diff, including identity, lifecycle, rotation edits, optional defaults, API, packaging and added client/research data. Fixed a fork-version regression suppressing upstream change notices, restored exact opener behaviour/help wording, and removed encoding-only noise. Both builds and 5,933 assertions pass. See [full review and retained-difference inventory](upstream-difference-audit.md). Native acceptance remains pending.


### Review against actual user requests

Restored Commands.cs and IPC Provider.cs exactly from the merged upstream baseline. `/wrath`, `/scombo`, inherited endpoint/event names, DTR/window names and runtime branding now follow upstream. Explicitly retained the upstream action-request prefix despite provisional internal packaging. The added Accord settings API remains separate. Removed false claims in the audit/rules that public renaming had been required or agreed. Retained rotation/API work is mapped to the actual user requests in the corrected upstream audit. Both builds and 5,935 checks pass, including compiled command constants; native compatibility remains unverified.


### Unrelated changes removed

Removed Engine/OptionsUi.cs and restored Window/ConfigWindow.cs exactly to upstream. Reverted unrelated null-safety, unused-method removal, UI unsubscription changes and login cleanup relocation in WrathCombo.cs. Its remaining lifecycle changes only attach the settings API, order historical resets before API exposure, reset continuation on territory changes and guard against duplicate engines. Upstream encoding/file endings restored for edited engine files. Both local builds and 5,935 checks pass; nothing installed or published.

## PvP shared controller overflow correction (0.2.9)

An in-game report exposed a setup dependency missed by the initial tests: PvP profiles with more than twelve controls allocated cross hotbar 2 unconditionally. If that page was game-shared, the safety check rejected the entire transaction before any keyboard layout was written. The affected log had repeated shared-slot errors and no completed PvP backup.

New overflow pairs use available unshared controller pages. If none are available, all keyboard controls are still placed, and unpaired controls are excluded from Bridge routes. Shared controller destinations are preserved, with a visible keyboard-only status. Existing pairs remain stable. Tests cover all 336 PvP job/variant combinations without controller overflow, allocation around protected pages, and existing pair stability. Build and 82,699 assertions pass; confirmation in the game remains pending. No engine or Bridge changes are required.

## PvE controller independence and action coverage (0.2.10)

The follow-up audit found the same shared-controller rejection in PvE. Keyboard destination planning now runs independently of optional controller mirrors; protected controller slots are skipped and explained in status. Paladin's extra knockback-immunity control also conflicted with linked bars 3–10. When its original keyboard destination is protected, it uses a free, unprotected slot on bars 1–2. No shared assignment is overwritten. Missing optional controller pairs are not registered or sent to Bridge as routes.

Regression coverage uses the production destination planner for all 32 PvE profiles, their base classes and all 16 variant combinations, with shared/unshared controllers, existing/missing extra pairs, protected utility bars and cleanup on/off. Every nonempty resolved action must appear exactly once on the keyboard; all destinations must be unique and unprotected; Bridge routes must validate. Existing 336 PvP job/variant checks remain passing. The full suite passes 120,779 assertions; Accord builds without warnings or errors. This verifies layout/action retention, not in-game execution of every rotation. No engine or Bridge source changes.
