# Accord development rules

Read [docs/ACCORD.md](docs/ACCORD.md) for the agreed premise and current implementation status. User instructions take precedence. Upstream CONTRIBUTING.md remains useful for source conventions; it does not override Accord's product requirements.

- Preserve upstream engine behaviour. No unsolicited refactoring, simplification, library extraction, broad renaming, formatting sweeps or architectural rewrites. Make only changes necessary for the requested feature.
- Add our reviewed setup layer on top of Wrath. Hotbar Bridge remains separate. Keep runtime settings and layouts consistent with all supported M/B/J combinations and current/all scope.
- Ship only Accord's UI. Do not hide or retain the original Wrath settings UI as an Advanced fallback. Preserve necessary configuration initialization independently before removing UI paths. Read-only diagnostics are allowed.
- Added automation requires a specific gap, source evidence, optional behaviour, ST/AoE review, manual-access assessment and focused validation. Disabled additions must preserve upstream behaviour. Do not claim a button is redundant until all its required functions are covered.
- Keep baseline configuration, variant toggles and upstream migrations distinct. Feature-only switching must not reset detailed settings. Use runtime-owned configuration updates and confirmed persistence; never write another loaded plugin's settings file.
- Preserve upstream commit history, licence and dependency notices. Track the upstream revision and isolate local patches so merges remain reviewable. Compilation and tests are not proof of in-game correctness.
- Do not ship the inherited Wrath identity as Accord or allow simultaneous loading with upstream Wrath. Identity, configuration isolation/migration and updater behaviour must be deliberately implemented before distributing builds.
- This is an independent nested repository. Do not add it as copied source to the parent HotbarTools repository. Do not publish personal configurations, exports or live game data.
