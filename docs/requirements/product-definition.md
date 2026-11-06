# Long Way Home — Product Definition

> A cozy 3D farming game where a stranded space traveller rebuilds an emptied lakeside town while gathering what they need to repair their ship.

Requirements (sections 5 and 6) are written by `requirements-intake`, and every `REQ-` is
logged in [requirements-log.md](requirements-log.md). Game design detail (numbers, story,
characters) lives in the [Game Design Doc](../GAME_DESIGN.md).

## 1. Product Vision

For players who enjoy relaxing farming and life games, Long Way Home offers a gentle, never-punishing daily loop on an alien world, tied to a clear story goal: rebuild Ember Bay and repair the ship to get home.

- **Problem**: many cozy farming games feel interchangeable, and their long-term goals tend to drift.
- **Business goal**: a commercial release on Steam (Windows, Linux, Steam Deck).
- **Success looks like**: mostly positive Steam reviews ("Very Positive") in the first year after release, with players reaching the ship-repair ending.

## 2. Users & Roles

| Role | Who they are | Can | Cannot |
| --- | --- | --- | --- |
| Player | Someone playing the single-player game | Farm, gather, rebuild, befriend residents, explore the mine and world, and save progress | Lose the farm or have crops die; play with others |

- **Open**: a Mod author role (players adding crops, items or residents through data files). Possible in a later phase, but not committed.

## 3. Phases

| Phase | Name | Promise | Requirements |
| --- | --- | --- | --- |
| Phase 1 (MVP) | Godot rebuild | The v0.1 farm loop plays the same on Windows, Linux and Steam Deck, with keyboard and mouse or a controller | REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-42 |
| Phase 1B | Farm art | The Phase 1 farm looks the way the art direction describes, with a camera the player can rotate and zoom, still at 30 fps on Steam Deck | REQ-52, REQ-53, REQ-54, REQ-55, REQ-56 |
| Phase 2 | Rebuilding Ember Bay | The player gathers materials and rebuilds the first town buildings, which unlock tools and the early mine | REQ-44, REQ-16, REQ-17, REQ-45, REQ-46, REQ-47, REQ-48, REQ-22, REQ-49, REQ-50, REQ-51 |
| Phase 3 | Life in town | Residents return and become friends; food, fishing, festivals and seasons give each day variety | REQ-43, REQ-26, REQ-27, REQ-28, REQ-29, REQ-30, REQ-31, REQ-32 |
| Phase 4 | The way home | The player reaches the bottom of the mine, restores the wider world and repairs the ship; story complete and released on Steam | REQ-33, REQ-34, REQ-35, REQ-36, REQ-37, REQ-38, REQ-39, REQ-40 |

## 4. Out of Scope

- Mobile and console builds (no iPhone, Android or consoles).
- Punishing failure: crops never die and the farm is never lost.
- Combat. Mine hazards may come later, but there will be no fighting.
- Faction or company choices.
- Multiplayer.

## 5. Functional Requirements

The actor in every requirement is the Player (section 2).

### Phase 1 — Godot rebuild

#### REQ-1 — Farm plot *(capability)*
The **Player** can till, plant, water and harvest tiles on a 12 x 9 farm plot. A watered crop grows one stage each night; an unwatered crop simply doesn't grow that night. A harvested tile stays tilled.
- **Acceptance**:
  - A crop watered every night ripens after its grow days.
  - A crop that misses a watering is delayed by one day and never dies.
- **Out of scope**: quality tiers.

#### REQ-2 — New game start *(capability)*
A **Player** starting a new game has 100 gold, a hoe, a watering can, 10 emberroot seeds and 5 dusktuber seeds.
- **Acceptance**:
  - A new game starts with exactly these items and gold.
- **Out of scope**: choosing a farm layout or character.

#### REQ-3 — Day clock *(capability)*
The **Player**'s day runs from 6:00 am to 2:00 am and lasts about 12 real minutes. The clock pauses while a menu is open.
- **Acceptance**:
  - A full day takes about 12 real minutes.
  - The clock does not advance while a menu is open.
