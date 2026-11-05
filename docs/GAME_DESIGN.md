# Long Way Home — Game Design Doc

Nov 5, 2026 · @Chris Gaine

> Snapshot of the living design doc at https://claude.ai/artifact/1eP2gWZqTNcAMPXXxLyaiN (rev 29). The live doc is the source of truth; re-export here as it evolves.

Long Way Home is a cozy 3D farming game (working title chosen 2026-11-05; the browser prototype is still called Little Acre, v0.1). This doc tracks how its systems connect, the numbers behind them, and what to build next. The title may change before release: three space-themed games on Steam already use "A Long Way Home" or "The Long Way Home", one of them with farming, so a subtitle or different name may be needed.

## Pillars

These are proposed starting points. Rewrite them until they feel right, then use them to settle feature debates.

1. **Relaxing, never punishing.** Mistakes cost a little time or energy, never the farm. Crops miss a day of growth instead of dying.
2. **Every day ends with a small win.** A harvest, a sale, an unlock or a new plant should land in each in-game day.
3. **Systems feed each other.** Every new feature must take from or give to an existing loop (gold, energy, time, items), not sit on its own.
4. **Readable at a glance.** Crop stage, watered state and what the Use button will do are always visible without menus.

## Systems map

> *Interactive diagram (6-step core loop, 4 supporting systems) — view it in the [live doc](https://claude.ai/artifact/1eP2gWZqTNcAMPXXxLyaiN).*

Gold buys seeds, seeds become crops, and crops ship for gold. Energy and the watering can cap how much of the plot you can work each day, and nothing grows or sells until you sleep. When you add a system, add its box and arrows here first.

## Systems

Nine systems exist in v0.1. Each row says how it works today, what it feeds, and the biggest open question. Add a row when a new system is proposed; if you can't fill "Feeds into", check it against pillar 3.

| System | How it works in v0.1 | Feeds into | Open question |
| --- | --- | --- | --- |
| Time | Day runs 6:00 am to 2:00 am at 7 game minutes per real second (about 3 real minutes per day). Pauses while a menu is open. | Lighting, sleep, crop growth | Is 3 minutes too short once there is more to do? |
| Sleep | Bed at the front door ends the day. Staying up past 2 am means passing out and waking with 50% energy. | Crop growth, shipping payout, weather roll, save | Should passing out cost gold too? |
| Energy | 100 per day. Tilling costs 2, watering 1; planting and harvesting are free. Refills fully on sleep. | Limits tilling and watering per day | What restores energy mid-day (food)? |
| Farming | Till a tile, plant a seed, crop grows 1 stage per watered night, harvest when ripe. Harvested tile stays tilled. | Basket | Do untouched tilled tiles revert to grass? Do crops die at season change? |
| Watering | Can holds 20 (40 after upgrade), 1 per tile, refills at the pond. Watered state resets each night. | Crop growth | Should upgrades water a 3-tile row at once? |
| Weather | 22% chance of rain each new day. Rain waters every tilled tile, including newly tilled ones. | Watering, lighting | Add storms or seasonal weather odds? |
| Inventory | Seeds per crop type, plus a basket of harvested crops. No size limit. | Shipping | Add a backpack limit to create a reason for chests? |
| Shipping | Drop the whole basket into the bin. Items sell at their base price overnight. | Gold | Add quality tiers (normal, silver, gold) for higher prices? |
| Shop | Seed stall sells 3 seed types and a one-time can upgrade (250g). Always open. | Seeds, can capacity | Opening hours? Seasonal stock? |
| Gathering (planned) | Not in v0.1. Axe on trees gives wood, pickaxe on rocks gives stone. Each swing costs energy, so gathering competes with farming for the day. Materials go into the backpack. | Rebuilds, carpenter (wood to planks), backpack upgrades | Do trees regrow and rocks respawn, and how fast? Decide alongside the first rebuild costs. |

Saving happens once a night in the player's browser, so a day can be replayed by reloading before bed.

## Data tables

These are the values hard-coded in the v0.1 prototype. Where later sections differ (Seasons and crops, starting tools), the later sections win. When the game moves to a real engine, each table becomes a JSON file the game loads, with the ID column as the key other tables reference.

### Crops

| ID | Name | Grow time (days) | Seed cost (g) | Sell price (g) | Season | Regrows |
| --- | --- | --- | --- | --- | --- | --- |
| carrot | Carrot | 3 | 15 | 35 | Any | No |
| potato | Potato | 5 | 30 | 75 | Any | No |
| pumpkin | Pumpkin | 8 | 60 | 190 | Any | No |

### Tools and upgrades

| ID | Name | Energy per use | Capacity | Cost (g) | Unlocks |
| --- | --- | --- | --- | --- | --- |
| hoe | Hoe | 2 | — | Starting tool | Tilled tile |
| can to | Watering can | 1 | 20 | Starting tool | Watered tile |
| axe | Axe | TBD | — | Bought from Maggie (price TBD) | Wood from trees |
| pickaxe | Pickaxe | TBD | — | Bought from Maggie (price TBD) | Stone from rocks |
| can\_large | Bigger watering can | 1 | 40 | 250 | Replaces can |

### Locations

| ID | Name | Interaction | Range (tiles) |
| --- | --- | --- | --- |
| farm | Farm plot | Till, plant, water, harvest | 12 x 9 grid |
| door | House door | Sleep | 1.25 |
| bin | Shipping bin | Ship basket | 1.7 |
| stall | Seed stall | Open shop | 2.2 |
| pond | Pond | Refill can | 3.8 from centre |

### Tuning constants

| Constant | Value | Used by |
| --- | --- | --- |
| Starting gold | 150g | New game |
| Starting seeds | 12 carrot | New game |
| Max energy | 100 | Energy |
| Pass-out energy | 50% | Sleep |
| Time rate | 7 game min per real sec | Time |
| Day start / end | 6:00 am / 2:00 am | Time |
| Rain chance | 22% per day | Weather |
| Season length | 28 days | Calendar (display only) |
| Walk / run speed | 3.6 / 5.2 tiles per sec | Player |

## Economy check

Pumpkins earn the most on every measure, about 2.4x carrots per tile per day, so once a player can afford them there is no reason to plant anything else. Numbers below are per tile, on a tile that is already tilled.

| Crop | Profit per harvest (g) | Gold per tile per day | Upfront cost per tile (g) |
| --- | --- | --- | --- |
| Carrot | 20 | 6.7 | 15 |
| Potato | 45 | 9.0 | 30 |
| Pumpkin | 130 | 16.3 | 60 |

Each growing day costs 1 energy to water, so gold per energy matches gold per tile per day.

Right now the only things holding pumpkins back are cash flow (150g starting gold buys 2 pumpkin seeds) and the 8-day wait. Ways to give the cheaper crops a role:

- Carrots regrow after harvest, so they earn without replanting.
- Seasons: pumpkins only in Fall, so each season has its own best crop.
- Other uses for crops (cooking for energy, NPC requests) that ask for specific crops, not just gold.

Decided since: pumpkins grow only in fall (see Seasons and crops), and rebuilds will ask for specific crops and materials, so every crop has a use.

## Long-term vision

You restore an emptied town. At the start only one or two buildings and families remain. You rebuild buildings yourself with wood, stone and gold, and each rebuild brings a resident back, who unlocks new goods, tools or areas. Later buildings need materials that only earlier ones unlock, so the town and world open up over time.

- Starting tool: the hoe only. The axe (wood) and pickaxe (stone) are bought from Maggie's corner of the general store, before it's rebuilt, since the rebuild itself needs wood and stone
- Target platforms: Windows and Linux, including Steam Deck
- Engine: Godot 4 (.NET build) with C#. No mobile or iPhone build planned
- Controls: keyboard and mouse plus a controller, routed through Godot's input map so every action works on both

## Backstory

A space traveller crash-lands on a planet of a humanoid species, near the emptied town. His goal is to restore his ship and return to space. Along the way he meets the townspeople, builds a life, and rebuilds the town with the same resources he gathers for the ship. The ending is left open for now.

The key ship parts lie in the mine, and each milestone deeper down gets him one step closer. The parts below are a first pass:

| Mine depth | Ship part (draft) |
| --- | --- |
| Shallow | Power cell |
| Middle | Navigation crystal |
| Deep | Hull alloy |
| Bottom | Engine core |

Idea to explore: some parts need the town to finish them, e.g. the blacksmith forges the alloy into plating, Tom builds a launch frame.

## First building: General store

The general store is the first building to restore. It grows in three stages, each costing more wood, stone and gold.

| Stage | Unlocks |
| --- | --- |
| 1. Reopened | Basic seeds, first backpack upgrade |
| 2. Storeroom | More seed types, bigger backpack, chests for the farm |
| 3. Expanded shop | Buys your crops; wider range of seeds and farm supplies |

## Carpenter

| Stage | Unlocks |
| --- | --- |
| 1. Reopened | Turns wood into planks; sells fences and chests |
| 2. Workshop | Repairs the bridge to a new area; farmhouse upgrades |
| 3. Builder | Rebuilds homes so more families can move back |

Rebuilt homes are their own track: each one gives a family room to return, bringing a shop, a service or new requests.

## Blacksmith

| Stage | Unlocks |
| --- | --- |
| 1. Reopened | Tool upgrades for axe, pickaxe, hoe and watering can |
| 2. Forge | Reopens the mine; smelts ore into iron |
| 3. Master smith | Best tools; metal parts (e.g. sprinklers) needed for later rebuilds |

## Seasons and crops

Each season starts with two crops; more seeds unlock as the general store expands.

| Season | Starting crops |
| --- | --- |
| Spring | Carrots, potatoes |
| Summer | Tomatoes, corn |
| Fall | Pumpkins, squash |
| Winter | None outdoors: a season for mining, fishing and rebuilding |

A greenhouse, built at some point in the restoration, allows growing through winter.

## Café

A separate building, opened by the carpenter's partner once the carpenter family has moved back. The general store stays focused on seeds, crops and supplies; the café is where you buy food that restores energy.

- Turns farm ingredients (crops now; eggs and milk once the ranch opens) into meals and baked goods
- Exact stages and recipes to be decided

## Residents

| Who | When | Role |
| --- | --- | --- |
| Maggie | From the start | Elderly shopkeeper; keeps a corner of the general store open |
| Clyde | From the start | Old fisherman at the dock |
| Carpenter family | Carpenter stage 1 | Tom (carpenter), June (partner, later runs the café) and Jesse (teen who helps in the workshop) |

## Restoration order

Upgrades interleave across buildings rather than finishing one building first. Tool upgrades come early because most other progress depends on them.

1. General store, stage 1
2. Carpenter, stage 1
3. Blacksmith, stage 1 (first tool upgrades)
4. General store, stage 2
5. Carpenter, stage 2 (bridge opens)
6. Blacksmith, stage 2 (mine opens)
7. Third stages of each, with homes rebuilt in between

## World layout

The setting is mountain and lake cabin country. From top to bottom:

| Area | What's there | How it opens |
| --- | --- | --- |
| Mountains | The old mine | Blacksmith stage 2 |
| Meadow | Your farm, above town | Start |
| Forest path | Trees and foraging between farm and town | Start |
| Town | Town square with the general store, carpenter, blacksmith and empty houses | Start (mostly empty) |
| Lakeshore | Dock for fishing | Start or early |
| Across the river | Old ranch with cows, chickens and sheep | Carpenter stage 2 rebuilds the bridge |
| Across the lake | Far shore, to be decided | Future ferry upgrade |

## Milestone 1: Godot rebuild

Milestone 1 rebuilds the v0.1 browser prototype in Godot for Windows and Linux. It is done when it plays the same as the browser version, with a controller, on both platforms. The town, story and new systems above come after this.

1. Setup: Godot 4 .NET build, a C# project with folders for scenes, scripts and data, in a Git repo
2. World: flat ground, the farm grid, house, shipping bin, stall and pond, from simple low-poly shapes
3. Player: character with a follow camera, moving with keyboard or controller through the input map
4. Farming: tiles you can till, plant, water and harvest; crop data loaded from a JSON file built from the Crops table
5. Day cycle: clock, energy, sleep, shipping payout and overnight growth
6. Interface: hotbar, clock, gold and a simple shop menu
7. Saving and builds: save to a file, export for Windows and Linux, and test on the Steam Deck

## Open questions and backlog

Answer these from playtesting. Tick a question once its answer is written into the Systems or Data tables above.

### Questions

- [ ] Camera: keep the fixed overhead angle, or allow rotate and zoom?
- [ ] Pace: does a 3-minute day feel rushed or relaxed?
- [ ] Long-term goal: money targets, house upgrades, relationships, or a story?
- [ ] Failure: should anything ever be lost (crops dying, gold on pass-out)?
- [x] Platform: decided. Godot, Windows and Linux (including Steam Deck); iPhone dropped

### Backlog ideas

- [ ] Chop trees and break rocks for wood and stone
- [ ] Crafting (chests, sprinklers, fences)
- [ ] Seasons that change crops, weather odds and the look of the map
- [ ] Town with NPCs, friendship and requests
- [ ] Animals (chickens, cows) as a second income loop
- [ ] Fishing at the pond
- [ ] Cooking crops into food that restores energy
