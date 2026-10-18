# Shared Accord intent implementation

Prepared for the 2026-10-18 test release: Accord 0.2.11, Wrathful Combo 0.2.9 and Hotbar Bridge 0.2.14. Builds and modeled tests do not establish native gameplay acceptance.

## Settings and layout ownership

The existing PvE active-class selection, four variants, live mode and cleanup preference are canonical for both modes. Migration backs up the original Accord configuration before retiring separate PvP choices. PvP retains its preparation history, pending first-visit layouts and mode-specific hotbar backup. Classes are never enabled by merging the old PvP list. Deselecting a class retains its configured engine settings and hotbars.

Accord applies scoped PvE and PvP settings through the fork API. ST and AoE express damage intent; manual burst, protection, support and class-mechanic selectors are added when their variant is off or a saved personal policy makes a delegated action unavailable. Distinct recipient, movement, healing, area-placement, limit-break and early-release controls remain separate. Manual sequence requests require a confirmed action before ST/AoE follows through, expire, and reset on context changes. No targetless Dancer preparation was added.

All 21 PvP jobs use reviewed executable profiles in `Accord.Plugin/pvp-presets.json`. The old profile generator now emits a historical research artifact instead of overwriting these profiles. Each profile has 20 logical positions, including empty placeholders: those are not 20 mandatory visible actions. ST/AoE remain bar 1 slots 1/2. Retained job controls and burst use bar 1; movement, common PvP recovery controls, selected role action and other manual selectors use bar 2. Where the native PvP kit uses the same damage path for ST/AoE, both retain that path; Ninja selects its separate area path and Scholar's area path includes eligible spread.

Keyboard actions compact through available positions within their own bar. PvE retains its fixed ST/AoE/movement anchors. Extras use job bar 2, not utility bars 3–10. Controller mappings stay stable; shared controller pages cannot block keyboard setup. Protected shared positions are excluded. If actual active controls exceed the remaining capacity, setup reports that instead of dropping actions. Vacated recognized job assignments are removed even with general cleanup off; unrelated assignments require the cleanup option.

Bridge translates action-type Sprint 3 and PvP Sprint 29057 at shared endpoints and linked bars. They compare as the same logical assignment, avoiding two-way conflicts or oscillation. Macros and unknown command types remain untouched. Accord omits its job Sprint when an actual shared map endpoint supplies it, otherwise adds a mode-appropriate fallback. Mode changes, class changes and periodic live checks re-evaluate layouts. Static mode changes settings without rewriting bars.

## Engine changes by job

| Job | Intent handling |
|---|---|
| PLD | Ranged Holy Spirit is independent of protection thresholds; ally protection and deliberate controls stay accessible. |
| WAR | A confirmed offensive request carries the grouped offensive package and native follow-through. |
| DRK | Self protection resolves to self; ally TBN and placed protection remain distinct. Native finishers remain available. |
| GNB | Offensive requests carry the burst package; movement alone does not request burst. Self Corundum targets self. |
| WHM | Offensive package handoff; healing selector retains Aquaveil and resolves its actual recipient. |
| SCH | Eligible damage spread on AoE and shield spread through healing; dedicated movement, pet and LB controls remain. |
| AST | Gravity follow-through, support outside damage immunity, resolved card recipients and confirmed heal recipient for Double Benefic. |
| SGE | Pneuma support outside damage immunity; initial Kardia resolves consistently, with deliberate recipient changes retained. |
| MNK | Native enhanced Phantom Rush; Wind knockback remains a distinct intent. |
| DRG | Geirskogul package handoff, native finisher and protection-owned Chaotic Spring recovery. |
| NIN | Separate ST/AoE paths; protection can initiate defensive Mudra without class mechanics enabled. Hidden-state priority remains native. |
| SAM | Meikyo/Ogi package handoff with native finishers. |
| RPR | Non-pooling Plentiful handling and normal Warrant expiry outside Enshroud; early release stays deliberate. |
| VPR | Ranged Uncoiled independent of burst; optional HP-gated Scales initiation with channel hold and Backlash. |
| BRD | Resolved reactive ally cleanse and self cleanse independent of enemy damage immunity; deliberate preventive Paean retained. |
| MCH | Offensive and tool-cycle handoff; normal Detonator expiry and separate early release. |
| DNC | Active Honing channel handling independent of initiation; allied Waltz eligibility and partner support; early release retained. |
| BLM | Grouped offensive/defensive selectors preserve elemental phase and personal policies; native damage fallback. |
| SMN | Offensive package handoff; self shield outside damage immunity, distinct ally shield and summon LBs. |
| RDM | Embolden-to-melee handoff with native finishers. |
| PCT | Accepted painting follows through into Muse/portrait without re-enabling new painting or changing palette intent. |

The added routing is optional, scoped to the new intent buttons, and enabled through the engine's granular settings API. Personal HP thresholds, timing and reserves are preserved; necessary continuation switches are explicit in the profiles. Hands-free master enablement, AutoActions, runner and exclusions are unchanged. Existing source hashes for hands-free components are verified by the mapping validator.

## Verification and limits

The Accord suite exercises all 512 PvE and 336 PvP job/variant states against the compiled engine schema, retained controls, settings ownership, per-bar destinations, shared-controller fallback, shared Sprint presence/absence, protected positions and transaction recovery. Bridge tests cover both Sprint directions, wrong-mode repair, conflict prevention and preserving other command types. The historical 504-action ledger remains a research inventory; its semantic checks are not an execution simulation.

Native action execution, confirmed recipient behavior and sequence timing still require in-game acceptance. Nothing in these changes executes game actions during development, installs a build or publishes a release.