- **Out of scope**: changing the day length in settings.

#### REQ-4 — Sleep and passing out *(capability)*
The **Player** ends the day by sleeping at the house door. A Player still awake at 2:00 am passes out and wakes the next morning with 50% energy; passing out costs nothing else.
- **Acceptance**:
  - Sleeping starts the next day, with crops grown and shipped items paid.
  - Passing out leaves the Player with half energy and the same gold and items.
- **Out of scope**: losing gold or items on passing out.

#### REQ-5 — Energy *(capability)*
The **Player** has 150 energy each day. Every tool swing or pour costs 2 energy; planting and harvesting are free. Energy refills fully on sleep.
- **Acceptance**:
  - 75 tool uses empty a full energy bar.
  - Planting and harvesting do not use energy.
  - Sleeping refills energy to 150.
- **Out of scope**: restoring energy during the day (see REQ-27).

#### REQ-6 — Watering can *(capability)*
The **Player**'s watering can holds 20 charges, uses one per tile watered, and refills at the pond.
- **Acceptance**:
  - A can with no charges can't water.
  - Refilling at the pond restores the can to full.
- **Out of scope**: watering several tiles at once (Phase 2 tool upgrades).

#### REQ-7 — Rain *(capability)*
Each new day has a 22% chance of rain for the **Player**'s farm. Rain waters every tilled tile, including tiles tilled that day.
- **Acceptance**:
  - On a rainy day every tilled tile counts as watered without the Player watering it.
- **Out of scope**: storms and season-dependent rain odds.

#### REQ-8 — Shipping bin *(capability)*
The **Player** drops their whole basket of harvested crops into the shipping bin; everything in it sells at its base price overnight.
- **Acceptance**:
  - The next morning the Player's gold has risen by the base price of everything shipped.
- **Out of scope**: quality tiers and price changes.

#### REQ-9 — Shop *(capability)*
The **Player** buys seeds and the watering can upgrade (40 charges) from a shop menu.
- **Acceptance**:
  - Buying an item takes its price in gold and adds the item.
  - A Player without enough gold can't buy it.
- **Out of scope**: selling crops to the shop.

#### REQ-10 — Seasons and native crops *(capability)*
The **Player** farms through 28-day seasons with native crops: emberroot (3 days) and dusktuber (5 days) can be planted only in spring, and moonmelon (8 days) only in fall. A crop already in the ground keeps growing to harvest across a season change.
- **Acceptance**:
  - Seeds can't be planted outside their season.
  - A crop planted late in its season can still be harvested after the season changes.
- **Out of scope**: outdoor crops in winter.

#### REQ-42 — Summer crops *(capability)*
The **Player** can buy and plant sunbell (4 days) and starcorn (7 days) in summer, so every season but winter has crops in Phase 1.
- **Acceptance**:
  - In summer the shop offers sunbell and starcorn seeds, and they can be planted only in summer.
  - Both ripen after their grow days and ship for their sell price.
- **Out of scope**: coilsquash (REQ-43).

#### REQ-11 — Nightly save *(capability)*
The **Player**'s game saves once each night and resumes from that morning when reloaded.
- **Acceptance**:
  - Quitting and reloading resumes at the start of the last day slept into.
- **Out of scope**: saving during the day.

#### REQ-12 — Readable at a glance *(ui)*
The **Player** can always see each crop's growth stage, whether a tile is watered, and what the Use button will do, without opening a menu.
- **Acceptance**:
  - Each of the three is identifiable on screen with no menu open.
- **Out of scope**: tutorials and tooltips.

### Phase 1B — Farm art

#### REQ-52 — Rotate and zoom camera *(capability)*
The **Player** can turn the camera a full 360° around their farmer and zoom in and out within set limits; zooming out tilts the view towards top-down and zooming in tilts it lower. Walking always follows the camera's facing. Replaces the fixed overhead angle.
- **Acceptance**:
  - The camera turns all the way round and zooms between its nearest and farthest limits, using only a controller and again using only keyboard and mouse.
  - At any camera angle, pushing forward walks the farmer away from the camera.
  - The targeted tile and the Use prompt stay correct at every angle and zoom.
