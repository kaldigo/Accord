# Accord: premise and direction

Accord is a maintained fork of [Wrath Combo](https://github.com/PunishXIV/WrathCombo) with our curated job setup layer and a simplified, exclusive user interface. It combines reviewed automation settings and matching keyboard/controller hotbar layouts. Hotbar Bridge remains a separate synchronization plugin.

## Starting point

Created 2026-10-07 from upstream commit `5d7652004b44ff25a0e9ddc34b868e487babda2c` (Fix SCH dots). This initial checkout preserves upstream runtime code, UI, plugin identity and dependencies. Only Accord documentation has been added. It is not yet a renamed or installable Accord release; do not run this baseline alongside Wrath. No runtime behaviour, live game configuration, or existing HotbarTools release has been changed.

The repository is https://github.com/kaldigo/Accord. `origin` points there; `upstream` points to PunishXIV/WrathCombo. Preserve upstream history so fixes and changes can be reviewed and merged. The sibling HotbarTools repository remains independent.

## Product premise

The user configures their jobs through reviewed setups rather than Wrath's full settings UI. Accord should provide one supported configuration path: choose a setup, its automation switches and scope, preview the matching hotbar/settings changes, then apply. It must package versioned settings that match the layouts; changing automation must not leave required actions inaccessible.

The shipped plugin must expose only Accord's UI. The original Wrath UI must not be retained as a hidden Advanced screen or alternate settings route. Audit upstream windows, commands and writable IPC routes when implementing this boundary. Read-only diagnostics, effective-setting exports and reset to reviewed settings are appropriate. Identify configuration initialization or other side effects embedded in UI code and preserve those outside the UI before removing it.

Preserve Wrath's rotation engine, action hooks, targeting machinery, configuration semantics and timing behaviour. Do not extract a new library, rewrite the engine, simplify job logic or reorganize upstream files merely to make the code cleaner. Add our setup layer on top and change engine behaviour only for a concrete, documented requirement. Keep changes small and easy to compare with upstream.

## Setup and automation

- Retain three global user-facing choices: Auto mitigation (M), Auto burst (B), and Auto job mechanics (J). Current/all scope controls which supported jobs receive them. Their implementation is job-specific; a switch may legitimately make no additional change.
- All-on means the maximum reviewed automation through the user's rotation presses, not hands-free action execution. Preserve manual functions that automation does not cover, including deliberate movement, targeting, emergency timing and situational utilities.
- Keep ST and AoE on separate buttons. Preserve approved controller positions, role consistency, ST/AoE/mobility positions and general tank/healer reserves. Optional keyboard compaction uses the selected variant and must preserve every remaining function.
- Follow the existing reviewed layouts rather than inventing a uniform eight-button claim. Approved job exceptions and the one-page twelve-slot noncombat layouts remain part of the source requirements. Do not add Sprint, Limit Break, Teleport or Return to job budgets; shared utility is separate.
- Preserve current/all application, macros, active-job status, the managed-job registry, backups, preview checks and restores. Class changes must display the actual applied state rather than merely the last global choice.
- Apply configuration inside Accord's own runtime using validated updates and the existing save lifecycle. Plan for updates without disabling the plugin, including base targeting/sliders, while handling caches, partial failures and queued saves correctly. Do not replace a live external Wrath configuration file.
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

## Initial implementation sequence

1. Establish and document this untouched upstream baseline.
2. Inventory startup/configuration/UI dependencies and the exact public preset data to import.
3. Add the Accord setup layer and validated in-memory configuration application without refactoring rotations.
4. Replace user-facing settings entry points with Accord's UI; preserve required initialization outside UI code.
5. Establish distinct plugin identity, configuration migration, updater feed and protection against loading alongside Wrath before distributing Accord.
6. Add missing automation one job/behaviour at a time, with disabled-path comparison and in-game checks.

This document records the agreed direction. Creating this repository does not mean the implementation or release steps above have already happened.
