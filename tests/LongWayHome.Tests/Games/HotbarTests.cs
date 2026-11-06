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
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 1);
        inventory.Add(ItemKind.Seed, CropTypeId.Dusktuber, 5);
        var session = new GameSession(
            TestGames.WithTiles(150, inventory, TestGames.TilledWith(Target, null)),
            TestGames.Shipped);

        Assert.Equal(ActionResult.Ok, session.Plant(Target, CropTypeId.Emberroot));
        Assert.Equal(0, session.Game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));

        var slots = session.Hotbar(HeldItem.SeedOf(CropTypeId.Emberroot));

        Assert.DoesNotContain(slots, slot => slot.Item == HeldItem.SeedOf(CropTypeId.Emberroot));
        var selected = Assert.Single(slots, slot => slot.Selected);
        Assert.Equal(HeldItem.Of(ToolId.Hoe), selected.Item);
        Assert.Equal(["Hoe", "Watering can", "Dusktuber seeds × 5"], slots.Select(slot => slot.Label));
    }

    [Fact]
    public void BR_P7_One_slot_per_seed_stack()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Starcorn, 4);
        inventory.Add(ItemKind.Seed, CropTypeId.Sunbell, 3);
        inventory.Add(ItemKind.Seed, CropTypeId.Moonmelon, 2);
        inventory.Add(ItemKind.Seed, CropTypeId.Dusktuber, 5);
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 10);
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 7);
        inventory.Add(ItemKind.Crop, CropTypeId.Moonmelon, 1);
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
}