- **Out of scope**: a free camera detached from the farmer; a first-person view.

#### REQ-53 — Farm ground art *(ui)*
The **Player** sees the farm plot and meadow as the art direction describes: meadow ground and native plants, the plot's mown margin and stone edging, untilled, tilled and watered soil, the Use target marker, and grass that changes by season.
- **Acceptance**:
  - Untilled, tilled and watered tiles and the target marker can each be told apart at a glance from any camera angle.
  - The meadow grass changes with the season.
- **Out of scope**: clearing the meadow (REQ-16).

#### REQ-54 — Crop and seed art *(ui)*
The **Player** sees emberroot, dusktuber, moonmelon, sunbell and starcorn drawn to the art direction at every growth stage and at night, with matching seed and crop icons on the hotbar and in the shop.
- **Acceptance**:
  - Each crop's growth stage can be read at a glance.
  - Ripe crops can be told apart by shape as well as colour.
- **Out of scope**: coilsquash (REQ-43).

#### REQ-55 — Farmhouse art *(ui)*
The **Player** sleeps in the run-down "grown, not built" farmhouse, with the ship hull-panel patch on its roof.
- **Acceptance**:
  - The farmhouse matches the art direction's run-down state.
- **Out of scope**: the restored farmhouse (Phase 2, with the carpenter).

#### REQ-56 — The farmer *(ui)*
The **Player** plays as the fixed farmer: chunky, about 3 heads tall, in his first outfit, with idle, walk, run, tool-use, sleep and pass-out animations.
- **Acceptance**:
  - Every Phase 1 action plays its own animation.
  - Which way the farmer faces is clear at every camera angle.
- **Out of scope**: character customisation; later outfits.

### Phase 2 — Rebuilding Ember Bay

#### REQ-44 — Gathering *(capability)* *(supersedes REQ-15, 2026-11-06)*
The **Player** chops trees for wood and sap with the axe (about 5 wood and 2 sap a tree), and breaks rocks for stone with the pickaxe (about 3 stone a rock). Each swing costs energy. Trees and rocks respawn every couple of days, including in the meadow, but never on tilled soil or within 2–3 tiles of it.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: stumps and large trees before the matching tool tier.
- **Open**: The exact no-respawn distance around tilled soil (2 or 3 tiles).
#### REQ-16 — Clearing the meadow *(capability)*
The **Player** starts with a cleared 6 x 6 plot; clearing the trees, stumps and rocks around it expands the area they can farm.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: buying extra land.

#### REQ-17 — Opening with Maggie *(capability)*
After the crash, Maggie guides the **Player** to the abandoned farmhouse, shows them how to use the hoe and watering can, gives them the starter seeds and points them to her store.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: a skippable full tutorial menu.

#### REQ-45 — Rebuilding buildings *(capability)* *(supersedes REQ-18, 2026-11-06)*
Each town building has a sign out front. The **Player** drops off the wood, stone and gold its next stage needs (later stages also need planks and ore); once everything is in, the building is rebuilt overnight, restoring its look and unlocking new goods, services or residents. This works before the carpenter reopens.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: the Player designing building layouts.
#### REQ-46 — General store *(capability)* *(supersedes REQ-19, 2026-11-06)*
The **Player** can shop at Maggie's corner of the general store from day one for seeds, the axe (50 gold) and the pickaxe (75 gold), and sells harvested crops to Maggie at the store. Rebuilding it through stages 2 to 4 adds more seeds, backpack upgrades, farm chests and, at stage 4, a shipping bin on the farm that sells overnight.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: seasonal stock and opening hours (undecided).
- **Open**: The Phase 1 build has a farm shipping bin from the start (REQ-8). When Phase 2 adds the store, does that bin stay (the design doc allows it in early playtest builds) or arrive only at store stage 4?
#### REQ-47 — Slot backpack and hotbar *(capability)* *(supersedes REQ-20, 2026-11-06)*
The **Player** carries items in a 12-slot backpack where each slot holds a stack of up to 99 of one item, and the tools take slots. Store stage 2 upgrades it to 24 slots and stage 3 to 36. The hotbar is one 6-slot row of the backpack shown on screen; a key or controller button cycles which row is the hotbar. When the backpack is full, extra items drop on the ground and stay there until the next morning.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: item weight.
#### REQ-48 — Carpenter *(capability)* *(supersedes REQ-21, 2026-11-06)*
Once the carpenter is rebuilt, the **Player** can have wood made into planks and planks plus sap made into sealed planks, ready the next morning, and buy fences and chests (stage 1). Stage 2 repairs the bridge, opening the area across the river (empty at first, with trees and rocks to gather), and offers farmhouse upgrades.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: rebuilding homes (REQ-32).
#### REQ-22 — Blacksmith and tool upgrades *(capability)*
The **Player** rebuilds Mira's house, then her forge. Stage 1 upgrades tools to Rosk and forges Rosk pumps; stage 2 smelts Groth for Groth tools and pumps. An upgraded tool does more per action for the same 2 energy.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: Forgelight tools (REQ-34).

