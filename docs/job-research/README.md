# Job research for Accord

Research snapshot: **2026-10-07**. Covers the 21 standard combat jobs supported by the existing setup. Limited jobs, crafting and gathering are outside this review. No rotation, settings or hotbar changes are implemented by these notes.

## Reading order

The [completed intent mapping](implemented-intents.md) records the final 21-job implementation decisions, including four-category ownership, manual continuation, support/protection boundaries and layouts. The [implementation log](../implementation.md) and [distribution record](../distribution.md) distinguish completed local work from pending native game acceptance. Earlier suggestions below are historical research, not an outstanding implementation checklist.

Start with the [resolved source audit, 2026-10-08](source-audit.md). It answers the implementation questions raised in the earlier [intent-and-coverage proposal](intent-proposal.md), corrects the false gaps, and separates actual work items from product choices and the two reported symptoms requiring live evidence. The proposal's old verification list and variant redesign are superseded, not an implementation backlog.

1. [All-job review](jobs.md): gameplay, candidate intents and utility first; Wrath/setup fit separately for each job.
2. [AST and MCH investigations](investigations.md): deeper source traces for the user's observations, with confirmed facts separated from hypotheses.
3. [Official action inventory and source manifest](sources-and-actions.json): source URLs, retrieval date, page hashes, guide version labels and official PvE action anchors, including role and listed pet actions.
4. [Wrath/setup inventory](runtime-inventory.json): existing logical slots, baseline preset names/IDs, variant deltas and selector entry points for every reviewed job.

