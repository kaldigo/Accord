# Accord / Wrath fork separation assessment

**Current status (2026-10-16):** separation implementation, API v2 and personalised-settings migration are complete in preview 0.2.2; native acceptance is pending. See [implemented API/migration contract](settings-api.md).

**Earlier implementation update (2026-10-16):** source separation is now implemented. The assessment below records the pre-split design and intended broader contract; see the implementation record at the end for what exists and what remains.

2026-10-16. Decision recorded and current working tree inspected; no runtime split performed by this assessment. Paths below refer to this currently combined repository.

## Agreed boundary

The fork provides rotation capabilities, granular settings and a safe API. Accord defines the intent model, translates its variants into job-specific engine settings, and applies those settings. The fork executes the configured behaviour using its own hooks, action confirmations, timing and targeting. It does not import Accord, interpret four Accord variants, read Accord's setup pack or require Accord's prepared-job registry.

This is two plugins, not a newly extracted rotation-engine library. Preserve upstream history and the existing engine structure. Hotbar Bridge remains a third, independent plugin.

## File-level changes

| Current code | Destination / required change |
| --- | --- |
| `WrathCombo/Accord/AccordSettings.cs`, `AccordUi.cs`, `JobIntents.cs` | Standalone Accord: variant definitions, UI, descriptions, live/cleanup choices and prepared-job metadata. Remove the engine namespace/runtime dependency. |
| `WrathCombo/Accord/SetupPack.cs`, `presets.json` | Accord: reviewed baselines, variant-to-settings mappings and layouts. Extend mappings to cover the granular settings replacing today's hard-coded variant checks. Stop relying only on a pinned version string and raw preset IDs; validate the required settings/capabilities against the API. |
| `WrathCombo/Accord/HotbarSetup.cs`, `HotbarTransaction.cs`, `Hotbars/*` | Accord: native hotbar I/O, catalog, cleanup, backups, restore and existing Bridge IPC. These do not require a rotation-engine rewrite. Review copied legacy model helpers rather than moving unused Wrath file-writing policies into the new client. |
| `WrathCombo/Accord/SetupController.cs` | Split responsibilities. Accord retains UI/DTR, global choices, class-change handling and layout orchestration. Replace `Service.Configuration`, `Populate`, `Mutate` and direct engine cache access with API requests. Move engine validation/persistence/cache work into the fork. Move handoff reset lifecycle out of Accord. |
| `WrathCombo/Accord/IntentRouting.cs` | Split, not wholesale move. Action selection and confirmed-sequence continuation stay in the fork. Replace `Prepared`, `SetupController.Enabled`, `VariantId` and setup-pack lookups with explicit engine settings and native job/base-class resolution. Variant ownership is expressed by Accord's mappings. |
| `WrathCombo/Accord/IntentHandoff.cs`, `BurstPolicy.cs` | Engine runtime: confirmed inputs/effects, finite continuation and allowed follow-up actions. Add independent engine configuration to opt into continuation. These mechanisms must work with Accord absent after settings are applied; they must not become an action queue or IPC-per-press service. |
| `Core/ActionReplacer.cs`, `CustomCombo/Functions/Misc.cs`, `Data/ActionWatching.cs` | Keep replacement, effective preset evaluation and input/effect observation in the fork; replace calls into the Accord namespace with engine-owned facilities. Continuation overrides remain transient effective behaviour, distinct from saved settings. |
| `Core/Configuration.cs` | Remove `AccordSettings` from the engine's configuration after an explicit migration path exists. Add engine-owned granular settings/defaults for new functionality. Accord gets its own configuration store. |
| `WrathCombo.cs`, `Commands.cs`, `Window/ConfigWindow.cs` | Separate entry points, commands, UI registration, framework subscriptions and disposal. Accord owns `/accord` and four DTR entries. The fork owns hooks and engine services; sanitisation/migrations remain independent of UI. Review restoration of the fork's own settings surface separately from hands-free rotation or external action requests. |
| `Services/IPC/Provider.cs`, `WrathCombo.API/*` | Implement/register the fork configuration contract and client wrapper. Existing discovery/lease infrastructure is reusable evidence, not a complete persistent setup API. Give the fork its own identifiable/versioned endpoint. |
| `WrathCombo.csproj`, `Accord.json`, `tools/package-release.ps1` | Produce two distinct plugin builds/manifests/config directories/feeds, plus a narrowly scoped API contract if needed. Standalone Accord must not ship a second set of rotation hooks. Existing preview packaging is combined-only. |
| `tests/*` | Keep Accord mapping/layout/rollback checks; move handoff tests with the engine. Add API validation/persistence/version/lifecycle and client failure-path tests. Existing combined tests are not proof of separation. |

