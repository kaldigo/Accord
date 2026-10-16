# Complete upstream difference review — 2026-10-16

Baseline: merged Wrath 1.0.4.27, `5365eb1edc13654be945a624b2311a8b8541a599` (`upstream/main`). Review includes committed changes and current working changes, rather than only the last turn. No upstream file deletion or dependency-pin difference remains. New documentation/research/setup data are additions, not replacements for upstream code.

## Findings corrected in this pass

1. **The fork package version suppressed upstream change notices.** `MajorChangesWindow` compared package 0.2.2 against upstream's 1.0.3.4 threshold, so restoring its registration alone was insufficient. Added an inherited-engine version shared by the notice, its default dismissal version and the settings API. Dismissal still follows upstream rules, but uses the upstream version domain. Regression checks cover notice eligibility, fresh defaults and agreement with the setup baseline.
2. **Unnecessary opener rewrites.** Restored upstream opener click handling, tooltip and status/visibility implementation, retaining upstream DTR identity. The subsequent user-request review restored the complete upstream command registration including `/scombo`.
3. **Review noise.** Removed BOM/EOF-only changes and incidental whitespace in already modified upstream files. Conflict-check source now matches upstream exactly.
4. **Contradictory instructions.** Removed stale product wording suggesting upstream functionality should remain suppressed; the true-fork rule governs.

## Retained engine differences and reasons

