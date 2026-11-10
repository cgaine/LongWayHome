using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Buildings;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #163 AC9: a save (still version 2) holds the Buildings as <c>buildings</c> of
/// <c>{ id, stage, delivered: [{ materialId, quantity }], deliveredGold }</c> in enum order. A file without it, or a
/// migrated version 1 file, loads with the new-Game Buildings; a malformed entry is refused, like <c>residents</c>.
/// </summary>
public sealed class BuildingSaveTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path);

    private static readonly List<Building> Delivered =
    [
        new(BuildingId.GeneralStore, 2, new Dictionary<MaterialId, int> { [MaterialId.Plank] = 12, [MaterialId.Stone] = 30 }, 450),
        new(BuildingId.Carpenter, 1, new Dictionary<MaterialId, int> { [MaterialId.Wood] = 3 }, 0),
        new(BuildingId.MirasHouse, 0),
        new(BuildingId.Blacksmith, 0),
    ];

    /// <summary>A rich Game with the given Buildings.</summary>
    private static GameState RichWith(IReadOnlyList<Building>? buildings)
    {
        var rich = SaveGames.Rich();
        return new GameState(
            rich.Id,
            rich.Date,
            rich.ClockMinutes,
            rich.Weather,
            rich.LastDayEnd,
            rich.CreatedAt,
            rich.SavedAt,
            rich.Player,
            rich.FarmPlot,
            rich.ShippingBin,
            rich.Residents,
            rich.OpeningDone,
            rich.PurchasedListings,
            rich.ResourceNodes,
            rich.NodeRegrowths,
            buildings);
    }

    private static JsonObject Entry(string id, int stage, int gold, params (string Material, int Quantity)[] delivered) => new()
    {
        ["id"] = id,
        ["stage"] = stage,
        ["delivered"] = new JsonArray(delivered
            .Select(item => (JsonNode)new JsonObject { ["materialId"] = item.Material, ["quantity"] = item.Quantity })
            .ToArray()),
        ["deliveredGold"] = gold,
    };

    /// <summary>The <c>buildings</c> array the game writes for <see cref="Delivered"/>.</summary>
    private static JsonArray DeliveredArray() => new(
        Entry("general_store", 2, 450, ("stone", 30), ("plank", 12)),
        Entry("carpenter", 1, 0, ("wood", 3)),
        Entry("miras_house", 0, 0),
        Entry("blacksmith", 0, 0));

    private void AssertUnreadable(int slot = 1)
    {
        var store = Store();
        var thrown = Assert.Throws<SaveStoreException>(() => store.Load(slot));
        Assert.Equal(slot, thrown.Slot);
        Assert.Equal(SaveSlotState.Unreadable, store.List()[slot - 1].State);
    }

    [Fact]
    public void AC7_A_save_written_after_the_rebuilding_Night_loads_with_the_store_at_stage_2_and_nothing_delivered()
    {
        // #165 AC7: what the Night saves after rebuilding the store (BR-B3, BR-G5).
        Store().Save(1, RichWith(BuildingGames.All(storeStage: 2)));

        var loaded = Store().Load(1);

        Assert.Equal(
            "GeneralStore:2:-:0,Carpenter:0:-:0,MirasHouse:0:-:0,Blacksmith:0:-:0",
            BuildingGames.Describe(loaded.Buildings));
    }

    [Fact]
    public void AC9_A_save_written_after_deliveries_loads_with_the_same_stages_and_delivered_amounts()
    {
        Store().Save(1, RichWith(Delivered));

        var loaded = Store().Load(1);

        Assert.Equal(
            "GeneralStore:2:Stone=30;Plank=12:450,Carpenter:1:Wood=3:0,MirasHouse:0:-:0,Blacksmith:0:-:0",
            BuildingGames.Describe(loaded.Buildings));
    }

    [Fact]
    public void AC9_The_file_holds_buildings_in_enum_order_as_id_stage_delivered_and_deliveredGold()
    {
        Store().Save(1, RichWith(Delivered));

        var document = _folder.ReadSlot(1);

        Assert.Equal(2, document["saveVersion"]!.GetValue<int>());
        Assert.True(document["buildings"] is JsonArray, "the saved file has no buildings array");
        Assert.True(
            JsonNode.DeepEquals(DeliveredArray(), document["buildings"]),
            $"buildings was {document["buildings"]?.ToJsonString()}");
    }

    [Fact]
    public void AC9_A_new_games_file_writes_the_new_game_buildings()
    {
        Store().Save(1, RichWith(null));

        var expected = new JsonArray(
            Entry("general_store", 1, 0), Entry("carpenter", 0, 0), Entry("miras_house", 0, 0), Entry("blacksmith", 0, 0));
        Assert.Equal(expected.ToJsonString(), _folder.ReadSlot(1)["buildings"]?.ToJsonString());
    }

    [Fact]
    public void AC9_A_v2_save_without_buildings_loads_with_the_new_game_buildings()
    {
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack()));

        var loaded = Store().Load(1);

        Assert.Equal(BuildingGames.NewGameBuildings, BuildingGames.Describe(loaded.Buildings));
    }

    [Fact]
    public void AC9_A_saved_file_with_its_buildings_removed_loads_the_new_game_buildings()
    {
        Store().Save(1, RichWith(Delivered));
        var document = _folder.ReadSlot(1);
        document.Remove("buildings");
        _folder.WriteSlot(1, document);

        Assert.Equal(BuildingGames.NewGameBuildings, BuildingGames.Describe(Store().Load(1).Buildings));
    }

    [Fact]
    public void AC9_A_v1_save_migrates_with_the_new_game_buildings()
    {
        _folder.WriteSlot(1, SaveDocuments.V1([]));

        var loaded = Store().Load(1);

        Assert.Equal(BuildingGames.NewGameBuildings, BuildingGames.Describe(loaded.Buildings));
    }

    [Fact]
    public void AC9_A_literal_v2_document_with_buildings_loads_them()
    {
        var document = SaveDocuments.V2(SaveDocuments.GoodBackpack());
        document["buildings"] = DeliveredArray();
        _folder.WriteSlot(1, document);

        Assert.Equal(
            "GeneralStore:2:Stone=30;Plank=12:450,Carpenter:1:Wood=3:0,MirasHouse:0:-:0,Blacksmith:0:-:0",
            BuildingGames.Describe(Store().Load(1).Buildings));
    }

    public static TheoryData<string> BadBuildings => new()
    {
        "unknown id",
        "missing building",
        "duplicate id",
        "out of enum order",
        "null entry",
        "negative stage",
        "unknown material",
        "quantity below 1",
        "negative gold",
        "entry without delivered",
        "not an array",
    };

    private static void BreakBuildings(JsonObject document, string how)
    {
        var buildings = DeliveredArray();
        switch (how)
        {
            case "unknown id":
                buildings[3]!["id"] = "bakery";
                break;
            case "missing building":
                buildings.RemoveAt(3);
                break;
            case "duplicate id":
                buildings[3]!["id"] = "general_store";
                break;
            case "out of enum order":
                var first = buildings[0]!.DeepClone();
                buildings[0] = buildings[1]!.DeepClone();
                buildings[1] = first;
                break;
            case "null entry":
                buildings[2] = null;
                break;
            case "negative stage":
                buildings[1]!["stage"] = -1;
                break;
            case "unknown material":
                buildings[0]!["delivered"]![0]!["materialId"] = "marble";
                break;
            case "quantity below 1":
                buildings[0]!["delivered"]![0]!["quantity"] = 0;
                break;
            case "negative gold":
                buildings[0]!["deliveredGold"] = -5;
                break;
            case "entry without delivered":
                buildings[1]!.AsObject().Remove("delivered");
                break;
            case "not an array":
                document["buildings"] = "general_store";
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(how), how, null);
        }

        document["buildings"] = buildings;
    }

    [Theory]
    [MemberData(nameof(BadBuildings))]
    public void AC9_A_save_whose_buildings_are_malformed_is_refused_on_load(string how)
    {
        // Control, in slot 2: the same document with a whole buildings array loads.
        var good = SaveDocuments.V2(SaveDocuments.GoodBackpack());
        good["buildings"] = DeliveredArray();
        _folder.WriteSlot(2, good);
        Store().Load(2);

        var document = SaveDocuments.V2(SaveDocuments.GoodBackpack());
        BreakBuildings(document, how);
        _folder.WriteSlot(1, document);

        AssertUnreadable();
    }
}