## Direct rotation coupling to remove

The working tree contains these specific Accord-aware branches:

- **AST:** `AST_Helper.TryAccordSupportCard` uses `Prepared` to enable utility cards on damage. Replace that with independent ST/AoE engine settings. Retain the actual explicit-recipient readiness/range/execution correction in AST and `CustomCombo/Functions/Action.cs`; it is an engine targeting fix, not a prepared-job capability. Remove Accord branding from helper names.
- **BRD:** song initiation checks `ClassMechanics`; Minne AoE and its path selection check `Prepared`; manual buff continuation reads `BurstFollowThrough` and `CanInitiate(Burst)`. Use song-initiation, support-path and continuation settings. Keep active-song and buff-order logic in the engine.
- **DNC:** Standard/Technical/Finishing Move initiation reads `ClassMechanics`; Curing Waltz reads `Support`. Expose action/path-specific switches; Accord groups them. Keep dance completion independent from starting a new dance.
- **SGE:** `TryAccordKardia` reads both `Prepared` and `Support`. Expose initial Kardia assignment on damage and its target-selection behaviour as engine settings; preserve one captured recipient and no automatic reassignment.
- **PCT/RPR:** AoE Tempera/Crest entry uses `Prepared`; expose actual AoE options instead of borrowing Accord presence as permission or implicitly coupling them to an ST flag.
- **MCH:** Dismantle's corrected eligibility branches on `Prepared`; decide its normal fork setting/fix semantics explicitly. Wildfire AoE continuation must use configured engine continuation, not Accord state. Preserve manual-Hypercharge input confirmation separately from ordinary automatic heat spending.
- **MNK/NIN:** buff-order fixes use Accord's handoff; move them to engine-owned configured continuation. TCJ Meisui override in `IntentRouting.PresetOverride` becomes an engine option for accepted TCJ continuation.
- **Shared protection:** `IntentRouting.Protection` currently gates all additions through Auto protection and uses hard-coded path selection. Retain the existing usefulness/phase guards but expose separate action/path settings; Accord's protection mapping chooses them.
- **Healer/DNC burst and AST draw:** `PresetOverride` currently returns global variant values directly. Saved engine flags should be the source of truth once Accord sets them; no special Accord override is needed for these initiation settings.

Existing preset-only mappings (including many tank flags) can continue using engine presets. Not every action needs a new setting. Add settings where initiation/continuation or ST/AoE coverage cannot currently be expressed independently. Do not turn `Prepared` into a renamed `AccordMode` switch.

## API requirements and current limitation

`Provider.Init()` omitted writable IPC registration at the initial assessment; the standalone restoration now registers it under the original `WrathCombo.*` (see [audit](fork-restoration-audit.md)). `SetComboState` and `SetComboOptionState` call `Leasing.AddRegistrationForCombo/Option`; their semantics are leased overrides, not the persisted baseline updates currently performed by `SetupController`. The existing wrapper also has hands-free auto-rotation methods: these are not the configuration mechanism for Accord's variants.

Proposed contract, with exact endpoint names/schema to be designed during implementation:

1. **Identify/discover:** fork identity, API/settings schema versions, supported jobs, setting identifiers/types/ranges and available capabilities. A matching upstream version alone cannot prove fork compatibility.
2. **Read:** saved and effective values, configuration revision and any override ownership/conflicts. Distinguish a transient continuation override from a changed saved setting.
3. **Validate/apply a batch:** preset membership plus typed boolean, numeric, array and targeting settings. Validate all keys, values, dependencies and job scope before changing anything. Support initial baseline/reset separately from a feature-only patch. Do not expose arbitrary whole-configuration JSON replacement as the public contract.
4. **Commit:** execute on the proper engine thread; persist through its own save lifecycle; refresh combo/target/IPC caches; report confirmed success and a resulting revision. On failure restore coherent state and report whether a commit occurred. Use operation IDs/revision checks so retries after disconnect cannot blindly overwrite newer edits.
5. **Observe lifecycle:** ready/unloading and configuration-changed notifications; Accord can reconcile its managed choices and report drift. No per-action IPC is required; successful-action observation stays entirely inside the fork.

