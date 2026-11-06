# Long Way Home — Art Direction

Draft, started Nov 6, 2026 on the `Graphics` branch.

This document says what Long Way Home should look like and why. It runs alongside the [Game Design Doc](GAME_DESIGN.md), which covers systems and story. The roadmap phases in the design doc and `docs/requirements/` are about gameplay and only ask for "simple low-poly shapes". This document is a parallel track, so new art can be designed now and applied to gameplay scenes one piece at a time later.

Everything here is a first pass. Sections marked **draft** are proposals to react to, not decisions.

## 1. Theme: familiar at first, different on a second look

> Ember Bay looks like a lakeside cabin town you could find on Earth, until you look twice.

The design doc asks for "a bit more than a light touch" of alien flavour: cozy and familiar, but clearly not Earth. This document turns that into one rule:

**The second-look rule.** Each thing the player sees should read as something familiar at first (a tree, a carrot, a goat, a moon) and then show one or two details that aren't from Earth. A cozy game needs the first read; the second look is what gives the setting its identity.

The rule sets how far each kind of thing can go:

| How far from Earth | What it covers | Why |
| --- | --- | --- |
| **Mostly familiar** | Tools, furniture, boats, roads, the residents' clothes | These carry the cozy cabin-country feel. They are made by people, so they can look like home. |
| **Familiar shapes, alien materials** | Buildings and fences | Decided: buildings are "grown, not built" (section 7.1). They keep cabin shapes and doors you recognise, but are made from living native timber and leaves. |
| **Clearly alien, still readable** | Plants, crops, animals, the sky, the light at night | This is where the planet shows itself. Each thing keeps an Earth shape you can recognise, so it stays readable. |
| **Really foreign** | The crashed ship, the strange metal from the deep mine, the lights in the deep levels | These things don't belong to Ember Bay either, so they should look out of place on this planet too. |

Ember Bay is the traveller's new home and the ship is the way back. Keeping those two visual languages apart supports the story: the town gets warmer and more alive as it is rebuilt, while the ship stays cold and different until its parts light it up.

### Alien themes already in the design doc

Every hook below comes from `GAME_DESIGN.md`. The art should build on these before inventing new ones.

| Theme | Where it appears | What it means for the art |
| --- | --- | --- |
| Two moons | Backstory, alien night sky | The clearest "this isn't Earth" signal. Nights are lit by two moons of different colours and sizes, which gives two soft shadows. |
| Plants that glow or grow oddly | Backstory; Glowing plants | Bioluminescence is the planet's signature. It is mostly a night-time reward, in line with "picked only at night". |
| Native crops | Seasons and crops | Each crop has a familiar silhouette and one alien detail (see section 4). |
| Native animals | Ranch and animals | Puffhen, mossback and woolhorn: familiar farm-animal roles with alien bodies. |
| Humanoid townsfolk with small differences | Backstory | People should read as people. The difference is in one or two small features (draft: see section 6). |
| Mountain and lake cabin country | World layout | The base setting is calm and grounded: wood, stone, pine-like forest, cold clear water. |
| A drowned lower town | The sunken town | Rooftops visible under the lake from the dock. A bit melancholy, but beautiful rather than grim. |
| Lights in the deep mine | Maggie's mine story | The mine gets darker and stranger the deeper you go, and the foreign colour of the ship metal starts to show. |
| Lanterns on the lake | Ember Festival; lantern stories | Warm lantern light is the town's own light: the human counterpart to the planet's cool glow. |
| The name "Ember" | Ember Bay, Emberroot, Ember Festival | Warm ember orange is the town's accent colour (see section 2). |

## 2. Colour (draft)

**Idea:** warm town, cool planet. The town, lanterns and people lean warm (ember orange, amber light), and the town's native timber is a warm mauve. The planet's living things lean a little cool and strange: foliage shifted toward teal, shadows toward violet, glows in cyan and soft violet. Earth-like greens and browns are still most of the screen, so the shift is felt rather than shouted.

