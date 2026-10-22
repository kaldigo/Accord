# Compact gathering intent controls

This replaces the expanded direct-action layouts from Accord 0.2.15.2. Wrath owns optional context selectors; Accord enables them for selected gatherers and installs matching controls. All actions still require a button press. Combat ST/AoE paths and Wrath Auto Mode are unaffected. Disabling the new presets preserves the existing engine behaviour and permits ordinary manually assigned actions.

Miner and Botanist use 14 controls (15 with a job Sprint). Fisher uses 22 (23 with a job Sprint). This fits two non-shared regular bars; Miner/Botanist fit one controller page and Fisher two. Shared bars are skipped. Clear other actions removes stale assignments from every unused non-shared regular/controller slot, including the previous third bar. Backups and exact restore remain in place.

## Miner and Botanist

Outside a gathering session, the five situational/low-cost controls expose detection, rare-node detection, nearest-node search, highest-level search and Sneak. At a node they expose their gathering actions. Detection and Truth remain direct toggles; search never spends GP or inserts a different action.

While the collectable window is visible, seven ordinary yield/chance/boon/crystal controls expose the seven collectable choices. Scour, Meticulous, Brazen, Focus, Priming, Scrutiny and Collect are independently selectable. Integrity remains Solid Reason/Ageless Words with the existing Eureka follow-up. Lower-cost chance and yield tiers, Gift I and II, and both crystal buffs remain deliberate choices; there is no forced expensive buff sequence or collectability threshold.

## Fisher

The native fishing state distinguishes equipment-ready/preparation from casting, line-in-water, bite, lure animation and hooking. A broad Fishing condition alone cannot safely make this distinction. While the line is out, Patience I/II offer Precision/Powerful Hookset, Prize Catch/Big-game Fishing offer Double/Triple Hook, and Surface Slap/Identical Cast offer Modest/Ambitious Lure. These remain separate choices. A locked or unaffordable hookset never falls back to casting a preparation buff. Ready/resting restores preparation controls.

Mooch II offers free Mooch only when the native action status reports it usable; otherwise the paid action remains visible. Bait stays directly accessible during rod fishing. Diving takes priority over rod state: Bait becomes Fathom, Makeshift Bait becomes Shark Eye, Mooch II becomes Shark Eye II and Spareful Hand becomes Truth of Oceans. Both detection toggles remain accessible. The other rod/spear counterparts are retained. GP recovery, both Patience tiers, swimbait storage, Rest, Quit, Release and Release List are separate controls. There is no bite-strength guess or automatic bait/lure selection.

## Compatibility and migration

Update both Wrath Combo and Accord. Accord rejects an engine missing the two new named presets before changing settings or bars. Gatherer setup revisions advance so selected, prepared jobs receive the new baseline; static mode still requires explicit hotbar setup, while live mode refreshes the selected class. Old Fisher replacement presets are disabled by the new setup; Wrath marks them as conflicting with its consolidated Fisher selector. New behaviour is an ordinary optional Wrath preset, independent of Accord variants.

## Button coverage

Each row lists the assigned root followed by other functions available through it. Context controls use normal game unlocks and native action validation. A higher-level root can expose a lower-level context action; the selector does not require the root to be learned first.

### BTN

| Slot | Root | Context functions |
| --- | --- | --- |
| 1 | Bountiful Harvest | Scour; Bountiful Harvest II |
| 2 | Blessed Harvest II | Meticulous Woodsman |
| 3 | Field Mastery III | Brazen Woodsman |
| 4 | Ageless Words | Wise to the World |
| 5 | Pioneer's Gift II | Collector's Focus |
| 6 | Nophica's Tidings | Priming Touch |
| 7 | The Giving Land | Scrutiny |
| 8 | the Twelve's Bounty | Collect |
| 9 | Flora Mastery | Triangulate |
| 10 | Luck of the Pioneer | Truth of Forests |
| 11 | Field Mastery | Arbor Call |
| 12 | Field Mastery II | Arbor Call II |
| 13 | Blessed Harvest | Sneak |
| 14 | Pioneer's Gift I | Direct manual choice |

### FSH

| Slot | Root | Context functions |
| --- | --- | --- |
| 1 | Cast | Hook; Gig |
| 2 | Bait | Fathom |
| 3 | Mooch II | Mooch; Shark Eye II |
| 4 | Chum | Baited Breath |
| 5 | Thaliak's Favor | Direct manual choice |
| 6 | Patience | Precision Hookset |
| 7 | Patience II | Powerful Hookset |
| 8 | Prize Catch | Double Hook; Nature's Bounty |
| 9 | Surface Slap | Modest Lure; Veteran Trade |
| 10 | Identical Cast | Ambitious Lure |
| 11 | Makeshift Bait | Shark Eye |
| 12 | Spareful Hand | Truth of Oceans |
| 13 | Big-game Fishing | Triple Hook |
| 14 | Snagging | Salvage |
| 15 | Fish Eyes | Vital Sight |
| 16 | Collect | Direct manual choice |
| 17 | Cast Light | Electric Current |
| 18 | Sneak | Direct manual choice |
| 19 | Rest | Direct manual choice |
| 20 | Quit | Direct manual choice |
| 21 | Release | Direct manual choice |
| 22 | Release List | Direct manual choice |

### MIN

| Slot | Root | Context functions |
| --- | --- | --- |
| 1 | Bountiful Yield | Scour; Bountiful Yield II |
| 2 | King's Yield II | Meticulous Prospector |
| 3 | Sharp Vision III | Brazen Prospector |
| 4 | Solid Reason | Wise to the World |
| 5 | Mountaineer's Gift II | Collector's Focus |
| 6 | Nald'thal's Tidings | Priming Touch |
| 7 | The Giving Land | Scrutiny |
| 8 | the Twelve's Bounty | Collect |
| 9 | Clear Vision | Prospect |
| 10 | Luck of the Mountaineer | Truth of Mountains |
| 11 | Sharp Vision | Lay of the Land |
| 12 | Sharp Vision II | Lay of the Land II |
| 13 | King's Yield | Sneak |
| 14 | Mountaineer's Gift I | Direct manual choice |

## Evidence and validation

Action IDs and coverage were checked against the installed English Action sheet. Gameplay references: [Miner](https://na.finalfantasyxiv.com/crafting_gathering_guide/miner/), [Botanist](https://na.finalfantasyxiv.com/crafting_gathering_guide/botanist/), [Fisher](https://na.finalfantasyxiv.com/crafting_gathering_guide/fisher/). Native fishing states are defined in [FFXIVClientStructs FishingEventHandler](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/Event/FishingEventHandler.cs). The collectable state uses the visible GatheringMasterpiece addon plus the gathering condition.

Regression tests compile the production selectors and exercise every action ID through travel, ordinary node, collectable, rod and spear contexts, including state transitions and free/paid Mooch. Accord simulations cover all variants, shared pages, stale third-bar removal, settled live refresh and exact restore. GatheringActionCheck independently checks every class action has an assigned root and enabled replacement. Full builds and packaging checks are required before publishing. Native in-game acceptance remains pending: verify low-level unlocks, entering/leaving collectables, Rest/recast, bites/lures, rod/spear transitions and shared-bar cleanup in game.