Accord owns the selected variants and the list of settings it manages. The fork owns their saved/effective engine values. An external/manual change must be detected; whether Accord offers reapply or automatically reasserts managed settings is an implementation policy to make explicit, not silently assume. Granular detailed settings outside a feature-only patch must survive toggles.

Persistence should preserve the last confirmed engine configuration when Accord unloads; temporary leases must not accidentally erase the user's setup. Existing competing leases must not allow Accord to report a change as effective when it is overridden. This is a proposed API lifecycle contract, not functionality already provided by the current code.

## Transactions and lifecycle across plugins

The current `SetVariant` mutates behaviour before applying live hotbars and already reports layout failures separately. After separation there is no shared transaction across engine persistence, Accord persistence and native hotbar/Bridge writes. Implement explicit acknowledgement/reconciliation states: rejected, behaviour committed/layout pending, applied, or failed with recovery information. Keep native hotbar rollback and backups in Accord; keep engine settings backup/rollback in the fork. Do not report a changed A/M selection as fully applied before acknowledgement.

With an absent/incompatible/unloading fork, Accord must report unavailable configuration, avoid claiming successful behaviour changes and avoid automatic live layout changes that assume unconfirmed settings. Restore remains an Accord hotbar operation; noncombat/manual-only setup need not inherently depend on a combat engine. On reconnect, read actual state before resuming. The engine must continue correctly from saved settings with Accord absent and independently reset continuation on death/logout/class/territory changes.

## Identity and migration

Accord and its compatible fork must run together. Move the existing upstream-Wrath conflict guard to the fork, which owns the hooks. Replace Accord's blanket Wrath conflict logic with capability/identity checks. Prevent upstream Wrath and the fork from both owning action replacement.

The current `Accord` configuration contains both engine data and nested Accord choices. A split must not reinterpret that file silently or rely on shared writes: preserve a backup and explicitly import/export each plugin's own portion with schema validation. Do not erase the existing prototype's recovery files. The fork's name, repository allocation, standalone UI/commands and migration delivery remain choices; no rename, repository creation or live configuration migration was performed in this review.

## Implementation order and completion criteria

1. Inventory and introduce missing granular engine settings while the combined build still compiles. Map every direct Accord branch above to its real setting/capability.
2. Make engine runtime independent of Accord state and prove configured continuation/integration works without a setup controller.
3. Implement and test the persistent batch configuration API, discovery, effective-state reporting and lifecycle semantics.
4. Move setup/UI/hotbars into a separate Accord entry point and replace direct configuration mutation with its API client.
5. Separate identities, conflict rules, UI/commands, storage/migration and packaging; preserve upstream history and engine initialisation.
6. Run the mapping/rollback suites plus new boundary tests, then repeat affected native acceptance checks with both plugins loaded and with Accord absent/unloaded.

Done means no engine reference to Accord variants/registry/setup data, no Accord direct engine configuration or hook access, two independently identifiable packages, correct compatibility/failure reporting, and preserved gameplay/hotbar requirements. The previous 4,766 assertions and preview are evidence for the combined implementation only.


## Implemented source split — 2026-10-16

