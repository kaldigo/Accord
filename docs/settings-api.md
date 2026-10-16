# WrathFork settings API v2

Implemented in preview 0.2.2, on upstream Wrath 1.0.4.27. Accord is a separate IPC client; it neither loads the fork assembly nor writes its configuration file. This document is the implemented contract; the earlier separation assessment is historical design discussion.

## Endpoints

All request/reply payloads are JSON strings. Invoke functions on the Dalamud framework thread. A failed reply has `Success: false` and `Error`; retain the operation journal on any unacknowledged apply. A transport failure does not prove that an update failed to commit.

| Endpoint | Arguments / result | Purpose |
| --- | --- | --- |
| `WrathFork.Settings.Read.v2` | none / string | Identity, API/schema version 2, upstream version, instance/configuration ID, canonical revision, operation sequence, saved settings, preset IDs, features and effective state |
| `WrathFork.Settings.Discover.v2` | none / string | Writable fields and value shapes, engine-defined global bounds, preset job/parent/conflict metadata, feature defaults/job scope, target selector names and collection limits |
| `WrathFork.Settings.Validate.v2` | string patch / string | Full dry validation; returns current revision and inactive-parent warnings without changing settings |
| `WrathFork.Settings.Apply.v2` | string request / string | Validated persistent batch; returns a durable receipt and current state |
| `WrathFork.Settings.Changed.v2` | string notification / void | `Ready`, `Changed` and `Unloading`, with instance and saved revision |

Read also supplies the original v1 revision for the former field set, allowing unchanged 0.2.1 settings to adopt the canonical v2 revision without a false drift warning.

Notifications are hints. Accord subscribes and retains a one-second poll for late load, reconnect and missed notifications. Transient action continuation does not generate per-action settings events.

## Settings and validation

A patch contains exactly `Settings` (object), `Enable` (integer preset IDs) and `Disable` (integer preset IDs). Disabling happens before enabling. Maps merge by key and arrays replace. Detailed values not present in the patch survive.

Writable content covers registered per-job booleans, numbers and option arrays; preset membership and burst/auto-action flags; targeting priorities and overrides; granular fork features; ordinary rotation timing/action-replacement settings; custom-action enablement; BLU spell preferences and ignored NPCs. Configuration IDs, operation receipts, opcodes, caches, UI state and hands-free rotation configuration are not writable through this API.

Validation rejects unknown fields/options/preset IDs, incorrect types, non-finite or unrepresentable numbers, oversized collections, unsupported feature/job combinations, nonexistent target selectors, out-of-range global settings and newly introduced active preset conflicts. Existing inactive child preferences are retained with warnings, rather than enabling their parents. Existing conflicts are not silently rewritten. Global ranges come from engine setting attributes. Per-job custom values use their registered storage types; existing rotation predicates retain their job-specific meaning. The API does not claim every ImGui slider's presentation range is a universal storage constraint. In particular migration must not silently clamp valid personal values to UI defaults.

Read's effective state reports current preset predicate results, saved/effective differences, lease ownership, continuation, current-job feature values and action replacement enablement. This is not a prediction that an action can execute: parent paths, cooldowns, resources, level and target eligibility still apply. Live state is marked unavailable when no player is available. Active leases block new persistent writes.

## Apply and recovery

Apply requires `OperationId` (UUID), `ConfigurationId`, `Instance`, `ExpectedRevision`, `ExpectedSequence` and `Patch`.

The engine validates before mutation, writes a private full-configuration backup, applies changes and refreshes combo/target/IPC caches, then persists the settings and receipt in the same configuration save. Mutation/refresh/save failures attempt restoration; rollback failure is explicitly reported. Backup failure prevents mutation.

Receipts contain the request hash and `CommittedRevision`. The exact request can be replayed, including after engine reload, to recover its acknowledgement without another settings write. Reusing an operation ID with different content is rejected. The last 256 receipts are retained. A monotonically increasing operation sequence prevents an expired request from being reapplied even if settings later return to their earlier revision. Unknown old-session requests, changed configurations and stale revisions/sequences require explicit review.

Accord writes its pending request and desired choices before calling Apply. It saves the acknowledged choices before removing that journal. On reconnect it replays the same request and invalidates its current-class cache so any pending live layout is reconsidered. It uses the receipt's committed revision, not a potentially newer current revision, so outside edits still trigger drift protection. An unresolved request blocks further writes; the UI can retain it as an abandoned diagnostic record and start a fresh review. This does not undo an engine commit. Live layout failure remains separate from behaviour persistence and retains existing hotbar backups/rollback.

## Combined configuration migration

A detected version 6 combined configuration is copied to an immutable-named backup in Accord's own directory. No engine configuration file is opened by Accord. Migration pauses automatic preparation and presents a preview before import. Existing prototype backups are also selectable from the settings/diagnostics section, including backups made by previews 0.2.0/0.2.1.

The preview maps recognised engine settings to an API patch and records every omitted root field, retired custom key and unknown preset ID in `settings-migration-preview.json`. Invalid values fail validation rather than being silently changed. UI/caches/opcodes/hands-free settings remain in the original backup; Accord's choices and hotbar recovery path are imported separately. The user can inspect the report before applying.

Personal values and targeting preferences are retained. The four saved variants determine only the managed intent switches and granular fork features. Imported jobs are marked prepared to prevent first-login/class-change baseline resets. Live mode is disabled; migration does not write hotbars. The fork backs up its existing configuration before accepting the import. A stale preview requires a fresh review. Interrupted imports use the same durable journal/receipt protocol as ordinary updates.

## Verification and release status

5,901 automated assertions/check cases pass, including actual compiled-engine validation and round trips for 512 job/variant setups, migration across all 16 variant choices, property-order-stable revisions, stale/session/configuration rejection, replay without overwriting later edits, expired receipts, malformed imports, backup/partial-populate/cache/save failures and rollback failure reporting. Both separate packages build.

Implementation is complete for this separation contract. Native in-game acceptance remains: actual Dalamud IPC transport/lifecycle, reload/reconnect, migration with real saved configuration, both interfaces and affected rotation/hotbar paths. No live configuration has been migrated, and no package has been installed or published by this work.


Accord's active-class selection is client-owned and is not an engine API setting. It gates job mappings, baseline resets and hotbar updates. Deselection retains already persisted engine settings. During migration, all personal settings are imported explicitly, but only selected jobs have intent-controlled switches remapped; imported prepared-job history does not expand an explicit selection.


The fork preserves the inherited leased API under `WrathCombo.*`, action requests under `WrathCombo.ActionRequest.*`, and the `OnActionUsed` event. The added persistent settings API for Accord remains `WrathFork.Settings.*.v2`; it does not replace existing interfaces. Active leases still prevent Accord persistent writes. Matching endpoint names preserves those contracts; other plugins may separately inspect plugin identity or version, which has not been tested in game.
