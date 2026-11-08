using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The Backpack screen's session API (#131): <see cref="GameSession.BackpackView"/> and <see cref="GameSession.MoveSlot"/>.</summary>
public class BackpackViewTests
{
    private static readonly GridPosition Target = new(5, 2);

    // ---- AC3: one entry per Slot with its item, quantity, label and icon ----

    [Fact]
    public void AC3_The_view_has_one_entry_per_slot_in_index_order()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        var view = session.BackpackView();

        Assert.Equal(12, view.Capacity);
        Assert.Equal(Enumerable.Range(0, 12), view.Slots.Select(slot => slot.Index));
        Assert.Equal(
            session.Game.Player.Backpack.Slots.Select(slot => (slot.Item, slot.Quantity, slot.ToolTier)),
            view.Slots.Select(slot => (slot.Item, slot.Quantity, slot.ToolTier)));
    }

    [Fact]
    public void AC3_The_view_labels_each_slot_as_the_hotbar_does()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        var view = session.BackpackView();

        Assert.Equal(
            [
                "Hoe", "Watering can", "Emberroot seeds × 1", "Dusktuber seeds × 2", "Moonmelon seeds × 3",
                "Sunbell seeds × 4", "Starcorn seeds × 5", "Emberroot × 6", "Dusktuber × 7", "Moonmelon × 8",
                "Sunbell × 9", "Starcorn × 10",
            ],
            view.Slots.Select(slot => slot.Label));
    }

    [Fact]
    public void AC3_Seed_slots_have_the_seed_icon_crop_slots_the_crop_icon_and_tools_none()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        var view = session.BackpackView();

        Assert.Null(view.Slots[0].IconPath);
        Assert.Null(view.Slots[1].IconPath);
        Assert.Equal(ItemIcons.SeedIcon(CropTypeId.Emberroot), view.Slots[2].IconPath);
        Assert.Equal(ItemIcons.SeedIcon(CropTypeId.Starcorn), view.Slots[6].IconPath);
        Assert.Equal(ItemIcons.CropIcon(CropTypeId.Emberroot), view.Slots[7].IconPath);
        Assert.Equal(ItemIcons.CropIcon(CropTypeId.Starcorn), view.Slots[11].IconPath);
    }

    [Fact]
    public void AC3_Empty_slots_show_empty_with_no_icon()
    {
        var session = TestGames.SessionWithBackpack(TestGames.StartingBackpack());

        var view = session.BackpackView();

        Assert.All(view.Slots.Skip(4), slot =>
        {
            Assert.Null(slot.Item);
            Assert.Equal(0, slot.Quantity);
            Assert.Null(slot.ToolTier);
            Assert.Equal("", slot.Label);
            Assert.Null(slot.IconPath);
        });
    }

    [Fact]
    public void AC3_Tool_slots_carry_their_tier()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.SetTier(ToolId.WateringCan, ToolTier.Rosk);
        var session = TestGames.SessionWithBackpack(backpack);

        var view = session.BackpackView();

        Assert.Equal(ToolTier.Basic, view.Slots[0].ToolTier);
        Assert.Equal(ToolTier.Rosk, view.Slots[1].ToolTier);
    }

    [Fact]
    public void AC3_A_24_slot_backpack_has_24_entries()
    {
        var backpack = TestGames.ToolsOnly();
        backpack.RaiseCapacity(24);
        var session = TestGames.SessionWithBackpack(backpack);

        var view = session.BackpackView();

        Assert.Equal(24, view.Capacity);
        Assert.Equal(Enumerable.Range(0, 24), view.Slots.Select(slot => slot.Index));
    }

    // ---- AC2 / BR-K5: the hotbar row ----

    [Fact]
    public void AC2_The_view_reports_hotbar_row_0_on_a_new_backpack()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        Assert.Equal(0, session.BackpackView().HotbarRow);
    }

    [Fact]
    public void BR_K5_After_CycleHotbar_the_view_reports_the_new_hotbar_row()
    {
        var session = TestGames.SessionWithBackpack(TestGames.DistinctBackpack());

        session.CycleHotbar();
        var row1 = session.BackpackView().HotbarRow;
        session.CycleHotbar();
        var row0 = session.BackpackView().HotbarRow;

        Assert.Equal(1, row1);
        Assert.Equal(0, row0);
    }

    // ---- AC11: MoveSlot ----

    [Fact]
    public void AC11_MoveSlot_raises_InventoryChanged_once_when_something_moved()
    {
        var session = TestGames.SessionWithBackpack(TestGames.StartingBackpack());
        var inventory = 0;
        var other = 0;
        session.InventoryChanged += () => inventory++;
        session.PlayerChanged += () => other++;
        session.TileChanged += _ => other++;
        session.ClockChanged += () => other++;
        session.DroppedItemsChanged += () => other++;

        var moved = session.MoveSlot(0, 7);

        Assert.True(moved);
        Assert.Equal(1, inventory);
        Assert.Equal(0, other);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(8, 3)]
    public void AC11_MoveSlot_that_moves_nothing_returns_false_and_raises_nothing(int from, int to)
    {
        var session = TestGames.SessionWithBackpack(TestGames.StartingBackpack());
        var before = TestGames.Describe(session.Game.Player.Backpack);
        var events = 0;
        session.InventoryChanged += () => events++;

        var moved = session.MoveSlot(from, to);

        Assert.False(moved);
        Assert.Equal(0, events);
        Assert.Equal(before, TestGames.Describe(session.Game.Player.Backpack));
    }

    [Fact]
    public void AC11_MoveSlot_costs_no_energy_and_changes_nothing_but_the_backpack()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.StartingBackpack(),
            TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: true));
        var game = session.Game;
        var before = (game.Player.Energy, game.Player.Gold, game.ClockMinutes, game.Date, game.Weather,
            game.Player.WateringCan.Charges, game.Player.Backpack.HotbarRow, game.DroppedItems.Count);
        var tiles = TestGames.Snapshot(game).Tiles;

        Assert.True(session.MoveSlot(2, 9));

        Assert.Equal(
            before,
            (game.Player.Energy, game.Player.Gold, game.ClockMinutes, game.Date, game.Weather,
                game.Player.WateringCan.Charges, game.Player.Backpack.HotbarRow, game.DroppedItems.Count));
        Assert.Equal(tiles, TestGames.Snapshot(game).Tiles);
        Assert.Equal("9:Seed:Emberroot:10:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[9]));
    }

    [Fact]
    public void AC11_After_moving_the_hoe_into_row_1_the_hotbar_shows_the_new_order()
    {
        var session = TestGames.SessionWithBackpack(TestGames.StartingBackpack());

        Assert.True(session.MoveSlot(0, 8));

        Assert.Equal(
            ["", "Watering can", "Emberroot seeds × 10", "Dusktuber seeds × 5", "", ""],
            session.Hotbar(0).Select(slot => slot.Label));
        Assert.Null(session.HeldItemAt(0));
        session.CycleHotbar();
        Assert.Equal(HeldItem.Of(ToolId.Hoe), session.HeldItemAt(2));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 12)]
    [InlineData(12, 0)]
    [InlineData(0, -1)]
    public void AC11_MoveSlot_out_of_range_throws(int from, int to)
    {
        var session = TestGames.SessionWithBackpack(TestGames.StartingBackpack());

        Assert.Throws<ArgumentOutOfRangeException>(() => session.MoveSlot(from, to));
    }
}