The governing design remains [buttons express intent](../ACCORD.md#agreed-automation-philosophy-buttons-express-intent). A resource, phase or standalone Wrath selector does not automatically deserve a separate button. Conversely, damage automation covering an action does not eliminate its deliberate movement, preparation, recovery or targeting use.

## Versions and evidence limits

The [official patch index](https://na.finalfantasyxiv.com/lodestone/special/patchnote_log/) lists 7.56 at review time. The official job pages label their PvE descriptions 7.5 and describe actions with level-100 trait effects. The guide manifest preserves each guide's own patch/date labels; these are not all current-patch guides. NIN is labelled 7.05; several healers and MCH are labelled 7.3. Some date/patch combinations also appear inconsistent. Do not silently relabel these sources as 7.56.

The [7.5 patch notes](https://na.finalfantasyxiv.com/lodestone/topics/detail/07320affa7e0fcd9685afcbe54fbf55405b6d822/) explicitly adjust WAR, SAM, RPR, VPR, SMN and SGE actions. The [7.56 notes](https://na.finalfantasyxiv.com/lodestone/topics/detail/a8a526ad64db45c8ca8d1c7fdcce8a5eedaa18bc/) were also checked. This is not a complete reconciliation of every intervening hotfix, potency breakpoint or opener. Use the official current action description for toolkit facts, and recheck numeric optimization before implementing it. The profiles intentionally avoid prescribing stale potency values and exact openers.

The guide pass establishes how a job works and the decisions it exposes. The initial second pass inventoried local selectors and the public setup. The 2026-10-08 source audit then traced the identified questions across all 21 jobs through relevant conditions, helpers and settings. This is not a certification of unrelated branches or in-game behavior. No damage logs, effective live configuration or paired target tests were available.

Local source snapshot: Accord `60721d6f0ac761f7dcd53f092d03a1dc6a242a0a`, inherited engine baseline `5d7652004b44ff25a0e9ddc34b868e487babda2c`. The public setup pack identifies Wrath 1.0.4.26; Job Setup is 0.2.16. The inventory hashes the public pack to make drift detectable. These versions do not establish the user's installed version.

Inputs from the parent project are `job-setup/JobSetup/presets.json`, `job-setup/variant-review.md` and the burst coverage review. Their statements are migration evidence, not proof that every condition is currently exercised. The inventory deliberately excludes personal configuration exports. Raw downloaded guide text is not included in this repository.

## How to interpret the inventories

Official rows include upgrade actions and separate pet-executed rows. Counts are **not** hotbar button counts. General commands, all traits and every pet-management UI command are not exhaustively inventoried by the action scraper. Review traits at the actual level and pet command availability when designing a layout.

The runtime inventory lists top-level `CustomCombo` selectors as entry points. Shared helpers, nested eligibility gates, action transformations and targeting are not fully represented by those names. A listed selector is not proof that an action is reachable from the selected ST/AoE button, enabled by the public pack, or valid on a particular recipient. Returned actions should not be assumed to pass recursively through another selector.

For an action-level implementation decision, record: intent; initiating control or delegated variant; ST/AoE/healing/mitigation route; level; resource/cooldown/content gates; recipient; readiness checks; movement/animation consequences; manual fallback; settings ownership; and resulting slot effect. Test disabled behavior against upstream and test execution in game before calling the change validated.

## Utility must survive the review

| Function | Examples | Design implication |
| --- | --- | --- |
| Travel speed | BRD/MCH/DNC Peloton | Explicit out-of-combat intent; lack of rotation-guide coverage is not a reason to omit it. |
| Preparation and downtime | NIN Hide/mudras, MNK Form Shift/Meditation, RPR Soulsow, BLM Umbral Soul/Transpose, PCT motifs | In-combat automation does not prove prepull access is redundant. |
| Persistent recipient assignment | DNC Closed Position, SGE Kardia | Assigning a recipient is different from consuming a heal on that recipient. |
| Recovery and cleansing | Healer raises/Esuna, SMN Resurrection, RDM Verraise, BRD Paean | Preserve recovery intent and recipient rules. |
| Deliberate movement | Gap closers, disengages, ally movement, returns, SCH Expedient | Availability does not mean it is safe to trigger as damage or mitigation. |
| Tank control | Stance, Provoke, Shirk, interrupts, stuns | Maintain explicit access and distinguish swap/control intent from damage. |
| Planned support | Shields, mitigation cards, delayed heals, fairy tools | Current missing HP is not a complete measure of usefulness. |
| General utility | Sprint, Limit Break, Teleport, Return | Remains in shared utility; do not count it against job-specific slots. |

## Findings to carry into design

- Keep ST/AoE, deliberate protection, recovery and movement intents distinct. A job may need additional preparation or support assignment, but final button/variant count should follow actual decisions rather than the number of gauges.
- AST utility-card routing is the clearest support integration case. Its reported enemy-target symptom is a separate investigation; the source already resolves an ally for several checks.
- Manual phase continuation, BRD reactive party cleanse, SMN retargeted Rekindle, SCH Soil's self-shield/spread sequence, SGE manual Kardia targeting and PCT motif preparation are already implemented and configured. Their precise limitations and recipient rules are recorded in the source audit.
- DNC already separates initiation from completion through its IncludeTS settings; the current pack's broader preset switching is a setup-migration question, not proof of missing engine behavior (corrected 2026-10-08). SCH shield preparation, SGE Kardia, and SMN's phase-dependent Astral Flow demonstrate why superficially redundant selectors cannot simply be removed.
- MCH has concrete timing/resource gates worth testing in the user's short, unsynced context. The BLM comparison alone does not establish a defect or justify spending every resource immediately.
- Retain the agreed settings-first approach to resource banking. No broad reserve, HP-threshold or burst-alignment rewrite follows from this research.

These are review priorities, not approval to implement every candidate. Before changing a job, reconcile its affected current-patch mechanics and trace the specific route in full. Final variants and revised layouts remain design work informed by this reference.

## PvP research

See [PvP setup research](pvp.md) and [21-job source/action inventory](pvp-inventory.json) for the 2026-10-17 evidence pass. PvP layouts and mode-isolated Bridge support remain to be implemented. Wrath hands-free PvP auto rotation remains unchanged.

Detailed PvP review: [four-variant mappings](pvp-variant-mappings.md), [complete action-access ledger](pvp-action-ledger.json). All 21 source files reviewed; exact initiation/continuation splits are recorded. Run `python tools/validate-pvp-mappings.py` to detect source drift and check semantic action coverage.
