# Long Way Home — Canonical Values

Every enumerated value in the domain, with the exact code value used in data files, saves and code, and the label shown to players. Cross-references: [domain-model.md](domain-model.md) and [product-definition.md](product-definition.md).

Scope: Phase 1 — Godot rebuild, and Phase 2 — Rebuilding Ember Bay (values marked *(planned, REQ-N)* are not built yet).

## Season

Stored in `Game.date.season` and `CropType.season`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `spring` | Spring | First Season of each Year. Emberroot, dusktuber. |
| `summer` | Summer | Sunbell, starcorn. |
| `fall` | Fall | Moonmelon. |
| `winter` | Winter | No outdoor crops. |

---

## Weather

Stored in `Game.weather`; rolled each Night.

| Code Value | Display Label | Behaviour |
|------------|---------------|-----------|
| `clear` | Clear | No effect. 78% chance. |
| `rain` | Rain | Every tilled Tile is watered all Day (BR-T7). 22% chance. |

---

## TileState

Stored in `Tile.state`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `grass` | Grass | Starting state; can be tilled. |
| `tilled` | Tilled soil | Never reverts to grass (BR-T8). |

---

## PlantingStatus

Derived, never stored: compares `Planting.daysGrown` with the CropType's `growDays`.

| Code Value | Display Label | Condition |
|------------|---------------|-----------|
| `growing` | Growing | `daysGrown < growDays` |
| `ripe` | Ready to harvest | `daysGrown = growDays` |

---

## DayEndReason

Stored in `Game.lastDayEnd`.

| Code Value | Display Label | Behaviour |
|------------|---------------|-----------|
| `slept` | Slept | Next Day starts with 150 energy. |
| `passed_out` | Passed out | Next Day starts with 75 energy; nothing else lost (BR-P4). |

---

## ItemKind

Stored in `InventoryStack.kind`; from Phase 2 in `ItemRef.kind` (Backpack, chest and dropped items).

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `seed` | Seeds | Plantable; cannot be shipped. |
| `crop` | Crop | Harvested; shipped through the Shipping Bin or sold to Maggie. |
| `material` | Material | *(planned, REQ-44)* See `MaterialId`. Stacks to 99; can't be sold or shipped. |
| `tool` | Tool | See `ToolId`. One per Slot, with a `ToolTier`; never dropped or sold (BR-K6). |
| `placeable` | Placeable | *(planned, REQ-48)* See `PlaceableId`. |

---

## ToolId

Stored in `Player.tools`; from Phase 2 in a Backpack Slot's `ItemRef`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `hoe` | Hoe | Tills grass. 2 energy per use. |
| `watering_can` | Watering can | Waters a tilled Tile. 2 energy per use, 1 charge per Tile. |
| `axe` | Axe | Chops trees and stumps. 50 gold at Maggie's store. |
| `pickaxe` | Pickaxe | Breaks rocks, ore deposits and debris. 75 gold; opens the mine (BR-M1). |

---

## ShopItemKind

Stored in `ShopListing.kind`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `seed` | Seeds | Offered only in the Crop type's Season (BR-L1). |
| `can_upgrade` | Watering can upgrade | Phase 1 only. Capacity 20 → 40; bought once per Game (BR-W4, retired in Phase 2). |
| `tool` | Tool | Axe or pickaxe, once each. |
| `backpack_upgrade` | Backpack upgrade | *(planned, REQ-58)* 24 slots (store stage 2), 36 slots (stage 3). |
| `placeable` | Fence or chest | *(planned, REQ-48, REQ-58)* |
| `farmhouse_upgrade` | Farmhouse upgrade | *(planned, REQ-48)* Carpenter stage 2, once; restores the farmhouse's look. |

---

## CropTypeId

Key of `CropType`; referenced by Planting, InventoryStack, ShippedItem and ShopListing.

| Code Value | Display Label | Season | Grow days | Seed cost | Sell price | Prototype ID | Notes |
|------------|---------------|--------|-----------|-----------|------------|--------------|-------|
| `emberroot` | Emberroot | `spring` | 3 | 15 | 35 | `carrot` |  |
| `dusktuber` | Dusktuber | `spring` | 5 | 30 | 75 | `potato` |  |
| `moonmelon` | Moonmelon | `fall` | 8 | 60 | 190 | `pumpkin` |  |
| `sunbell` | Sunbell | `summer` | 4 | 20 | 50 | `tomato` |  |
| `starcorn` | Starcorn | `summer` | 7 | 45 | 130 | `corn` |  |

---

## ToolTier

