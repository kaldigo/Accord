# Three-bar manual fallback

Accord 0.2.15 adds ordinary PvE manual layouts for all 21 supported combat jobs and their existing base-class aliases. This is a patch-downtime fallback, independent of Wrath settings. Crafting, gathering, Blue Mage and PvP assignments are unchanged.

## Behaviour

With **Save manual hotbars before logout** enabled (the default), a hook at the installed SDK's resolved `AgentLobby.HandleLogout` entry prepares the selected combat jobs before the game begins the lobby transition. It runs synchronously on the framework thread, snapshots the complete destination area, writes and reads back the manual assignments, and requests a native `HOTBAR.DAT` save with `SaveFile(true)` before forwarding the original logout call. Immediate/sanctuary logout does not rely on a countdown. Preparation errors are logged and printed; the original logout still proceeds. An unavailable hook is reported in Accord, and the explicit preparation button remains available.

On login, a pending per-character record suppresses normal static/live/PvP setup writes. Accord waits for the compatible engine to respond successfully through its existing validated settings API, then restores the exact preceding assignments for still-selected jobs, including their third regular bar, controller pages and base classes. It does not regenerate a potentially different preset in place of the original snapshot. Afterwards existing static/live behaviour resumes. Missing/incompatible Wrath leaves the manual assignments intact; Accord itself need not run for those ordinary game hotbars to work.

**Prepare manual hotbars now** (`/accord manual`) prepares the fallback without logging out and holds it for the rest of that session. **Restore Accord hotbars** (`/accord normal`) restores it explicitly when the compatible engine is available. Ordinary Accord settings/layout operations are blocked while a fallback is pending. Disabling the automatic logout option does not erase an existing recovery record.

## Layout scope

The complete [per-job button tables](manual-layouts.md) are generated from the production layout planner after validating the assigned IDs against installed game data.

- Regular bars **1â€“3** (36 slots) and controller pages **1â€“3**. The old Bridge per-slot reservations do not restrict this manual profile, including regular bar 1 slots 9â€“12. All previous assignments in this area are captured and restored.
- Native game-shared destination bars are rejected before writing; change those bars to job-specific in the game's sharing settings. Bars 4â€“10, controller pages 4â€“8, and unrelated jobs remain untouched.
- No HUD positions, sizes, visibility, keybinds, sharing preferences or controller activation settings are changed. Enable/place regular bar 3 in your HUD and bind its keys once. Controller page 3 must be accessible through your controller settings/cycling. This keeps the existing two-bar HUD geometry intact.
- Prioritize rotations and movement/healing on the first two bars, use common role positions across jobs, and put overflow, setup and situational tools on the third. Sprint and Limit Break occupy bar 3 slots 12 and 11; controller mirrors use fixed endpoints.
- Always-changing native actions (Viper combos, Eukrasia, Confiteor follow-ups, etc.) use their normal assigned source button. Optional native follow-ups also receive dedicated buttons, so disabled Action Change Settings cannot strand an ability. With native replacements enabled some displayed buttons may therefore duplicate a follow-up temporarily.
- The practical omissions are **Scholar Physick/Repose** and **Astrologian Repose**. All other audited job/role controls, including Summon Eos, are retained. Restore Physick temporarily if doing level-synced Scholar content below Adloquium. Consumables remain on utility bars outside this layout.

## Recovery and limits

Records are under Accord's configuration directory at `manual-fallback/<hashed-character-key>/`. An immutable `original-*.json` snapshot is written before the active `pending.json` journal and before any native mutation. Phases Preparing/Manual/Restoring permit restart recovery from either original or manual assignments. Read-back failures attempt rollback of every touched slot. A second logout while already manual never replaces the original snapshot.

If a destination becomes game-shared or contains an outside edit matching neither snapshot, automatic restoration pauses with an explanation; both the edit and original backup remain. Deselected jobs are not rewritten, and their original assignments remain in the archived snapshot. PvP-mode logout writes saved PvE storage without modifying live or saved PvP slots.

Crashes, forced termination, power loss, and disconnect paths that bypass normal logout cannot guarantee preparation. Use the explicit preparation button before known maintenance. Native save completion, logout path coverage and HUD usability require in-game acceptance; compilation and simulated tests do not establish those.

## Validation

- The production planner/transaction code is exercised by `ManualFallbackTests`: capacity and job coverage, consistent role positions, former Bridge slots, exact restoration, aliases, character isolation, repeated logout, native sharing, outside edits, partial failures, interrupted recovery, and PvP separation.
- The full mapping/recovery/schema suite passed **240,518 assertions**, including 512 PvE variant combinations and 336 PvP combinations.
- `dotnet run --project tools/ManualActionCheck/ManualActionCheck.csproj -c Release -- <game-sqpack-directory>` verifies all **648** distinct per-job job/role assignments against installed English game sheets, including the pinned normal Emergency Tactics ID (3586), and GeneralAction Limit Break (3).
- Before relying on the feature: apply to disposable bars, test normal logout and Exit Game both inside and outside a sanctuary, log back in with both plugins disabled and switch through selected jobs, then enable compatible Wrath/Accord and verify the original assignments return. Also check static/live modes, PvP logout, disabled native action changes, and fallback after character death. Native acceptance is pending.
