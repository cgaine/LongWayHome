using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Farming;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #164: the save paths around placed objects that the AC9 tests don't reach — an item carrying ids for the wrong kind,
/// a fence saved with slots, and starting nodes never landing on a placed object's Tile (BR-F1) when a save gains them.
/// </summary>
public sealed class PlacedObjectSaveGuardTests : IDisposable
{
    private static readonly GridPosition SpawnTile = new(20, 20);

    private static readonly NodeSpawn[] Spawns = [NodeGames.Spawn(SpawnTile.X, SpawnTile.Y, NodeKind.Rock)];

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path, Spawns);

    private static JsonObject Slot(JsonObject document, int index) =>
        document["player"]!["backpack"]!["slots"]!.AsArray()
            .Select(entry => entry!.AsObject())
            .Single(entry => entry["index"]!.GetValue<int>() == index);

    private JsonObject SavedWithFenceInSlot5()
    {
        Store().Save(1, TestGames.WithBackpack(TestGames.RestoredBackpack(new BackpackSlot(5, PlacedGames.Fence, 1, null))));
        return _folder.ReadSlot(1);
    }

    public static TheoryData<string> WrongIds => new()
    {
        "a placeable with an id",
        "a placeable with a materialId",
        "a placeable without a placeableId",
        "a material with a placeableId",
        "a seed with a placeableId",
    };

    [Theory]
    [MemberData(nameof(WrongIds))]
    public void A_saved_item_whose_ids_dont_match_its_kind_is_refused(string how)
    {
        var document = SavedWithFenceInSlot5();
        var item = Slot(document, 5)["item"]!.AsObject();
        switch (how)
        {
            case "a placeable with an id":
                item["id"] = "fence";
                break;
            case "a placeable with a materialId":
                item["materialId"] = "wood";
                break;
            case "a placeable without a placeableId":
                item.Remove("placeableId");
                break;
            case "a material with a placeableId":
                item["kind"] = "material";
                item["materialId"] = "wood";
                break;
            case "a seed with a placeableId":
                item["kind"] = "seed";
                item["id"] = "emberroot";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(how), how, null);
        }

        _folder.WriteSlot(1, document);

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }

    [Fact]
    public void BR_F2_a_saved_fence_with_slots_is_refused()
    {
        Store().Save(1, PlacedGames.With(TestGames.ToolsOnly(), [PlacedGames.FenceAt(new GridPosition(60, 30))]));
        var document = _folder.ReadSlot(1);
        document["farmPlot"]!["placedObjects"]![0]!["slots"] = new JsonArray(
            new JsonObject { ["index"] = 0, ["item"] = null, ["quantity"] = 0, ["toolTier"] = null });
        _folder.WriteSlot(1, document);

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }

    [Fact]
    public void BR_F1_a_save_without_resource_nodes_places_no_starting_node_under_a_placed_object()
    {
        Store().Save(1, PlacedGames.With(TestGames.ToolsOnly(), [PlacedGames.FenceAt(SpawnTile)]));
        var document = _folder.ReadSlot(1);
        document.Remove("resourceNodes");
        _folder.WriteSlot(1, document);

        var loaded = Store().Load(1);

        Assert.Null(loaded.NodeAt(AreaId.Farm, SpawnTile));
        Assert.Equal(PlaceableId.Fence, loaded.FarmPlot.PlacedAt(SpawnTile)!.Placeable);
    }

    [Fact]
    public void BR_F1_a_save_gaining_the_farms_starting_nodes_skips_a_placed_objects_tile()
    {
        Store().Save(1, PlacedGames.With(TestGames.ToolsOnly(), [PlacedGames.ChestAt(SpawnTile)]));
        var document = _folder.ReadSlot(1);
        document["resourceNodes"] = new JsonArray();
        document["nodeAreasPlaced"] = new JsonArray();
        _folder.WriteSlot(1, document);

        var loaded = Store().Load(1);

        Assert.Null(loaded.NodeAt(AreaId.Farm, SpawnTile));
        Assert.Equal(PlaceableId.Chest, loaded.FarmPlot.PlacedAt(SpawnTile)!.Placeable);
    }
}