Stored with each tool's Backpack Slot and in tool-upgrade Orders.

| Code Value | Display Label | Available from | Notes |
|------------|---------------|----------------|-------|
| `basic` | Basic | Start / Maggie's store | Can 20 charges. 4 swings a tree or rock. |
| `rosk` | Rosk | Blacksmith stage 1 | Hoe 3 in a row; can 40. 3 swings. |
| `groth` | Groth | Blacksmith stage 2 | Hoe 3 x 3; can 60, 3 in a row. 2 swings; axe clears stumps. |

---

## MaterialId *(planned, REQ-44)*

| Code Value | Display Label | Comes from |
|------------|---------------|------------|
| `wood` | Wood | Trees, stumps |
| `sap` | Sap | Trees |
| `stone` | Stone | Rocks, mine debris |
| `rosk_ore` | Rosk ore | Rosk deposits, mine levels 1–5 |
| `groth_ore` | Groth ore | Groth deposits, mine levels 3–5 |
| `glimmercap` | Glimmercap | Picked in the mine |
| `plank` | Plank | Carpenter Order: 2 wood + 5 gold |
| `sealed_plank` | Sealed plank | Carpenter Order: 1 plank + 1 sap + 10 gold |
| `rosk_pump` | Rosk pump | Blacksmith Order: 20 Rosk ore + 30 planks + 500 gold (draft) |

---

## PlaceableId *(planned, REQ-48)*

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `fence` | Fence | Decoration on untilled ground. |
| `chest` | Chest | 36 Slots of storage on the farm. |

---

## NodeKind

| Code Value | Display Label | Tool | Grows back |
|------------|---------------|------|------------|
| `tree` | Twinbough tree | Axe | Yes |
| `stump` | Stump | Groth axe | No |
| `rock` | Rock | Pickaxe | Yes |
| `rosk_deposit` | Rosk deposit | Pickaxe | Yes |
| `groth_deposit` | Groth deposit | Pickaxe | Yes |
| `glimmercap` | Glimmercap | Hand | Yes |
| `debris` | Debris | Pickaxe | No |

---

## AreaId *(planned, REQ-49, REQ-48)*

| Code Value | Display Label | Opens |
|------------|---------------|-------|
| `farm` | The meadow | Start |
| `forest_path` | Forest path | Start |
| `town` | Ember Bay | Start |
| `lakeshore` | Lakeshore | Start |
| `across_river` | Across the river | Carpenter stage 2 |
| `mine` | The old mine | Pickaxe bought |

---

## BuildingId

Stored in `ShopListing.vendor` (`general_store` or `carpenter`); *(planned, REQ-45)* the Building key and its stages. Until #124, the general store counts as stage 1 and every other Building as stage 0.

| Code Value | Display Label | Stages in Phase 2 *(planned, REQ-45)* | Starts at *(planned, REQ-45)* |
|------------|---------------|-------------------|-----------|
| `general_store` | General store | 1–4 | 1 (Maggie's corner) |
| `carpenter` | Carpenter | 0–2 | 0 |
| `miras_house` | Mira's house | 0–1 | 0 |
| `blacksmith` | Blacksmith | 0–2 | 0 |

---

## OrderKind and Provider *(planned, REQ-48, REQ-57)*

| Code Value | Display Label | Provider |
|------------|---------------|----------|
| `plank` | Planks | `carpenter` (Carpenter) |
| `sealed_plank` | Sealed planks | `carpenter` |
| `tool_upgrade` | Tool upgrade | `blacksmith` (Blacksmith) |
| `rosk_pump` | Rosk pump | `blacksmith` |

---

## MineLevelState *(planned, REQ-49)*

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `blocked` | Blocked | Levels 1–4 at start; debris to clear. |
| `cleared` | Cleared | Staircase down open (BR-M2). |
| `flooded` | Flooded | Level 5 at start. |
| `drained` | Drained | After a Rosk pump (BR-M3). |

---

## ResidentId

| Code Value | Display Label | Arrives |
|------------|---------------|---------|
| `maggie` | Maggie | Start |
| `clyde` | Clyde | Start |
| `tom` | Tom | Carpenter stage 1 |
| `june` | June | Carpenter stage 1 |
| `jesse` | Jesse | Carpenter stage 1 |
| `mira` | Mira | Mira's house stage 1 and mine level 5 reached |

---

## WaterSource

| Code Value | Display Label | Where |
|------------|---------------|-------|
| `pond` | Pond | Farm |
| `river` | River | Meadow edge and town |
| `lake` | Lake | Lakeshore |
