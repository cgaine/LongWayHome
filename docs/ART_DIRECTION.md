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
| Watered soil | Dark brown | `#4A3226` | Must stay clearly darker than dry soil (pillar 4). |
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

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Sky and light page)

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

**The moons in detail** (colours match the art sandbox, `game/art/sandbox/ArtSandbox.cs`):

| | Hearth | Wisp |
| --- | --- | --- |
| Colour | Soft gold `#F2D38A` | Blue-white `#CFE2FF` |
| Size in the sky | About twice Earth's Moon | About half Hearth's size |
| Path | Rises in the east at dusk; still up when the day ends at 2 am | Rises about 9 pm, crosses the whole sky and sets about 1 am |
| Shadow | Higher and brighter: shorter, darker, warm-tinted | Lower and fainter: longer, lighter, cool |
| Second look | A faint tilted ring | A thin trail of haze behind it as it moves |
| Phases (draft) | One cycle per Season (28 days), full on day 14 | One cycle a week, full on days 7, 14, 21 and 28 |

So both are full together on day 14 of every Season. Phases are display only for now.

**Sky by time of day:**

| Time | Sky (top → horizon) | Sun and moons |
| --- | --- | --- |
| Morning | `#8EC5E8` → mint `#CDE9DA` → peach `#FFE2B8` where the sun rises | Low sun `#FFE2B8` over the mountains; mist bands over the lake and meadow |
| Day | `#8EC5E8` → mint `#CDE9DA` | High sun, short soft shadows |
| Dusk | Amber `#F0A060` → rose `#B85C86` | Low orange sun in the west, Hearth rising in the east, windows lit |
| Night | Indigo `#1B2142` → `#2A3160`, with stars | Hearth and Wisp; two faint shadows; glowing plants, leaf tips and lit windows |

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
Crop glows show only on ripe crops, so a glow also means "ready to harvest"; they wake between 19:30 and 20:30 and stay on until the day ends (#94).

## 5. Plants, trees and glowing plants (draft)

- **Trees:** three native species shaped like trees you know. See section 5.1.
- **Grass and wild plants:** mostly familiar, with a few patches of native plants: curled fronds, round puffballs, tall bead-like stalks.
- **Glowing plants:** see section 5.2.

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

### 5.2 Glowing wild plants (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Glowing plants page)

The planet's signature. They're picked by hand and only at night (design doc), so they're dull and easy to miss by day and unmistakable after dark. Each has its own glow colour and silhouette, so they can be told apart in the dark and from any camera angle.

| Plant | Season and place | By day | At night | Glow |
| --- | --- | --- | --- | --- |
| Dawnmoss | Spring; on rocks along the forest path | Pale green moss cushion on a lilac-grey rock | A soft cyan carpet over the rock; fades at sunrise | Cyan `#6FF2E0` |
| Firefrond | Summer; forest path and riverbank | Curled dark-red fern fronds (fiddleheads) | The brightest glow of all: warm amber that pools light on the ground around it, matching its "lights up dark places" effect | Amber `#FFB04A`, the largest glow radius |
| Ashpetal | Fall; meadow edges and riverbank | Grey-white five-petal flower, a few fallen petals | Petals glow peach-white and drift up into the air | Peach-white `#FFE6C8` |
| Frostlantern | Winter; in the snow | A frosted bulb hanging from a curved stalk, like a lamp post | Glows cool blue from inside, like a lantern | Ice blue `#8FC8FF` |
| Snowwick | Winter; rare | A thin white candle-like stem with a closed tip | A single bright violet-white flame at the tip | Violet `#B48CFF`, the "rare" glow (section 2) |
| Glimmercap | All year; forest and mine | A cluster of grey-teal mushrooms | Caps turn teal with bright cyan speckles | Cyan; common, so cheap to draw and place in numbers |

**Ready to pick.** At night, small motes in the plant's glow colour rise above it, so you can see from a distance which plants can be picked.

**Picking.** The same target marker as the farm tiles (cream highlight, ember corners) sits under the plant the Use button will take, with a "Pick" prompt (section 7.2).

**At night on the path.** Plants of different colours light the forest path together with the trees' night glows (lilac spirepine cones, amber sap, cyan coilwillow tips), so the path stays readable and inviting after dark (pillar 1). Sketched on the "forest path at night" board.

## 6. Creatures and people (draft)

- **Farm animals:** puffhen, mossback and woolhorn. See section 6.3.
- **Townsfolk:** see section 6.2.
- **The farmer:** a human from Earth. See section 6.1.
- **Tools in hand:** what the farmer carries. See section 6.4.

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
3. **At home:** a local work shirt in honey, teal overalls with the Earth patch sewn onto the bib, a woven sun hat with an ember band. He keeps the flight boots and the wrist unit. He also keeps the flight coverall: sealed with the diving helmet Mira forges, it becomes his diving suit for the lake (design doc, Beyond the farming loop).

Decided: the outfit changes with the story. Draft trigger for each step: a story beat (e.g. a lantern story or a rebuild), not a menu choice. If the game later gets a wardrobe, these become the first three outfits.

**Fixed character.** Decided: in this version the farmer is a fixed character with no customisation. A character creator (skin tone, hair, outfit) is a possible future enhancement, not in scope now.

**Readability (pillar 4):**

- From the overhead camera, the cap's brim (later the hat's brim) shows which way he faces. It replaces the prototype's "nose" block.
- The tool in hand is always visible, so the next action reads at a glance (section 6.4).
- Size and collision stay as they are now (capsule 1.6 m tall, radius 0.35) so the art upgrade doesn't change gameplay.

