# Completed intent mapping, 2026-10-08

All-variant source recheck (2026-10-17): [current matrix, settings prerequisites and source findings](all-variant-audit.md) supersedes earlier layout-retention and completion claims below.

Protection correction (2026-10-16): [current routing and action-access audit](protection-routing.md) supersedes the general-selector retention described below. Implemented locally; native acceptance remains pending.

This is the implementation decision record for all 21 combat jobs. It supersedes open implementation suggestions in the earlier research, while retaining their source evidence. `WrathCombo/Accord/JobIntents.cs` exposes the category boundaries in the UI. Noncombat jobs keep their full 12-slot pages, unchanged by combat variants.

| Job | Burst initiation and continuation | Additional class mechanics | Support and deliberate controls |
| --- | --- | --- | --- |
| PLD | Fight or Flight; existing Requiescat and spell combo | None | Targeted Sheltron/Intervention, added Cover; Veil/Passage and invulnerability controls retained |
| WAR | Berserk/Inner Release; existing spenders and follow-ups | None | Raw Intuition/Nascent targeting, Thrill selector and invulnerability retained |
| DRK | Delirium/Blood Weapon or Living Shadow; remaining buff through damage | None | Carve also preserves interrupt; targeted TBN and Living Dead remain separate |
| GNB | Bloodfest/No Mercy; remaining buff, cartridge/Reign/Continuation handling through damage | None | Heart of Stone/Corundum targeting, mitigation selector and Superbolide retained |
| WHM | Presence of Mind, then native Glare IV | None; lilies keep existing paths | Healing selectors; added Presence of Mind and direct Benediction. Bell, Rescue and movement remain deliberate |
| SCH | Chain Stratagem, then native Baneful Impaction | Conditional Dissipation on healing paths | Soil's fairy/shield preparation remains a protection/healing intent; Expedient separate. Added Chain button |
| AST | Divination, then native Oracle | Draw initiation; held offensive and eligible utility cards continue | Eligibility/execution share the resolved ally; utility cards also considered on both damage paths. Synastry/Macrocosmos deliberate. Added Draw/Divination |
| SGE | No additional burst initiator | No extra gate for Addersgall/Rhizomata/Eukrasia | Support initially assigns missing Kardia on ST/AoE using the healing resolver; never moves existing Kardia. Manual Kardia/Pneuma and healing selectors retained |
| MNK | Brotherhood/Riddle of Fire; remaining buff through damage, including reverse initiation order | Perfect Balance; accepted forms/Blitz continue | Meditation/Form Shift preparation retained; movement/defensive timing separate |
| DRG | Lance Charge/Battle Litany; remaining buff through damage | Geirskogul/Life; accepted procs continue | Movement and Elusive Jump separate |
| NIN | Hide supplies Mug/Trick upgrades; remaining debuff through damage | Kassatsu/TCJ; accepted mudras and TCJ Meisui continue | Hide/Kassatsu prepull access retained; protection cannot interrupt mudras/TCJ |
| SAM | Ikishoten; Ogi/Kaeshi/Zanshin through native conditions | Meikyo; accepted combo continues | Meditate, movement and Third Eye remain distinct |
| RPR | Arcane Circle; Plentiful Harvest through native conditions | Enshroud; phase and finishers continue | Soulsow preparation, movement and deliberate defenses retained |
| VPR | Serpent's Ire; native follow-through | Reawaken; phase and Legacy continue | Slither, recovery and manual protection retained |
| BRD | Barrage initiates buffs; ST/AoE completes group with manual Raging-first ordering | Song initiation; active cycle continues | Support owns Paean/Minne on both paths; manual controls retained. Added song/Paean buttons |
| MCH | Barrel/Wildfire or confirmed manual Hypercharge; follow-through on both paths | None; heat/battery/tools unchanged | Conditional Dismantle/Tactician on both paths; Flamethrower/Peloton retained |
| DNC | Devilment only | Standard/Technical initiation; accepted steps/finishers continue | Support owns conditional Waltz; manual Waltz/Improvisation/Peloton retained. Added Standard/Devilment; Technical retained |
| BLM | Ley Lines; existing elemental rotation | None | Addle follows protection; Manaward, Transpose/Umbral Soul and movement deliberate |
| SMN | Searing Light; existing demi/proc continuation | None; normal summon cycle unchanged | Support owns Rekindle/Lux Solaris; manual Astral Flow and added Lux Solaris preserve targeted healing, movement and deliberate party recovery |
| RDM | Embolden/Manafication; remaining buff through damage | None; native melee/finishers continue | Vercure/resurrection and movement deliberate; party protection follows protection variant |
| PCT | Starry Muse; native procs and hammer follow-through | None; motifs remain native | Conditional Tempera conversion on both paths; manual shielding, Hammer and movement retained |

