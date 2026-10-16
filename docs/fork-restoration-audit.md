# Standalone fork restoration audit — 2026-10-16

Compared the working fork against the merged Wrath 1.0.4.27 baseline (`5365eb1edc13654be945a624b2311a8b8541a599`), inspecting all changed engine files and tracing removed startup, UI, command, IPC and update registrations. Intent-routing changes remain deliberate engine additions.

| Area | Finding and result |
| --- | --- |
| Auto rotation | Runner restored in the preceding change. Removed the combined prototype's forced `RotationConfig.Enabled = false` on startup; the fork now respects its saved setting and existing instance/pause rules. Accord never switches the master toggle on. |
| Settings interface | Restored upstream sidebar and page routing: PvE, PvP, Auto-Rotation, Custom Actions, Settings, About and debug access. Restored drag/drop and search cleanup. The extra Engine options page was subsequently removed as unnecessary for Accord; the added settings remain available through the API. |
| Commands | `/wrath` now uses the full upstream dispatcher, including auto controls, preset commands, job navigation and diagnostics; `/scombo` is retained. |
| Job navigation | Restored current-job navigation on job changes and normal main/config window opening routes. |
| Conflict checks | Reconnected periodic plugin/game conflict checks and their UI. Existing upstream-Wrath load refusal and runtime conflict protection remain. |
| Opener indicator | Restored the optional `Wrath Combo Opener` entry, status text, click toggle, configured visibility, hidden-entry warning and disposal. |
| Leased integration API | Restored inherited provider registration as `WrathCombo.*`, preserving lease semantics, readiness, cache refresh, upstream IPC availability checks and cleanup. Accord continues using the separate persistent settings API. |
| Action requests | Restored initialization of `WrathCombo.ActionRequest.*`; action-use notifications remain `OnActionUsed`. These are engine-owned interfaces. |
| Settings migrations | Historical feature resets already run during fork initialization, independently of the login-message handler; no missing migration callback was found. |
| Shutdown | Removed an accidental extra settings-API tick during disposal; retained proper window, provider, service and event cleanup. |

## Deliberate differences retained

- Correction following the user's true-fork clarification: restored the auto-status DTR under `Wrath Combo`, with upstream click actions, active count, short text and locked/paused indicators. It coexists with Accord's variant entries.
- Restored upstream login/MOTD, major-change window registration and debug navigation. Their exclusion was not required for Accord. The upstream announcement source remains unchanged; the fallback welcome retains the upstream wording.
- Commands, inherited IPC contracts, DTR names and window identity match upstream. Earlier claims that renaming them was required or agreed were incorrect. Internal package/configuration separation and the additional Accord settings API remain; external consumers that inspect plugin identity still need in-game verification.
- The inherited IPC availability service still uses upstream's status endpoint and existing fallback behaviour. This audit does not replace that policy or invent a fork-hosted endpoint.
- Personal combined-config import deliberately leaves UI, opcodes and master auto-rotation preferences in its backup; restoring functionality does not silently import or activate them.

## Validation

Both local Release plugins build and 5,910 existing assertions pass, including compiled engine schema validation for all 512 setup/variant combinations. The upstream diff was reviewed again after restoration. These checks do not execute Dalamud UI, IPC registration or native action execution. In-game acceptance still needs the restored pages, `/wrath auto`, opener visibility/toggle, conflict display and an existing Wrath lease/action-request consumer. Nothing installed or published.