**Build notes:** chunky: about 3 heads tall, with a big head, wide body, short legs and oversized boots and hands, for a cozy read and so the face and the tool read from the overhead camera (decided: chunkier than the first 4-head draft). Low-poly with three tones per part (highlight, lit and shade facets), a soft dark outline, a little contact shadow (under the cap brim, under the chin, on the ground) and small details that read up close: boot toe caps and laces, knee pads, a chest pocket, fabric folds at elbows and knees, stitching on the overalls, a weave on the hat. One material using a palette texture. Rigged to Godot's humanoid skeleton profile so shared animations can be retargeted. First animations needed: idle, walk, hoe swing, watering, plant, harvest, carry, sleep and pass out.

### 6.2 The townsfolk (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Townsfolk page)

**Same world, same build.** The residents share the farmer's chunky proportions (about 3 heads tall), low-poly shading and soft outline, with their own heights and builds (Tom broad and tall, Maggie and Ottlie small, Jesse slight). They read as warm, friendly people first.

**What makes them not quite human** (the farmer is the only Earth human, section 6.1). Warm and friendly first, subtly less Earth-human on a second look:

1. **Pointed ears:** a little longer than human ears, angled up and back. Chosen because it reads from the overhead camera, where facial details don't.
2. **Cool undertone:** skin stays close to the human range, but the shadows of the face, neck and hands lean slightly blue or green (each resident has their own undertone, below). It reads as a little alien without being obvious; lit skin stays warm.
3. **Cheek marks:** freckle-like marks on both cheeks, clearly darker than the skin by day, in a pattern unique to each person, like a fingerprint. At night they glow in the same colour as that person's hair glow.
4. **Hair colours Earth doesn't grow:** teal, lavender-silver and white alongside browns and auburns.
5. **Glowing hair, only after dark:** a few strands or the tips of the hair glow softly, in each resident's own colour, close to their hair colour. By day, hair looks normal.

At night, every resident's marks and hair glow in their own colour, so people can be told apart across the dark square.

**Undertone, glow colour and marks:**

| Resident | Undertone (in the shadows) | Glow colour (hair and marks) | Cheek marks |
| --- | --- | --- | --- |
| Maggie | Blue-lilac `#6A7AB0` | Lavender `#D8C8FF` | Two rows of stitch-like dots |
| Clyde | Blue `#4A7A9A` | Teal shimmer `#6FF2E0`; his cap hides his hair, so the shimmer is in his beard tips and the hair at his temples | Two short waves, like ripples |
| Tom | Green `#4A8A6A` | Warm copper `#FFB870`, in his hair and beard | Three bold dots in a triangle |
| June | Teal-green `#3E8A7A` | Ember `#FF9A50`, on the edges of her curls | A curve of dots |
| Jesse | Blue `#5A7AB0` | Mint `#8AFFC8`, on the spike tips and the lock over the eye | One bold dot between two small ones |
| Mira | Teal-green `#3E7A72` | Indigo `#A89CFF`, a cool glow for very dark hair, mostly in the ponytail | Three short dashes |
| Ottlie | Blue `#6A8AB0` | Moonlight white `#E0FFFA`, along the braid | A long trail of dots from temple to jaw, spread out with age |

**The residents:**

| Resident | Arrives | Look | Palette |
| --- | --- | --- | --- |
| Maggie | From the start | Elderly and small. Lavender-silver bun, round glasses, cardigan, long skirt, cream shop apron | Lavender `#9A7EA8`, teal skirt `#3E6E6A`, cream `#EDE3CF` |
| Clyde | From the start | Old fisherman. Teal knit cap, big white beard, mustard oilskin coat, fishing rod over his shoulder | Mustard `#C9A040`, teal cap `#3E6E7A`, slate `#4A5568` |
| Tom | Carpenter stage 1 | The biggest build in town. Rust check shirt, rolled sleeves, short brown beard, tool belt with a hammer | Rust `#B0503A`, brown `#6E5A44`, leather `#7A5232` |
| June | Carpenter stage 1 | Auburn curls, cream blouse, teal skirt, ember-orange café apron, always a mug in hand | Auburn `#A0482E`, teal `#3E8F82`, ember `#E9773A` |
| Jesse | Carpenter stage 1 | Teen, slight. Messy teal hair over one eye, violet hoodie, dark trousers, white trainers | Teal hair `#2F6B6E`, violet `#6A5A9A` |
| Mira | Blacksmith stage 1 | Dark ponytail with an ember headband, leather smith apron with straps, gloves, rolled sleeves, forge hammer | Charcoal `#4A4652`, leather `#7A4A2E`, ember band |
| Ottlie | Later (lumen-reader) | Small elder. Long white braid with glowing buds, violet shawl, moss-green dress, staff topped with a glowing bulb | Moss `#4E7A6A`, violet `#7A68A8`, glow `#6FF2E0` |

