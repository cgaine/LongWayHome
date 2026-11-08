# Long Way Home — Technical Blueprint

How the game is built. Inputs: [product-definition.md](product-definition.md) (phases, constraints, §9 deferred technology points), [domain-model.md](domain-model.md) (single-context, Phase 1 rules `BR-G*`, `BR-P*`, `BR-W*`, `BR-T*`, `BR-C*`, `BR-S*`, `BR-L*`; Phase 2 adds `BR-K*`, `BR-N*`, `BR-F*`, `BR-B*`, `BR-O*`, `BR-U*`, `BR-M*`, `BR-A*`, `BR-R*`, `BR-I*`) and [canonical-values.md](canonical-values.md).

**Scope.** Phase 1 — Godot rebuild, built so later phases add to it without restructuring. This is a single-player desktop game, not a web app, so the team's standard web choices (Minimal API, React SPA, Azure hosting, Aspire, PostgreSQL, JWT auth) **do not apply** and are not used.

## Settled from the product definition

| §9 Deferred item | Decision |
|---|---|
| "Engine: Godot 4 (.NET build) with C#." | **Accepted.** Godot 4.x .NET build, C#, latest stable 4.x at project setup, pinned (see *Local development*). |
| "Each table becomes a JSON file the game loads, with the ID column as the key other tables reference." | **Accepted.** Reference data is JSON under `game/data/`, keyed by the canonical ID (`CropTypeId` etc.). Saves are JSON too. |

Business constraints that bind the architecture (§7): Windows and Linux including Steam Deck; every action on keyboard and mouse and on a controller; solo or tiny team; must pass Steam and Steam Deck review.

## 1. Architecture

**Pattern: engine-independent game core with a thin Godot presentation layer** (ports and adapters).

- All game rules live in plain C# class libraries with **no reference to Godot**. They hold the state (`Game` aggregate), run every Player action as a command, and run the Night.
- The Godot project only renders state, plays animation and sound, reads input, and forwards input to the core as commands.
- Rationale: the 31 business rules can be unit-tested in milliseconds without launching the engine (fits a solo developer and the `tdd` workflow). The rules stay readable as one place, and the engine can be upgraded without touching them. The cost is a small mapping layer between core state and Godot nodes, which is acceptable at this size.

Rejected: rules in node scripts (simple at first, but only testable by running the engine); a full ECS (unneeded at this scale).

## 2. Solution structure

```
LongWayHome.sln                      # everything, including tests; what CI builds and tests
global.json                          # pins the .NET SDK
.github/workflows/ci.yml             # CI: build + test with coverage; exports on main
src/
  LongWayHome.Domain/                # entities, value objects, rules. No dependencies.
  LongWayHome.Application/           # commands, Night processing, ports. → Domain
  LongWayHome.Infrastructure/        # JSON data + save adapters. → Application, Domain
game/                                # the Godot project
  project.godot
  LongWayHome.Game.csproj            # Godot.NET.Sdk; → Application, Infrastructure, Domain
  LongWayHome.Game.sln               # the solution the Godot editor builds and export requires; game + src
  export_presets.cfg                 # Windows Desktop and Linux presets used by CI
  data/                              # reference data JSON (crops, shop, tuning)
  scenes/                            # .tscn scenes
  scripts/                           # C# node scripts, by area
  art/                               # new art (shaders, the art sandbox), apart from gameplay scenes
    icons/                           # seed and crop icons (SVG, palette colours only)
  ui/                                # HUD and menus; DialogueBox.tscn, the dialogue box (#138)
  assets/                            # models, textures, audio
tests/
  LongWayHome.Tests/                 # xUnit; Domain + Application + Infrastructure tests
docs/
```

| Project | Responsibility | References |
|---|---|---|
| `LongWayHome.Domain` | `Game` aggregate and its children, value objects, rule checks. Throws or returns a typed rejection when a rule is broken. | none |
| `LongWayHome.Application` | `GameSession` (the loaded Game plus its commands), `NightProcessor`, `NewGameFactory`, ports `IGameDataSource`, `ISaveStore`, `IRandomSource`. | Domain |
| `LongWayHome.Infrastructure` | `JsonGameDataSource`, `JsonSaveStore`, save-format migrations, `SystemRandomSource`. Uses `System.Text.Json`. | Application, Domain |
| `LongWayHome.Game` (Godot) | Scenes, node scripts, input, HUD, the composition root (an autoload `GameRoot`). | all three |
| `LongWayHome.Tests` | Unit tests named after the rules they prove. | all three (not the Godot project) |

All class libraries target the same .NET version as the Godot .NET SDK in use (net8.0 for current Godot 4.x) so the Godot project can reference them.

**Dependency direction:** Domain ← Application ← Infrastructure ← Game. Nothing below the Godot project references Godot.

## 3. Layering

### Domain (`LongWayHome.Domain`)