| Files/area | Necessary purpose and review result |
| --- | --- |
| AST.cs / AST_Helper.cs | Approved support-card routing on damage and resolved-recipient card eligibility. Damage routing is opt-in; the healing-card target fix is deliberately applied whenever existing heal retargeting is enabled. It is the agreed AST correction, not a default feature removal. |
| BRD_Helper.cs | Configurable song initiation and AoE Minne; confirmed manual-burst follow-through. Song initiation defaults on, AoE addition and handoff default off, preserving upstream behaviour when unconfigured. |
| DNC.cs | Separate initiation and Curing Waltz controls without removing dance continuation. Existing behaviour controls default on. |
| MCH.cs | Optional Dismantle eligibility correction and confirmed manual-burst Wildfire follow-through. Both are inactive without their respective fork options. No speculative damage/burst rewrite was retained. |
| MNK_Helper.cs / NIN_Helper.cs | Narrow cooldown-order exceptions after confirmed manual burst; ordinary automatic timing remains intact. |
| PCT.cs / PCT_Helper.cs / RPR.cs | Opt-in protection coverage on AoE. |
| SGE.cs / SGE_Helper.cs | Opt-in initial Kardia through ST/AoE; preserves existing assignment and ordinary upstream path when disabled. |
| Core/ActionReplacer.cs | Reviewed protection routing before normal combos, while retaining disabled-job/PvP, movement and penalty checks. New routing options default off. |
| CustomCombo/Functions/Action.cs | Target-aware readiness helper for the agreed ally-targeting work. Existing callers retain the original default target sentinel/readiness rules. |
| CustomCombo/Functions/Misc.cs / Data/ActionWatching.cs | Observe actual input/success and temporarily allow only the agreed continuation presets. No action queue or generic preset rewrite. |
| Core/Configuration.cs | Independent fork feature settings, configuration identity and durable API receipts; inherited-engine notice version. |
| Engine/IntentRouting.cs, IntentHandoff.cs, BurstPolicy.cs, Options.cs | Configurable runtime implementation of the agreed intent work. Bounded follow-through, job-scoped settings and upstream-compatible defaults. |
| Engine/OptionsUi.cs | Removed: the extra standalone settings page was not needed for Accord. Engine settings and their API remain. |
| Engine/SettingsApi.cs / SettingsSchema.cs and Contract/*.cs | Versioned settings discovery, validation, persistence, backup, replay and migration for Accord. Registration reads defaults but does not select Accord jobs or enable variants. Existing leased API remains separate; leases block conflicting persistent writes. |
| Engine/UpstreamVersion.cs / Window/MajorChangesWindow.cs | Keep upstream version-based behaviour working despite independent package versioning. |
| Commands.cs / Services/IPC/Provider.cs | Restored byte-for-byte to upstream: `/wrath`, `/scombo`, `WrathCombo.*` and `OnActionUsed`. The prior renaming had no Accord requirement. |
| Window/ConfigWindow.cs | Restored exactly to upstream. |
| WrathCombo.cs | Separate identity/conflict protection; API lifecycle; continuation reset on context changes; upstream DTR names. Upstream runner, indicators, UI, commands, IPC, announcements, conflict checks and login hooks are connected. Historical resets run before API exposure so later imports cannot be invalidated by delayed first setup. Unrelated disposal, callback, null-check and dead-code changes have been reverted to upstream. Only the API lifecycle, required initialization ordering, continuation reset and duplicate-engine guard remain. |
| Global.cs | Alias for source-shared engine feature identifiers. |
| WrathCombo.csproj / WrathFork.json | Independent assembly, plugin/config/update identity, source-shared contract and isolated local build outputs. Upstream manifest/source and attribution retained. |
| Directory.Build.props | Compatibility alias for the pinned upstream ECommons dependency with the available Lumina SDK; dependency checkout/pins unchanged. |

Twenty feature-default checks now explicitly protect upstream-compatible unconfigured behaviour. Four controls that restrict existing behaviour default on (DNC standard/technical initiation, Curing Waltz, BRD song initiation); additions and continuation default off. This does not disable previously saved, explicitly configured fork options when Accord is absent.

## Additions outside upstream engine code

Accord.Plugin contains the separate client, reviewed setup pack, class selection, global intent variants, static/live hotbar management, protected cleanup and recoverable settings migration. These are the requested Accord functionality. Shared slot protections, static/live scope and default-none class selection remain. Contract adds IPC wire types and transaction validation; tests cover these and compiled engine compatibility. Research files and docs record agreed mappings and evidence. The README preserves upstream material below the fork introduction. Build/packaging tools and ignore rules keep previews/tests isolated and exclude private runtime exports.

## Correction: requests versus implementation assumptions

The previous review incorrectly treated my own identity choices as user requirements. `/wrathfork`, removal of `/scombo`, renamed inherited IPC and changed DTR/window names were not necessary for Accord. They are reverted. Action-request registration explicitly uses the original `WrathCombo.ActionRequest` contract because deriving it from the provisional package name would otherwise rename it silently. Branding in the runtime window, welcome fallback and PunishLib registration also follows upstream again.

The retained work maps to the actual requests:

| User request | Retained change |
| --- | --- |
| “rotation functionality and settings in the fork” and “Accord control those settings through the API” | Granular engine settings, persistent settings API, validation/recovery, and separate Accord client. The extra settings endpoint is additive; no inherited endpoint is replaced. |
| “buttons are the intent and the automation follows through” | Confirmed initiation tracking, bounded burst/TCJ follow-through, and narrow job-specific timing exceptions. |
| Auto burst, protection, support and class-mechanic variants, with ST/AoE carrying the accepted automation | Optional routing/initiator controls in the jobs and protection hook. These are implementation choices serving that request, not individually user-specified algorithms. Unconfigured defaults preserve upstream paths. |
| AST card eligibility should account for retargeting even with an enemy selected | Resolved-recipient readiness and execution; optional support-card use through damage paths. |
| Select which classes Accord manages, default none; configure Auto-Mode | Client-only selection and reviewed setting/AutoActions mapping. No master-auto activation by Accord. |
| Keep Wrath a true fork and retain its auto icon and `/wrath` | Upstream command, UI, indicators, runner, announcements and existing IPC retained. No separate naming requirement is inferred for these. |

Internal `WrathFork` package/configuration naming remains a provisional implementation choice for separate packaging and protecting existing configuration during development—not a user-approved final product name. It does not justify changing commands or integration contracts. The inherited-engine version helper preserves upstream notice behaviour under that provisional package version. Local dependency compatibility, build outputs and tests support this implementation. Upstream callback cleanup is unchanged; conflict guards prevent duplicate engine hooks.

## Validation and limits

Both Release builds pass; 5,935 assertions pass, including all 512 job/variant combinations, feature defaults, migration isolation, recovery, and upstream notice versioning. Engine build retains 72 existing warnings; client has none. `git diff --check` passes. Local packages rebuilt; nothing installed or published. The earlier blanket conclusion was unreliable because it accepted identity assumptions. The request-based correction above supersedes it. No additional removal was identified after restoring those interfaces; native compatibility is not asserted. Native DTR/UI, action execution, targeting and third-party IPC compatibility remain in-game acceptance items; source review and builds do not prove those behaviours.

## File inventory

Every changed tracked path is listed below, plus new untracked audit/version files. Engine files were examined at diff/function level; newly added client/data/test/document files are classified by their Accord role above.

- `.gitignore`
- `AGENTS.md`
- `Accord.Plugin/Accord.csproj`
- `Accord.Plugin/Accord.json`
- `Accord.Plugin/AccordSettings.cs`
- `Accord.Plugin/AccordUi.cs`
- `Accord.Plugin/EngineClient.cs`
- `Accord.Plugin/HotbarSetup.cs`
- `Accord.Plugin/HotbarTransaction.cs`
- `Accord.Plugin/Hotbars/ActionCatalog.cs`
- `Accord.Plugin/Hotbars/HotbarCleanup.cs`
- `Accord.Plugin/Hotbars/Models.cs`
- `Accord.Plugin/Hotbars/NativeHotbars.cs`
- `Accord.Plugin/JobIntents.cs`
- `Accord.Plugin/MigrationPreparation.cs`
- `Accord.Plugin/Plugin.cs`
- `Accord.Plugin/SetupController.cs`
- `Accord.Plugin/SetupPack.cs`
- `Accord.Plugin/presets.json`
- `Contract/Features.cs`
- `Contract/SettingsMigration.cs`
- `Contract/SettingsPatch.cs`
- `Contract/SettingsTransaction.cs`
- `Directory.Build.props`
- `README.md`
- `WrathCombo.slnx`
- `WrathCombo/Combos/PvE/AST/AST.cs`
- `WrathCombo/Combos/PvE/AST/AST_Helper.cs`
- `WrathCombo/Combos/PvE/BRD/BRD_Helper.cs`
- `WrathCombo/Combos/PvE/DNC/DNC.cs`
- `WrathCombo/Combos/PvE/MCH/MCH.cs`
- `WrathCombo/Combos/PvE/MNK/MNK_Helper.cs`
- `WrathCombo/Combos/PvE/NIN/NIN_Helper.cs`
- `WrathCombo/Combos/PvE/PCT/PCT.cs`
- `WrathCombo/Combos/PvE/PCT/PCT_Helper.cs`
- `WrathCombo/Combos/PvE/RPR/RPR.cs`
- `WrathCombo/Combos/PvE/SGE/SGE.cs`
- `WrathCombo/Combos/PvE/SGE/SGE_Helper.cs`
- `WrathCombo/Commands.cs`
- `WrathCombo/Core/ActionReplacer.cs`
- `WrathCombo/Core/Configuration.cs`
- `WrathCombo/CustomCombo/Functions/Action.cs`
- `WrathCombo/CustomCombo/Functions/Misc.cs`
- `WrathCombo/Data/ActionWatching.cs`
- `WrathCombo/Engine/BurstPolicy.cs`
- `WrathCombo/Engine/IntentHandoff.cs`
- `WrathCombo/Engine/IntentRouting.cs`
- `WrathCombo/Engine/Options.cs`
- `WrathCombo/Engine/OptionsUi.cs`
- `WrathCombo/Engine/SettingsApi.cs`
- `WrathCombo/Engine/SettingsSchema.cs`
- `WrathCombo/Engine/UpstreamVersion.cs`
- `WrathCombo/Global.cs`
- `WrathCombo/Services/IPC/Provider.cs`
- `WrathCombo/Window/ConfigWindow.cs`
- `WrathCombo/Window/MajorChangesWindow.cs`
- `WrathCombo/WrathCombo.cs`
- `WrathCombo/WrathCombo.csproj`
- `WrathCombo/WrathFork.json`
- `docs/ACCORD.md`
- `docs/distribution.md`
- `docs/fork-restoration-audit.md`
- `docs/implementation.md`
- `docs/job-research/README.md`
- `docs/job-research/implemented-intents.md`
- `docs/job-research/intent-proposal.md`
- `docs/job-research/investigations.md`
- `docs/job-research/jobs.md`
- `docs/job-research/runtime-inventory.json`
- `docs/job-research/source-audit.md`
- `docs/job-research/sources-and-actions.json`
- `docs/separation.md`
- `docs/settings-api.md`
- `docs/upstream-difference-audit.md`
- `tests/Accord.Tests.csproj`
- `tests/EngineSchema.cs`
- `tests/Program.cs`
- `tests/SeparationTests.cs`
- `tools/package-release.ps1`


## Removal of unrelated changes

Removed the additional Engine options page and its implementation. Restored upstream null handling, DrawUI method, UI-event unsubscription, main-window cleanup and login-event cleanup position. Removed the redundant logout reset branch (the required API tick already clears continuation on logout/death/job/character changes). Restored upstream encoding and file endings on modified engine files. No upstream commands or configuration UI changes remain. Necessary engine settings and the Accord API remain. Both plugin builds and 5,935 checks pass after these removals.
