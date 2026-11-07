using System.Text.Json.Nodes;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// The save 1 → 2 step (#128): a Phase 1 save, written as literal version 1 JSON, loads through
/// <see cref="JsonSaveStore"/> with its tools and stacks packed into Backpack Slots (BR-K1, BR-K2).
/// </summary>
public sealed class SaveV1ToV2Tests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private GameState Load(JsonObject document)
    {
        _folder.WriteSlot(1, document);
        return new JsonSaveStore(_folder.Path).Load(1);
    }

    private static string Empty(int index) => $"{index}:-:-:0:-";

    private static IEnumerable<string> Slots(GameState game) =>
        game.Player.Backpack.Slots.Select(TestGames.DescribeSlot);

    [Fact]
    public void Phase_1_save_keeps_loading_its_gold_energy_and_can()
    {
        var game = Load(SaveDocuments.V1(
            new JsonArray(SaveDocuments.Stack("seed", "emberroot", 10)), gold: 321, capacity: 40, charges: 13, upgraded: true));

        Assert.Equal(new Gold(321), game.Player.Gold);
        Assert.Equal(77, game.Player.Energy);
        Assert.Equal(40, game.Player.WateringCan.Capacity);
        Assert.Equal(13, game.Player.WateringCan.Charges);
        Assert.True(game.Player.WateringCan.Upgraded);
    }

    [Fact]
    public void AC13_V1_save_loads_with_hoe_in_slot_0_basic_can_in_slot_1_then_its_stacks_in_saved_order()
    {
        var game = Load(SaveDocuments.V1(new JsonArray(
            SaveDocuments.Stack("crop", "emberroot", 7),
            SaveDocuments.Stack("seed", "sunbell", 4),
            SaveDocuments.Stack("seed", "starcorn", 2))));

        var backpack = game.Player.Backpack;
        Assert.Equal(12, backpack.Capacity);
        Assert.Equal(0, backpack.HotbarRow);
        Assert.Equal(
            [
                "0:Tool:Hoe:1:Basic",
                "1:Tool:WateringCan:1:Basic",
                "2:Crop:Emberroot:7:-",
                "3:Seed:Sunbell:4:-",
                "4:Seed:Starcorn:2:-",
                .. Enumerable.Range(5, 7).Select(Empty),
            ],
            Slots(game));
    }

    [Fact]
    public void AC13_V1_save_with_an_empty_inventory_loads_with_only_the_tools()
    {
        var game = Load(SaveDocuments.V1([]));

        Assert.Equal(
            ["0:Tool:Hoe:1:Basic", "1:Tool:WateringCan:1:Basic", .. Enumerable.Range(2, 10).Select(Empty)],
            Slots(game));
    }

    [Fact]
    public void AC13_The_step_replaces_player_tools_and_inventory_with_player_backpack_at_version_2()
    {
        var migrated = SaveMigrator.Default.Migrate(
            SaveDocuments.V1(new JsonArray(SaveDocuments.Stack("seed", "emberroot", 10))));

        Assert.Equal(2, migrated["saveVersion"]!.GetValue<int>());
        var player = migrated["player"]!.AsObject();
        Assert.False(player.ContainsKey("tools"));
        Assert.False(player.ContainsKey("inventory"));
        Assert.True(player.ContainsKey("wateringCan"));
        var backpack = player["backpack"]!.AsObject();
        Assert.Equal(12, backpack["capacity"]!.GetValue<int>());
        Assert.Equal(0, backpack["hotbarRow"]!.GetValue<int>());
        var slots = backpack["slots"]!.AsArray();
        Assert.Equal(12, slots.Count);
        Assert.Equal("tool", slots[0]!["item"]!["kind"]!.GetValue<string>());
        Assert.Equal("hoe", slots[0]!["item"]!["id"]!.GetValue<string>());
        Assert.Equal("basic", slots[0]!["toolTier"]!.GetValue<string>());
        Assert.Equal("watering_can", slots[1]!["item"]!["id"]!.GetValue<string>());
        Assert.Equal("seed", slots[2]!["item"]!["kind"]!.GetValue<string>());
        Assert.Equal("emberroot", slots[2]!["item"]!["id"]!.GetValue<string>());
        Assert.Equal(10, slots[2]!["quantity"]!.GetValue<int>());
    }

    [Fact]
    public void AC14_V1_save_with_an_upgraded_can_loads_with_the_can_at_rosk_keeping_capacity_40_and_charges()
    {
        var game = Load(SaveDocuments.V1([], capacity: 40, charges: 13, upgraded: true));

        Assert.Equal("1:Tool:WateringCan:1:Rosk", TestGames.DescribeSlot(game.Player.Backpack.Slots[1]));
        Assert.Equal(ToolTier.Basic, game.Player.Backpack.TierOf(ToolId.Hoe));
        Assert.Equal(40, game.Player.WateringCan.Capacity);
        Assert.Equal(13, game.Player.WateringCan.Charges);
        Assert.True(game.Player.WateringCan.Upgraded);
    }

    [Fact]
    public void AC15_V1_stack_above_99_loads_split_into_slots_of_at_most_99()
    {
        var game = Load(SaveDocuments.V1(new JsonArray(
            SaveDocuments.Stack("seed", "emberroot", 250),
            SaveDocuments.Stack("crop", "dusktuber", 3))));

        Assert.Equal(12, game.Player.Backpack.Capacity);
        Assert.Equal(
            [
                "0:Tool:Hoe:1:Basic",
                "1:Tool:WateringCan:1:Basic",
                "2:Seed:Emberroot:99:-",
                "3:Seed:Emberroot:99:-",
                "4:Seed:Emberroot:52:-",
                "5:Crop:Dusktuber:3:-",
                .. Enumerable.Range(6, 6).Select(Empty),
            ],
            Slots(game));
    }

    [Fact]
    public void AC15_V1_save_needing_13_slots_loads_with_capacity_24()
    {
        var game = Load(SaveDocuments.V1(new JsonArray(SaveDocuments.Stack("crop", "emberroot", 11 * 99))));

        var backpack = game.Player.Backpack;
        Assert.Equal(24, backpack.Capacity);
        Assert.Equal(Enumerable.Range(0, 24), backpack.Slots.Select(slot => slot.Index));
        Assert.All(backpack.Slots.Skip(2).Take(11), slot => Assert.Equal(99, slot.Quantity));
        Assert.Equal(Enumerable.Range(13, 11).Select(Empty), backpack.Slots.Skip(13).Select(TestGames.DescribeSlot));
        Assert.Equal(11 * 99, backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }

    [Fact]
    public void AC15_V1_save_needing_more_than_24_slots_loads_with_capacity_36()
    {
        var game = Load(SaveDocuments.V1(new JsonArray(
            SaveDocuments.Stack("crop", "emberroot", 20 * 99),
            SaveDocuments.Stack("seed", "dusktuber", 3 * 99))));

        var backpack = game.Player.Backpack;
        Assert.Equal(36, backpack.Capacity);
        Assert.Equal(20 * 99, backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
        Assert.Equal(3 * 99, backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
        Assert.Equal(25, backpack.Slots.Count(slot => slot.Item is not null));
    }

    [Fact]
    public void AC15_V1_save_that_overflows_36_slots_keeps_them_full_and_discards_the_excess()
    {
        var game = Load(SaveDocuments.V1(new JsonArray(SaveDocuments.Stack("crop", "emberroot", 40 * 99))));

        var backpack = game.Player.Backpack;
        Assert.Equal(36, backpack.Capacity);
        Assert.Equal(34 * 99, backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
        Assert.Equal(ToolTier.Basic, backpack.TierOf(ToolId.Hoe));
        Assert.Equal(ToolTier.Basic, backpack.TierOf(ToolId.WateringCan));
    }
}
