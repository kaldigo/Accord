# Custom-action startup crash

An installation/reload crash was traced from the managed exception in a local crash dump to `CustomActionManager.FormatNameDetour`. It threw `NullReferenceException` across the native callback boundary.

The manager enables its hooks in its constructor. `CustomActionSetup.Manager` and the enclosing plugin's `CustomActions` field cannot be assigned until their constructors return. Name and tooltip hooks nevertheless looked up `P.CustomActions.Manager.Actions`, making callbacks during that interval unsafe. Depending on the global plugin instance also allowed a manager's callback to consult a different instance during replacement.

Both callbacks now look up their own already-initialized `_actions` dictionary, as the action-row callback already does. Before registration, unknown IDs pass through to the original native callback. Registered custom actions return this manager's own pointers. No settings, layouts, gathering selectors or dependencies change.

The regression test compiles the production name/tooltip methods and invokes them with a null plugin, null setup, null manager, and an old manager still published during replacement. It checks original-call arguments, unknown IDs, registered pointers and independence from the global instance. Running it against the published pre-fix source reproduces `NullReferenceException`; the corrected source passes. Full build, compatible-client regression and package checks accompany publication. In-game reload acceptance remains separate.

Personal logs and the crash dump remain local and are not included in the repository or package.
