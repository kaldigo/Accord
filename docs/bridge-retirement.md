# Accord without Hotbar Bridge — 0.2.14

Accord uses its existing regular and cross hotbar designs directly for compact PvE/PvP setup, live updates and restores. There is no Bridge configuration import, migration step or saved Bridge route state. Uninstall Bridge and use the updated Accord. The rotation engine is unchanged.

Cleanup preserves Accord's shared utility positions, regular utility bars 3–10 and game-shared bars. Backed-up layout transactions and mode/character checks remain. Old Accord backups still restore their assignments; obsolete Bridge route metadata is ignored.

Sprint conversion independently scans all 120 regular slots and 128 cross slots on the current active job/mode, including utility and game-shared bars. General Action Sprint (type 10, ID 4), PvE Sprint (action 3) and PvP Sprint (action 29057) become the correct binding for the active mode. Other actions and macros are unchanged. This works in static/live mode and on unselected classes. It changes bindings in place and saves a recovery batch; it executes no actions.

Combat, loading and unfinished job/mode switches defer writes. An old loaded Bridge must be disabled or uninstalled so it cannot race Accord. Sprint already on utility bars prevents an extra job-layout fallback. Cleanup preserves Sprint in otherwise unused positions.

Accord records only which PvP jobs it has observed ready on each character, in `pvp-observed-jobs.json`, to avoid writing a never-initialized inactive page. First visits complete queued selected-class setup. This record is independent of Bridge.

Build and simulation checks cover every supported job with utility/fallback Sprint, cleanup, restores, partial-write rollback, native readiness guards, mode/character isolation and all 248 active slots. A damaged Bridge configuration is deliberately present during the tests and has no effect. Native in-game acceptance remains pending; no installed plugins or live hotbars were changed.