| Role | Colour | Hex | Notes |
| --- | --- | --- | --- |
| Meadow grass, lit | Fresh green with a little yellow | `#7FA65A` | Close to Earth, so the farm feels like home. |
| Meadow grass, shade | Teal-green | `#3F7A6C` | Shadows shift toward teal instead of just getting darker. |
| Native foliage | Blue-green | `#3E8F82` | Trees and wild plants: the first "not quite Earth" colour you notice. |
| Leaf undersides and new growth | Dusty violet | `#7A68A8` | Shows when leaves sway in the wind; a second-look detail. |
| Dry tilled soil | Warm brown | `#8A5A3C` | |
| Watered soil | Dark brown | `#3E2A20` | Must stay clearly darker than dry soil (pillar 4). |
| Wood (buildings, fences) | Native mauve timber | `#9C7E8C` | Living wood with a wavy grain; warm, but clearly not Earth wood. Trunks `#6B5A78`. |
| Stone | Grey with a lilac cast | `#8F8A96` | |
| Lake, deep | Blue-teal | `#2D6A86` | |
| Lake, shallow | Turquoise | `#5FB3AA` | Lets the sunken rooftops show through near the dock. |
| Town accent | Ember orange | `#E9773A` | Roofs, awnings, lanterns, the HUD. |
| Day sky | Soft blue with a mint horizon | `#8EC5E8` → `#CDE9DA` | A hint of mint near the horizon is a quiet alien sky cue. |
| Dusk sky | Amber into rose | `#F0A060` → `#B85C86` | Longer and richer than an Earth dusk; the most "postcard" time of day. |
| Night sky | Deep indigo | `#1B2142` | Dark enough for glows to read, never black. |
| Glow: cool | Cyan | `#6FF2E0` | Most glowing plants. |
| Glow: rare | Violet | `#B48CFF` | Rare plants and deep-mine light. |
| Lantern light | Warm amber | `#FFB04A` | Human light: the town, lanterns, windows at night. |
| Ship and strange metal | Pale blue-silver with an iridescent edge | `#C9D6E8` | Belongs to neither the town nor the planet. |

Readability comes first (pillar 4): tilled, watered and planted tiles and each crop stage must stay distinct in every lighting state and for common colour blindness. A colour that fails that check is changed, however nice it looks.

## 3. Light and the sky (draft)

Time of day is already a system (6:00 am to 2:00 am). The light should make each part of the day feel different:

| Time | Look |
| --- | --- |
| Morning (6–9 am) | Low mist over the meadow and lake, cool light warming quickly. |
| Day (9 am–5 pm) | Bright, soft shadows, clear colours. The best light for reading the farm. |
| Dusk (5–8 pm) | Long amber and rose light. The first moon rises. Lanterns and windows switch on. |
| Night (8 pm–2 am) | Two moons, indigo sky, glowing plants wake up. Still bright enough to farm and read tiles. |

**The two moons** (draft names: big **Hearth**, small **Wisp**):

- Hearth: large, soft gold, slow. Main night light, warm.
- Wisp: small, blue-white, crosses the sky faster. Second, cooler light.
- When both are up, objects cast two faint shadows: a small detail that sells "not Earth".
- Later idea: the moons' phases could connect to systems (e.g. some glowing plants only bloom when both are full).

Night must never punish the player (pillar 1): it is darker and moodier, but tiles, crops and prompts stay readable.

## 4. Crops (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Crops page)

Each crop: a familiar silhouette, one alien detail, and its own colour so it reads at a glance. The descriptions come from Seasons and crops in the design doc.

