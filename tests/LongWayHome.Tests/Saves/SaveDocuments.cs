using System.Text.Json.Nodes;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>Games carrying a Backpack, for the save version 2 tests (#128).</summary>
internal static class BackpackSaveGames
{
    /// <summary>
    /// <see cref="SaveGames.Rich"/> with its Player carrying a 24-Slot Backpack: Basic hoe (0), Rosk can (1), 99 + 21
    /// sunbell seeds (2, 3), an empty Slot 4, 2 starcorn seeds (5), 4 dusktuber crops (6), and an upgraded can at 13 of
    /// 40.
    /// </summary>
    public static GameState Rich()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.SetTier(ToolId.WateringCan, ToolTier.Rosk);
        backpack.RaiseCapacity(24);
        backpack.Add(ItemRef.Seed(CropTypeId.Sunbell), 120);
        backpack.Add(ItemRef.Crop(CropTypeId.Emberroot), 7);
        backpack.Add(ItemRef.Seed(CropTypeId.Starcorn), 2);
        backpack.Add(ItemRef.Crop(CropTypeId.Dusktuber), 4);
        backpack.Remove(ItemRef.Crop(CropTypeId.Emberroot), 7);

        var rich = SaveGames.Rich();
        return new GameState(
            rich.Id,
            rich.Date,
            rich.ClockMinutes,
            rich.Weather,
            rich.LastDayEnd,
            rich.CreatedAt,
            rich.SavedAt,
            new Player(new Gold(1234), 77, new WateringCan(40, 13, true), backpack),
            rich.FarmPlot,
            rich.ShippingBin);
    }
}

/// <summary>
/// Literal save documents written the way the game writes them: a version 1 document with <c>player.tools</c> and
/// <c>player.inventory</c>, or a version 2 document with <c>player.backpack</c>. The 108 grass Tiles are generated.
/// </summary>
internal static class SaveDocuments
{
    /// <summary>A version 1 document: both tools, the given inventory stacks in order, and the given can.</summary>
    public static JsonObject V1(
        JsonArray inventory, int gold = 1234, int capacity = 20, int charges = 20, bool upgraded = false) =>
        Document(1, new JsonObject
        {
            ["gold"] = gold,
            ["energy"] = 77,
            ["tools"] = new JsonArray("hoe", "watering_can"),
            ["wateringCan"] = Can(capacity, charges, upgraded),
            ["inventory"] = inventory,
        });

    /// <summary>A version 1 inventory stack: <c>{ kind, cropTypeId, quantity }</c>.</summary>
    public static JsonObject Stack(string kind, string cropTypeId, int quantity) =>
        new() { ["kind"] = kind, ["cropTypeId"] = cropTypeId, ["quantity"] = quantity };

    /// <summary>A version 2 document carrying the given backpack and a 20-charge Basic can.</summary>
    public static JsonObject V2(JsonObject backpack, int gold = 1234) =>
        Document(2, new JsonObject
        {
            ["gold"] = gold,
            ["energy"] = 77,
            ["wateringCan"] = Can(20, 20, false),
            ["backpack"] = backpack,
        });

    /// <summary>
    /// A valid 12-Slot backpack: Basic hoe (0), Basic can (1), 10 emberroot seeds (2), 3 dusktuber crops (3), the rest
    /// empty.
    /// </summary>
    public static JsonObject GoodBackpack() => Backpack(
        12,
        0,
        Slot(0, "tool", "hoe", 1, "basic"),
        Slot(1, "tool", "watering_can", 1, "basic"),
        Slot(2, "seed", "emberroot", 10, null),
        Slot(3, "crop", "dusktuber", 3, null));

    /// <summary>The description of <see cref="GoodBackpack"/> as <see cref="TestGames.Describe"/> writes it.</summary>
    public static string GoodBackpackDescribed => string.Join(
        ",",
        new[] { "0:Tool:Hoe:1:Basic", "1:Tool:WateringCan:1:Basic", "2:Seed:Emberroot:10:-", "3:Crop:Dusktuber:3:-" }
            .Concat(Enumerable.Range(4, 8).Select(index => $"{index}:-:-:0:-")));

    /// <summary>A backpack of the given capacity: the given Slots, and an empty Slot at every other index.</summary>
    public static JsonObject Backpack(int capacity, int hotbarRow, params JsonObject[] filled)
    {
        var byIndex = filled.ToDictionary(slot => slot["index"]!.GetValue<int>());
        var slots = new JsonArray();
        for (var index = 0; index < capacity; index++)
        {
            slots.Add(byIndex.TryGetValue(index, out var slot) ? slot : EmptySlot(index));
        }

        return new JsonObject { ["capacity"] = capacity, ["hotbarRow"] = hotbarRow, ["slots"] = slots };
    }

    /// <summary>A filled Slot: <c>{ index, item: { kind, id }, quantity, toolTier }</c>.</summary>
    public static JsonObject Slot(int index, string kind, string id, int quantity, string? toolTier) => new()
    {
        ["index"] = index,
        ["item"] = new JsonObject { ["kind"] = kind, ["id"] = id },
        ["quantity"] = quantity,
        ["toolTier"] = toolTier,
    };

    /// <summary>An empty Slot: <c>{ index, item: null, quantity: 0, toolTier: null }</c>.</summary>
    public static JsonObject EmptySlot(int index) => new()
    {
        ["index"] = index,
        ["item"] = null,
        ["quantity"] = 0,
        ["toolTier"] = null,
    };

    private static JsonObject Can(int capacity, int charges, bool upgraded) =>
        new() { ["capacity"] = capacity, ["charges"] = charges, ["upgraded"] = upgraded };

    private static JsonObject Document(int version, JsonObject player)
    {
        var tiles = new JsonArray();
        for (var y = 0; y < 9; y++)
        {
            for (var x = 0; x < 12; x++)
            {
                tiles.Add(new JsonObject
                {
                    ["position"] = new JsonObject { ["x"] = x, ["y"] = y },
                    ["state"] = "grass",
                    ["watered"] = false,
                    ["planting"] = null,
                });
            }
        }

        return new JsonObject
        {
            ["saveVersion"] = version,
            ["id"] = "6f1c2a8e-3b4d-4e5f-8a9b-0c1d2e3f4a5b",
            ["date"] = new JsonObject { ["year"] = 2, ["season"] = "summer", ["day"] = 14 },
            ["clock"] = 780,
            ["weather"] = "rain",
            ["lastDayEnd"] = "passed_out",
            ["createdAt"] = "2026-10-01T08:00:00+01:00",
            ["savedAt"] = "2026-11-02T21:15:30+00:00",
            ["player"] = player,
            ["farmPlot"] = new JsonObject { ["width"] = 12, ["height"] = 9, ["tiles"] = tiles },
            ["shippingBin"] = new JsonObject { ["items"] = new JsonArray() },
        };
    }
}
