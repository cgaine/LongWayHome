# Long Way Home — Canonical Values

Every enumerated value in the domain, with the exact code value used in data files, saves and code, and the label shown to players. Cross-references: [domain-model.md](domain-model.md) and [product-definition.md](product-definition.md).

Scope: Phase 1 — Godot rebuild.

## Season

Stored in `Game.date.season` and `CropType.season`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `spring` | Spring | First Season of each Year. Emberroot, dusktuber. |
| `summer` | Summer | No Phase 1 crops (open question on REQ-10). |
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

Stored in `InventoryStack.kind`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `seed` | Seeds | Plantable; cannot be shipped. |
| `crop` | Crop | Harvested; shipped through the Shipping Bin. |

---

## ToolId

Stored in `Player.tools`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `hoe` | Hoe | Tills grass. 2 energy per use. |
| `watering_can` | Watering can | Waters a tilled Tile. 2 energy per use, 1 charge per Tile. |

---

## ShopItemKind

Stored in `ShopListing.kind`.

| Code Value | Display Label | Notes |
|------------|---------------|-------|
| `seed` | Seeds | Offered only in the Crop type's Season (BR-L1). |
| `can_upgrade` | Watering can upgrade | Capacity 20 → 40; bought once per Game (BR-W4). |

---

## CropTypeId

Key of `CropType`; referenced by Planting, InventoryStack, ShippedItem and ShopListing.

| Code Value | Display Label | Season | Grow days | Seed cost | Sell price | Prototype ID |
|------------|---------------|--------|-----------|-----------|------------|--------------|
| `emberroot` | Emberroot | `spring` | 3 | 15 | 35 | `carrot` |
| `dusktuber` | Dusktuber | `spring` | 5 | 30 | 75 | `potato` |
| `moonmelon` | Moonmelon | `fall` | 8 | 60 | 190 | `pumpkin` |
