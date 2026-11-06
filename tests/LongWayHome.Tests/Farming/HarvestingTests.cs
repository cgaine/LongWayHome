using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Farming;

/// <summary>Harvesting a ripe Planting: BR-T5, BR-T6, BR-T8, BR-P2, BR-P7 (#24).</summary>
public class HarvestingTests
{
    private static readonly GridPosition Target = new(6, 3);

    private static readonly CropType Emberroot = TestGames.Crop(CropTypeId.Emberroot);

    [Fact]
    public void AC5_harvesting_a_ripe_planting_removes_it_adds_a_new_crop_stack_of_1_and_keeps_tile_and_energy()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        var result = game.Harvest(Target, Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Null(game.FarmPlot[Target].Planting);
        Assert.Contains(new InventoryStack(ItemKind.Crop, CropTypeId.Emberroot, 1), game.Player.Inventory.Stacks);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.Equal(150, game.Player.Energy);
    }

    [Fact]
    public void AC5_harvesting_adds_one_to_an_existing_crop_stack()
    {
        var inventory = TestGames.StartingSeeds();
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 4);
        var game = TestGames.WithTiles(150, inventory, TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        var result = game.Harvest(Target, Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(5, game.Player.Inventory.Quantity(ItemKind.Crop, CropTypeId.Emberroot));
        Assert.Single(game.Player.Inventory.Stacks, stack => stack.Kind == ItemKind.Crop);
        Assert.Equal(10, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Moonmelon)]
    public void AC5_harvesting_adds_one_crop_of_the_planting_type(CropTypeId crop)
    {
        var game = TestGames.WithTiles(150, new Inventory(), TestGames.TilledWith(Target, TestGames.Ripe(crop)));

        var result = game.Harvest(Target, TestGames.Crop(crop));

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal([new InventoryStack(ItemKind.Crop, crop, 1)], game.Player.Inventory.Stacks);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AC6_harvesting_leaves_watered_as_it_was(bool watered)
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot), watered));

        var result = game.Harvest(Target, Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(watered, game.FarmPlot[Target].Watered);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AC7_harvesting_a_growing_planting_returns_NotRipe_and_changes_nothing(int daysGrown)
    {
        var planting = TestGames.PlantingOf(CropTypeId.Emberroot, daysGrown);
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, planting, watered: true));
        var before = TestGames.FullSnapshot(game);

        var result = game.Harvest(Target, Emberroot);

        Assert.Equal(ActionResult.Rejected(Rejection.NotRipe), result);
        Assert.Equal(planting, game.FarmPlot[Target].Planting);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Theory]
    [InlineData(TileState.Grass)]
    [InlineData(TileState.Tilled)]
    public void AC7_harvesting_a_tile_with_no_planting_returns_NoPlanting_and_changes_nothing(TileState state)
    {
        var game = TestGames.WithTiles(150, null, new Tile(Target, state, false, null));
        var before = TestGames.FullSnapshot(game);

        var result = game.Harvest(Target, Emberroot);

        Assert.Equal(ActionResult.Rejected(Rejection.NoPlanting), result);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void BR_T6_harvest_leaves_tile_tilled_and_watered_state_unchanged()
    {
        var tile = TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot), watered: true);

        var result = tile.Harvest(Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Null(tile.Planting);
        Assert.Equal(TileState.Tilled, tile.State);
        Assert.True(tile.Watered);
    }

    [Fact]
    public void BR_T6_tile_harvest_checks_NoPlanting_then_NotRipe()
    {
        var empty = TestGames.TilledWith(Target, null);
        var growing = TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 2));

        Assert.Equal(ActionResult.Rejected(Rejection.NoPlanting), empty.Harvest(Emberroot));
        Assert.Equal(ActionResult.Rejected(Rejection.NotRipe), growing.Harvest(Emberroot));
        Assert.NotNull(growing.Planting);
    }

    [Fact]
    public void BR_T6_harvest_rejection_matches_harvest_and_changes_nothing()
    {
        var growingAt = new GridPosition(0, 0);
        var ripeAt = new GridPosition(1, 0);
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(growingAt, TestGames.PlantingOf(CropTypeId.Emberroot, 1)),
            TestGames.TilledWith(ripeAt, TestGames.Ripe(CropTypeId.Emberroot)));
        var before = TestGames.FullSnapshot(game);

        Assert.Equal(Rejection.NoPlanting, game.HarvestRejection(Target, Emberroot));
        Assert.Equal(Rejection.NotRipe, game.HarvestRejection(growingAt, Emberroot));
        Assert.Null(game.HarvestRejection(ripeAt, Emberroot));
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void BR_T8_a_harvested_tile_stays_tilled_and_can_be_planted_again()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        game.Harvest(Target, Emberroot);
        var till = game.Till(Target, 2);
        var replant = game.Plant(Target, CropTypeId.Emberroot);

        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), till);
        Assert.Equal(ActionResult.Ok, replant);
    }

    [Fact]
    public void BR_T5_a_refused_harvest_never_removes_a_growing_planting()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Dusktuber, 4);
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, planting));

        for (var i = 0; i < 5; i++)
        {
            game.Harvest(Target, TestGames.Crop(CropTypeId.Dusktuber));
        }

        Assert.Equal(planting, game.FarmPlot[Target].Planting);
    }

    [Fact]
    public void BR_P2_harvesting_costs_no_energy_even_at_0_energy()
    {
        var game = TestGames.WithTiles(0, null, TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        var result = game.Harvest(Target, Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(0, game.Player.Energy);
        Assert.Equal(1, game.Player.Inventory.Quantity(ItemKind.Crop, CropTypeId.Emberroot));
    }

    [Fact]
    public void BR_P7_harvested_crops_of_one_type_merge_into_one_stack()
    {
        var first = new GridPosition(0, 0);
        var second = new GridPosition(1, 0);
        var game = TestGames.WithTiles(
            150,
            new Inventory(),
            TestGames.TilledWith(first, TestGames.Ripe(CropTypeId.Emberroot)),
            TestGames.TilledWith(second, TestGames.Ripe(CropTypeId.Emberroot)));

        game.Harvest(first, Emberroot);
        game.Harvest(second, Emberroot);

        Assert.Equal([new InventoryStack(ItemKind.Crop, CropTypeId.Emberroot, 2)], game.Player.Inventory.Stacks);
    }

    [Fact]
    public void Tile_harvest_with_a_different_crop_type_throws_and_keeps_the_planting()
    {
        var ripe = TestGames.Ripe(CropTypeId.Emberroot);
        var tile = TestGames.TilledWith(Target, ripe);

        Assert.Throws<ArgumentException>(() => tile.Harvest(TestGames.Crop(CropTypeId.Dusktuber)));
        Assert.Equal(ripe, tile.Planting);
    }

    [Fact]
    public void Game_harvest_with_a_different_crop_type_throws_and_changes_nothing()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));
        var before = TestGames.FullSnapshot(game);

        Assert.Throws<ArgumentException>(() => game.Harvest(Target, TestGames.Crop(CropTypeId.Dusktuber)));
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void A_planting_past_its_grow_days_is_not_ripe_and_is_not_harvested()
    {
        // AC8: Ripe exactly when DaysGrown == growDays.
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, Emberroot.GrowDays + 1)));

        Assert.Equal(ActionResult.Rejected(Rejection.NotRipe), game.Harvest(Target, Emberroot));
        Assert.NotNull(game.FarmPlot[Target].Planting);
    }
}
