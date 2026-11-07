using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The HUD hotbar: <see cref="GameSession.Hotbar"/>.</summary>
public class HotbarTests
{
    private static readonly GridPosition Target = new(5, 2);

    [Fact]
    public void AC4_New_game_hotbar_lists_tools_then_seeds_with_hoe_selected()
    {
        var session = TestGames.NewSession();

        var slots = session.Hotbar(HeldItem.Of(ToolId.Hoe));

        Assert.Equal(
            ["Hoe", "Watering can", "Emberroot seeds × 10", "Dusktuber seeds × 5"],
            slots.Select(slot => slot.Label));
        Assert.True(slots[0].Selected);
        Assert.Single(slots, slot => slot.Selected);
        Assert.Equal(session.HeldItems(), slots.Select(slot => slot.Item));
    }

    [Fact]
    public void AC17_Held_items_from_the_backpack_are_hoe_can_then_one_seed_type_each_in_crop_type_order()
    {
        var session = TestGames.SessionWithBackpack(TestGames.BackpackWith(
            (ItemRef.Seed(CropTypeId.Starcorn), 4),
            (ItemRef.Crop(CropTypeId.Moonmelon), 2),
            (ItemRef.Seed(CropTypeId.Dusktuber), 5),
            (ItemRef.Seed(CropTypeId.Emberroot), 10)));

        var items = session.HeldItems();

        Assert.Equal(
            [
                HeldItem.Of(ToolId.Hoe),
                HeldItem.Of(ToolId.WateringCan),
                HeldItem.SeedOf(CropTypeId.Emberroot),
                HeldItem.SeedOf(CropTypeId.Dusktuber),
                HeldItem.SeedOf(CropTypeId.Starcorn),
            ],
            items);
    }

    [Fact]
    public void AC17_New_game_hotbar_from_the_backpack_is_unchanged()
    {
        var session = TestGames.SessionWithBackpack(TestGames.StartingBackpack());

        var slots = session.Hotbar(HeldItem.Of(ToolId.Hoe));

        Assert.Equal(
            ["Hoe", "Watering can", "Emberroot seeds × 10", "Dusktuber seeds × 5"],
            slots.Select(slot => slot.Label));
    }

    [Fact]
    public void BR_P7_Seeds_split_across_slots_are_one_held_item_labelled_with_their_total()
    {
        var session = TestGames.SessionWithBackpack(TestGames.BackpackWith(
            (ItemRef.Seed(CropTypeId.Emberroot), 150),
            (ItemRef.Seed(CropTypeId.Dusktuber), 5)));

        var slots = session.Hotbar(HeldItem.SeedOf(CropTypeId.Emberroot));

        Assert.Equal(
            ["Hoe", "Watering can", "Emberroot seeds × 150", "Dusktuber seeds × 5"],
            slots.Select(slot => slot.Label));
        Assert.Single(session.HeldItems(), item => item == HeldItem.SeedOf(CropTypeId.Emberroot));
    }

    [Fact]
    public void AC17_HeldLabel_shows_the_seed_total_across_slots()
    {
        var session = TestGames.SessionWithBackpack(TestGames.BackpackWith((ItemRef.Seed(CropTypeId.Emberroot), 120)));

        Assert.Equal("Emberroot seeds × 120", session.HeldLabel(HeldItem.SeedOf(CropTypeId.Emberroot)));
        Assert.Equal("Hoe", session.HeldLabel(HeldItem.Of(ToolId.Hoe)));
        Assert.Equal("Watering can", session.HeldLabel(HeldItem.Of(ToolId.WateringCan)));
    }

    [Fact]
    public void AC5_Holding_seeds_selects_their_slot()
    {
        var session = TestGames.NewSession();

        var slots = session.Hotbar(HeldItem.SeedOf(CropTypeId.Emberroot));

        var selected = Assert.Single(slots, slot => slot.Selected);
        Assert.Equal("Emberroot seeds × 10", selected.Label);
        Assert.Equal(HeldItem.SeedOf(CropTypeId.Emberroot), selected.Item);
    }

    [Fact]
    public void AC5_Planting_the_last_seed_removes_its_slot_and_selects_the_hoe()
    {
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Seed(CropTypeId.Emberroot), 1);
        inventory.Add(ItemRef.Seed(CropTypeId.Dusktuber), 5);
        var session = new GameSession(
            TestGames.WithTiles(150, inventory, TestGames.TilledWith(Target, null)),
            TestGames.Shipped);