| Crop | Familiar read | Alien detail | Colour | At night |
| --- | --- | --- | --- | --- |
| Emberroot (in game) | Carrot | Feathery fronds over an orange shoulder that shows above the soil; "warm to the touch", with a glowing core and a heat shimmer on cool mornings | Ember orange `#E9773A` | The core glows warm orange |
| Dusktuber (in game) | Beet | A deep purple bulb half out of the soil, speckled like a night sky; its leaves curl into small violet spirals at the tips | Dusk purple `#4A2A66` | The speckles light up like stars |
| Moonmelon (in game) | Pumpkin | A pale blue-white ribbed gourd on a trailing vine | Moon blue-white `#C9DCF5` | Glows softly; the biggest night glow on the farm |
| Sunbell (planned) | Raspberry bush | Sun-coloured berries that hang upside down, point first, with a green star of sepals on top; gold at the top shading to orange at the tip. Decided: they don't ring | Sun gold `#F7C531` to orange `#E9821A` | No glow |
| Starcorn (planned) | Corn | A tall stalk with a big golden cob (decided: bigger than the first draft) whose kernels glitter like tiny stars | Gold `#E8B830` with white sparkles | The kernels sparkle |
| Coilsquash (planned) | Squash | Green with a cream band that spirals around it, and a curling tail | Green `#6FA050`, cream `#F0E6C0` | No glow |

**Silhouettes:** ripe crops differ in shape as well as colour, so they read from the camera even in poor light: low and round (dusktuber, moonmelon, coilsquash), a frond tuft (emberroot), a bush (sunbell) and tall (starcorn).

**Growth stages.** Seed, sprout, young and ripe, as in the game now (REQ-12). The art upgrade changes how they look, not what stage they show:

| Stage | Look |
| --- | --- |
| Seed | A small soil mound with a seed on top in the crop's colour |
| Sprout | Two seed leaves on a short stem, with a bud in the crop's colour |
| Young | Crop-specific foliage at about half height: emberroot fronds, dusktuber's curled rosette, moonmelon's vine with a pale bud, a sunbell bush with a small green berry, a short starcorn stalk, coilsquash's tendrils |
| Ripe | The full crop, as in the table above |

Each stage is taller or wider than the last, so stage reads from size as well as shape.

**Soil (pillar 4):** dry tilled soil `#8A5A3C` with furrows; watered soil `#4A3226`, much darker with a wet sheen, so the watered state reads at every stage and under every crop.

**Night:** four of the six crops have a night detail (glow or sparkle), which rewards going out after dark and ties the farm to the glowing plants. Night stays readable: crops are darker, but every crop and stage is still recognisable.

## 5. Plants, trees and glowing plants (draft)

- **Trees:** three native species shaped like trees you know. See section 5.1.
- **Grass and wild plants:** mostly familiar, with a few patches of native plants: curled fronds, round puffballs, tall bead-like stalks.
- **Glowing plants** (from the Glowing plants starter set): dull and easy to miss by day, unmistakable at night.

| Plant | By day | By night |
| --- | --- | --- |
| Dawnmoss | Pale green moss on rocks | Soft cyan carpet that fades at sunrise |
| Firefrond | Curled red fern | Warm amber glow that lights its surroundings (its effect is "lights up dark places") |
| Ashpetal | Grey-white flower | Glowing petals that drift down |
| Frostlantern | Frosty bulb on a stalk | Cool blue lantern shape |
| Snowwick | Thin white candle-like stem | A single bright flame-shaped tip; rare, so it should look special |
| Glimmercap | Small mushroom cluster | Speckled cyan caps; common, so cheap to draw and place in numbers |

### 5.1 Native trees (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Trees page)

Three species, each with a familiar silhouette. They share a family look: violet spiral trunks (the same mauve timber Ember Bay grows its buildings from, section 7.1), blue-green canopies with violet undersides, and amber sap that shows as glowing seams in the bark. The sap is the one the design doc uses for sealed mine planks ("glows and hardens waterproof"), so it's visible on every tree and stump.

| Species | Looks like | Alien detail | Where | At night |
| --- | --- | --- | --- | --- |
| Spirepine | A pine | Drooping tiers like layered skirts with violet undersides, a curled tip, lilac cones | Forest path and mountains, near the mine | Cones glow lilac |
| Twinbough | An oak | A twisted trunk that splits in two, and a puffy canopy (decided: no hanging pods or lanterns; renamed from the draft "Lanternwood") | Meadow and forest path; most of the meadow you clear. Its timber is what the town's buildings grow from | Sap glows amber in the bark |
| Coilwillow | A willow | Silvery-teal hanging strands that end in little coils | Riverbank and lakeshore | Strand tips glow cyan |

