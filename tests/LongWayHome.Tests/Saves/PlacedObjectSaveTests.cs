using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Farming;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #164 AC9: a save (still version 2) holds the Farm plot's Placed objects as <c>farmPlot.placedObjects</c> of
/// <c>{ position, placeable, slots? }</c>, a chest's 36 slots written like Backpack Slots. A file without it loads
/// with none; a malformed one, one overlapping a tilled Tile or a node, or a chest holding a tool or not exactly 36
/// slots, is refused. A placeable in a Slot is <c>{ "kind": "placeable", "placeableId": "fence" }</c>.
/// </summary>
public sealed class PlacedObjectSaveTests : IDisposable
{
    private static readonly GridPosition FenceTile = new(60, 30);

    private static readonly GridPosition ChestTile = new(61, 30);

    private static readonly GridPosition NodeTile = new(20, 20);

    private static readonly NodeSpawn[] Spawns = [NodeGames.Spawn(NodeTile.X, NodeTile.Y, NodeKind.Rock)];

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path, Spawns);

    /// <summary>A Game with a fence and a chest holding 12 stone in its slot 0, and the rock at <see cref="NodeTile"/>.</summary>
    private static GameState GameWithFenceAndChest() => PlacedGames.With(
        TestGames.ToolsOnly(),
        [PlacedGames.FenceAt(FenceTile), PlacedGames.ChestAt(ChestTile, new BackpackSlot(0, PlacedGames.Stone, 12, null))],
        nodes: [NodeGames.Node(NodeTile.X, NodeTile.Y, NodeKind.Rock)]);

    /// <summary>A plain Game (nothing placed) with the rock at <see cref="NodeTile"/>.</summary>
    private static GameState PlainGame() => PlacedGames.Plain(
        TestGames.ToolsOnly(), nodes: [NodeGames.Node(NodeTile.X, NodeTile.Y, NodeKind.Rock)]);

    private static JsonObject Position(GridPosition at) => new() { ["x"] = at.X, ["y"] = at.Y };

    private static JsonObject ChestSlot(int index, JsonObject? item, int quantity, string? tier = null) => new()
    {
        ["index"] = index,
        ["item"] = item,
        ["quantity"] = quantity,
        ["toolTier"] = tier,
    };

    private static JsonArray ChestSlots(int count, params JsonObject[] filled)
    {
        var byIndex = filled.ToDictionary(slot => slot["index"]!.GetValue<int>());
        var slots = new JsonArray();
        for (var index = 0; index < count; index++)
        {
            slots.Add(byIndex.TryGetValue(index, out var slot) ? slot : ChestSlot(index, null, 0));
        }

        return slots;
    }

    private static JsonObject StoneItem() => new() { ["kind"] = "material", ["materialId"] = "stone" };

    /// <summary>A whole <c>placedObjects</c> array: the fence, and the chest with 12 stone in slot 0.</summary>
    private static JsonArray PlacedObjectsArray() => new(
        new JsonObject { ["position"] = Position(FenceTile), ["placeable"] = "fence" },
        new JsonObject
        {
            ["position"] = Position(ChestTile),
            ["placeable"] = "chest",
            ["slots"] = ChestSlots(36, ChestSlot(0, StoneItem(), 12)),
        });

    private static JsonObject Entry(JsonObject document, int index) =>
        document["farmPlot"]!["placedObjects"]!.AsArray()[index]!.AsObject();

    private static JsonObject Slot(JsonObject document, int index) =>
        document["player"]!["backpack"]!["slots"]!.AsArray()
            .Select(entry => entry!.AsObject())
            .Single(entry => entry["index"]!.GetValue<int>() == index);

    // ---- round trip ----

    [Fact]
    public void AC9_a_placed_fence_and_a_chest_holding_12_stone_load_back_in_place_with_the_stone()
    {
        Store().Save(1, GameWithFenceAndChest());

        var loaded = Store().Load(1);

        var fence = loaded.FarmPlot.PlacedAt(FenceTile);
        var chest = loaded.FarmPlot.PlacedAt(ChestTile);
        Assert.NotNull(fence);
        Assert.NotNull(chest);
        Assert.Equal(PlaceableId.Fence, fence.Placeable);
        Assert.Equal(PlaceableId.Chest, chest.Placeable);
        Assert.Equal(2, loaded.FarmPlot.PlacedObjects.Count);
        Assert.Equal(36, chest.Slots.Count);
        Assert.Equal(PlacedGames.Stone, chest.Slots[0].Item);
        Assert.Equal(12, chest.Slots[0].Quantity);
        Assert.Equal(PlacedGames.Describe(GameWithFenceAndChest().FarmPlot), PlacedGames.Describe(loaded.FarmPlot));
    }

    [Fact]
    public void AC9_the_file_holds_placed_objects_under_farm_plot_as_position_placeable_and_slots()
    {
        Store().Save(1, GameWithFenceAndChest());

        var document = _folder.ReadSlot(1);

        Assert.Equal(2, document["saveVersion"]!.GetValue<int>());
        Assert.True(document["farmPlot"]!["placedObjects"] is JsonArray, "the saved farmPlot has no placedObjects array");
        var entries = document["farmPlot"]!["placedObjects"]!.AsArray().Select(entry => entry!.AsObject()).ToList();
        var fence = Assert.Single(entries, entry => entry["placeable"]!.GetValue<string>() == "fence");
        var chest = Assert.Single(entries, entry => entry["placeable"]!.GetValue<string>() == "chest");
        Assert.Equal(FenceTile.X, fence["position"]!["x"]!.GetValue<int>());
        Assert.Equal(FenceTile.Y, fence["position"]!["y"]!.GetValue<int>());
        Assert.True(fence["slots"] is null || fence["slots"]!.AsArray().Count == 0, "a fence has no slots");
        var slots = chest["slots"]!.AsArray();
        Assert.Equal(36, slots.Count);
        var slot0 = slots.Select(entry => entry!.AsObject()).Single(entry => entry["index"]!.GetValue<int>() == 0);
        Assert.Equal("material", slot0["item"]!["kind"]!.GetValue<string>());
        Assert.Equal("stone", slot0["item"]!["materialId"]!.GetValue<string>());
        Assert.Equal(12, slot0["quantity"]!.GetValue<int>());
    }

    [Fact]
    public void AC9_a_game_with_nothing_placed_writes_an_empty_placed_objects_array()
    {
        Store().Save(1, PlainGame());

        Assert.Equal("[]", _folder.ReadSlot(1)["farmPlot"]!["placedObjects"]?.ToJsonString());
    }

    [Fact]
    public void AC9_a_version_2_save_without_placed_objects_loads_with_none()
    {
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack()));

        var loaded = new JsonSaveStore(_folder.Path, []).Load(1);

        Assert.Empty(loaded.FarmPlot.PlacedObjects);
    }

    [Fact]
    public void AC9_a_saved_file_with_its_placed_objects_property_removed_loads_with_none()
    {
        Store().Save(1, GameWithFenceAndChest());
        var document = _folder.ReadSlot(1);
        document["farmPlot"]!.AsObject().Remove("placedObjects");
        _folder.WriteSlot(1, document);

        Assert.Empty(Store().Load(1).FarmPlot.PlacedObjects);
    }

    // ---- placeables in Slots ----

    [Theory]
    [InlineData(PlaceableId.Fence, "fence")]
    [InlineData(PlaceableId.Chest, "chest")]
    public void AC9_a_placeable_slot_is_saved_with_kind_placeable_and_its_placeable_id(PlaceableId placeable, string code)
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(5, ItemRef.Placeable(placeable), 7, null)));

        Store().Save(1, game);

        var slot = Slot(_folder.ReadSlot(1), 5);
        var item = slot["item"]!.AsObject();
        Assert.Equal("placeable", item["kind"]!.GetValue<string>());
        Assert.Equal(code, item["placeableId"]!.GetValue<string>());
        Assert.False(item.ContainsKey("id"), "a placeable has no id");
        Assert.Equal(7, slot["quantity"]!.GetValue<int>());
    }

    [Fact]
    public void AC9_placeables_in_backpack_slots_round_trip()
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(4, PlacedGames.Fence, 12, null),
            new BackpackSlot(9, PlacedGames.Chest, 2, null)));

        Store().Save(1, game);
        var loaded = Store().Load(1);

        Assert.Equal(
            PlacedGames.Describe(game.Player.Backpack.Slots),
            PlacedGames.Describe(loaded.Player.Backpack.Slots));
    }

    [Fact]
    public void AC9_a_saved_placeable_slot_with_an_unknown_code_is_refused()
    {
        Store().Save(1, PlainGame());
        var document = _folder.ReadSlot(1);
        Slot(document, 5)["item"] = new JsonObject { ["kind"] = "placeable", ["placeableId"] = "gate" };
        Slot(document, 5)["quantity"] = 1;
        _folder.WriteSlot(1, document);

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }

    // ---- refusals ----

    public static TheoryData<string> BadPlacedObjects => new()
    {
        "a chest slot holding a tool",
        "a chest with 35 slots",
        "a chest with 37 slots",
        "a chest with no slots",
        "a chest slot over 99",
        "a fence on a tilled tile",
        "a chest on a node",
        "two objects on one tile",
        "an unknown placeable",
        "a null entry",
        "an entry without a position",
        "not an array",
    };

    /// <summary>Writes a whole <c>placedObjects</c> array into the document with one defect.</summary>
    private static void BreakPlacedObjects(JsonObject document, string how)
    {
        var placed = PlacedObjectsArray();
        var farmPlot = document["farmPlot"]!.AsObject();
        switch (how)
        {
            case "a chest slot holding a tool":
                placed[1]!["slots"]![3] = ChestSlot(3, new JsonObject { ["kind"] = "tool", ["id"] = "axe" }, 1, "basic");
                break;
            case "a chest with 35 slots":
                placed[1]!["slots"] = ChestSlots(35, ChestSlot(0, StoneItem(), 12));
                break;
            case "a chest with 37 slots":
                placed[1]!["slots"] = ChestSlots(37, ChestSlot(0, StoneItem(), 12));
                break;
            case "a chest with no slots":
                placed[1]!.AsObject().Remove("slots");
                break;
            case "a chest slot over 99":
                placed[1]!["slots"]![0] = ChestSlot(0, StoneItem(), 100);
                break;
            case "a fence on a tilled tile":
                SaveDocuments.SetTile(document, FenceTile.X, FenceTile.Y, "tilled", false);
                break;
            case "a chest on a node":
                placed[1]!["position"] = Position(NodeTile);
                break;
            case "two objects on one tile":
                placed[1]!["position"] = Position(FenceTile);
                break;
            case "an unknown placeable":
                placed[0]!["placeable"] = "gate";
                break;
            case "a null entry":
                placed[0] = null;
                break;
            case "an entry without a position":
                placed[0]!.AsObject().Remove("position");
                break;
            case "not an array":
                farmPlot["placedObjects"] = "fence";
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(how), how, null);
        }

        farmPlot["placedObjects"] = placed;
    }

    [Fact]
    public void AC9_a_save_with_a_whole_placed_objects_array_loads_it()
    {
        // The control for the refusals below: the same file with a whole array loads with both objects.
        Store().Save(1, PlainGame());
        var document = _folder.ReadSlot(1);
        document["farmPlot"]!["placedObjects"] = PlacedObjectsArray();
        _folder.WriteSlot(1, document);

        var loaded = Store().Load(1);

        Assert.Equal(PlacedGames.Describe(GameWithFenceAndChest().FarmPlot), PlacedGames.Describe(loaded.FarmPlot));
    }

    [Theory]
    [MemberData(nameof(BadPlacedObjects))]
    public void AC9_a_save_whose_placed_objects_are_malformed_is_refused_on_load(string how)
    {
        // Control, in slot 2: the same file with a whole placedObjects array loads, so the refusal below is the defect's.
        Store().Save(2, PlainGame());
        var control = _folder.ReadSlot(2);
        control["farmPlot"]!["placedObjects"] = PlacedObjectsArray();
        _folder.WriteSlot(2, control);
        Assert.Equal(2, Store().Load(2).FarmPlot.PlacedObjects.Count);

        Store().Save(1, PlainGame());
        var document = _folder.ReadSlot(1);
        BreakPlacedObjects(document, how);
        _folder.WriteSlot(1, document);

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }
}
