# Requirements log

Every requirement processed by `requirements-intake`, with the decision taken. Append-only:
a changed decision is a new entry that supersedes the old one. REQ numbers are permanent
and cited from product-definition.md, domain-model.md, the blueprint, design-system.md
and roadmap issues.

---

### REQ-1 — Farm plot
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Till a tile, plant a seed, crop grows 1 stage per watered night, harvest when ripe"
- **Kind**: capability
- **Statement**: The Player can till, plant, water and harvest tiles on a 12 x 9 farm plot. A watered crop grows one stage each night; an unwatered crop simply doesn't grow that night. A harvested tile stays tilled. Out of scope: quality tiers.
- **Acceptance**: A crop watered every night ripens after its grow days. A crop that misses a watering is delayed by one day and never dies.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-2 — New game start
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Starting gold 100g; starting seeds 10 carrot, 5 potato"
- **Kind**: capability
- **Statement**: A Player starting a new game has 100 gold, a hoe, a watering can, 10 emberroot seeds and 5 dusktuber seeds. Out of scope: choosing a farm layout or character.
- **Acceptance**: A new game starts with exactly these items and gold.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-3 — Day clock
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Day runs 6:00 am to 2:00 am, about 12 real minutes per day. Pauses while a menu is open."
- **Kind**: capability
- **Statement**: The Player's day runs from 6:00 am to 2:00 am and lasts about 12 real minutes. The clock pauses while a menu is open. Out of scope: changing the day length in settings.
- **Acceptance**: A full day takes about 12 real minutes. The clock does not advance while a menu is open.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-4 — Sleep and passing out
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Bed at the front door ends the day. Staying up past 2 am means passing out and waking with 50% energy."
- **Kind**: capability
- **Statement**: The Player ends the day by sleeping at the house door. A Player still awake at 2:00 am passes out and wakes the next morning with 50% energy; passing out costs nothing else. Out of scope: losing gold or items on passing out.
- **Acceptance**: Sleeping starts the next day, with crops grown and shipped items paid. Passing out leaves the Player with half energy and the same gold and items.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-5 — Energy
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "150 per day. Every tool swing or pour costs 2; planting and harvesting are free. Refills fully on sleep."
- **Kind**: capability
- **Statement**: The Player has 150 energy each day. Every tool swing or pour costs 2 energy; planting and harvesting are free. Energy refills fully on sleep. Out of scope: restoring energy during the day (see REQ-27).
- **Acceptance**: 75 tool uses empty a full energy bar. Planting and harvesting do not use energy. Sleeping refills energy to 150.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-6 — Watering can
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Can holds 20 (40 after upgrade), 1 per tile, refills at the pond."
- **Kind**: capability
- **Statement**: The Player's watering can holds 20 charges, uses one per tile watered, and refills at the pond. Out of scope: watering several tiles at once (Phase 2 tool upgrades).
- **Acceptance**: A can with no charges can't water. Refilling at the pond restores the can to full.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-7 — Rain
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "22% chance of rain each new day. Rain waters every tilled tile, including newly tilled ones."
- **Kind**: capability
- **Statement**: Each new day has a 22% chance of rain for the Player's farm. Rain waters every tilled tile, including tiles tilled that day. Out of scope: storms and season-dependent rain odds.
- **Acceptance**: On a rainy day every tilled tile counts as watered without the Player watering it.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-8 — Shipping bin
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Drop the whole basket into the bin. Items sell at their base price overnight."
- **Kind**: capability
- **Statement**: The Player drops their whole basket of harvested crops into the shipping bin; everything in it sells at its base price overnight. Out of scope: quality tiers and price changes.
- **Acceptance**: The next morning the Player's gold has risen by the base price of everything shipped.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-9 — Shop
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Simple shop menu; seed stall sold 3 seed types and a can upgrade"
- **Kind**: capability
- **Statement**: The Player buys seeds and the watering can upgrade (40 charges) from a shop menu. Out of scope: selling crops to the shop.
- **Acceptance**: Buying an item takes its price in gold and adds the item. A Player without enough gold can't buy it.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-10 — Seasons and native crops
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Crop seasons (carrot/potato spring, pumpkin fall); crops native to the planet"
- **Kind**: capability
- **Statement**: The Player farms through 28-day seasons with native crops: emberroot (3 days) and dusktuber (5 days) can be planted only in spring, and moonmelon (8 days) only in fall. A crop already in the ground keeps growing to harvest across a season change. Out of scope: outdoor crops in winter.
- **Acceptance**: Seeds can't be planted outside their season. A crop planted late in its season can still be harvested after the season changes.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: Summer has no crop in Phase 1. Add sunbell and starcorn now (numbers to be decided), or accept an empty summer until REQ-25?
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-11 — Nightly save
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Saving happens once a night"
- **Kind**: capability
- **Statement**: The Player's game saves once each night and resumes from that morning when reloaded. Out of scope: saving during the day.
- **Acceptance**: Quitting and reloading resumes at the start of the last day slept into.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-12 — Readable at a glance
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Crop stage, watered state and what the Use button will do are always visible without menus."
- **Kind**: ui
- **Statement**: The Player can always see each crop's growth stage, whether a tile is watered, and what the Use button will do, without opening a menu. Out of scope: tutorials and tooltips.
- **Acceptance**: Each of the three is identifiable on screen with no menu open.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-13 — Both control schemes
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Controls: keyboard and mouse plus a controller, so every action works on both"
- **Kind**: nfr
- **Statement**: The Player can do every action with keyboard and mouse, and every action with a controller. Out of scope: touch controls.
- **Acceptance**: a full in-game day can be played start to finish using only a controller, and again using only keyboard and mouse.
- **Verdict**: new
- **Phase**: Phase 1 (non-functional)
- **Documents**: product-definition (6 Non-Functional Requirements; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-14 — Runs on PC and Steam Deck
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Target platforms: Windows and Linux, including Steam Deck"
- **Kind**: nfr
- **Statement**: The Player can play on Windows, Linux and Steam Deck. Out of scope: macOS, mobile and consoles.
- **Acceptance**: a full in-game day plays without crashing on each of Windows, Linux and Steam Deck, and all text is readable on the Steam Deck screen.
- **Verdict**: new
- **Phase**: Phase 1 (non-functional)
- **Documents**: product-definition (6 Non-Functional Requirements; 3 Phases)
- **Rule changes**: none
- **Open questions**: Target frame rate on Steam Deck.
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-15 — Gathering
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Axe on trees gives wood, pickaxe on rocks gives stone... trees and rocks respawn every couple of days"
- **Kind**: capability
- **Statement**: The Player chops trees for wood and sap with the axe, and breaks rocks for stone with the pickaxe. Each swing costs energy, and trees and rocks respawn every couple of days. Out of scope: stumps and large trees before the matching tool tier.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-16 — Clearing the meadow
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "A cleared 6 x 6 plot; the rest of the meadow is overgrown... Clearing it both expands the farm and gives the first wood and stone."
- **Kind**: capability
- **Statement**: The Player starts with a cleared 6 x 6 plot; clearing the trees, stumps and rocks around it expands the area they can farm. Out of scope: buying extra land.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-17 — Opening with Maggie
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Opening (draft): after the crash, he stumbles into town and meets Maggie..."
- **Kind**: capability
- **Statement**: After the crash, Maggie guides the Player to the abandoned farmhouse, shows them how to use the hoe and watering can, gives them the starter seeds and points them to her store. Out of scope: a skippable full tutorial menu.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-18 — Rebuilding buildings
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "You rebuild buildings yourself with wood, stone and gold, and each rebuild brings a resident back"
- **Kind**: capability
- **Statement**: The Player spends wood, stone and gold to rebuild a town building one stage at a time; each finished stage restores the building's look and unlocks new goods, services or residents. Out of scope: the Player designing building layouts.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-19 — General store
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "General store... open from day one at level 1 (Maggie's corner) and grows through three rebuilds"
- **Kind**: capability
- **Statement**: The Player can shop at Maggie's corner of the general store from day one for seeds, the axe (50 gold) and the pickaxe (75 gold). Rebuilding it through stages 2 to 4 adds more seeds, backpack upgrades, farm chests and, at stage 4, buying the Player's crops. Out of scope: seasonal stock and opening hours (undecided).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-20 — Slot backpack
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Backpack (draft): slot-based; each slot holds a stack of one item, up to 99. Start with 12 slots..."
- **Kind**: capability
- **Statement**: The Player carries items in a 12-slot backpack where each slot holds a stack of up to 99 of one item, and the tools take slots. Store stage 2 upgrades it to 24 slots and stage 3 to 36. Out of scope: item weight.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-21 — Carpenter
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Carpenter stages 1-2"
- **Kind**: capability
- **Statement**: Once the carpenter is rebuilt, the Player can have wood made into planks and planks plus sap made into sealed planks, and buy fences and chests (stage 1). Stage 2 repairs the bridge to a new area and offers farmhouse upgrades. Out of scope: rebuilding homes (REQ-32).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-22 — Blacksmith and tool upgrades
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Tool upgrade levels; Blacksmith stages 1-2; Mira's house then Blacksmith stage 1"
- **Kind**: capability
- **Statement**: The Player rebuilds Mira's house, then her forge. Stage 1 upgrades tools to Rosk and forges Rosk pumps; stage 2 smelts Groth for Groth tools and pumps. An upgraded tool does more per action for the same 2 energy. Out of scope: Forgelight tools (REQ-34).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-23 — Early mine
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Early mine, levels 1-5 (draft)"
- **Kind**: capability
- **Statement**: Buying the pickaxe opens the old mine, with Maggie telling its story. The Player clears debris on levels 1 to 4, collects stone, Rosk ore and glimmercaps, returns to any cleared level by ladder or lift, and drains level 5 with Mira's first pump. Out of scope: danger (creatures, collapses).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-24 — First residents return
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Homes start early... the Hollis family lives above the workshop... Mira needs a home"
- **Kind**: capability
- **Statement**: The Player sees the Hollis family (Tom, June and Jesse) move in when the carpenter reopens, and Mira move in when her house is rebuilt. Out of scope: other households (REQ-32).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-25 — More native crops
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Summer: sunbell, starcorn; Fall: moonmelon, coilsquash"
- **Kind**: capability
- **Statement**: The Player can buy and grow sunbell and starcorn in summer and coilsquash in fall as the general store expands. Out of scope: winter outdoor crops.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: May move to Phase 1, depending on the REQ-10 question.
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-26 — Friendships and lanterns
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Each resident has a five-step friendship meter... lanterns lit in each resident's window"
- **Kind**: capability
- **Statement**: The Player raises a five-step friendship with each resident by talking daily, giving gifts they like and finishing their requests. Each step lights a lantern in that resident's window and unlocks a reward and a piece of their story. Out of scope: romance and marriage.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-27 — Café meals
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Café... you buy food that restores energy"
- **Kind**: capability
- **Statement**: The Player buys dishes at June's café that restore energy: a light dish (30 energy, 60 gold) and a hearty dish (60 energy, 120 gold) each season. Out of scope: cooking at home; selling crops to June.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-28 — Fishing
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Fishing (draft)"
- **Kind**: capability
- **Statement**: The Player fishes with Clyde's old rod: cast, wait for a nibble, then reel while keeping the line tension in a safe zone. Too much tension snaps the line. The fish vary by spot, season, time of day and weather. Out of scope: fishing from a boat (REQ-38).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-29 — Ember Festival
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Ember Festival"
- **Kind**: capability
- **Statement**: June's fifth lantern starts the Ember Festival as a project the Player helps prepare. After the first one it repeats every year on a fixed late-summer date and grows as residents return. Out of scope: other festivals.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-30 — Glowing night plants
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Glowing plants (draft)"
- **Kind**: capability
- **Statement**: The Player picks wild glowing plants by hand, only at night, on the forest path, the riverbank and in the mine, and can sell them. Most are seasonal. Out of scope: remedies and growing them (REQ-40).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-31 — Greenhouse
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "A greenhouse, built at some point in the restoration, allows growing through winter."
- **Kind**: capability
- **Statement**: The Player can grow crops through winter in a greenhouse. Out of scope: growing out-of-season crops outdoors.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-32 — Home rebuilds and returning families
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Builder: rebuilds homes so more families can move back; arrival order and triggers"
- **Kind**: capability
- **Statement**: Carpenter stage 3 lets the Player rebuild homes so families can move back. The first is Clyde's fish-market kid, who comes back at Clyde's second lantern and buys fish for more than the bin pays. Out of scope: the ranchers, Ottlie, the diver and the schoolteacher (Phase 4).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-33 — Deep mine
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "The flooded mine is the main progression gate... Depth: about 60 levels"
- **Kind**: capability
- **Statement**: The Player goes down to level 60 of the mine. Each level from 6 is shored up with sealed planks, and every fifth level needs a pump forged by Mira. The ore changes with depth: Rosk, then Groth, then Embrite. Out of scope: danger (creatures, collapses).
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-34 — Master smith
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Master smith: best tools; metal parts (e.g. sprinklers)"
- **Kind**: capability
- **Statement**: Blacksmith stage 3 lets the Player get Forgelight tools and metal parts such as sprinklers.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-35 — Ship parts and crash site
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "The key ship parts lie in the mine; crash site (draft)"
- **Kind**: capability
- **Statement**: The Player finds the power cell (level 15), navigation crystal (30), hull alloy (45) and engine core (60) in the mine and installs each in its slot at the crash site, which lights up a section of the ship.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-36 — Ending
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "The ending is left open for now."
- **Kind**: capability
- **Statement**: Once the Player has installed all four ship parts, the story reaches its ending.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: What the ending is (leave, stay, or a choice).
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-37 — Ranch and animals
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Ranch and animals (draft)"
- **Kind**: capability
- **Statement**: Once the bridge is repaired and the ranch house rebuilt, the ranchers return and the Player can buy puffhens, mossbacks and woolhorns. They live in a coop and barn Tom builds on the farm, are fed daily, and give eggs, milk and wool. Out of scope: breeding and hatching.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-38 — Sunken town and diving
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "The sunken town"
- **Kind**: capability
- **Statement**: Clyde's rowboat lets the Player fish over the sunken town. His family's barn and Mira's diving helmet then let the Player salvage relics from it, with the diver kid's help. Out of scope: lowering the lake.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-39 — Museum
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Relics go to a museum..."
- **Kind**: capability
- **Statement**: Once the Player finds relics and rebuilds the retired schoolteacher's home, the schoolteacher opens the Ember Bay museum. Each donated relic fills a display and unlocks a piece of the lower town's history. Out of scope: selling relics.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-40 — Ottlie's remedies
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Lumen-reader Ottlie; glowing plants"
- **Kind**: capability
- **Statement**: When the Player reaches the darker mine levels, Ottlie returns, turns glowing plants into remedies (for example energy tonics, light for dark levels), and later teaches the Player to grow some on the farm.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 4
- **Documents**: product-definition (5 Functional Requirements, Phase 4; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### REQ-41 — Ferry to the far shore
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145; product definition interview, 2026-11-06. Source wording: "Across the lake: far shore, to be decided; future ferry upgrade (parked)"
- **Kind**: capability
- **Statement**: The Player can take a ferry, rebuilt by Clyde's boatbuilder kid, to the far shore across the lake.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: After release (not placed in a phase)
- **Documents**: none (logged only)
- **Rule changes**: none
- **Open questions**: What is on the far shore.
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —
### REQ-42 — Summer crops
- **Date**: 2026-11-06
- **Source**: Phase 1 readiness, answer to the REQ-10 open question
- **Kind**: capability
- **Statement**: The Player can buy and plant sunbell (4 days) and starcorn (7 days) in summer, so every season but winter has crops in Phase 1. Draft numbers: sunbell seed 20, sell 50; starcorn seed 45, sell 130; tuned in playtesting. Out of scope: coilsquash (REQ-43).
- **Acceptance**: in summer the shop offers sunbell and starcorn seeds, and they can be planted only in summer; both ripen after their grow days and ship for their sell price.
- **Verdict**: new
- **Phase**: Phase 1
- **Documents**: product-definition (5 Functional Requirements, Phase 1; 3 Phases); domain-model (`CropType` rows sunbell, starcorn; `CropTypeId` values); canonical-values (`CropTypeId`, `Season`)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: Phase 1 epic (see Phase 1 readiness)
- **Supersedes**: REQ-25 (sunbell and starcorn part)

### REQ-43 — Coilsquash
- **Date**: 2026-11-06
- **Source**: Phase 1 readiness; remainder of REQ-25 after REQ-42
- **Kind**: capability
- **Statement**: The Player can buy and grow coilsquash in fall once the general store expands. Out of scope: winter outdoor crops.
- **Acceptance**: to settle at phase readiness
- **Verdict**: planned
- **Phase**: Phase 3
- **Documents**: product-definition (5 Functional Requirements, Phase 3, replaces the REQ-25 entry; 3 Phases)
- **Rule changes**: none
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: Phase 3 entry on #2
- **Supersedes**: REQ-25 (coilsquash part)

### Phase 1 readiness — 2026-11-06
- **Source**: requirements-intake phase readiness, started from epic-planning for Phase 1
- **REQs in the phase**: REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-42 (new)
- **Settled**:
  - REQ-10: open question settled. Summer gets sunbell and starcorn in Phase 1 (REQ-42, superseding that part of REQ-25; coilsquash continues as REQ-43 in Phase 3).
  - REQ-14: open question settled. Steam Deck holds a steady 30 fps at 1280 x 800; added to its acceptance.
  - REQ-1 to REQ-9, REQ-11 to REQ-13: no change; described by domain-model.md (BR-G1–G5, BR-P1–P7, BR-W1–W4, BR-T1–T9, BR-C1–C2, BR-S1–S2, BR-L1–L2) and technical-blueprint.md.
- **Documents**: product-definition (REQ-42 added, REQ-25 entry replaced by REQ-43, REQ-14 acceptance); domain-model (`CropType` sunbell, starcorn); canonical-values (`CropTypeId`, `Season`); technical-blueprint (Deck 30 fps target and cap)
- **Rule changes**: none
- **Ready**: all REQs in Phase 1. Handed to epic-planning.

### REQ-44 — Gathering
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "Trees along the forest path and farm edges give about 5 wood and 2 sap each... trees and rocks respawn every couple of days, including in the meadow, but never on tilled soil or within 2–3 tiles of it"
- **Kind**: capability
- **Statement**: The Player chops trees for wood and sap with the axe (about 5 wood and 2 sap a tree), and breaks rocks for stone with the pickaxe (about 3 stone a rock). Each swing costs energy. Trees and rocks respawn every couple of days, including in the meadow, but never on tilled soil or within 2–3 tiles of it. Out of scope: stumps and large trees before the matching tool tier.
- **Acceptance**: to settle at phase readiness
- **Verdict**: changed
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2, replaces the REQ-15 entry; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: The exact no-respawn distance around tilled soil (2 or 3 tiles).
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: REQ-15

### REQ-45 — Rebuilding buildings
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "How a rebuild works: each building has a sign out front. You drop off the wood, stone and gold it needs, and once everything is in, the building is rebuilt overnight. This works before Tom arrives."
- **Kind**: capability
- **Statement**: Each town building has a sign out front. The Player drops off the wood, stone and gold its next stage needs (later stages also need planks and ore); once everything is in, the building is rebuilt overnight, restoring its look and unlocking new goods, services or residents. This works before the carpenter reopens. Out of scope: the Player designing building layouts.
- **Acceptance**: to settle at phase readiness
- **Verdict**: changed
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2, replaces the REQ-18 entry; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: REQ-18

### REQ-46 — General store
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "Selling: until store level 4 you carry crops to the store and sell them to Maggie. At level 4 she gives you a shipping bin for the farm, which sells overnight. (Early playtest builds can keep the bin from the start.)"
- **Kind**: capability
- **Statement**: The Player can shop at Maggie's corner of the general store from day one for seeds, the axe (50 gold) and the pickaxe (75 gold), and sells harvested crops to Maggie at the store. Rebuilding it through stages 2 to 4 adds more seeds, backpack upgrades, farm chests and, at stage 4, a shipping bin on the farm that sells overnight. Out of scope: seasonal stock and opening hours (undecided).
- **Acceptance**: to settle at phase readiness
- **Verdict**: changed
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2, replaces the REQ-19 entry; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: The Phase 1 build has a farm shipping bin from the start (REQ-8). When Phase 2 adds the store, does that bin stay (the design doc allows it in early playtest builds) or arrive only at store stage 4?
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: REQ-19

### REQ-47 — Slot backpack and hotbar
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "The hotbar is one 6-slot row of the backpack shown on screen; a key or controller button cycles which row is the hotbar. With a full backpack, extra items drop on the ground and stay until the next morning."
- **Kind**: capability
- **Statement**: The Player carries items in a 12-slot backpack where each slot holds a stack of up to 99 of one item, and the tools take slots. Store stage 2 upgrades it to 24 slots and stage 3 to 36. The hotbar is one 6-slot row of the backpack shown on screen; a key or controller button cycles which row is the hotbar. When the backpack is full, extra items drop on the ground and stay there until the next morning. Out of scope: item weight.
- **Acceptance**: to settle at phase readiness
- **Verdict**: changed
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2, replaces the REQ-20 entry; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: REQ-20

### REQ-48 — Carpenter
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "Across the river: Old ranch (empty at first: a new area with trees and rocks to gather)"; "Drop off materials and they're ready the next morning."
- **Kind**: capability
- **Statement**: Once the carpenter is rebuilt, the Player can have wood made into planks and planks plus sap made into sealed planks, ready the next morning, and buy fences and chests (stage 1). Stage 2 repairs the bridge, opening the area across the river (empty at first, with trees and rocks to gather), and offers farmhouse upgrades. Out of scope: rebuilding homes (REQ-32).
- **Acceptance**: to settle at phase readiness
- **Verdict**: changed
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2, replaces the REQ-21 entry; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: REQ-21

### REQ-49 — Early mine
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "Travel: a staircase built into the mine connects the levels; every fifth cleared level adds a shortcut from the entrance"; "Early Groth: a few small Groth deposits appear on levels 3–5"
- **Kind**: capability
- **Statement**: Buying the pickaxe opens the old mine, with Maggie telling its story. The Player clears debris on levels 1 to 4 and collects stone, Rosk ore, glimmercaps and a few small Groth deposits on levels 3 to 5. A staircase built into the mine connects the levels, and every fifth cleared level adds a shortcut from the entrance. Level 5 is drained with Mira's first pump. Out of scope: danger (creatures, collapses).
- **Acceptance**: to settle at phase readiness
- **Verdict**: changed
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2, replaces the REQ-23 entry; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: REQ-23

### REQ-50 — First residents return
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "She returns once that house is repaired and you have explored mine levels 1–5; once she moves in, she asks you to rebuild the blacksmith."
- **Kind**: capability
- **Statement**: The Player sees the Hollis family (Tom, June and Jesse) move in when the carpenter reopens. Mira moves in once her house is rebuilt and the Player has explored mine levels 1 to 5; she then asks the Player to rebuild the blacksmith. Out of scope: other households (REQ-32).
- **Acceptance**: to settle at phase readiness
- **Verdict**: changed
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2, replaces the REQ-24 entry; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: REQ-24

### REQ-51 — Refilling at any water
- **Date**: 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 171. Source wording: "refills at any water: the pond on the farm, the river or the lake"
- **Kind**: capability
- **Statement**: The Player refills the watering can at any water: the pond on the farm, the river and the lake, as each area opens. Extends REQ-6. Out of scope: carrying water in other containers.
- **Acceptance**: to settle at phase readiness
- **Verdict**: new
- **Phase**: Phase 2
- **Documents**: product-definition (5 Functional Requirements, Phase 2; 3 Phases)
- **Rule changes**: none (Phase 2 is not yet in domain-model.md)
- **Open questions**: —
- **Deferred**: —
- **Roadmap**: —
- **Supersedes**: —

### Design doc rev 171 sync — 2026-11-06
- **Source**: docs/GAME_DESIGN.md rev 145 → rev 171
- **Changed**: REQ-15 → REQ-44, REQ-18 → REQ-45, REQ-19 → REQ-46, REQ-20 → REQ-47, REQ-21 → REQ-48, REQ-23 → REQ-49, REQ-24 → REQ-50; REQ-51 new (extends REQ-6).
- **No change**:
  - REQ-1: the design doc now settles its two open questions as already written: crops keep growing past a season change (REQ-10) and tilled tiles never revert to grass (BR-T8).
  - REQ-2: starting seeds already read emberroot and dusktuber.
  - REQ-6: Phase 1 has only the farm pond; river and lake refills are REQ-51.
  - REQ-8, REQ-9: Phase 1 keeps the farm shipping bin and shop menu. The design doc allows the bin from the start in early playtest builds; when it moves to store stage 4 is the REQ-46 open question.
  - REQ-22: the watering can upgrade shapes (Groth 3 tiles in a row, Forgelight 3 x 3, one charge per tile) and early Groth on mine levels 3–5 fit "does more per action for the same 2 energy".
  - Rebuild costs, Tom's conversion prices, sealed-plank counts and Mira's house cost are tuning numbers and stay in the design doc.
- **Not in requirements**: the new design-doc Art direction section belongs in docs/ART_DIRECTION.md, which is being worked on the `Graphics` branch.
- **Documents**: product-definition (Phase 2 entries replaced, REQ-51 added; 3 Phases); GAME_DESIGN.md snapshot (rev 171)
- **Rule changes**: none
