Manual fallback preview (0.2.15, 2026-10-19): three-bar manual PvE layouts can be saved before logout and exact previous Accord layouts restored on login when compatible Wrath is available. See [manual fallback](manual-fallback.md). Local build/data/recovery checks are recorded there; native acceptance remains pending.

Identity decision (2026-10-18): the engine replaces stock Wrath using InternalName and assembly `WrathCombo`, display name `Wrath Combo`. This supersedes the provisional `WrathFork` installation identity below. Accord remains separate. Preserve existing `WrathCombo.*` IPC and the private `WrathFork.Settings.*` v2 API/contract identity for transaction compatibility. Current legacy fork settings take precedence over an older stock configuration in the one-time backed-up migration. Never allow legacy WrathFork to remain installed alongside the restored identity. See [identity migration](identity-migration.md).

# Separate plugin previews

Current 0.2.14 local preview: compact PvE/PvP layouts no longer require Hotbar Bridge. Accord uses its own regular/controller layouts directly; Sprint adapts across all active regular/cross bars, including utility bars, in static and live modes. See [Bridge retirement and validation](bridge-retirement.md). Earlier Bridge requirements below are historical. Native acceptance remains pending.

The 0.2.8 preview contains two plugins. **Wrathful Combo** (internal name `WrathFork`) owns rotations, settings and the settings API. **Accord** owns reviewed mappings, variants and hotbars. Both are needed for reviewed combat setup. Disable upstream Wrath and the old combined Accord before using these builds; Hotbar Bridge stays separate.

Run `pwsh -File tools/package-release.ps1` with .NET 10 and installed Dalamud API 15 development dependencies. It builds both projects, runs mapping/contract checks and validates every job/variant against the compiled engine schema, then writes `artifacts/v0.2.8-preview/`:

- `WrathFork.zip` and `Accord.zip`, each with its own identity and dependency notices.
- `WrathFork.pluginmaster.testing.json` and `Accord.pluginmaster.testing.json`, separate testing feeds.
- `SHA256SUMS.txt`, checksums for both packages.
- `pluginmaster.json`, entries for the shared HotbarTools plugin feed.

Fork output is `WrathCombo/bin/WrathFork/Release/`; client output is `Accord.Plugin/bin/Release/`. Nothing is copied into XIVLauncher or published. Generated release URLs are proposed destinations and do not work until a release is published. Previous 0.1.0 artifacts remain combined-prototype builds.

`/wrath` opens the engine configuration; `/accord` opens setup. Accord never writes the fork configuration file. The fork backs up and persists validated API changes itself, retaining them when Accord unloads. Accord stores its own variants and hotbar recovery records. Incompatible/missing engine status blocks dependent setup/live operations; hotbar restore stays local.

A combined Accord config is backed up and prompts for separate setup. Accord choices are retained, preparation state is reset and live mode is disabled. Personalised engine settings are imported through a previewed, validated API transaction; unsupported/retired fields are listed in the local report and retained in the backup. Interrupted imports are recoverable by operation ID. This migration path has not been exercised in game. See [separation implementation](separation.md) for API coverage and remaining limitations.

## Native acceptance record

Local code/data checks are recorded in `docs/implementation.md`. Before stable promotion, record the game patch, Dalamud/Bridge versions, Accord version and outcomes for:

1. Separate Accord and WrathFork UIs and four clickable DTR A/M controls; choices persist across reload/class change.
2. Static toggles change behaviour without hotbar writes; explicit selected-class setup and live variant/class transitions match the selected layout. Unselected classes receive no job-specific Accord updates; deselection retains their saved settings/hotbars. Check selection persistence, base-class inheritance, select none and migration from prepared jobs.
3. Cleanup on/off, regular/cross hotbars, actual Bridge shared pairs including disabled shared mappings, native shared bars, backup and restore. Begin with a disposable layout and retain the backup.
4. AST with enemy selected and the same eligible resolved ally as direct selection: support-card offer and execution, range failure and manual retargeting disabled. Preserve the enemy target.
5. Manual and automatic burst on ST and AoE: especially BRD's Raging-first path, DRK, GNB, MNK, NIN, RDM and MCH. Failed input must not initiate a handoff; no repeated button presses for buffs already handed off.
6. DNC accepted dance completion, BRD accepted song cycle, NIN accepted TCJ/Meisui, and each manual class phase. Switching to M must not strand an accepted sequence.
7. Protection/support A/M, AoE additions, Kardia's initial assignment without reassignment, Curing Waltz and SMN support. No utility replaces NIN mudras or DNC steps.
8. Logout/death/territory/class resets and the Wrath conflict guard. No hands-free action execution.

Also verify engine-only operation with Accord unloaded, load order/reconnect, persistence after both reload, outside-edit detection, rejected settings updates and recovery. These native checks are pending. No damage improvement is claimed from the MCH/BLM clear-time comparison. Export diagnostics using Accord's UI when needed; exports are private and must not be uploaded with release artifacts.

See [settings API and migration contract](settings-api.md) for v2 endpoints, validation and recovery semantics.


Standalone fork acceptance additionally covers restored Auto-Rotation/PvP/Custom Actions pages, `/wrath` subcommands, saved master-auto state, optional opener indicator, conflict checks and existing Wrath IPC consumers. See [audit](fork-restoration-audit.md).


## Startup fix � 0.2.4

The live Dalamud log identified a constructor failure: status-entry initialization accessed the current player from the plugin loader thread. Entries now start hidden and their player-dependent visibility/text is updated by the existing framework update handler. The installation fix and matching icons are included in 0.2.4. In-game reload confirmation remains pending.


0.2.5 fixes icon dimensions to 512x512 (the Dalamud maximum) and gives Accord a 900x600 opening size, scaled for UI scale and clamped to the display. The window remains resizable and scrollable.


0.2.6 includes the all-job variant audit, Dancer preparation/Improvisation completion, Machinist Flamethrower delegation, Monk/Samurai protection routing, and preserved distinct-action access. Use Hotbar Bridge 0.2.10 for additional Paladin/Gladiator slots. Native acceptance is the purpose of this preview.

## Upstream settings import

On first load, Wrathful Combo imports the neighboring `WrathCombo.json` only if its own `WrathFork.json` does not exist. Disable upstream Wrath first. The full configuration is copied, including rotation, targeting, custom job settings and auto-mode preferences; upstream settings are never modified. Existing fork configurations always take precedence, even if empty or damaged. Invalid source settings stop loading with an import error rather than creating defaults. Removing the fork configuration deliberately makes it eligible for import again. Accord selection remains separate and defaults to no jobs selected.

0.2.7 adds first-run upstream settings import, current-class live update recovery and keyboard compaction with Bridge routes. It removes the added targetless Dancer initiation while preserving upstream preparation and continuation. Use Hotbar Bridge 0.2.10. Existing fork settings are retained. Native testing remains pending.

0.2.8 adds all 21 PvP intent layouts and required engine settings, mode-specific setup/backups, per-bar compaction and periodic live repair. Update Accord and Wrathful Combo together and use Hotbar Bridge 0.2.11. PvP class selection starts empty. Hands-free auto rotation is unchanged. Native testing remains pending.
