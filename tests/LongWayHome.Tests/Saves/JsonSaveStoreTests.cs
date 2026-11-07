using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// <see cref="JsonSaveStore"/>: round trip, file shape, crash safety (.tmp, .bak), listing and refusing bad files.
/// </summary>
public sealed class JsonSaveStoreTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path);

    /// <summary>Saves a valid Game to slot 1, edits its file, and returns the edited document.</summary>
    private void SaveThenEdit(Action<JsonObject> edit)
    {
        Store().Save(1, SaveGames.Rich());
        var document = _folder.ReadSlot(1);
        edit(document);
        _folder.WriteSlot(1, document);
    }

    private void AssertUnreadable(int slot = 1)
    {
        var store = Store();
        var thrown = Assert.Throws<SaveStoreException>(() => store.Load(slot));
        Assert.Equal(slot, thrown.Slot);
        Assert.False(string.IsNullOrWhiteSpace(thrown.Reason));
        Assert.Equal(SaveSlotState.Unreadable, store.List()[slot - 1].State);
    }

    private static JsonArray Tiles(JsonObject document) => document["farmPlot"]!["tiles"]!.AsArray();

    private static JsonObject PlantedTile(JsonObject document) =>
        Tiles(document).Select(tile => tile!.AsObject()).Single(tile => tile["planting"] is not null);

    // ---- #128: save version 2 and the Backpack ----

    [Fact]
    public void AC12_A_saved_backpack_loads_back_slot_for_slot()
    {
        var game = BackpackSaveGames.Rich();

        Store().Save(1, game);
        var loaded = new JsonSaveStore(_folder.Path).Load(1);

        Assert.Equal(24, loaded.Player.Backpack.Capacity);
        Assert.Equal(game.Player.Backpack.HotbarRow, loaded.Player.Backpack.HotbarRow);
        Assert.Equal(TestGames.Describe(game.Player.Backpack), TestGames.Describe(loaded.Player.Backpack));
        Assert.Equal(new Gold(1234), loaded.Player.Gold);
        Assert.Equal(77, loaded.Player.Energy);
        Assert.Equal((40, 13, true), (loaded.Player.WateringCan.Capacity, loaded.Player.WateringCan.Charges, loaded.Player.WateringCan.Upgraded));
        Assert.Equal(game.ShippingBin.Items, loaded.ShippingBin.Items);
        Assert.Equal(game.FarmPlot[SaveGames.Growing].Planting, loaded.FarmPlot[SaveGames.Growing].Planting);
    }

    [Fact]
    public void AC12_The_file_has_saveVersion_2_and_writes_every_backpack_slot()
    {
        Store().Save(1, BackpackSaveGames.Rich());

        var document = _folder.ReadSlot(1);

        Assert.Equal(2, document["saveVersion"]!.GetValue<int>());
        var player = document["player"]!.AsObject();
        Assert.False(player.ContainsKey("tools"));
        Assert.False(player.ContainsKey("inventory"));
        var backpack = player["backpack"]!.AsObject();
        Assert.Equal(24, backpack["capacity"]!.GetValue<int>());
        Assert.Equal(0, backpack["hotbarRow"]!.GetValue<int>());
        var slots = backpack["slots"]!.AsArray();
        Assert.Equal(Enumerable.Range(0, 24), slots.Select(slot => slot!["index"]!.GetValue<int>()));
        Assert.True(JsonNode.DeepEquals(SaveDocuments.Slot(0, "tool", "hoe", 1, "basic"), slots[0]));
        Assert.True(JsonNode.DeepEquals(SaveDocuments.Slot(1, "tool", "watering_can", 1, "rosk"), slots[1]));
        Assert.True(JsonNode.DeepEquals(SaveDocuments.Slot(2, "seed", "sunbell", 99, null), slots[2]));
        Assert.True(JsonNode.DeepEquals(SaveDocuments.EmptySlot(4), slots[4]), slots[4]?.ToJsonString());
        Assert.True(JsonNode.DeepEquals(SaveDocuments.Slot(6, "crop", "dusktuber", 4, null), slots[6]));
        Assert.True(JsonNode.DeepEquals(SaveDocuments.EmptySlot(23), slots[23]));
    }

    [Fact]
    public void AC12_A_literal_v2_document_loads_its_backpack()
    {
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack(), gold: 222));

        var loaded = Store().Load(1);

        Assert.Equal(new Gold(222), loaded.Player.Gold);
        Assert.Equal(12, loaded.Player.Backpack.Capacity);
        Assert.Equal(SaveDocuments.GoodBackpackDescribed, TestGames.Describe(loaded.Player.Backpack));
    }

    /// <summary>Breaks one rule in a valid v2 backpack (BR-K1, BR-K2, BR-K6, the hotbar row, the item id).</summary>
    private static void Break(JsonObject backpack, string how)
    {
        var slots = backpack["slots"]!.AsArray();
        switch (how)
        {
            case "capacity_13":
                backpack["capacity"] = 13;
                slots.Add(SaveDocuments.EmptySlot(12));
                break;
            case "capacity_24_with_12_slots":
                backpack["capacity"] = 24;
                break;
            case "slot_count_not_capacity":
                slots.RemoveAt(slots.Count - 1);
                break;
            case "duplicate_index":
                slots[11]!["index"] = 10;
                break;
            case "index_out_of_range":
                slots[11]!["index"] = 12;
                break;
            case "item_quantity_0":
                slots[2]!["quantity"] = 0;
                break;
            case "item_quantity_100":
                slots[2]!["quantity"] = 100;
                break;
            case "tool_quantity_2":
                slots[0]!["quantity"] = 2;
                break;
            case "tool_with_no_tier":
                slots[0]!["toolTier"] = null;
                break;
            case "seed_with_a_tier":
                slots[2]!["toolTier"] = "basic";
                break;
            case "empty_slot_with_a_quantity":
                slots[5]!["quantity"] = 3;
                break;
            case "empty_slot_with_a_tier":
                slots[5]!["toolTier"] = "basic";
                break;
            case "hotbar_row_2":
                backpack["hotbarRow"] = 2;
                break;
            case "hotbar_row_negative":
                backpack["hotbarRow"] = -1;
                break;
            case "hoe_missing":
                slots[0] = SaveDocuments.EmptySlot(0);
                break;
            case "hoe_twice":
                slots[4] = SaveDocuments.Slot(4, "tool", "hoe", 1, "basic");
                break;
            case "can_missing":
                slots[1] = SaveDocuments.EmptySlot(1);
                break;
            case "can_twice":
                slots[4] = SaveDocuments.Slot(4, "tool", "watering_can", 1, "basic");
                break;
            case "id_not_matching_kind":
                slots[2]!["item"] = new JsonObject { ["kind"] = "seed", ["id"] = "hoe" };
                break;
            case "backpack_missing_slots":
                backpack.Remove("slots");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(how), how, null);
        }
    }

    [Theory]
    [InlineData("capacity_13")]
    [InlineData("capacity_24_with_12_slots")]
    [InlineData("slot_count_not_capacity")]
    [InlineData("duplicate_index")]
    [InlineData("index_out_of_range")]
    [InlineData("item_quantity_0")]
    [InlineData("item_quantity_100")]
    [InlineData("tool_quantity_2")]
    [InlineData("tool_with_no_tier")]
    [InlineData("seed_with_a_tier")]
    [InlineData("empty_slot_with_a_quantity")]
    [InlineData("empty_slot_with_a_tier")]
    [InlineData("hotbar_row_2")]
    [InlineData("hotbar_row_negative")]
    [InlineData("hoe_missing")]
    [InlineData("hoe_twice")]
    [InlineData("can_missing")]
    [InlineData("can_twice")]
    [InlineData("id_not_matching_kind")]
    [InlineData("backpack_missing_slots")]
    public void AC16_A_v2_backpack_that_breaks_a_rule_is_unreadable_and_the_slot_falls_back_to_its_bak(string how)
    {
        var broken = SaveDocuments.GoodBackpack();
        Break(broken, how);
        _folder.WriteSlot(1, SaveDocuments.V2(broken, gold: 222));
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack(), gold: 111), ".bak");

        var loaded = Store().Load(1);

        Assert.Equal(new Gold(111), loaded.Player.Gold);
        Assert.Equal(SaveDocuments.GoodBackpackDescribed, TestGames.Describe(loaded.Player.Backpack));
        var info = Store().List()[0];
        Assert.Equal(SaveSlotState.Saved, info.State);
        Assert.Equal(111, info.Gold);
    }

    [Theory]
    [InlineData("capacity_13")]
    [InlineData("can_missing")]
    [InlineData("item_quantity_100")]
    public void AC16_A_broken_v2_backpack_with_no_bak_is_unreadable(string how)
    {
        var broken = SaveDocuments.GoodBackpack();
        Break(broken, how);
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack(), gold: 111));
        Assert.Equal(111, Store().Load(1).Player.Gold.Value);
        _folder.WriteSlot(1, SaveDocuments.V2(broken, gold: 222));

        AssertUnreadable();
    }

    [Fact]
    public void AC1_store_round_trips_every_saved_attribute()
    {
        var game = SaveGames.Rich();

        Store().Save(1, game);
        var loaded = new JsonSaveStore(_folder.Path).Load(1);

        SavedState.AssertSame(game, loaded);
        Assert.NotEmpty(loaded.ShippingBin.Items);
    }

    [Fact]
    public void AC1_a_game_with_no_last_day_end_round_trips()
    {
        var game = SaveGames.Rich(lastDayEnd: null, withBin: false);

        Store().Save(3, game);

        SavedState.AssertSame(game, Store().Load(3));
    }

    [Fact]
    public void AC6_enums_are_written_as_canonical_codes()
    {
        Store().Save(1, SaveGames.Rich(date: new GameDate(1, Season.Spring, 20), weather: Weather.Rain, lastDayEnd: DayEndReason.PassedOut));

        var document = _folder.ReadSlot(1);

        Assert.Equal(2, document["saveVersion"]!.GetValue<int>());
        Assert.Equal("passed_out", document["lastDayEnd"]!.GetValue<string>());
        Assert.Equal("rain", document["weather"]!.GetValue<string>());
        Assert.Equal("spring", document["date"]!["season"]!.GetValue<string>());
        var filled = document["player"]!["backpack"]!["slots"]!.AsArray()
            .Where(slot => slot!["item"] is not null)
            .ToList();
        var tools = filled
            .Where(slot => slot!["item"]!["kind"]!.GetValue<string>() == "tool")
            .Select(slot => $"{slot!["item"]!["id"]!.GetValue<string>()}:{slot["toolTier"]!.GetValue<string>()}")
            .Order()
            .ToList();
        Assert.Equal(["hoe:basic", "watering_can:rosk"], tools);
        var states = Tiles(document).Select(tile => tile!["state"]!.GetValue<string>()).ToHashSet();
        Assert.Equal(new HashSet<string> { "grass", "tilled" }, states);
        var stacks = filled
            .Where(slot => slot!["item"]!["kind"]!.GetValue<string>() != "tool")
            .Select(slot => $"{slot!["item"]!["kind"]!.GetValue<string>()}:{slot["item"]!["id"]!.GetValue<string>()}")
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["crop:emberroot", "seed:starcorn", "seed:sunbell"], stacks);
        Assert.Equal("starcorn", PlantedTile(document)["planting"]!["cropTypeId"]!.GetValue<string>());
        Assert.Equal("summer", PlantedTile(document)["planting"]!["plantedOn"]!["season"]!.GetValue<string>());
    }

    [Fact]
    public void AC6_file_has_the_documented_shape()
    {
        Store().Save(1, SaveGames.Rich());

        var document = _folder.ReadSlot(1);

        Assert.Equal(12, document["farmPlot"]!["width"]!.GetValue<int>());
        Assert.Equal(9, document["farmPlot"]!["height"]!.GetValue<int>());
        Assert.Equal(108, Tiles(document).Count);
        Assert.Equal(780, document["clock"]!.GetValue<int>());
        Assert.Equal(1234, document["player"]!["gold"]!.GetValue<int>());
        Assert.Equal(40, document["player"]!["wateringCan"]!["capacity"]!.GetValue<int>());
        Assert.Equal(13, document["player"]!["wateringCan"]!["charges"]!.GetValue<int>());
        Assert.True(document["player"]!["wateringCan"]!["upgraded"]!.GetValue<bool>());
        var planting = PlantedTile(document)["planting"]!.AsObject();
        Assert.Equal(4, planting["daysGrown"]!.GetValue<int>());
        Assert.False(planting.ContainsKey("status"));
        var shipped = document["shippingBin"]!["items"]!.AsArray()
            .Select(item => $"{item!["cropTypeId"]!.GetValue<string>()}:{item["quantity"]!.GetValue<int>()}")
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(["emberroot:12", "sunbell:3"], shipped);
    }

    [Fact]
    public void AC7_saving_over_a_slot_keeps_the_previous_file_as_bak()
    {
        var store = Store();
        store.Save(2, SaveGames.Plain(gold: 111));
        Assert.False(File.Exists(_folder.SlotFile(2, ".bak")));

        store.Save(2, SaveGames.Plain(gold: 222));

        Assert.Equal(111, _folder.ReadSlot(2, ".bak")["player"]!["gold"]!.GetValue<int>());
        Assert.Equal(222, _folder.ReadSlot(2)["player"]!["gold"]!.GetValue<int>());
        Assert.Equal(222, store.Load(2).Player.Gold.Value);
        Assert.False(File.Exists(_folder.SlotFile(2, ".tmp")));
    }

    [Fact]
    public void AC8_partial_tmp_is_ignored()
    {
        var game = SaveGames.Rich();
        Store().Save(1, game);
        File.WriteAllText(_folder.SlotFile(1, ".tmp"), "{\"saveVersion\": 1, \"id\": \"");

        var store = Store();

        SavedState.AssertSame(game, store.Load(1));
        Assert.Equal(SaveSlotState.Saved, store.List()[0].State);
    }

    [Fact]
    public void AC9_corrupt_file_falls_back_to_bak()
    {
        var store = Store();
        store.Save(1, SaveGames.Plain(gold: 111));
        store.Save(1, SaveGames.Plain(gold: 222));
        File.WriteAllText(_folder.SlotFile(1), "{ not json");

        Assert.Equal(111, Store().Load(1).Player.Gold.Value);
        var info = Store().List()[0];
        Assert.Equal(SaveSlotState.Saved, info.State);
        Assert.Equal(111, info.Gold);
    }

    [Fact]
    public void AC9_both_corrupt_is_unreadable()
    {
        var store = Store();
        store.Save(1, SaveGames.Plain(gold: 111));
        store.Save(1, SaveGames.Plain(gold: 222));
        File.WriteAllText(_folder.SlotFile(1), "{ not json");
        File.WriteAllText(_folder.SlotFile(1, ".bak"), "");

        AssertUnreadable();
    }

    [Fact]
    public void AC9_corrupt_file_with_no_bak_is_unreadable()
    {
        File.WriteAllText(_folder.SlotFile(1), "garbage");

        AssertUnreadable();
    }

    [Fact]
    public void AC10_list_reports_each_slot()
    {
        var game = SaveGames.Rich(gold: 4321, date: new GameDate(3, Season.Fall, 7));
        var store = Store();

        store.Save(2, game);
        var slots = store.List();

        Assert.Equal(
            [
                new SaveSlotInfo(1, SaveSlotState.Empty, null, null, null),
                new SaveSlotInfo(2, SaveSlotState.Saved, new GameDate(3, Season.Fall, 7), 4321, SaveGames.SavedAt),
                new SaveSlotInfo(3, SaveSlotState.Empty, null, null, null),
            ],
            slots);
    }

    [Fact]
    public void AC10_list_of_an_empty_folder_is_every_slot_empty()
    {
        var slots = Store().List();

        Assert.Equal(SaveSlots.Count, slots.Count);
        Assert.Equal([1, 2, 3], slots.Select(info => info.Slot));
        Assert.All(slots, info => Assert.Equal(new SaveSlotInfo(info.Slot, SaveSlotState.Empty, null, null, null), info));
    }

    [Fact]
    public void Save_creates_the_folder_when_missing()
    {
        var folder = Path.Combine(_folder.Path, "nested", "saves");
        var store = new JsonSaveStore(folder);
        Assert.All(store.List(), info => Assert.Equal(SaveSlotState.Empty, info.State));

        store.Save(1, SaveGames.Plain());

        Assert.True(File.Exists(Path.Combine(folder, "slot-1.json")));
    }

    [Fact]
    public void Loading_an_empty_slot_throws()
    {
        var thrown = Assert.Throws<SaveStoreException>(() => Store().Load(2));

        Assert.Equal(2, thrown.Slot);
    }

    public static TheoryData<string, JsonNode?> BadVersions => new()
    {
        { "above current", JsonValue.Create(3) },
        { "below 1", JsonValue.Create(0) },
        { "missing", null },
        { "not an integer", JsonValue.Create("one") },
    };

    [Theory]
    [MemberData(nameof(BadVersions))]
    public void AC11_a_file_with_a_bad_save_version_is_unreadable(string because, JsonNode? version)
    {
        Assert.NotNull(because);
        SaveThenEdit(document =>
        {
            document.Remove("saveVersion");
            if (version is not null)
            {
                document["saveVersion"] = version.DeepClone();
            }
        });

        AssertUnreadable();
    }

    [Fact]
    public void AC12_unknown_crop_type_is_unreadable()
    {
        SaveThenEdit(document => PlantedTile(document)["planting"]!["cropTypeId"] = "nope");

        AssertUnreadable();
    }

    [Fact]
    public void AC12_unknown_crop_type_in_the_shipping_bin_is_unreadable()
    {
        SaveThenEdit(document => document["shippingBin"]!["items"]![0]!["cropTypeId"] = "nope");

        AssertUnreadable();
    }

    [Fact]
    public void BR_T1_107_tiles_is_unreadable()
    {
        SaveThenEdit(document => Tiles(document).RemoveAt(Tiles(document).Count - 1));

        AssertUnreadable();
    }

    [Fact]
    public void BR_T1_a_duplicated_grid_position_is_unreadable()
    {
        SaveThenEdit(document => Tiles(document)[1]!["position"] = new JsonObject { ["x"] = 0, ["y"] = 0 });

        AssertUnreadable();
    }

    [Fact]
    public void BR_P7_zero_quantity_stack_is_unreadable()
    {
        // BR-K2: a filled Slot holds 1 to 99; Slot 2 is the first item stack after the two tools.
        SaveThenEdit(document => document["player"]!["backpack"]!["slots"]![2]!["quantity"] = 0);

        AssertUnreadable();
    }

    [Fact]
    public void BR_W1_charges_above_capacity_is_unreadable()
    {
        SaveThenEdit(document => document["player"]!["wateringCan"]!["charges"] = 99);

        AssertUnreadable();
    }

    [Fact]
    public void BR_W1_negative_charges_is_unreadable()
    {
        SaveThenEdit(document => document["player"]!["wateringCan"]!["charges"] = -1);

        AssertUnreadable();
    }

    [Fact]
    public void A_date_with_day_29_is_unreadable()
    {
        SaveThenEdit(document => document["date"]!["day"] = 29);

        AssertUnreadable();
    }

    [Fact]
    public void An_integer_enum_is_unreadable()
    {
        SaveThenEdit(document => document["weather"] = 1);

        AssertUnreadable();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void AC13_slot_out_of_range_throws_from_load(int slot)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Store().Load(slot));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void AC13_slot_out_of_range_throws_from_save(int slot)
    {
        var store = Store();

        Assert.Throws<ArgumentOutOfRangeException>(() => store.Save(slot, SaveGames.Plain()));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_folder.Path));
    }

    [Fact]
    public void AC3_load_keeps_the_saved_clock()
    {
        Store().Save(1, SaveGames.Rich(clock: 360));

        Assert.Equal(360, Store().Load(1).ClockMinutes);
    }
}
