# Auto protection routing correction

Follow-up, 2026-10-17: [all-variant source recheck](all-variant-audit.md) adds Samurai Third Eye/Tengentsu and Monk Riddle of Earth automation, separates Earth's Reply support, and documents why direct party-heal access remains. Its decisions supersede earlier retained-button claims here.

Implemented locally, 2026-10-16. Supersedes earlier notes retaining general mitigation selectors in Auto protection. User request: route automated protection through ST/AoE, keep separate buttons only for distinct intent, and preserve action access. An extra Paladin job slot is explicitly authorized.

## Routing and retained controls

| Jobs | Auto protection change | Distinct controls retained |
| --- | --- | --- |
| PLD | Disable Bulwark and Divine Veil selectors; existing ST/AoE mitigation handles ordinary defenses, including pack Arm's Length | Clemency, Passage of Arms, knockback immunity, Sheltron, Cover, Hallowed Ground |
| WAR | Disable Thrill selector; enable existing Thrill and pack Arm's Length routes alongside existing automatic defenses | Knockback immunity, Equilibrium emergency recovery, Raw Intuition/ally protection, Holmgang |
| DRK | Disable Dark Mind selector; enable boss Dark Mind and pack Arm's Length | Knockback immunity, Oblation ally protection, TBN, Living Dead |
| GNB | Disable Camouflage selector; enable boss Camouflage and pack Arm's Length | Knockback immunity, Aurora ally healing, Heart of Stone/Corundum, Superbolide |
| WHM | Disable Asylum selector; conditional Asylum/Temperance remain on damage; optional Divine Caress follow-up added on both advanced damage paths | Asylum field placement, Bell, Benediction and existing healing controls |
| SCH | Disable Soil selector; existing automatic Soil remains | Soil placement; separate Deployment Tactics selector provides Recitation/Adlo/shield spreading; Succor and fairy healing remain on healing buttons; Expedient |
| AST | Disable Collective selector; enable existing Neutral Sect shield follow-through | Collective channel; existing healing buttons retain their healing paths and Neutral Sect/Sun Sign handling |
| SGE | Disable Holos selector; existing automatic Kerachole/Holos remain | Panhaima for repeated-hit shielding; Prognosis retains shield preparation/Philosophia; existing targeted healing controls |
| MNK, DRG, NIN, SAM, VPR | Remove Feint button in Auto protection; ST/AoE routes already cover it | Existing knockback, movement and personal recovery/timed defenses |
| RPR | Remove Feint and Crest buttons in Auto protection; both damage paths cover them | Existing movement, recovery and knockback controls |
| BRD, MCH, DNC | Remove general party-defense button in Auto protection; both damage paths cover delegated actions | Existing movement, recovery, support and channel controls |
| BLM | Remove Addle and Manaward buttons; add optional Manaward AoE route using exactly the existing ST conditions/settings | Existing movement and elemental preparation controls |
| SMN | Remove Addle button | Radiant Aegis permits use of the charge reserved by automatic two-charge/timing conditions |
| RDM | Remove general Barrier/Addle selector button | Existing healing, resurrection and movement controls |
| PCT | Remove Addle button | Tempera retains deliberate personal/party conversion or release decisions; the damage helper uses its existing incoming-damage/party-coverage policy |

The two new fork features default off and are job-scoped. No existing cooldown, resource reserve, HP threshold or target priority is reset on variant changes. New preset memberships are controlled by the protection variant, without a baseline revision reset. Manual mode restores baseline mitigation selector memberships. Static mode changes settings only; explicit setup or live mode applies matching hotbars.

## Hotbar safety

Paladin's thirteenth control is regular hotbar 3 slot 1, paired to cross hotbar 2 slot 1. Bridge adds only explicit Paladin/Gladiator pairs, never global defaults. Shared bars, shared mappings, custom collisions and conflicting routes are rejected. The additional mapping is staged under the existing external-edit lock and saved with the layout routes; an aborted operation discards the staged mapping. Existing maps and live-sync preference are retained.

With cleanup off, empty positions still preserve unrelated assignments. An explicitly retired defense button can be cleared only where its stored type/action matches the known retired action. With cleanup on, the existing protected cleanup scope applies. Native hotbar edits retain backup and rollback handling.

The subsequent [all-variant audit](all-variant-audit.md) also enables the Adloquium and Deployment retarget settings for Scholar's preparation selector; this keeps the checked recipient and both executed actions consistent.

## Validation

The full job/variant matrix checks valid engine presets/settings, independent variants, exact restoration of manual selectors, retained distinct controls, no baseline mutation, and preset-only switching. Additional tests cover overflow scope, collisions, idempotence, external-lock requirement, abort rollback and committed persistence. Builds and these tests do not establish in-game action timing or native hotbar behaviour. Native acceptance remains outstanding.