Folders: `Games/` (Game, GameDate, ClockMinutes, Weather, DayEndReason), `Players/` (Player, WateringCan, InventoryStack, ItemKind, ToolId), `Farming/` (FarmPlot, Tile, Planting, TileState, PlantingStatus, GridPosition), `Shipping/` (ShippingBin, ShippedItem), `Catalog/` (CropType, ShopListing, ShopItemKind, CropTypeId, Season), `Residents/` (Resident, ResidentId; #138).

- Value objects are C# `record struct`s or enums whose serialized values are the canonical code values (`spring`, `passed_out`, …), via a snake_case enum converter.
- Rule enforcement lives on the aggregate, and each check cites its rule in a comment, e.g. `// BR-T3` in `Tile.Plant(...)`.
- Every mutating method returns a result (`ActionResult` with `Ok` or a `Rejection` code) rather than throwing for ordinary player mistakes ("no charges", "not ripe"), so the UI can show why.

| Area | Enforced in | Rules |
|---|---|---|
| Calendar, clock, day end | `Game`, `GameDate`, `ClockMinutes` | BR-G1, BR-G2, BR-G3 |
| Energy, gold, inventory | `Player`, `InventoryStack` | BR-P1, BR-P2, BR-P5, BR-P7 |
| Watering can | `WateringCan` | BR-W1, BR-W2, BR-W3, BR-W4 |
| Tiles and plantings | `FarmPlot`, `Tile`, `Planting` | BR-T1–BR-T3, BR-T5, BR-T6, BR-T8, BR-T9 |
| Crop and shop data | `CropType`, `ShopListing` (validated on load) | BR-C1, BR-C2 |
| Shipping | `ShippingBin` | BR-S1 |
| Residents | `Game` (one Resident per ResidentId), `NewGameFactory` | BR-R1 |

### Application (`LongWayHome.Application`)

- `GameSession` exposes one method per Player action: `Till`, `Plant`, `Water`, `Harvest`, `RefillCan`, `ShipAll`, `Buy`, `Sleep`, plus `AdvanceClock(realSeconds)` (BR-G2) and `PassOut` when the clock hits 02:00 (BR-G3). Each call goes through the `Game` root.
- `NightProcessor` runs the Night steps in the fixed order from the domain model: growth (BR-T4) → payout (BR-S2) → dropped items cleared (BR-K4) → date (BR-G1) → weather (BR-G4) → watering reset and rain (BR-T7) → energy (BR-P3, BR-P4) → save (BR-G5). It is one method so the order is tested in one place.
- `NewGameFactory` builds the starting state (BR-P6, BR-T1), with Maggie and Clyde present (BR-R1).
- `ResidentCodes`, `ResidentLines` (display names and greetings) and `DialogueFlow` (the dialogue box's line-stepping, `Finished` once per conversation) are pure helpers for the Residents and the dialogue box (#138); `Art/ResidentArt` holds Maggie's and Clyde's part tables.
- `ShopService` lists today's offers (BR-L1) and buys (BR-L2).
- **Phase 2** *(planned, REQ-44 to REQ-46, REQ-48 to REQ-51, REQ-57, REQ-58)*: `GameSession` gains `Swing` (axe or pickaxe on a node; built in #152), `CycleHotbar`, `Place`/`PickUpPlaced`, `SellToMaggie` (built in #146, with `SellAllToMaggie` and `SellableCrops`), `Deliver` (rebuild sign), `PlaceOrder`/`CollectOrder`, `UsePump`, `EnterArea` (built in #136 for the four start areas)/`EnterMineLevel`, `RefillCan(WaterSource)` at the pond, the river and the lake (built in #137) and the Opening steps (built in #139: `GiveStarterSeeds`, `CompleteOpening`, driven by `OpeningFlow`). `NightProcessor` takes the Phase 2 order from the domain model: growth → payout (once the bin exists) → rebuilds → orders → residents → dropped items cleared → node regrowth → date → weather → watering reset → energy → save, still one method with one order test. `NewGameFactory` places the meadow's starting nodes from `NodeSpawn` (BR-N7, built in #151). Node regrowth (BR-N4, built in #153) uses no randomness: it is a fixed 2-Night wait, so it's testable without `IRandomSource`.
- Ports: `IGameDataSource` (crop types, shop listings, tuning), `ISaveStore` (list, load, save slots), `IRandomSource` (weather roll; seeded in tests).
- Events out to the presentation layer: `GameSession` raises C# events (`TileChanged`, `InventoryChanged`, `DroppedItemsChanged`, `DayEnded`, `NightCompleted`). The Godot layer subscribes; the core never calls into Godot.

### Infrastructure (`LongWayHome.Infrastructure`)

- `JsonGameDataSource` parses `crops.json`, `shop.json` and `tuning.json` **from strings**. The Godot layer reads the files with `Godot.FileAccess`, because `res://` files are packed into the `.pck` in exported builds and `System.IO` can't open them. Validation on load enforces BR-C2 and that every reference resolves.
- `JsonSaveStore` writes one file per slot under a folder path it's given (the Godot layer passes the globalized `user://saves` path). Writes go to a temp file and are then renamed, so a crash mid-save never corrupts the slot.
- Save files carry `saveVersion`. `SaveMigrator` upgrades older versions step by step when later phases add data (backpack slots, buildings), so Phase 1 saves keep loading. `residents` (#138) is optional within version 2: a version 2 file written before it loads with the new-Game Residents (BR-R1), and a malformed one is refused.
- Phase 2 writes `saveVersion` 2. The 1 → 2 step (`Saves/Migrations/SaveV1ToV2.cs`) follows the domain model's migration note, with its own migration test: stacks into slots, can upgrade → Rosk can, the default `residents` (Maggie and Clyde present, #138), `openingDone` true (#139); *(planned, REQ-47)* 12 x 9 tiles into the cleared start region, store at stage 1.
- *(planned, REQ-44, REQ-45, REQ-48, REQ-49, REQ-57)* New reference data files beside `crops.json`: `materials.json` (built in #152), `nodes.json` (spawn points per area and mine level; built in #151 with the farm's), `buildings.json` (stage costs and unlocks), `orders.json` (recipes) and `tools.json` (tier effects). All parsed from strings and validated on load like the crop table.
- `shop.json` gains vendor and stage columns (built in #145): every listing has `vendor`, `requiresStage`, `oncePerGame` and a nullable `toolId`, validated on load; the axe and pickaxe are listed. `ShopService.VendorStage` is the single place a vendor's stage is read (general store 1, others 0 until #124).

### Presentation (the Godot project)

This takes the place of the "frontend" in the web stack.

```
game/
  scenes/
    Main.tscn                 # title screen → save slots → farm
    Farm.tscn                 # farm plot, house, bin, shop point, pond; the south exit to the forest path (#136); the river strip down the east edge (FarmRiverView) and its river water points (#137)
    AreaCommon.tscn           # the gameplay nodes every area shares: Player, HUD, menus, the dialogue box (#138), clock, rain, night glow, NightReturn, NightTransition (#136)
    ForestPath.tscn           # 14 x 40: winding path between Twinbough trees, river on the east (#136)
    Town.tscn                 # 40 x 28 Ember Bay: square, store, carpenter, blacksmith, broken bridge stub (#136); river water points along the bank (#137); Maggie in front of the store's shop window, facing the square (#138)
    Lakeshore.tscn            # 40 x 16: shore, dock, lake, the river's mouth (#136); lake water points along the shore and at the dock end (#137); Clyde at the end of the dock, facing the lake (#138)
    Resident.tscn             # one Resident (ResidentView): its interaction point (range 1.5) with ResidentTalk (#138)
    Player.tscn               # character + follow camera
  scripts/
    Core/GameRoot.cs          # autoload: composition root, owns GameSession
    Farm/FarmPlotView.cs      # draws Tiles and Plantings from state
    Farm/TileView.cs          # one Tile's slab; a grass Tile's detail is a GrassTileModels mesh (GrassTileArt layout and quarter turn from its grid position, in the Season's grass)
    Player/PlayerController.cs # moves the Player; tracks the feet tile and picks up DroppedItems there (#130)
    Player/ToolTargeting.cs   # which Tile the Use action targets
    Player/FarmerAnimator.cs  # plays the farmer's locomotion and action clips (#103)
    World/InteractionPoint.cs # door, bin, shop, pond, river, lake
    World/WaterRefill.cs      # a water point's refill: RefillCan at its Source (pond, river, lake) (#137)
    World/DroppedItemsView.cs # one billboard icon per DroppedItem on its tile, from state; no collision or shadows (#130)
    World/AreaExit.cs         # an exit trigger on an area's edge: EnterArea, then the target's scene (#136)
    World/AreaArrival.cs      # where the Player walks in from a neighbouring area, facing into it (#136)
    World/AreaScenes.cs       # AreaId → scene path for the open areas (#136)
    World/NightReturn.cs      # after a Night away from the farm, changes to the farm with the night message pending (#136)
    World/ResidentView.cs     # a Resident, shown with a capsule collider only when Game.IsPresent; names its point after its code and turns to face the Player in range (#138)
    World/ResidentTalk.cs     # interact in range of a Resident's point opens the dialogue box with ResidentLines (#138)
    Ui/Hud.cs                 # hotbar, clock, gold, energy
    Ui/UsePrompt.cs           # shows what Use will do (REQ-12)
    Ui/ShopMenu.cs
    Ui/DialogueBox.cs         # the bottom-of-screen dialogue box over DialogueFlow: Show(speaker, lines), Finished; pauses the tree while shown (BR-G2) (#138)
    Ui/BackpackMenu.cs        # the Backpack screen: every slot in rows of six, pick up and put down (REQ-47) (#131)
    Ui/IconTextures.cs        # the seed and crop icon textures, loaded once (paths from ItemIcons) (#95)
    Ui/SaveSlotsMenu.cs
  art/                        # new art, apart from gameplay scenes and scripts (ART_DIRECTION §8)
    ArtMaterials.cs           # Shared palette material; Apply(node, role) colours a code-built shape (#76)
    ArtMeshes.cs              # Merge (one mesh per multi-part shape, #77) and MergeSkinned (one bone per part, #102)
    PartMeshes.cs             # merges a CropPart table into one mesh (a part's Sides sets how round it is; cylinders 1.5 m or wider default to round), for the sandbox's house, trees and rocks
    crops/CropModels.cs       # one cached, merged mesh per (crop, stage) from CropArt (Application/Art/CropArt.cs) (#90)
    crops/NightGlowView.cs    # sets the art_night shader global from the clock in the farm (NightGlow.At) (#94)
    farmer/
      Farmer.tscn             # the farmer (#102): skinned on a humanoid-profile skeleton, outlined, held item in hand
      FarmerModel.cs          # builds it from FarmerArt and HeldItemArt (Application/Art/); ShowHeld(item)
      FarmerAnimations.cs     # the farmer's AnimationLibrary (#103): one clip per FarmerClip from FarmerMoves (Application/Art/)
    farm/
      Meadow.tscn             # the farm's ground: meadow, shade patches, mown margin, edging, path, props (#77)
      MeadowView.cs           # builds its meshes and MultiMeshes from MeadowLayout (Application/Art/MeadowLayout.cs), and the wild grass cover: one MultiMesh per GrassTileArt layout over MeadowLayout.GrassCover
      Farmhouse.tscn          # the run-down farmhouse (#104): one Model mesh, no collision (Farm.tscn's House keeps its box)
      FarmhouseView.cs        # merges FarmhouseArt.RunDown (Application/Art/FarmhouseArt.cs) into that one mesh under the shared material
      RoundFarmhouseModels.cs # the concept art's round farmhouse, run-down and restored, from RoundFarmhouseArt (Application/Art/); art sandbox only for now
      GrassTileModels.cs      # the grass detail meshes: each GrassTileArt layout in each Season's grass for the plot's Tiles, and its wild MeadowParts for the meadow's cover
      NatureModels.cs         # the Twinbough (TreeArt: each stage and season) and the rocks (RockArt: rock, boulder, Rosk and Groth ore); the sandbox, and the areas' scenery (#136)
    town/
      ResidentModels.cs       # one cached, merged, outlined mesh per Resident from ResidentArt (Application/Art/); Maggie and Clyde (#138)
      TownModels.cs           # the general store at each StoreStage (GeneralStoreArt) and the carpenter at each CarpenterStage (CarpenterArt), the blacksmith at each BlacksmithStage (BlacksmithArt), all on the shared RoundBuilding shape, and the rebuild sign (RebuildSignArt), with their night-only lights; the sandbox, and the town at the starting stages (#136)
    areas/
      AreaArt.cs              # flat ground and water, ribbons, one MultiMesh per repeated mesh, trunk collision (#136)
      ForestPathView.cs       # the forest path's ground, path and river from ForestPathLayout (Application/Art/); its trees are nodes (#154)
      TownView.cs             # the town's scenery from TownLayout, buildings from TownModels
      LakeshoreView.cs        # the lakeshore's scenery from LakeshoreLayout
      FarmRiverView.cs        # the river strip down the farm's east edge, shallows then deep water; no collision (#137)
    icons/
      seed_<crop>.svg         # seed packet icons for the hotbar and the shop, palette colours only (#95)
      crop_<crop>.svg         # harvested-crop icons for the HUD crops row, palette colours only (#95)
      material_<code>.svg     # Phase 2 material icons (wood, sap, stone, rosk_ore, groth_ore, glimmercap, plank, sealed_plank), art sandbox only for now
      tool_<code>.svg         # Phase 2 tool icons (axe, pickaxe), art sandbox only for now
    materials/
      palette.tres            # the one shared ShaderMaterial: low_poly.gdshader on palette.png
    palette/
      palette.png             # 64 x 128 swatch texture (8 columns of 8 x 8 swatches; grown from 64 x 64 in #138), written from ArtPalette (Application/Art/ArtPalette.cs)
    sandbox/
      ArtSandbox.tscn         # palette, tile, crop, farmer, farmer animation, shape, round-farmhouse, Twinbough and rock samples under the four light presets (#75, #90, #102, #103)
      ArtSandbox.cs
      SandboxCamera.cs        # orbits the scene centre with CameraView, as the follow camera; the move_* actions slide the centre
      SandboxPan.cs           # that slide: camera-relative, kept over the ground (pure C#, unit-tested)
      SandboxLook.cs          # preset and style switching, the on-screen label (pure C#, unit-tested)
    shaders/
      low_poly.gdshader       # flat and stepped low-poly shading, colour sampled from the palette
      outline.gdshader        # the farmer's soft dark inverted-hull outline, a next pass on his body only (#102)
  data/ crops.json  shop.json  tuning.json
```

- **One shared palette material (#76, ART_DIRECTION §8):** every new model takes its colour from `art/materials/palette.tres`. Shapes built in code call `ArtMaterials.Apply(node, role)`, which sets the material and the node's `swatch` instance parameter to the role's UV centre; imported models are UV-mapped onto `art/palette/palette.png`. The swatch list is `ArtPalette` in `LongWayHome.Application/Art/` (pure C#, unit-tested), and it is **append-only**, so UVs already mapped stay valid; a test checks the committed PNG matches it (`UPDATE_PALETTE=1` regenerates it). The shading style is switched project-wide by the global shader uniform `art_stepped_shading` (`[shader_globals]` in `project.godot`), not per material. The global `art_night` (0 by day to 1 at night, also in `[shader_globals]`) drives the crop night glows (#94): `ArtMeshes.Merge` writes a glowing part's `Glow` and `Twinkle` into the mesh's UV2 (only when some part glows), and the shader adds `EMISSION` from UV2 scaled by `art_night`. `NightGlowView` sets it from the clock in the farm (`NightGlow.At` in `LongWayHome.Application/Art/`) and resets it to 0 on leaving; the art sandbox sets it to 1 under its Night preset. No bloom (REQ-14). Characters are skinned: `ArtMeshes.MergeSkinned` merges parts like `Merge` and weights every vertex of a part fully to that part's bone (#102). The farmer (`art/farmer/FarmerModel.cs`, from `FarmerArt` and `HeldItemArt` in `LongWayHome.Application/Art/`) is one skinned mesh under a `Skeleton3D` whose bones carry `SkeletonProfileHumanoid` names, drawn with a copy of the shared material whose next pass is `art/shaders/outline.gdshader` (an unshaded inverted hull), so only he is outlined; with his contact shadow and held item that is a handful of draws (REQ-14).
- **The meadow (#77, ART_DIRECTION §7.2):** `Farm.tscn` instances `art/farm/Meadow.tscn` for its ground, and colours the pond's `Shallows` and `Deep` discs with the shared material directly in the scene. Where the scattered props, shade patches, edging stones and stepping stones go, and each mesh node's palette role, come from `MeadowLayout` in `LongWayHome.Application/Art/` (pure C#, seeded, unit-tested); `MeadowView` only builds the meshes, one `MultiMeshInstance3D` per prop part to keep draw calls low. None of it has collision or changes a rule.
- **The farmhouse (#104, ART_DIRECTION §7.1):** `Farm.tscn`'s `House` (its `StaticBody3D` and 6 x 3 x 4 collision box, unchanged) instances `art/farm/Farmhouse.tscn`, whose `FarmhouseView` merges the run-down part table `FarmhouseArt.RunDown` (`LongWayHome.Application/Art/`, pure C#, unit-tested) into one opaque mesh under the shared material: one draw, no glow (REQ-14). The house's look never moves the collision box, the sleep door point or the bin (BR-G3); the restored state (Phase 2) will be a second part list beside `RunDown`.

- **Node scripts hold no rules.** They read state from `GameSession`, send commands to it, and react to its events.
- **Input:** every action is a Godot Input Map action bound to both keyboard and mouse and a controller: `move_*`, `run`, `use_tool`, `interact`, `tool_next`, `tool_prev`, `hotbar_cycle` (X and the controller View button; REQ-47), `open_menu`, `pause`, and the camera's `camera_rotate_left`, `camera_rotate_right`, `camera_zoom_in`, `camera_zoom_out` (Z/C/R/V and the right stick; REQ-52). Nothing reads raw keys or buttons (REQ-13). The UI follows the last-used device for prompts, for every action.
- **Readable at a glance (REQ-12):** each growth stage has its own model, bigger than the one before it; every crop (emberroot, dusktuber, sunbell, starcorn and moonmelon) is drawn to ART_DIRECTION §4 by `CropArt` and `CropModels` (#90, #91, #92): Seed (a light dirt mound with a seed in the crop's colour), Sprout (a stem with two seed leaves in a V and a bud in the crop's colour), Young (emberroot's orange shoulder under five fronds; dusktuber's low rosette of leaves curling violet; sunbell's three canes in a ring of leaves with one small green berry; starcorn's short stalk with four drooping leaves; moonmelon's low trailing vine with veined leaves and a pale bud) and Ripe (emberroot's tall frond tuft over a wide orange shoulder with a warm core; dusktuber's big speckled purple bulb under four curling leaves; sunbell's rounded bush hung with six gold-to-orange berries, point down under green sepal stars; starcorn's tall stalk with a big golden cob between peeled husks, white diamond sparkles and a gold tassel; moonmelon's pale blue-white ribbed gourd with a crown and stalk, on a trailing vine with leaves and a tendril); each Tile is a slab in one of three states (grass with small tufts, brown tilled soil with furrows, and much darker watered soil with a wet sheen), with dark grooves between the slabs instead of grid lines; the Use target marker is a cream highlight with four ember-orange corner brackets on the targeted Tile, drawn over everything; `UsePrompt` names the action Use will perform on the targeted Tile, or why it can't.
- **Pausing (BR-G2):** opening any menu pauses the scene tree, and `GameSession.AdvanceClock` is only called from an unpaused node.
- **The farmer's animations are cosmetic (#103):** `FarmerAnimator` (pausable, on the Player) plays the clips of `FarmerMoves` (pure, in `LongWayHome.Application/Art/`, with `FarmerAnimationState` choosing the clip) built by `art/farmer/FarmerAnimations.cs` on the one `AnimationPlayer` of `Farmer.tscn`. A clip only follows a successful action (`ActionResult.IsOk`), after the action has happened, so it never delays or decides anything; the day-end clips play before `Sleep()` (only when the `sleep` clip ends before 02:00 even a minute late) or up to 02:00 (`pass_out`), without moving the day end (BR-G3).
- **Art sandbox:** starting with the user argument `--art-sandbox` (`godot --path game -- --art-sandbox` from source, `<exported binary> -- --art-sandbox` on the Deck; combinable with `--show-fps`) makes the title screen open `art/sandbox/ArtSandbox.tscn` instead of the save slots, once per run. It is where shading and colour are judged on the Deck before art reaches the farm, and `pause` returns to the title screen. Its camera turns and zooms like the follow camera, and the `move_*` actions (left stick, D-pad, WASD, arrow keys) slide it left, right, forward and back. It also shows the concept art's round farmhouse (`RoundFarmhouseArt`, run-down and restored), the Twinbough at each stage and season (`TreeArt`), the rocks the pickaxe breaks (`RockArt`), the farmer holding the Phase 2 axe and pickaxe (`HeldItemArt.PartsFor(GatherTool)`, through `FarmerModel.ShowGatherTool`) the tool and material icons (`ItemIcons.ToolIcon`, `ItemIcons.MaterialIcon`), the general store, the carpenter and the blacksmith at each rebuild stage with their rebuild signs (`GeneralStoreArt`, `CarpenterArt`, `BlacksmithArt`, `RebuildSignArt`), none of which the game uses yet. Gameplay scenes adopt approved art by instancing scenes and materials from `art/` (the meadow, #77; the farmhouse, #104; the player scene's `Model` instances `art/farmer/Farmer.tscn`, #102), never anything under `art/sandbox/`; scripts other than `SaveSlotsMenu` reference nothing in `art/` except the shared material helpers `ArtMaterials` and `ArtMeshes` (the tile slabs, #78) and the crop builder `CropModels` (#90), the farmer's `FarmerModel` (found by type under the Player's `Model` by `HeldItemView`, #102) and the farmer's clip names from `FarmerAnimations` (used by `FarmerAnimator`, #103), and the Residents' `ResidentModels` (used by `ResidentView`, #138), and the seed and crop icons in `art/icons/` (#95; their paths come from `ItemIcons` in `LongWayHome.Application/Art/`, so no script names an `art/` path), and never the sandbox. The area scenes (#136) instance their scenery scripts from `art/areas/`; `Town.tscn`'s `TownView` builds the store, carpenter and blacksmith from `TownModels`, and the forest path and town trees from `NatureModels`.
- **Renderer:** Forward+ by default. The Steam Deck target is a steady 30 fps at 1280 × 800 (REQ-14); if it's missed, the fallback is the Mobile renderer with a lower resolution scale, and the frame rate is capped at 30 on Deck to save battery.

### Areas and scenes *(built for the four start areas in #136 and the 72 x 36 farm in #150; across the river and the mine planned, REQ-48, REQ-49)*

- **Built (#136, REQ-17, REQ-49):** `farm`, `forest_path`, `town` and `lakeshore` are `Farm.tscn`, `ForestPath.tscn`, `Town.tscn` and `Lakeshore.tscn`, laid out north to south, each joined to the next by a 3-tile exit centred on x = 0. Every area scene instances `AreaCommon.tscn` as its last child, `Common` (Player, HUD, pause, shop and backpack menus, `DayClock`, `RainView`, `NightGlowView`, `NightReturn` and, last, `NightTransition`), and overrides `Common/Player`'s transform for where the Player stands when the scene loads; `RainView` uses the area's own `DirectionalLight3D` and `WorldEnvironment`. An `AreaExit` (an `Area3D` 3 x 1 tiles in the gap of the edge's `Boundary` wall) calls `GameSession.EnterArea` and, on `Ok`, sets `GameRoot.PendingArrivalId` and changes to the target from `AreaScenes`, deferred out of the physics callback; the target's matching `AreaArrival` (a `Marker3D` 1.5 tiles inside the edge, outside the trigger, facing in) places the Player once every node is ready. `GameSession.CurrentArea` (not saved) is the farm on every new or loaded session and after every Night; pickups see only the current area's DroppedItems and tools have no target off the farm, except the axe and pickaxe on the forest path's nodes (#154: its own 14 x 40 grid, `AreaGrid` in the Domain, mapped by `ForestPathGrid`/`AreaGridLayout`; `ResourceNodesView` and `DroppedItemsView` take an exported `Area` code). After a Night that ended elsewhere, `NightReturn` changes to the farm and leaves the night message on `GameRoot.PendingNight` for the farm's `NightTransition`. Scenery is placed by pure layouts in `LongWayHome.Application/Art/` (`ForestPathLayout`, `TownLayout`, `LakeshoreLayout`, unit-tested) and built by `art/areas/` views as one `MultiMeshInstance3D` per repeated mesh; collision (edges, river banks, the waterline, the dock, buildings, tree trunks) is box and cylinder shapes. AC11's Steam Deck 30 fps per area is a smoke-checklist row.
- Each Area (`farm`, `forest_path`, `town`, `lakeshore`, `across_river`) is its own scene, and each mine level 1–5 is one scene, loaded one at a time. Walking off an Area's edge or taking the mine stairs swaps the scene; `GameSession` keeps the whole `Game`, so leaving an Area loses nothing. Only the current Area is in memory and drawn.
- **Built (#150, BR-T1, REQ-16):** the farm scene is the 72 x 36 meadow. `FarmGrid` maps through the pure `FarmGridLayout` (tile (X, Y) at world (X − 36 + 0.5, Y − 4.5 + 0.5)), so the Phase 1 block (`FarmPlot.Phase1Block`, X 30–41, Y 0–8) stays where the 12 x 9 plot was; the walls, the south exit and arrival, the river bank (x 36.5) and its points moved to the new edges, and the pond, the farmhouse and the stall stand in the strip north of the grid. `FarmPlotView` draws untouched grass in batches planned by `GrassBatches` (one `MultiMeshInstance3D` for the grass slabs, one per `GrassTileArt` layout and one per native plant kind, rebuilt on `NightCompleted` and when a Tile is tilled); a `TileView` exists only for a Tile that isn't grass. A 12 x 9 `farmPlot` (a version 1 save, or a version 2 one written before #150) loads with its Tiles in the Phase 1 block and grass elsewhere.
- Resource nodes are drawn as one `MultiMeshInstance3D` per node kind and stage, built from the core's node list, the same way the meadow props are, so hundreds of trees and rocks stay a handful of draw calls.
- The Opening and Maggie's mine story are scripted sequences in the presentation layer that call `GameSession` commands for each step; the core only records `openingDone` and the story having been told. Built for the Opening (#139): `OpeningFlow` (Application) holds the step order, Maggie's lines, the step hints and whether the clock runs, moving on only through each step's own input; `game/scripts/Opening/OpeningSequence.cs` in `Farm.tscn` plays it (crash scene, Maggie and her waypoints, the dialogue blocks) and gates the farm's south `AreaExit` and the door's `DoorSleep` while it runs. Dialogue boxes are menus, so the clock pauses (BR-G2): `ui/DialogueBox.tscn` (#138), in every area through `AreaCommon.tscn`, pauses the tree on `Show(speaker, lines)` and resumes it, emitting `Finished`, when the last line is advanced past or skipped; its presses are handled in `_Input`, so none reaches gameplay.

## 4. Key patterns

| Pattern | Where | Why |
|---|---|---|
| Aggregate root (`Game`) | Domain | One consistency boundary per save; the whole state saves atomically each Night. |
| Command methods returning results | `GameSession` | Player mistakes are expected outcomes, not exceptions; the UI shows the reason. |
| Ports and adapters | Application ↔ Infrastructure / Godot | Rules are tested without files or the engine. |
| Seeded randomness | `IRandomSource` | Weather (BR-G4) is reproducible in tests. |
| Data-driven reference data | `game/data/*.json` | Tuning changes without code changes, and later modding (open question in the product definition). |
| Versioned saves with migrations | `SaveMigrator` | Later phases add data without breaking existing saves. |

**Code organization:** one type per file, file name matches the type name, one interface per file, and no file holds two aggregates or unrelated entities. Godot scenes and their scripts share a name (`Hud.tscn` / `Hud.cs`).

## 5. Data store

| Phase | Store | Notes |
|---|---|---|
| Phase 1 onward | **JSON files** | Reference data in `game/data/` (shipped in the build); one save file per slot in the user's data folder (`user://saves/slot-<n>.json`; on Linux and Steam Deck `~/.local/share/godot/app_userdata/Long Way Home/saves`, unless a custom user dir is set at project setup). |
| Release (Phase 4) | Same files + Steam Cloud | Steam Auto-Cloud syncs the saves folder; no code change. |

There is no database and no server. Upgrade path: only `ISaveStore` changes if the save format or location ever does.

## 6. Testing

- **Unit tests** (`LongWayHome.Tests`, xUnit) cover Domain and Application. Tests are named after the rule they prove, e.g. `BR_T4_planting_on_unwatered_tile_does_not_grow`, so `tdd` can map each BR- to a test.
- **Night order test:** one test drives a full Night and checks every step in order.
- **Data tests:** the shipped `game/data/*.json` load and pass validation.
- **Save round-trip and migration tests** for `JsonSaveStore`.
- **Coverage:** coverlet, reported in CI; the `tdd` gate of 80% of changed lines applies to the class libraries.
- **Godot layer:** a manual [smoke checklist](../smoke-checklist.md) per release build (play a full Day on controller only and on keyboard only; REQ-13, REQ-14). Automated scene tests (e.g. GdUnit4) are a later option, not Phase 1.

## 7. Builds and distribution

| Environment | What it is | How it's made |
|---|---|---|
| **Dev** | The Godot editor on the developer's machine | Run from the editor; tests with `dotnet test`. |
| **Test** | Playtest builds | CI exports Windows and Linux builds on every merge to `main` as workflow artifacts. Later, uploaded to a private Steam beta branch for playtesters. |
| **Prod** | The Steam release | Tagged release builds uploaded to Steam's default branch (Phase 4). |

- Export presets: **Windows Desktop (x86_64)** and **Linux (x86_64)**. Steam Deck runs the Linux build natively.
- Steam Deck review needs controller-only play, readable text at 1280 × 800 and the Deck's on-screen keyboard where text is typed. Those are tracked by REQ-13 and REQ-14.
- Steamworks integration (achievements, overlay) is not part of Phase 1; it is decided when Phase 4 is readied.

### CI (GitHub Actions, from Phase 1)

- **On every pull request:** `dotnet build` and `dotnet test` with coverage.
- **On merge to `main`:** also install the pinned Godot .NET version and its export templates, export the Windows and Linux presets headless, and upload both as artifacts.
- The Godot version is set in one workflow variable and matches the version pinned for local development.

## 8. Local development environment

- **Tools:** Godot 4.x .NET (the pinned version), the .NET SDK pinned in `global.json`, and any C# IDE (Rider, VS Code with the C# extension, or Visual Studio).
- **Running:** open `game/project.godot` in the Godot editor and press Play. `dotnet test` at the repository root runs the test suite without the editor.
- **Pinning:** the Godot version is recorded in `project.godot` (`config/features`) and in the CI workflow, and the README states it. Upgrading Godot is its own change.
- **No containers, Aspire, database or proxy** are used; there is no server.
- **Steam Deck testing:** copy the Linux export to the Deck (or use Godot's remote deploy over SSH) and run it in Game Mode.

## 9. Risks and trade-offs

| Risk | Mitigation | Owner |
|---|---|---|
| `res://` files can't be read with `System.IO` in exported builds | Read through `Godot.FileAccess` in the Godot layer and parse strings in Infrastructure; the CI export catches regressions | Developer |
| A save written mid-crash corrupts a slot | Write to a temp file, then rename; keep the previous file as `.bak` | Developer |
| Later phases change the save shape | `saveVersion` plus `SaveMigrator`, with a migration test per version | Developer |
| Rules leak into node scripts over time | Review rule: scripts call `GameSession` only; any `BR-` logic belongs in the core and its tests | Developer |
| Steam Deck frame rate or legibility falls short | Low-poly art, renderer and resolution fallback, Deck testing in every phase | Developer |
| The 72 x 36 meadow and its nodes push the Steam Deck under 30 fps *(planned, REQ-16)* | One area loaded at a time; MultiMesh per node kind; the farm's grass Tiles batched (built, #150); measure on the Deck with `--show-fps` when the meadow lands, and shrink the draft size (a tuning number) if it can't hold | Developer |
| Godot or .NET version upgrades break the build | Pin both; upgrade on a branch with a CI export | Developer |
| Solo scope pressure | Phases follow the product definition's promises; the core/presentation split keeps rule changes cheap | Product owner |

## 10. Issue hierarchy

Work is tracked as GitHub issues with **sub-issues** expressing Roadmap → Epic → Feature → User story (the roadmap is issue #2). Attach a child to its parent with the child's REST id, not its number:

```bash
child_id=$(gh api /repos/cgaine/LongWayHome/issues/<child-number> --jq .id)
gh api --method POST /repos/cgaine/LongWayHome/issues/<parent-number>/sub_issues \
  --field sub_issue_id="$child_id"
```

`epic-planning` attaches epics to the roadmap and features to their epic this way. The `bug-fix` skill attaches a fix issue as a sub-issue of the affected feature issue when one can be identified.