        Assert.Equal(ActionResult.Ok, session.Plant(Target, CropTypeId.Emberroot));
        Assert.Equal(0, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));

        var slots = session.Hotbar(HeldItem.SeedOf(CropTypeId.Emberroot));

        Assert.DoesNotContain(slots, slot => slot.Item == HeldItem.SeedOf(CropTypeId.Emberroot));
        var selected = Assert.Single(slots, slot => slot.Selected);
        Assert.Equal(HeldItem.Of(ToolId.Hoe), selected.Item);
        Assert.Equal(["Hoe", "Watering can", "Dusktuber seeds × 5"], slots.Select(slot => slot.Label));
    }

    [Fact]
    public void BR_P7_One_slot_per_seed_stack()
    {
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Seed(CropTypeId.Starcorn), 4);
        inventory.Add(ItemRef.Seed(CropTypeId.Sunbell), 3);
        inventory.Add(ItemRef.Seed(CropTypeId.Moonmelon), 2);
        inventory.Add(ItemRef.Seed(CropTypeId.Dusktuber), 5);
        inventory.Add(ItemRef.Seed(CropTypeId.Emberroot), 10);
        inventory.Add(ItemRef.Crop(CropTypeId.Emberroot), 7);
        inventory.Add(ItemRef.Crop(CropTypeId.Moonmelon), 1);
        var session = new GameSession(TestGames.WithEnergy(150, inventory), TestGames.Shipped);

        var slots = session.Hotbar(HeldItem.Of(ToolId.WateringCan));

        Assert.Equal(
            [
                HeldItem.Of(ToolId.Hoe),
                HeldItem.Of(ToolId.WateringCan),
                HeldItem.SeedOf(CropTypeId.Emberroot),
                HeldItem.SeedOf(CropTypeId.Dusktuber),
                HeldItem.SeedOf(CropTypeId.Moonmelon),
                HeldItem.SeedOf(CropTypeId.Sunbell),
                HeldItem.SeedOf(CropTypeId.Starcorn),
            ],
            slots.Select(slot => slot.Item));
        Assert.Equal(
            [
                "Hoe",
                "Watering can",
                "Emberroot seeds × 10",
                "Dusktuber seeds × 5",
                "Moonmelon seeds × 2",
                "Sunbell seeds × 3",
                "Starcorn seeds × 4",
            ],
            slots.Select(slot => slot.Label));
        var selected = Assert.Single(slots, slot => slot.Selected);
        Assert.Equal(HeldItem.Of(ToolId.WateringCan), selected.Item);
    }

    [Fact]
    public void Selected_item_not_in_the_cycle_falls_back_to_the_hoe()
    {
        var session = TestGames.NewSession();

        var slots = session.Hotbar(HeldItem.SeedOf(CropTypeId.Moonmelon));

        var selected = Assert.Single(slots, slot => slot.Selected);
        Assert.Equal(HeldItem.Of(ToolId.Hoe), selected.Item);
        Assert.Same(slots[0], selected);
    }

    [Fact]
    public void Null_held_item_throws()
    {
        var session = TestGames.NewSession();

        Assert.Throws<ArgumentNullException>(() => session.Hotbar(null!));
    }

    // ---- #129: the hotbar is one 6-slot row of the Backpack ----

    private static readonly string[] DistinctRow0 =
        ["Hoe", "Watering can", "Emberroot seeds × 1", "Dusktuber seeds × 2", "Moonmelon seeds × 3", "Sunbell seeds × 4"];

    private static readonly string[] DistinctRow1 =
        ["Starcorn seeds × 5", "Emberroot × 6", "Dusktuber × 7", "Moonmelon × 8", "Sunbell × 9", "Starcorn × 10"];

    [Fact]
    public void AC1_With_12_slots_the_hotbar_shows_slots_0_to_5()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        var slots = session.Hotbar(0);

        Assert.Equal(DistinctRow0, slots.Select(slot => slot.Label));
        Assert.Equal(
            session.Game.Player.Backpack.Slots.Take(6).Select(slot => (slot.Item, slot.Quantity)),
            slots.Select(slot => (slot.SlotItem, slot.Quantity)));
    }

    [Fact]
    public void AC1_After_CycleHotbar_the_hotbar_shows_slots_6_to_11()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        session.CycleHotbar();
        var slots = session.Hotbar(0);

        Assert.Equal(DistinctRow1, slots.Select(slot => slot.Label));
        Assert.Equal(
            session.Game.Player.Backpack.Slots.Skip(6).Select(slot => (slot.Item, slot.Quantity)),
            slots.Select(slot => (slot.SlotItem, slot.Quantity)));
    }

    [Fact]
    public void AC1_A_second_CycleHotbar_shows_slots_0_to_5_again()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        session.CycleHotbar();
        session.CycleHotbar();

        Assert.Equal(0, session.Game.Player.Backpack.HotbarRow);
        Assert.Equal(DistinctRow0, session.Hotbar(0).Select(slot => slot.Label));
    }

    [Fact]
    public void AC1_After_CycleHotbar_HeldItemAt_reads_the_same_position_of_the_next_row()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());
        Assert.Equal(HeldItem.Of(ToolId.Hoe), session.HeldItemAt(0));

        session.CycleHotbar();

        Assert.Equal(HeldItem.SeedOf(CropTypeId.Starcorn), session.HeldItemAt(0));
    }

    [Fact]
    public void AC2_CycleHotbar_changes_only_the_hotbar_row()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.StartingBackpack(),
            TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: true));
        var game = session.Game;
        var before = (TestGames.FullSnapshot(game), game.Player.Gold, game.ClockMinutes, game.Date,
            game.Player.WateringCan.Charges);

        session.CycleHotbar();

        Assert.Equal(1, game.Player.Backpack.HotbarRow);
        Assert.Equal(
            before,
            (TestGames.FullSnapshot(game), game.Player.Gold, game.ClockMinutes, game.Date, game.Player.WateringCan.Charges));
    }

    [Fact]
    public void AC2_CycleHotbar_raises_InventoryChanged_once_and_no_other_change_event()
    {
        var session = TestGames.NewSession();
        var inventory = 0;
        var other = 0;
        session.InventoryChanged += () => inventory++;
        session.PlayerChanged += () => other++;
        session.TileChanged += _ => other++;
        session.ClockChanged += () => other++;

        session.CycleHotbar();

        Assert.Equal(1, inventory);
        Assert.Equal(0, other);
    }

    [Fact]
    public void AC3_The_hotbar_always_has_six_entries_and_an_empty_slot_is_an_empty_entry()
    {
        var session = TestGames.NewSession();

        var slots = session.Hotbar(0);

        Assert.Equal(6, slots.Count);
        Assert.Equal(["Hoe", "Watering can", "Emberroot seeds × 10", "Dusktuber seeds × 5", "", ""], slots.Select(slot => slot.Label));
        Assert.All(slots.Skip(4), slot =>
        {
            Assert.Null(slot.Item);
            Assert.Null(slot.SlotItem);
            Assert.Equal(0, slot.Quantity);
        });
    }

    [Fact]
    public void AC3_An_empty_row_is_six_empty_entries()
    {
        var session = TestGames.NewSession();
        session.CycleHotbar();

        var slots = session.Hotbar(0);

        Assert.Equal(6, slots.Count);
        Assert.All(slots, slot =>
        {
            Assert.Null(slot.Item);
            Assert.Equal("", slot.Label);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void AC3_Only_the_held_position_is_selected(int position)
    {
        var session = TestGames.NewSession();

        var slots = session.Hotbar(position);

        Assert.Equal(Enumerable.Range(0, 6).Select(index => index == position), slots.Select(slot => slot.Selected));
    }

    [Theory]
    [InlineData(5, 1, 0)]
    [InlineData(0, -1, 5)]
    [InlineData(2, 1, 3)]
    [InlineData(3, -1, 2)]
    public void AC4_Stepping_the_held_position_wraps_within_the_row(int position, int delta, int expected) =>
        Assert.Equal(expected, HotbarPosition.Step(position, delta));

    [Fact]
    public void AC8_A_seed_slot_label_shows_that_slots_quantity_not_the_total()
    {
        var session = TestGames.SessionWithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(2, ItemRef.Seed(CropTypeId.Emberroot), 12, null),
            new BackpackSlot(3, ItemRef.Seed(CropTypeId.Emberroot), 30, null)));

        var slots = session.Hotbar(0);

        Assert.Equal("Emberroot seeds × 12", slots[2].Label);
        Assert.Equal("Emberroot seeds × 30", slots[3].Label);
        Assert.Equal(HeldItem.SeedOf(CropTypeId.Emberroot), slots[2].Item);
        Assert.Equal(12, slots[2].Quantity);
    }

    [Fact]
    public void AC8_A_crop_slot_is_labelled_crop_type_times_quantity_and_holds_no_held_item()
    {
        var session = TestGames.SessionWithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(4, ItemRef.Crop(CropTypeId.Moonmelon), 7, null)));

        var slot = session.Hotbar(0)[4];

        Assert.Equal($"{CropLabels.For(CropTypeId.Moonmelon)} × 7", slot.Label);
        Assert.Null(slot.Item);
        Assert.Equal(ItemRef.Crop(CropTypeId.Moonmelon), slot.SlotItem);
        Assert.Equal(7, slot.Quantity);
    }

    [Fact]
    public void AC8_Tool_slots_are_labelled_Hoe_and_Watering_can()
    {
        var session = TestGames.NewSession();

        var slots = session.Hotbar(0);

        Assert.Equal(("Hoe", HeldItem.Of(ToolId.Hoe)), (slots[0].Label, slots[0].Item));
        Assert.Equal(("Watering can", HeldItem.Of(ToolId.WateringCan)), (slots[1].Label, slots[1].Item));
    }

    [Fact]
    public void HeldItemAt_is_the_tool_or_seed_in_the_held_slot()
    {
        var session = TestGames.NewSession();

        Assert.Equal(HeldItem.Of(ToolId.Hoe), session.HeldItemAt(0));
        Assert.Equal(HeldItem.Of(ToolId.WateringCan), session.HeldItemAt(1));
        Assert.Equal(HeldItem.SeedOf(CropTypeId.Emberroot), session.HeldItemAt(2));
        Assert.Equal(HeldItem.SeedOf(CropTypeId.Dusktuber), session.HeldItemAt(3));
    }

    [Fact]
    public void HeldItemAt_an_empty_or_crop_slot_is_null()
    {
        var session = TestGames.SessionWithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(2, ItemRef.Crop(CropTypeId.Emberroot), 3, null)));

        Assert.Null(session.HeldItemAt(2));
        Assert.Null(session.HeldItemAt(5));
    }
}