Each resident's palette is distinct in hue and value from the others and from the farmer's blue-silver, so they're told apart from the overhead camera.

**Still to design:** the rancher family, the retired schoolteacher, and Clyde's three grown kids (design doc, Returning households).

### 6.3 Farm animals (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Farm animals page)

Three native creatures in familiar farm roles (design doc, Ranch and animals). Round, soft shapes and gentle faces keep them cozy; the second look is in their bodies and their night glow. They use the same low-poly shading and soft outline as the characters, with three tones per part, contact shadows and small details: the puffhen's layered wing feathers, scaled legs and three-toed feet; the mossback's knees and cloven hooves; the woolhorn's curly wool texture, ridged horns and cloven hooves.

| Animal | Familiar read | Look | Product | At night |
| --- | --- | --- | --- | --- |
| Puffhen (coop) | A hen | Almost a ball with legs: round, fluffy cream body `#F2E2C8` with scalloped fluff, an orange beak and legs, and a violet spiral curl on its head | Speckled eggs that glow amber `#FFB04A` in the dark | Its curl glows amber |
| Mossback (barn) | A cow | A slow, gentle grey-blue grazer `#8C98A8` with darker slate patches, head low to graze. A cow's head: broad flat forehead, small pale horn nubs, floppy ears with pink insides, and a broad, flat pink-grey muzzle `#C9A8A8` with big nostrils and a wet shine. Its back is a living moss cushion: layered dark-to-light greens (`#3E6A34`, `#5E8C46`, `#94C066`), a fuzzy top edge, stippled texture, strands draping down its sides, teal lichen, a fern sprig, violet flowers and a tiny mushroom; its tail ends in a moss tuft | Pale mint milk `#DCF0E4` in a jar | The moss on its back glows cyan `#6FF2E0` |
| Woolhorn (barn) | A goat | A shaggy lavender-cream wool cloud `#E8E0EC` on thin dark legs, a darker face with a wool beard, spiral horns (the planet's spiral motif) and amber eyes with a bar-shaped pupil | Wool | The wool tips shimmer lilac `#D8C2FF` |

**Young animals** (bought from the ranchers) are the same design at about 60 % size.

**Mood reads at a glance (pillar 4):**

| Mood | Cue |
| --- | --- |
| Happy (fed and content) | A small heart in the animal's own glow colour above its head. Happy animals give better products (design doc): a happy puffhen's egg glows brighter |
| Hungry (not fed today) | A thought bubble with a leaf |
| Asleep (night) | Eyes closed, its night glow showing |

**Still to design:** the coop and barn (grown, like the farmhouse, section 7.1), feed troughs, and products as inventory icons.

### 6.4 Tools in hand (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Tools in hand page)

**Always in hand.** Whatever is selected in the hotbar is in the farmer's hand, so the next action reads at a glance (pillar 4). That includes seeds, which the game already lets him hold. He holds it in the hand without the wrist unit, so both stay visible. Long tools stand head up beside him; the watering can and the seed pouch hang below the hand, so they never read as long tools.

**Short and stocky.** Tools follow the farmer's chunky build (section 6.1): short, thick handles and oversized heads, so a long tool stands only about chest high beside him and the head reads from the camera. The sheet shows him in his "at home" outfit (work shirt, teal overalls, sun hat), the look he farms in for most of the game.

**Mostly familiar.** Tools are made by people, so they keep Earth shapes (section 1). The second look is in the materials: handles of native mauve timber with its wavy grain, and heads of the planet's own metals.

| Item | Status | Silhouette |
| --- | --- | --- |
| Hoe | In game, starting tool | An L: the blade juts out from the top of the handle and turns down |
| Watering can | In game, starting tool | A round body with a long spout and a sprinkler rose, carried by its top handle |
| Seeds | In game | A cloth pouch tied at the neck; a patch and a few seeds peeking out show the crop's colour |
| Axe | Planned (Maggie, 50g) | A single wedge head with a pale sharpened edge and a short poll |
| Pickaxe | Planned (Maggie, 75g) | A curved, double-pointed head: the widest silhouette |

**From above.** The camera rotates freely and tilts toward top-down when zoomed out, so what shows from overhead is the head: a short arm and plate (hoe), a round body with a spout (can), a small pouch (seeds), a wedge on one side (axe) and a long crescent across both sides (pickaxe).

**Upgrade levels.** Mira upgrades every tool through the same four metals (design doc, Tool upgrade levels). Each level reads three ways, so it never relies on colour alone:

| Level | Metal | Head | Handle |
| --- | --- | --- | --- |
| Basic | Plain grey `#9A98A2` | Smallest; the basic can is dented | Plain timber |
| Rosk (copper tier) | Rose copper `#C77A52` | Larger; the can gets rivets | 1 band, cord grip |
| Groth (iron tier) | Dark blue-grey iron `#59627A` | Larger again; the can's rose widens for 3 tiles | 2 bands, leather grip |
| Forgelight (steel tier) | Pale steel `#C9D2E0` with an Embrite inlay `#B48CFF` | Largest; the widest can rose for 3 x 3 | 3 bands, ember-orange grip |

The head grows with what the tool covers: the hoe blade widens from 1 tile to 5 x 5, the can's body grows with its uses, and the axe and pickaxe heads grow with what they can break.

**At night.** Ordinary metals darken with the scene but keep their shape. Forgelight's Embrite inlay glows soft violet, the rare glow from the deep mine (section 2), so the best tools read in the dark and carry a little light into the mine.

**Palette:** timber `#9C7E8C` (light `#B396A3`, shade `#7E6474`, grain `#6E5664`), Basic `#9A98A2`, Rosk `#C77A52`, Groth `#59627A`, Forgelight `#C9D2E0`, Embrite `#B48CFF`, Forgelight grip `#E9773A`, seed pouch `#D9C39A`.

**Fits the game:** held tools are visual only. They don't change the farmer's capsule or the tile he targets, and they become the props for the first tool animations (hoe swing, watering, plant, harvest).

**Still to design:** the fishing rod (Clyde's first lantern), sprinklers and other Forgelight parts, tools as hotbar icons, and the tool-use animations.

## 7. Places

| Place | Mood | Key visual |
| --- | --- | --- |
| Meadow (farm) | Home, open, sunny | Tidy plot surrounded by wilder, overgrown native plants waiting to be cleared |
| Forest path | Sheltered, a bit mysterious | Blue-green canopies, light shafts by day, glowing plants by night |
| Town | Tired at first, warmer with each rebuild | Rebuilt buildings get fresh wood, ember-orange roofs and lit windows; unbuilt ones stay grey and boarded up. Progress is visible from the square |
| Lakeshore and dock | Calm, a little melancholy | Turquoise shallows with drowned rooftops below; lanterns on the water at the festival |
| River | Lively | Snowmelt falls, rocky creek, wide slow meadow stretch |
| Mountains and mine | Gets stranger the deeper you go | Upper levels: timber and lantern light. Deep levels: violet glows and softly glowing Embrite, then the ship-metal colour |
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

### 7.2 The farm plot and meadow ground (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Farm plot and meadow page)

**The meadow.** Mostly familiar, Earth-like meadow green (`#7FA65A`) so the farm feels like home, with patches that shift toward teal in shade (`#3F7A6C`). Scattered through it are small native plants that give the second look: curled teal fronds, violet puffballs on stems, and stalks of small amber beads. Grass tufts, rocks (lilac-grey stone `#9A95A2`), stumps and saplings fill the overgrown parts the farmer clears to expand the farm (design doc: "the rest of the meadow is overgrown with trees, stumps and rocks"). Mountains sit on the horizon.

**The plot reads apart from the wild meadow:**

- A mown margin of lighter, shorter grass (`#8CB862`) around the plot.
- A low edging of rounded lilac-grey stones along all four sides. It's low enough not to block movement or the camera.
- A dirt path (`#B8946C`, with stepping stones) from the farmhouse door to the plot.
- The refill pond sits beside the plot, with turquoise shallows and a deeper blue centre, and a coilwillow behind it (section 7.4).

**Tile states (pillar 4).** Each tile is a 1 x 1 m slab with a little visible thickness at the front and side, so the grid reads without drawn lines:

| State | Look |
| --- | --- |
| Grass (untilled) | Meadow green with a few grass tufts and an occasional native plant |
| Tilled | Warm brown soil `#8A5A3C` with furrows |
| Watered | Dark soil `#4A3226` with a wet sheen; clearly darker than tilled at every stage and at night |

**Target marker.** The tile the Use button will act on gets a soft cream highlight (`#FFF6E0`) with ember-orange corner brackets (`#E9773A`). It looks the same on every tile type, and it replaces the prototype's faint grid outline as the main "what will happen" cue.

**Seasons (grass):** spring fresh green `#8CB862`, summer meadow green `#7FA65A`, fall golden `#A8A458`, winter snow `#E4ECF0` with grass tips showing. The mown margin is a lighter shade of each Season's meadow: spring `#A6D07C`, summer `#8CB862`, fall `#C0BC72`; in winter it is snow `#E4ECF0` like the meadow. Tilled and watered soil look the same all year.

**Night:** tiles darken with the rest of the scene, but tilled, watered and the target marker all stay distinct.

### 7.3 Town buildings (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Town buildings page)

**Grown, like the farmhouse.** Every town building uses the farmhouse's language (section 7.1): living violet trunk posts, leaf roofs, mauve timber, arched windows and an ember-orange door. Buildings are told apart by silhouette and props, so each one reads from across the square and from any camera angle.

**Restoration is visible.** Before a rebuild, a building looks like the run-down farmhouse: wilted leaves, gaps in the roof, boarded windows and weeds. Each rebuild stage makes it more alive: fresh leaves first, then new growth and extensions grown onto the side. This follows "a building that comes back to life sprouts new leaves" (section 7.1).

| Building | Who | Identity | Stages (design doc) |
| --- | --- | --- | --- |
| General store | Maggie | Leaf awning over a big round-topped shop window, produce crates, round sprout sign | 1. Maggie's corner: one fixed corner (a lit window, a small awning, a few crates) on an otherwise wilted building. 2. Reopened: the whole building alive. 3. Storeroom: a grown storeroom on the side with sacks. 4. Expanded shop: a second awning, more crates and string lights |
| Carpenter | Tom and the Hollis family | Big braced double doors, plank stacks, saw sign; the family lives upstairs | 0. Empty. 1. Reopened. 2. Workshop: a side shed with a workbench and sap barrels that glow amber (sealed planks). 3. Builder: timber frames for new homes and stacks of sealed planks |
| Blacksmith | Mira | The heaviest building: a stone lower storey, an open forge arch that glows orange, anvil, quench trough, hammer sign, big stone chimney | 0. Empty: a cold, dark forge. 1. Reopened: the forge glows again. 2. Forge: a smelting furnace on the side, more smoke. 3. Master smith: a teal weathervane and finished metal parts on display |
| Café | June | A round window, leaf parasols over outdoor tables, a chalkboard, a mug sign and string lights | Empty, then open (stages still to be decided in the design doc) |
| Homes | Returning households | The farmhouse design | Empty, then rebuilt |

**At night.** Amber windows and string lights are the town's warm light (section 2). The forge glows orange, sap barrels glow amber and the leaf tips glow cyan. Homes show **friendship lanterns** in the window, one per friendship step, as the design doc proposes, so the town visibly gets brighter as friendships grow.

**Still to design:** the ranch house, the museum, the dock houses for Clyde's family, and the bridge (only sketched in the town square's mood sketch). The town square is section 7.5.

### 7.4 The pond and the shipping bin (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Pond and shipping bin page)

The two things beside the plot that the farmer uses every day. Each shows where to stand and what state it's in at a glance (pillar 4).

**The refill pond.** A natural meadow pond with an irregular shoreline, not a perfect circle. Clear turquoise shallows over a pebbly bed shade into a dark blue centre (`#7CC2B4` at the edge to `#173F58` in the middle). It has a damp bank and grass growing to the water's edge, a few lilac-grey stones half in the water, and a coilwillow leaning over it with its reflection upside down in the water.

- **Where to stand:** a flat refill stone on the plot side, inside the 3.8 m refill range. The shoreline is kept steady there so the stone always sits at the water.
- **Moving water:** shimmer drifting across the surface, ripples spreading now and then, lily pads bobbing, and the willow strands and reeds swaying, so the pond feels alive even when the farmer stands still. In game this is a light water shader, cheap enough for the Steam Deck.
- **Refilling:** ripples spread from the refill stone and a few drops splash up when the can fills.
- **Second look:** a warm spring feeds the deep centre, so bubbles rise there all year. Lily pads have violet edges, there are amber bead reeds and curled fronds, and in summer a glassfly with four round wings hovers over the water.
- **Seasons:** spring pads in flower; summer full pads and the glassfly; fall golden willow leaves floating and olive pads; winter ice on the shallows, but the warm-spring centre steams and stays open, so the can can be refilled all year (pillar 1).
- **Night:** Hearth and Wisp reflect gold and blue, and the coilwillow's strand tips glow cyan over the water.
- **Fits the game:** 5 m across with a 3 m deep centre, as now. The bank follows the existing collision edge.

**The shipping bin.** Round, like the round buildings explored on the Town page: a squat drum of mauve timber staves under a small cone of leaves, with a leaf-tip fringe and a sprout for a handle. Two living violet trunk ribs climb its sides, an ember-orange band runs under the lid, and Maggie's round sprout sign on the front marks it as the store's bin.

| State | Look |
| --- | --- |
| Empty (morning) | Lid closed, flag down: yesterday's crops have sold |
| In range (within 1.7 m) | The leaf lid tips back, showing a woven liner, before the prompt appears |
| Crops waiting | Lid closed with crops peeking out and an ember-orange flag up, like a mailbox: they sell tonight |
| Night | A small lantern on a rib lights up, so the bin can be found in the dark; the flag still shows |

**Fits the game:** about 1 m across and 0.8 m tall, so its collision becomes a short cylinder instead of today's 1.2 x 0.8 m box. The 1.7 m range stays.

**Palette:** water `#7CC2B4`, `#3F8E98`, `#245E7A`, `#173F58`; bank `#5A4A3A`; stone `#9A95A2`; bin staves `#AC939E` to `#755C6A`, leaf lid `#3E8F82`, band and flag `#E9773A`, sign `#F0E6C0`, lantern light `#FFB04A`.

### 7.5 The town square (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Town square page: the concept, day one vs restored vs night, and the square's props)

**The heart of the town, and where progress shows.** The plaza is roughly round, like the town's round buildings, but its edge is worn and uneven and breaks up into loose stones in the grass. The well sits just off its centre. Progress can be seen from the square (section 7), but the town around it is not a neat ring: buildings are spread loosely at different distances and angles, some up short paths of their own, so it reads as a village that grew over time rather than a planned layout.

**Set back from the water.** The town sits a short walk from the lake and the river, not on their banks. Belts of native trees (spirepine and twinbough) lie between the town and the water, and winding dirt paths lead through them to the dock and the bridge. A gap in the trees to the south-east keeps the lake in view from the square.

**Layout** (draft; follows the design doc's world layout):

| Direction | What's there |
| --- | --- |
| North | The general store at the head of the square; the forest path to the farm leaves to its north-west |
| West | The carpenter, set back from the plaza; homes scattered along the path west |
| North-east | The blacksmith and a small home, each up its own short path; the path to the bridge over the river to the ranch winds past it through the trees |
| East | June's café, on the way down to the dock |
| South-west and south | More homes, at the ends of short paths |
| South-east | The path through the trees to the dock on the lake, with a drowned rooftop in the shallows |

**The plaza.** Lilac-grey paving `#B3ADB8` laid in rings around the well, with darker joints `#9A94A2`. An ember sandstone spiral `#D49A6E` winds out from the centre: the planet's spiral motif, set in stone. Paths out of the square are packed dirt `#B8946C`, like the farm path. **Second look:** moss in the spiral's joints glows faint cyan after dark.

**Props.** Grown like the buildings, so each one comes back to life with the town:

| Prop | Restored | Day one |
| --- | --- | --- |
| Well | A low stone drum; two living trunk posts arch over it to a small leaf cone with a sprouting crown; a rope and a timber bucket; clear blue water `#2D6A86`. **Second look:** the water glows cyan `#6FF2E0` at night | Wilted roof with gaps, grey posts, no bucket, dark still water, weeds |
| Lamp posts (an uneven ring at the plaza's edge, then more along the paths to the dock, bridge, farm and homes) | A living trunk that curls at the top like a fern frond, with a cream lantern and an ember cap hanging from the curl; amber light `#FFB04A` at night | Grey post, cracked dark lantern. Only the lamp by Maggie's corner works |
| Rebuild signs | Each building's sign out front (design doc, Restoration order): a grown board with leaf trim and three pips for wood, stone and gold that fill as each is delivered. A thin amber outline at night keeps it findable. Gone once the building is rebuilt | One in front of every empty building |
| Benches | Curved timber seat and back on two trunk legs, with a sprout at one end | Faded grey timber |
| Flower beds | Round stone beds of native flowers that glow violet at night | Not there yet |
| String lights | From the store to the café; ready for the Ember Festival | Not there yet |

**Day one to restored:**

| State | Look |
| --- | --- |
| Day one | Every building wilted, with a rebuild sign out front, except Maggie's corner of the store (section 7.3). Cracked paving with weeds and fallen leaves, stones missing from the spiral, the well dry and dark, the path lanterns dead, the bridge broken. At night only Maggie's window and one lamp give warm light |
| Restored | Every building grown back. The spiral whole, benches and flower beds back, string lights up, the bridge mended, residents out |
| Restored at night | The brightest place on the map: amber windows, lanterns and string lights, cyan leaf tips, the glowing well and spiral. Residents are told apart by their glow (section 6.2) |

**Lamps show progress (draft).** Each rebuild relights the lamps nearest that building, so the square and then the paths out of it fill with light as the town comes back. Restored, the lanterns lead the way to the dock and the bridge after dark.

**Fits the game (draft):** a plaza about 24 m across, a 3 m well and 3 m lamp posts. The well's collision is a short cylinder; benches and beds stay off the walking lines between the paths. Everything must read from the overhead camera and from every rotation (section 9, Camera).

**Still open:** whether the well can refill the watering can in town, and how the square is dressed for the Ember Festival.

### 7.6 The early mine, levels 1 to 5 (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (Early mine page: level 1, the entrance, level 5 flooded and drained, and the mine's props)

**The old miners' tunnels.** The mine opens when you buy the pickaxe (design doc, Early mine). The upper levels are timber and lantern light (section 7): warm pools of light in the dark, with the planet's glow showing in the corners. They feel quiet and a little eerie, never threatening; there's no danger in this version.

**Shape.** Rounded chambers joined by short tunnels, seen from above like the farm. Packed-earth floor `#7A685E`, rock walls `#6E6070` with strata and, on a second look, a faint teal band in the rock. Rails and an old ore cart run through.

**Timber frames.** The miners' supports are square, braced frames of mauve native timber `#9C7E8C`. They were cut, not grown, because people made them (section 1, "Mostly familiar"). A frame marks every tunnel mouth, so the way on reads from overhead.

**Light.** It's dark between lanterns. Old lanterns hang from the frames and wall hooks, and the farmer carries a small belt lantern that lights a pool around him. Glimmercaps glow cyan in the corners, so nothing is pure black. Draft: a level's lanterns relight once it's cleared, so cleared levels are warm and new ones are dark (the same idea as the town square's lamps, section 7.5).

**Roots from above.** Twinbough roots break through the ceiling with glowing amber sap seams: the forest overhead, and a hint of the sap that seals the planks for the deeper levels.

**What's in it:**

| Thing | Look | Notes |
| --- | --- | --- |
| Rock | Faceted lilac-grey `#8F8A96` | Gives stone |
| Rosk ore | A rock with rose-copper veins `#C77A52` that curl into little spirals, with bright flecks | Levels 1 to 15 |
| Groth ore | A rock with blocky dark blue-grey crystals `#59627A` | Small deposits from level 3 (design doc, Early Groth) |
| Debris | Rubble with broken old beams | Blocks tunnels and hides the way down; cleared with the pickaxe |
| Glimmercaps | Grey-teal caps that glow cyan (section 5.2) | Forage; small light sources |
| Relic | A miner's old lunch tin with the town's spiral stamped on the lid, half-buried, with a glint | For the museum |
| Stairs down | A round stone shaft with steps spiralling down: the spiral motif | Every fifth cleared level adds a shortcut from the entrance (design doc) |

Each ore reads by shape as well as colour (pillar 4).

**The entrance.** A heavy timber portal in the lilac-grey mountainside among spirepines, near the crash site, with a pick in a circle carved on the lintel and rails running out. Before you buy the pickaxe, it's boarded up, with a creeper and weeds. When Maggie tells the mine's story, the boards come down and are stacked beside the mouth, lanterns hang on either side, and warm light shows from inside.

**How levels 1 to 5 change:**

| Levels | Feel | Second look |
| --- | --- | --- |
| 1 and 2 | Dry, warm, the most lanterns; daylight down the entrance steps on level 1 | Twinbough roots with glowing sap; a teal band in the rock |
| 3 and 4 | Damper and darker: drips, puddles, fewer working lanterns, more glimmercaps; the first Groth | Glimmercaps in clusters |
| 5 | The first flooded level. The low chamber is under still, dark water, with Harlan's rusted, broken pump on the shore. Once drained with Mira's first pump (bright rose copper on a sealed-plank base, with a lit gauge and a pipe up the wall), a few puddles stay and the stairs down are uncovered | A faint violet light `#B48CFF` far down under the water: the "lights in the deep levels that weren't lanterns" from Maggie's story |

**Fits the game (draft):** each level is a small map read from the overhead camera. Walls on the camera side are low and show only their tops, so the farmer is never hidden; frames and lanterns sit on the back walls. Whether level 5 is a plain pump or the first water-flow puzzle is still open (design doc, open question 4).

## 8. Style and technical direction (draft)

- **Style:** stylised 3D, a step up from the choppy low-poly prototype: cozy, readable and light enough for the Steam Deck. Simple shapes, with colour doing most of the work, and no realistic textures. This builds on what the prototype already uses (REQ-12 "low-poly"), so the upgrade is a better version of the same direction, not a change.
- **Shapes** (design doc): smooth shading on organic things (crops, rocks, characters, animals) and harder edges on buildings. Plants and trees are slightly off from Earth (spiral trunks, oversized leaves); buildings keep familiar, cozy shapes (section 7.1). See the shading question in section 9.
- **One palette:** one tight palette (section 2) is used everywhere, in the world, the characters and the HUD.
- **Lighting carries the mood:** soft shadows, light ambient occlusion and light that shifts from warm to cool through the day (section 3). Most of the alien feel comes from the sky, the light colour and the glows, which are cheap to render. Models stay simple.
- **Glow is the main alien signature:** crops, wild glowing plants, Embrite (the rare glowing mineral in the deep mine) and the crash site glow softly at night.
- **Motion:** gentle wind sway on grass, plants and leaves, driven by a shader; slow "breathing" pulses on glows; light ripples on water. Small motion makes a low-poly world feel alive.
- **Steam Deck first:** every effect must hold the frame rate on the Deck at 1280 x 800 (the risk register in `docs/requirements/technical-blueprint.md` already plans for low-poly art and Deck testing). Effects that cost too much get a lower-cost fallback.
- **Separate from gameplay:** new art lives in its own folder and scenes, never in the rules (Domain, Application). A gameplay scene adopts a new asset by swapping a model or material, without changing any rules.

### 8.1 HUD and menus (draft)

Concept art: https://claude.ai/artifact/AByqzWrCTcYWpQ89e8FKuf (HUD and menus page)

**One small UI kit** for the HUD and every menu, drawn at the Steam Deck's 1280 x 800. Warm plum panels and cream text are the town's warm side (section 2); ember is the accent and the focus colour. Type is Nunito (rounded and cozy, clear on the Deck). Every text is 20 px or more, 22 px for body text, as the game's text-size test already requires.

| Role | Colour |
| --- | --- |
| Panel | `#241C30` at about 88% opacity, with a mauve `#9C7E8C` line |
| Text / muted text | `#F6F1E4` / `#CFC6D8` |
| Accent and focused button | Ember `#E9773A`, with an amber `#FFB04A` focus ring |
| Energy | Ember to gold `#E9773A` → `#F7C531` |
| Water | `#6FC3E8` |
| Gold | `#F7D27A` |
| Disabled | `#9A92A6`, with a dashed border |

**The HUD** shows what the game shows today:

- **Top left, status:** energy as an ember-to-gold bar with its number; the watering can as 20 water pips, one per charge; gold with a sprout coin. The energy bar turns ember when the farmer is tired.
- **Top right, clock:** weather icon and time; a day arc from 6 am to 2 am along which the sun (Hearth after dark) moves, with a tick where dusk starts; then the date.
- **Bottom centre, hotbar:** six slots, ready for the backpack row (REQ-47). Icons instead of words, with the stack count in the corner; the selected slot rises with an ember border and its name shows above. The change-item glyphs sit at each end.
- **Prompts, above the hotbar:** the Use prompt with its glyph (REQ-12). An interaction (Ship crops, Refill can, Sleep, Shop) stacks above it with an ember border. A rejection ("Already watered") is greyed and dashed, with no glyph.
- **Glyphs** follow the last device used (REQ-13): keys are square cream chips, controller buttons round ones.
- The panels are nearly opaque, so the HUD reads the same over morning mist and the indigo night.

**Menus.** Every menu works with a controller alone: one item always has focus, and the glyphs for the current device sit at the bottom.

| Screen | Look |
| --- | --- |
| Title | "Long Way Home" over the meadow at dusk with Hearth rising; Continue (with the slot's date and gold), New game, Load game, Quit, as in the game |
| Shop (Maggie's corner) | Maggie in an arched window beside the list. Each listing has an icon, its Season and grow days, and its price; out-of-season seeds stay listed but greyed, saying why |
| Pause | "Paused" with the date and time. Proposal: adds Controls and Quit to title to today's Resume, and a line saying the farm was saved last night |
| Night | The two moons over the lake, "You slept." or "You passed out…", the new date. Proposal: a summary of what sold overnight and the gold earned, then Saved, or the save-failed line in ember |

**Still to design:** the backpack screen (REQ-47), the save slot list and overwrite question, settings, and talking to residents (dialogue boxes and the friendship lanterns).

## 9. Open questions

- [x] How alien overall? Decided: a little further than the first draft for buildings, which are "grown, not built" (section 7.1). The rest keeps "familiar at first, different on a second look".
- [ ] Moon names: Hearth and Wisp, or something else?
- [x] Townsfolk differences: pointed ears, a cool undertone in the shadows, cheek marks unique to each person, non-Earth hair colours, and hair and marks that glow in each person's own colour after dark (section 6.2)
- [x] The farmer: fixed character for now; customisation is a future enhancement
- [x] The farmer: his outfit changes with the story
- [x] The farmer: chunky proportions, about 3 heads tall
- [x] The farmhouse: restored by a house upgrade, available once the carpenter moves back
- [ ] The farmhouse: any further upgrade stages after restored (e.g. a bigger house)?
- [ ] Flat shading (sharp facets) or soft stepped shading (smoother, more painterly)? The design doc proposes smooth shading on organic things and harder edges on buildings (section 8). The farmer and animal sheets (sections 6.1 and 6.3) use faceted three-tone shading, so the two need settling.
- [x] Camera: decided 2026-11-06. Rotate and zoom (REQ-52): free 360° rotation, with a tilt that goes towards top-down when zoomed out. Check the art from every angle and across the zoom range.

## 10. Next steps

**Process** (design doc): settle style, palette, camera angle and scale (tile and character size) early, and leave detailed assets until each area's design is stable. Merge graphics work into `main` early and then add it phase by phase, rather than keeping a long-lived separate branch.

1. Settle the theme and palette above.
2. Build an art sandbox in `game/art/`: a scene, separate from the farm, to try the palette, sky, two moons, water, glow, trees, rocks, the farmhouse and the crops at each time of day.
3. Review it on the Steam Deck.
4. Apply the approved pieces to the farm scene one at a time.