**Seasons** (the design doc's backlog has seasons changing the look of the map):

| Season | Look |
| --- | --- |
| Spring | Lighter canopies with pink-violet blossom |
| Summer | Full blue-green canopies |
| Fall | Twinbough turns magenta, coilwillow gold, spirepine stays green with violet cones; fallen leaves on the ground |
| Winter | Snow on the outer tips of the spirepine's lower tiers (decided: on the outside, where the branches reach out) and a cap on its top tier; twinbough drops its leaves, with snow on its bare branches; frosted coilwillow strands |

**Chopping and regrowth** (matches the axe levels and respawn rule in the design doc):

| State | Look |
| --- | --- |
| Sapling | A small version of the tree: what regrows every couple of days after a chop |
| Full tree | About 5 wood with the basic axe |
| Old giant | About 1.3 times bigger; needs the steel axe and gives the most wood |
| Stump | Cut face with growth rings that glow with sap, a dribble of sap, wood chips around it and a new shoot already sprouting. The iron axe clears it |

**Night:** each species has its own glow (lilac cones, amber sap, cyan tips), so the forest path is lit and readable after dark and stumps show their glowing sap rings.

## 6. Creatures and people (draft)

- **Puffhen:** a round, fluffy bird, almost a ball with legs. Its eggs glow faintly.
- **Mossback:** a slow, gentle grazer with real moss and little plants growing on its back.
- **Woolhorn:** a shaggy goat-like animal with spiral horns (the same spiral as coilsquash and the trunks: spirals are a family trait of the planet's life).
- **Townsfolk:** human proportions and warm, readable faces. Proposed small differences (choose one or two): slightly longer pointed ears, a faint pattern of freckle-like marks across the cheeks, or eyes that catch the light at night. Nothing that makes them hard to read as friendly.
- **The farmer:** a human from Earth. See section 6.1.

### 6.1 The farmer (draft)

Concept sheet: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf

**Who:** a spacer from Earth, not a farmer by trade. He crash-lands near Ember Bay and learns to farm from Maggie. He is the only Earth human on the planet.

**The plain human.** The townsfolk are humanoid with small differences; the farmer is the ordinary one they are measured against: round ears, plain eyes, Earth proportions. His look stays grounded so theirs can be the one that differs.

**Day-one outfit (ship gear):**

| Item | Look | Colour |
| --- | --- | --- |
| Flight coverall | One-piece, zip front, navy collar, belt and sleeve bands | Pale blue-silver `#C9D6E8`, shade `#A3B3CB`, navy `#34405E` |
| Flight boots | Heavy, slightly oversized | Charcoal `#3B3F4A` |
| Mission cap | Worn, with an Earth patch on the front | Navy `#34405E`, patch `#3D7FC4` with green |
| Earth patch | Small blue-and-green planet on the chest | `#3D7FC4`, `#5BAF6A` |
| Wrist unit | The only ship tech that still works; could show the time in the world, matching the HUD clock | Pale blue screen `#9FD3FF` |

The coverall uses the ship's blue-silver from section 2, so he starts out looking like he belongs to the ship, not the town.

**His look changes over time.** As Ember Bay becomes home, the town's warm colours replace the ship's cool ones:

1. **Day one:** coverall, boots, mission cap.
2. **Settling in:** a gift from the town, an ember-orange knitted scarf (draft: from Maggie).
3. **At home:** a local work shirt in honey, teal overalls with the Earth patch sewn onto the bib, a woven sun hat with an ember band. He keeps the flight boots and the wrist unit.

Decided: the outfit changes with the story. Draft trigger for each step: a story beat (e.g. a lantern story or a rebuild), not a menu choice. If the game later gets a wardrobe, these become the first three outfits.

**Fixed character.** Decided: in this version the farmer is a fixed character with no customisation. A character creator (skin tone, hair, outfit) is a possible future enhancement, not in scope now.

**Readability (pillar 4):**

- From the overhead camera, the cap's brim (later the hat's brim) shows which way he faces. It replaces the prototype's "nose" block.
- The tool in hand is always visible, so the next action reads at a glance.
- Size and collision stay as they are now (capsule 1.6 m tall, radius 0.35) so the art upgrade doesn't change gameplay.

**Build notes:** chunky: about 3 heads tall, with a big head, wide body, short legs and oversized boots and hands, for a cozy read and so the face and the tool read from the overhead camera (decided: chunkier than the first 4-head draft). Low-poly with three tones per part (highlight, lit and shade facets), a soft dark outline, a little contact shadow (under the cap brim, under the chin, on the ground) and small details that read up close: boot toe caps and laces, knee pads, a chest pocket, fabric folds at elbows and knees, stitching on the overalls, a weave on the hat. One material using a palette texture. Rigged to Godot's humanoid skeleton profile so shared animations can be retargeted. First animations needed: idle, walk, hoe swing, watering, plant, harvest, carry, sleep and pass out.

## 7. Places

| Place | Mood | Key visual |
| --- | --- | --- |
| Meadow (farm) | Home, open, sunny | Tidy plot surrounded by wilder, overgrown native plants waiting to be cleared |
| Forest path | Sheltered, a bit mysterious | Blue-green canopies, light shafts by day, glowing plants by night |
| Town | Tired at first, warmer with each rebuild | Rebuilt buildings get fresh wood, ember-orange roofs and lit windows; unbuilt ones stay grey and boarded up. Progress is visible from the square |
| Lakeshore and dock | Calm, a little melancholy | Turquoise shallows with drowned rooftops below; lanterns on the water at the festival |
| River | Lively | Snowmelt falls, rocky creek, wide slow meadow stretch |
| Mountains and mine | Gets stranger the deeper you go | Upper levels: timber and lantern light. Deep levels: violet glows, then the ship-metal colour |
| Crash site | Out of place | Smooth, pale, iridescent hull against the organic forest; each installed part lights a section |

### 7.1 The farmhouse: grown, not built

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (The Farmhouse page: the run-down, restored and night board, and the "A little more alien" board)

**Decided direction: "Grown, not built".** Four directions were compared: the Earth-style cabin, grown, a toadstool-cap roof and a lake-shell roof. Grown was chosen. Ember Bay's people don't just cut timber; they grow their buildings, coaxing living native trees into posts and roofs. It sets the look for the whole town, not just the farmhouse.

**What it is:** an old Ember Bay home in the meadow, left empty when families moved away. Maggie shows the farmer to it on day one, and it becomes his first home on the planet.

**Familiar shape, alien material.** It still reads as a cabin from across the farm: a gabled roof, a door in the middle facing the plot, two windows and a stone base. Up close, it's grown:

- **Living corner posts:** twisted violet trunks that are still alive, branching at the top into small blue-green leaf clusters with violet undersides.
- **Leaf roof:** giant overlapping teal leaves in rows instead of shingles, with a fringe of leaf tips hanging over the eaves and the gable edges, and a new frond sprouting from the ridge.
- **Mauve timber walls:** planks of native wood with a wavy grain.
- **Arched windows and door,** and a leaf-shaped attic window in the gable.
- **Ember-orange door with a spiral carving:** the town's warm accent and the planet's spiral motif.

**At night:** the leaf tips along the eaves and on the posts glow cyan, and the windows glow warm amber with light spilling onto the ground: the town's warm light against the planet's cool glow (section 2).

**A piece of the ship.** His first repair is to patch the worst gap in the leaf roof with a hull panel from the crash: pale blue-silver, riveted, with a navy stripe. It stays when the house is restored, and new leaves grow up around its edges, so a little of the ship is always part of his home.

**Run-down to restored.** The house is alive, so restoring it means helping it grow back:

| State | Look |
| --- | --- |
| Run-down (day one) | Wilted olive-brown leaf roof with thinning rows and several gaps; drooping, dry leaf tips; bare grey trunks with dead twigs instead of leaf clusters; greyed walls with missing planks; one window boarded up and one cracked; the faded door left ajar; weeds and a creeper at the base; fallen leaves on the ground. No glow at night. |
| Restored | Green leaf roof in full rows, with the hull-panel patch; fresh teal leaf tips; living violet trunks with leaf clusters and new shoots; mauve walls; arched windows with clean glass; the ember-orange door with its spiral carving; a flower box of native plants; a lantern by the door; a stone chimney with smoke. |

A stone chimney sits on the back half of the roof in both states.

**What restores it.** Decided: a house upgrade that becomes available once the carpenter (Tom) moves back to Ember Bay. Until then the house stays run-down, apart from the hull-panel patch. Ordering the upgrade from Tom switches it from run-down to restored. Cost and build time are a gameplay decision, still to set in the design doc.

**Palette:** mauve timber `#9C7E8C` (side `#7E6474`, grain `#6E5664`, trim `#5A4458`), living trunks `#6B5A78`, run-down: wilted roof `#8A7A4A`, dry tips `#9A7E44`, grey trunks `#6E6670`, greyed walls `#8A7C80`; leaf roof `#3E8F82` (rows `#2F7266`, tips `#4FA294`, far side `#2F6B60`), ember door `#E9773A`, stone `#8F8A96`, window glow `#FFB04A`, leaf-tip glow `#6FF2E0`.

**Fits the game:** the same 6 x 4 m footprint and 3 m walls as now, with the door on the side facing the plot, so the collision box and the sleep door point don't move. The roof is a little taller than the prototype's, and the posts and leaf fringe stick out by a few centimetres, which doesn't affect collision. The front step stays within about 0.3 m of the wall, clear of the shipping bin.

**For the town:** every rebuilt building is grown the same way, and the grown look is how restoration shows: a building that comes back to life sprouts new leaves.

## 8. Style and technical direction (draft)

- **Style:** stylised low-poly with flat or softly stepped shading, simple shapes, and colour doing most of the work. No realistic textures. This matches what the prototype already uses (REQ-12 "low-poly"), so the upgrade is a better version of the same direction, not a change.
- **Lighting carries the mood:** most of the alien feel comes from the sky, the light colour and the glows, which are cheap to render. Models stay simple.
- **Motion:** gentle wind sway on plants, slow "breathing" pulses on glows, light ripples on water. Small motion makes a low-poly world feel alive.
- **Steam Deck first:** every effect must hold the frame rate on the Deck at 1280 x 800 (the risk register in `docs/requirements/technical-blueprint.md` already plans for low-poly art and Deck testing). Effects that cost too much get a lower-cost fallback.
- **Separate from gameplay:** new art lives in its own folder and scenes, never in the rules (Domain, Application). A gameplay scene adopts a new asset by swapping a model or material, without changing any rules.

## 9. Open questions

- [x] How alien overall? Decided: a little further than the first draft for buildings, which are "grown, not built" (section 7.1). The rest keeps "familiar at first, different on a second look".
- [ ] Moon names: Hearth and Wisp, or something else?
- [ ] Which small differences for the townsfolk (ears, cheek marks, eyes)?
- [x] The farmer: fixed character for now; customisation is a future enhancement
- [x] The farmer: his outfit changes with the story
- [x] The farmer: chunky proportions, about 3 heads tall
- [x] The farmhouse: restored by a house upgrade, available once the carpenter moves back
- [ ] The farmhouse: any further upgrade stages after restored (e.g. a bigger house)?
- [ ] Flat shading (sharp facets) or soft stepped shading (smoother, more painterly)?
- [ ] Camera: the art should be checked from the camera the game ends up using (the design doc still has "fixed overhead or rotate and zoom" open).

## 10. Next steps

1. Settle the theme and palette above.
2. Build an art sandbox in `game/art/`: a scene, separate from the farm, to try the palette, sky, two moons, water, glow, trees, rocks, the farmhouse and the crops at each time of day.
3. Review it on the Steam Deck.
4. Apply the approved pieces to the farm scene one at a time.