## Protection selector reconciliation

Automatic and manual tank selectors intentionally have different entry conditions. A manual mitigation press is itself permission to protect now; damage first needs to establish usefulness. Accord enables existing automatic counterparts rather than invoking unconditional manual cooldown choices from damage.

- PLD: pack/tankbuster Rampart, Sentinel/Guardian and Bulwark, plus conditional Reprisal/Divine Veil.
- WAR: pack/tankbuster Rampart and Vengeance/Damnation, plus conditional Reprisal, Shake It Off and Equilibrium.
- DRK: pack/tankbuster Rampart and Shadow Wall/Vigil, plus conditional Reprisal/Dark Missionary.
- GNB: pack/tankbuster Rampart/Nebula, Aurora/Camouflage, plus conditional Reprisal/Heart of Light.
- Healers: incoming-damage Asylum/Temperance, Soil, Collective/Neutral Sect and Kerachole/Holos remain protection-owned. Healing buttons continue functioning when protection is manual.
- Melee: Feint covers both damage paths; RPR Crest also covers AoE using its existing eligibility. Mudra/TCJ are protected from insertion.
- Ranged: Troubadour/Tactician/Samba cover both paths with incoming-damage, weave, ally-range and non-stacking checks. Dismantle checks for an absent debuff, fixing the pinned advanced branch's inability to initiate it. DNC steps cannot be interrupted.
- Casters: Addle covers both paths; RDM Barrier and PCT Tempera also cover AoE with existing conditions. SMN Aegis retains its charge/timing rules.

Automatic party defenses share cooldowns with manual controls. Short targeted defenses, invulnerabilities, Arms Length timing, Passage channeling and shield preparation retain deliberate controls. Generic damage-path use for these is not required by this mapping. Baseline self-recovery (such as Second Wind/Bloodbath) remains an emergency rotation function; Auto support does not disable it. Reserves, HP thresholds, healer spending and encounter classification are unchanged.

## Bounded burst handoff review

Only multi-initiator jobs need a temporary handoff: MNK, DRG, BRD, NIN, DRK, RDM and GNB use a 20-second bound; MCH uses 30 seconds to cover the Barrel/Hypercharged allowance. These are maximum acceptance intervals, not new rotation schedules. Successful local action effects activate them; failed presses and icon lookups do not. Buffs do not extend an active interval. Death, class/character/territory transitions and logout reset them.

`BurstPolicy` permits only relevant buff leaves and required cooldown/buff parents. It cannot enable rotation parents, Perfect Balance, Geirskogul or TCJ. Readiness, resource, range, weave, HP and content checks still apply. Single-initiator jobs need no timer: native statuses/gauges already carry the sequence.

The source review resolved order assumptions: BRD's automatic Finale-first order differs from the Raging-first manual selector; MNK Brotherhood required Riddle of Fire still ready; NIN Mug held for Trick's next cooldown even after a reverse-order accepted burst. Narrow handoff conditions resolve these without removing normal automatic initiation rules. TCJ Meisui follows the actual TCJ Suiton/Huton result, not ordinary Shadow Walker generation.

## Layout and settings outcome

All 512 combinations retain reviewed positions and the 12-slot ceiling. Initiators disappear only where the reviewed layout permits it; deliberate overrides and mixed-purpose selectors remain. Shared utilities stay outside the job budget. New controls occupy unused positions. Static mode may leave a missing manual control after switching to M until setup is pressed; live mode updates the current layout.

Variant changes update controlled membership only. Resolved data and runtime agree on healer/DNC burst ownership and AST draw ownership. The interface shows current four-category descriptions instead of imported three-variant prose. No personal configuration was imported; the parent HotbarTools repository was not changed.

The source/layout/settings review is complete. Native acceptance is pending in [distribution](../distribution.md); local checks do not prove game timing, targeting or hotbar I/O. The MCH clear-time observation remains insufficient to establish a damage defect.
