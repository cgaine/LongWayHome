# Long Way Home — Technical Blueprint

How the game is built. Inputs: [product-definition.md](product-definition.md) (phases, constraints, §9 deferred technology points), [domain-model.md](domain-model.md) (single-context, Phase 1, rules `BR-G*`, `BR-P*`, `BR-W*`, `BR-T*`, `BR-C*`, `BR-S*`, `BR-L*`) and [canonical-values.md](canonical-values.md).

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
  ui/                                # HUD and menus
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

Folders: `Games/` (Game, GameDate, ClockMinutes, Weather, DayEndReason), `Players/` (Player, WateringCan, InventoryStack, ItemKind, ToolId), `Farming/` (FarmPlot, Tile, Planting, TileState, PlantingStatus, GridPosition), `Shipping/` (ShippingBin, ShippedItem), `Catalog/` (CropType, ShopListing, ShopItemKind, CropTypeId, Season).

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

### Application (`LongWayHome.Application`)

- `GameSession` exposes one method per Player action: `Till`, `Plant`, `Water`, `Harvest`, `RefillCan`, `ShipAll`, `Buy`, `Sleep`, plus `AdvanceClock(realSeconds)` (BR-G2) and `PassOut` when the clock hits 02:00 (BR-G3). Each call goes through the `Game` root.
- `NightProcessor` runs the Night steps in the fixed order from the domain model: growth (BR-T4) → payout (BR-S2) → date (BR-G1) → weather (BR-G4) → watering reset and rain (BR-T7) → energy (BR-P3, BR-P4) → save (BR-G5). It is one method so the order is tested in one place.
- `NewGameFactory` builds the starting state (BR-P6, BR-T1).
- `ShopService` lists today's offers (BR-L1) and buys (BR-L2).
- Ports: `IGameDataSource` (crop types, shop listings, tuning), `ISaveStore` (list, load, save slots), `IRandomSource` (weather roll; seeded in tests).
- Events out to the presentation layer: `GameSession` raises C# events (`TileChanged`, `InventoryChanged`, `DayEnded`, `NightCompleted`). The Godot layer subscribes; the core never calls into Godot.

### Infrastructure (`LongWayHome.Infrastructure`)

- `JsonGameDataSource` parses `crops.json`, `shop.json` and `tuning.json` **from strings**. The Godot layer reads the files with `Godot.FileAccess`, because `res://` files are packed into the `.pck` in exported builds and `System.IO` can't open them. Validation on load enforces BR-C2 and that every reference resolves.
- `JsonSaveStore` writes one file per slot under a folder path it's given (the Godot layer passes the globalized `user://saves` path). Writes go to a temp file and are then renamed, so a crash mid-save never corrupts the slot.
- Save files carry `saveVersion`. `SaveMigrator` upgrades older versions step by step when later phases add data (backpack slots, buildings), so Phase 1 saves keep loading.

### Presentation (the Godot project)

This takes the place of the "frontend" in the web stack.

```
game/
  scenes/
    Main.tscn                 # title screen → save slots → farm
    Farm.tscn                 # farm plot, house, bin, shop point, pond
    Player.tscn               # character + follow camera
  scripts/
    Core/GameRoot.cs          # autoload: composition root, owns GameSession
    Farm/FarmPlotView.cs      # draws Tiles and Plantings from state
    Farm/TileView.cs
    Player/PlayerController.cs
    Player/ToolTargeting.cs   # which Tile the Use action targets
    World/InteractionPoint.cs # door, bin, shop, pond
    Ui/Hud.cs                 # hotbar, clock, gold, energy
    Ui/UsePrompt.cs           # shows what Use will do (REQ-12)
    Ui/ShopMenu.cs
    Ui/SaveSlotsMenu.cs
  art/                        # new art, apart from gameplay scenes and scripts (ART_DIRECTION §8)
    ArtMaterials.cs           # Shared palette material; Apply(node, role) colours a code-built shape (#76)
    materials/
      palette.tres            # the one shared ShaderMaterial: low_poly.gdshader on palette.png
    palette/
      palette.png             # 64 x 64 swatch texture, written from ArtPalette (Application/Art/ArtPalette.cs)
    sandbox/
      ArtSandbox.tscn         # palette, tile and shape samples under the four light presets (#75)
      ArtSandbox.cs
      SandboxCamera.cs        # orbits the scene centre with CameraView, as the follow camera
      SandboxLook.cs          # preset and style switching, the on-screen label (pure C#, unit-tested)
    shaders/
      low_poly.gdshader       # flat and stepped low-poly shading, colour sampled from the palette
  data/ crops.json  shop.json  tuning.json
```

- **One shared palette material (#76, ART_DIRECTION §8):** every new model takes its colour from `art/materials/palette.tres`. Shapes built in code call `ArtMaterials.Apply(node, role)`, which sets the material and the node's `swatch` instance parameter to the role's UV centre; imported models are UV-mapped onto `art/palette/palette.png`. The swatch list is `ArtPalette` in `LongWayHome.Application/Art/` (pure C#, unit-tested), and it is **append-only**, so UVs already mapped stay valid; a test checks the committed PNG matches it (`UPDATE_PALETTE=1` regenerates it). The shading style is switched project-wide by the global shader uniform `art_stepped_shading` (`[shader_globals]` in `project.godot`), not per material.

- **Node scripts hold no rules.** They read state from `GameSession`, send commands to it, and react to its events.
- **Input:** every action is a Godot Input Map action bound to both keyboard and mouse and a controller: `move_*`, `run`, `use_tool`, `interact`, `tool_next`, `tool_prev`, `open_menu`, `pause`, and the camera's `camera_rotate_left`, `camera_rotate_right`, `camera_zoom_in`, `camera_zoom_out` (Z/C/R/V and the right stick; REQ-52). Nothing reads raw keys or buttons (REQ-13). The UI follows the last-used device for prompts, for every action.
- **Readable at a glance (REQ-12):** each growth stage has its own model; watered soil is visibly darker; `UsePrompt` names the action Use will perform on the targeted Tile, or why it can't.
- **Pausing (BR-G2):** opening any menu pauses the scene tree, and `GameSession.AdvanceClock` is only called from an unpaused node.
- **Art sandbox:** starting with the user argument `--art-sandbox` (`godot --path game -- --art-sandbox` from source, `<exported binary> -- --art-sandbox` on the Deck; combinable with `--show-fps`) makes the title screen open `art/sandbox/ArtSandbox.tscn` instead of the save slots, once per run. It is where shading and colour are judged on the Deck before art reaches the farm, and `pause` returns to the title screen. Nothing in `scenes/` or `scripts/` other than `SaveSlotsMenu` references `art/`.
- **Renderer:** Forward+ by default. The Steam Deck target is a steady 30 fps at 1280 × 800 (REQ-14); if it's missed, the fallback is the Mobile renderer with a lower resolution scale, and the frame rate is capped at 30 on Deck to save battery.

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
