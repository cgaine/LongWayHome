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
| **Mostly familiar** | Buildings, tools, furniture, fences, boats, roads, the residents' clothes | These carry the cozy cabin-country feel. They are built by people, so they can look like home. |
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

**Idea:** warm town, cool planet. The town, wood, lanterns and people lean warm (ember orange, honey wood). The planet's living things lean a little cool and strange: foliage shifted toward teal, shadows toward violet, glows in cyan and soft violet. Earth-like greens and browns are still most of the screen, so the shift is felt rather than shouted.

| Role | Colour | Hex | Notes |
| --- | --- | --- | --- |
| Meadow grass, lit | Fresh green with a little yellow | `#7FA65A` | Close to Earth, so the farm feels like home. |
| Meadow grass, shade | Teal-green | `#3F7A6C` | Shadows shift toward teal instead of just getting darker. |
| Native foliage | Blue-green | `#3E8F82` | Trees and wild plants: the first "not quite Earth" colour you notice. |
| Leaf undersides and new growth | Dusty violet | `#7A68A8` | Shows when leaves sway in the wind; a second-look detail. |
| Dry tilled soil | Warm brown | `#8A5A3C` | |
| Watered soil | Dark brown | `#3E2A20` | Must stay clearly darker than dry soil (pillar 4). |
| Wood (buildings, fences) | Honey wood | `#A8774C` | Warm and familiar. |
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

## 4. Crops

Each crop: a familiar silhouette, one alien detail, and its own colour so it reads at a glance. The descriptions come from Seasons and crops in the design doc.

| Crop | Familiar read | Alien detail | Colour |
| --- | --- | --- | --- |
| Emberroot | Carrot | The root's shoulder shows above the soil and has a faint warm inner glow ("warm to the touch"); a little heat shimmer on cold mornings | Ember orange |
| Dusktuber | Potato or beet | Deep purple, smooth, with paler speckles like a night sky; leaves curl at the tips | Dusk purple |
| Moonmelon | Pumpkin | Pale blue-white gourd with faint ribs; glows softly at night | Moon blue-white |
| Sunbell | Tomato or pepper plant | Fruit hangs like bells and sways (and rings) in the wind | Sun yellow |
| Starcorn | Corn | Kernels glitter in the light like tiny stars | Gold with sparkle |
| Coilsquash | Squash | Grows in a spiral | Green and cream bands |

Growth stages (seed, sprout, young, ripe) keep the readable stage shapes already in the game (REQ-12). The art upgrade changes how they look, not what stage they show.

## 5. Plants, trees and glowing plants (draft)

- **Trees:** shaped like pines and broadleaf trees, with a twist: blue-green canopies, slightly spiralled trunks, and violet undersides that show in the wind.
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

**Build notes:** chunky: about 3 heads tall, with a big head, wide body, short legs and oversized boots and hands, for a cozy read and so the face and the tool read from the overhead camera (decided: chunkier than the first 4-head draft). Low-poly, flat facets with a lit side and a shade side, one material using a palette texture. Rigged to Godot's humanoid skeleton profile so shared animations can be retargeted. First animations needed: idle, walk, hoe swing, watering, plant, harvest, carry, sleep and pass out.

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

## 8. Style and technical direction (draft)

- **Style:** stylised low-poly with flat or softly stepped shading, simple shapes, and colour doing most of the work. No realistic textures. This matches what the prototype already uses (REQ-12 "low-poly"), so the upgrade is a better version of the same direction, not a change.
- **Lighting carries the mood:** most of the alien feel comes from the sky, the light colour and the glows, which are cheap to render. Models stay simple.
- **Motion:** gentle wind sway on plants, slow "breathing" pulses on glows, light ripples on water. Small motion makes a low-poly world feel alive.
- **Steam Deck first:** every effect must hold the frame rate on the Deck at 1280 x 800 (the risk register in `docs/requirements/technical-blueprint.md` already plans for low-poly art and Deck testing). Effects that cost too much get a lower-cost fallback.
- **Separate from gameplay:** new art lives in its own folder and scenes, never in the rules (Domain, Application). A gameplay scene adopts a new asset by swapping a model or material, without changing any rules.

## 9. Open questions

- [ ] How alien overall? This draft sets it at "familiar at first, different on a second look". Should any area go further (e.g. the forest)?
- [ ] Moon names: Hearth and Wisp, or something else?
- [ ] Which small differences for the townsfolk (ears, cheek marks, eyes)?
- [x] The farmer: fixed character for now; customisation is a future enhancement
- [x] The farmer: his outfit changes with the story
- [x] The farmer: chunky proportions, about 3 heads tall
- [ ] Flat shading (sharp facets) or soft stepped shading (smoother, more painterly)?
- [ ] Camera: the art should be checked from the camera the game ends up using (the design doc still has "fixed overhead or rotate and zoom" open).

## 10. Next steps

1. Settle the theme and palette above.
2. Build an art sandbox in `game/art/`: a scene, separate from the farm, to try the palette, sky, two moons, water, glow, trees, rocks, the farmhouse and the crops at each time of day.
3. Review it on the Steam Deck.
4. Apply the approved pieces to the farm scene one at a time.