- `WrathCombo` builds provisional `WrathFork` 0.2.0: rotation hooks, confirmation/continuation, targeting, granular per-job features, native engine settings UI and `/wrath`. No Accord variant, setup-pack or prepared-job state is referenced by runtime engine code.
- `Accord.Plugin` builds independent `Accord` 0.2.0: variants/mappings, reviewed baselines, DTR/UI, static/live hotbars, Bridge integration, backups and `/accord`. It has no engine assembly reference. `Contract` is shared wire/validation source, not a runtime plugin dependency.
- Existing initiation presets remain saved engine flags. Twenty new granular feature switches replace Accord-aware branches. DNC Standard/Technical gates are distinct; continuation remains separate from initiation. The engine continues with its last saved settings when Accord unloads.
- `WrathFork.Settings.Read.v1` and `WrathFork.Settings.Apply.v1` are synchronous framework-thread JSON IPC. Read reports fork identity, API/settings schema 1, pinned upstream version, instance/configuration identifiers, revision, feature names, preset identifiers and saved setting shapes. Apply accepts only whitelisted setting maps plus preset enable/disable lists; it validates keys/types, numeric representation, array limits and actual target selectors before mutation. Full UI-specific slider ranges, preset parent/conflict dependencies and job-scope validation are not yet a complete public schema.
- Apply checks instance and expected revision, rejects existing leases, makes a private configuration backup, persists through Dalamud and refreshes combo/target/IPC state. It attempts restoration on failure and reports recovery failure explicitly. Reapplying identical values avoids a backup/write. This is persistent configuration, not a lease and not a per-action API.
- Accord polls once per second. Absence/incompatibility blocks behaviour changes and dependent live writes. Reconnect rereads state. Outside edits pause automatic reconciliation; explicit setup or a variant change reapplies managed flags while retaining unrelated details. Read currently reports saved settings, not a full effective override/continuation trace. Events and operation-ID replay receipts remain future API work; revisions protect stale retries.
- A combined prototype configuration is backed up before replacement. The migration screen retains Accord choices, clears preparation markers, disables live mode and requires the user's explicit start before applying reviewed settings to the new engine. It does not automatically import personalised engine settings. Recovery files remain in the existing Accord configuration directory.
- Both local packages have separate package/configuration stores and generated testing feeds; Accord uses `/accord` and the engine retains `/wrath` and `/scombo`. No install, live migration, repository allocation or publication performed. `WrathFork` is provisional naming.
- Engine settings registration is independent of opening configuration UI. Unsupported BST's callback-installing type initializer is excluded from eager schema registration. A build-scoped Lumina ExcelPage alias resolves SDK ambiguity in ECommons without modifying its submodule checkout.
- Schema verification exposed 93 obsolete integer options in the public tank baselines. Removed only keys documented in upstream's old-config detection plus four removed MitOptions names; current mitigation options and preset mapping remain intact. All 512 job/variant combinations are validated against actual compiled settings registrations and configuration round-tripping.

Still required before a usable release: in-game validation of loading/unloading/reconnect, both UIs, API persistence/failure paths, static/live hotbars and the affected rotation/targeting cases. The broader discovery/effective-state/events contract and automatic personalised-engine migration are not claimed complete. Noncombat setup currently uses the same engine confirmation path; engine-free noncombat setup remains a possible refinement.


### Upstream refresh - 2026-10-16

Current preview: 0.2.1 on Wrath 1.0.4.27 (`5365eb1edc13654be945a624b2311a8b8541a599`). The split is retained. API/client/setup compatibility pins and upstream dependency revisions were updated; both builds and 5,851 checks pass. See implementation.md for merge details. Native acceptance remains pending.


### API and migration completion - preview 0.2.2

Supersedes the earlier API/migration limitations above: v2 adds discovery, dry validation, effective preset/lease/continuation reporting, lifecycle/change notifications, canonical revisions and durable operation replay with sequence protection. Accord journals requests and recovers acknowledgement across reloads. Personalised combined settings now have a preview/import workflow, omitted-field report, retained recovery metadata and preparation markers that prevent baseline overwrite after import. See settings-api.md for the precise supported scope and validation semantics. Both packages build; 5,901 checks pass. Native game acceptance remains pending. Engine-free noncombat setup remains an optional future Accord feature, not a separation requirement.


Standalone interfaces suppressed by the combined prototype have now been audited and restored where appropriate. See [fork restoration audit](fork-restoration-audit.md) for findings, deliberate exclusions and native acceptance requirements.

## Live hotbar recovery correction - 2026-10-17

Class/character changes now wake the live update immediately; readiness or write failures remain pending and retry after 250 ms. Only the current selected class receives live hotbar writes, including variant, active-selection, cleanup and live-mode changes. Explicit static setup retains the all-selected scope. Successful behaviour and hotbar application completes the transition; failed application does not consume it. The blanket outside-edit revision pause is removed: transitions read fresh engine state and reapply Accord-owned switches while retaining detailed settings for prepared jobs. API transaction revision/sequence checks and pending-operation recovery remain enforced. This supersedes the earlier once-per-second/external-edit-pause description.