#### REQ-49 — Early mine *(capability)* *(supersedes REQ-23, 2026-11-06)*
Buying the pickaxe opens the old mine, with Maggie telling its story. The **Player** clears debris on levels 1 to 4 and collects stone, Rosk ore, glimmercaps and a few small Groth deposits on levels 3 to 5. A staircase built into the mine connects the levels, and every fifth cleared level adds a shortcut from the entrance. Level 5 is drained with Mira's first pump.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: danger (creatures, collapses).
#### REQ-50 — First residents return *(capability)* *(supersedes REQ-24, 2026-11-06)*
The **Player** sees the Hollis family (Tom, June and Jesse) move in when the carpenter reopens. Mira moves in once her house is rebuilt and the Player has explored mine levels 1 to 5; she then asks the Player to rebuild the blacksmith.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: other households (REQ-32).

#### REQ-51 — Refilling at any water *(capability)*
The **Player** refills the watering can at any water: the pond on the farm, the river and the lake, as each area opens. Extends REQ-6.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: carrying water in other containers.
### Phase 3 — Life in town

#### REQ-43 — Coilsquash *(capability)* *(supersedes REQ-25, 2026-11-06)*
The **Player** can buy and grow coilsquash in fall once the general store expands. Sunbell and starcorn moved to Phase 1 (REQ-42).
- **Acceptance**: to settle at phase readiness
- **Out of scope**: winter outdoor crops.

#### REQ-26 — Friendships and lanterns *(capability)*
The **Player** raises a five-step friendship with each resident by talking daily, giving gifts they like and finishing their requests. Each step lights a lantern in that resident's window and unlocks a reward and a piece of their story.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: romance and marriage.

#### REQ-27 — Café meals *(capability)*
The **Player** buys dishes at June's café that restore energy: a light dish (30 energy, 60 gold) and a hearty dish (60 energy, 120 gold) each season.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: cooking at home; selling crops to June.

#### REQ-28 — Fishing *(capability)*
The **Player** fishes with Clyde's old rod: cast, wait for a nibble, then reel while keeping the line tension in a safe zone. Too much tension snaps the line. The fish vary by spot, season, time of day and weather.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: fishing from a boat (REQ-38).

#### REQ-29 — Ember Festival *(capability)*
June's fifth lantern starts the Ember Festival as a project the **Player** helps prepare. After the first one it repeats every year on a fixed late-summer date and grows as residents return.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: other festivals.

#### REQ-30 — Glowing night plants *(capability)*
The **Player** picks wild glowing plants by hand, only at night, on the forest path, the riverbank and in the mine, and can sell them. Most are seasonal.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: remedies and growing them (REQ-40).

#### REQ-31 — Greenhouse *(capability)*
The **Player** can grow crops through winter in a greenhouse.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: growing out-of-season crops outdoors.

#### REQ-32 — Home rebuilds and returning families *(capability)*
Carpenter stage 3 lets the **Player** rebuild homes so families can move back. The first is Clyde's fish-market kid, who comes back at Clyde's second lantern and buys fish for more than the bin pays.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: the ranchers, Ottlie, the diver and the schoolteacher (Phase 4).

### Phase 4 — The way home

#### REQ-33 — Deep mine *(capability)*
The **Player** goes down to level 60 of the mine. Each level from 6 is shored up with sealed planks, and every fifth level needs a pump forged by Mira. The ore changes with depth: Rosk, then Groth, then Embrite.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: danger (creatures, collapses).

#### REQ-34 — Master smith *(capability)*
Blacksmith stage 3 lets the **Player** get Forgelight tools and metal parts such as sprinklers.
- **Acceptance**: to settle at phase readiness

#### REQ-35 — Ship parts and crash site *(capability)*
The **Player** finds the power cell (level 15), navigation crystal (30), hull alloy (45) and engine core (60) in the mine and installs each in its slot at the crash site, which lights up a section of the ship.
- **Acceptance**: to settle at phase readiness

#### REQ-36 — Ending *(capability)*
Once the **Player** has installed all four ship parts, the story reaches its ending.
- **Acceptance**: to settle at phase readiness
- **Open**: What the ending is (leave, stay, or a choice).

#### REQ-37 — Ranch and animals *(capability)*
Once the bridge is repaired and the ranch house rebuilt, the ranchers return and the **Player** can buy puffhens, mossbacks and woolhorns. They live in a coop and barn Tom builds on the farm, are fed daily, and give eggs, milk and wool.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: breeding and hatching.

#### REQ-38 — Sunken town and diving *(capability)*
Clyde's rowboat lets the **Player** fish over the sunken town. His family's barn and Mira's diving helmet then let the Player salvage relics from it, with the diver kid's help.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: lowering the lake.

#### REQ-39 — Museum *(capability)*
Once the **Player** finds relics and rebuilds the retired schoolteacher's home, the schoolteacher opens the Ember Bay museum. Each donated relic fills a display and unlocks a piece of the lower town's history.
- **Acceptance**: to settle at phase readiness
- **Out of scope**: selling relics.

#### REQ-40 — Ottlie's remedies *(capability)*
When the **Player** reaches the darker mine levels, Ottlie returns, turns glowing plants into remedies (for example energy tonics, light for dark levels), and later teaches the Player to grow some on the farm.
- **Acceptance**: to settle at phase readiness

## 6. Non-Functional Requirements

#### REQ-13 — Both control schemes *(nfr)*
The **Player** can do every action with keyboard and mouse, and every action with a controller.
- **Acceptance**: a full in-game day can be played start to finish using only a controller, and again using only keyboard and mouse.
- **Out of scope**: touch controls.

#### REQ-14 — Runs on PC and Steam Deck *(nfr)*
The **Player** can play on Windows, Linux and Steam Deck.
- **Acceptance**: a full in-game day plays without crashing on each of Windows, Linux and Steam Deck; on Steam Deck it holds a steady 30 fps at 1280 x 800 (no drops below 30 during a normal day), and all text is readable on the Deck screen.
- **Out of scope**: macOS, mobile and consoles.

## 7. Business Constraints

- Runs on Windows and Linux, including Steam Deck.
- Every action works with keyboard and mouse and with a controller.
- Built by a solo developer or tiny team, so scope must fit that.
- Must pass Steam's store review and Steam Deck compatibility review.

## 8. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| The title clashes with existing Steam games | High | Medium | Settle a subtitle or new name before the store page goes up |
| Players see it as a Stardew Valley clone | Medium | High | Lean on the alien setting, lantern friendships and the ship story |
| Scope creep | High | High | Phase promises decide where new ideas go; release is cut at Phase 4 |
| Pacing feels rushed or the economy is lopsided | Medium | Medium | Playtest each phase and tune the data tables |

## 9. Deferred to Technical Blueprint

Technology points raised during definition, in the speaker's words, for `solution-blueprint`.

- "Engine: Godot 4 (.NET build) with C#." *(design doc, 2026-11-06)*
- "When the game moves to a real engine, each table becomes a JSON file the game loads, with the ID column as the key other tables reference." *(design doc, 2026-11-06)*
