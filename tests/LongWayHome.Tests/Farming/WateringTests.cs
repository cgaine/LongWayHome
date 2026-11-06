using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Farming;

/// <summary>Watering a Tile and refilling the can: BR-T9, BR-W1, BR-W2, BR-W3, BR-P1, BR-P2 (#25).</summary>
public class WateringTests
{
    private const int ToolEnergyCost = 2;

    private static readonly GridPosition Target = new(4, 5);

    [Fact]
    public void AC1_watering_a_dry_tilled_tile_in_a_new_game_waters_it_and_leaves_19_charges_and_148_energy()
    {
        var game = TestGames.NewSession().Game;
        Assert.Equal(20, game.Player.WateringCan.Charges);
        Assert.Equal(150, game.Player.Energy);
        game.FarmPlot[Target].Till();

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Ok, result);
        Assert.True(game.FarmPlot[Target].Watered);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.Equal(19, game.Player.WateringCan.Charges);
        Assert.Equal(20, game.Player.WateringCan.Capacity);
        Assert.Equal(148, game.Player.Energy);
    }

    [Fact]
    public void AC2_watering_an_already_watered_tile_returns_AlreadyWatered_and_changes_nothing()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, null, watered: true));
        var before = TestGames.WaterSnapshot(game);

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyWatered), result);
        Assert.Equal(20, game.Player.WateringCan.Charges);
        Assert.Equal(150, game.Player.Energy);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void AC3_watering_a_grass_tile_returns_NotTilled_and_changes_nothing()
    {
        var game = TestGames.WithEnergy(150);
        var before = TestGames.WaterSnapshot(game);

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.NotTilled), result);
        Assert.Equal(TileState.Grass, game.FarmPlot[Target].State);
        Assert.False(game.FarmPlot[Target].Watered);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void AC4_with_0_charges_watering_returns_CanEmpty_and_changes_nothing()
    {
        var game = TestGames.WithCan(150, TestGames.CanWith(0), null, TestGames.TilledWith(Target, null));
        var before = TestGames.WaterSnapshot(game);

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.CanEmpty), result);
        Assert.False(game.FarmPlot[Target].Watered);
        Assert.Equal(0, game.Player.WateringCan.Charges);
        Assert.Equal(150, game.Player.Energy);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void AC4_repeated_watering_with_an_empty_can_never_takes_charges_below_0()
    {
        var tiles = Enumerable.Range(0, 5).Select(x => TestGames.TilledWith(new GridPosition(x, 0), null)).ToArray();
        var game = TestGames.WithCan(150, TestGames.CanWith(0), null, tiles);

        var results = tiles.Select(tile => game.Water(tile.Position, ToolEnergyCost)).ToList();

        Assert.All(results, result => Assert.Equal(ActionResult.Rejected(Rejection.CanEmpty), result));
        Assert.Equal(0, game.Player.WateringCan.Charges);
        Assert.All(tiles, tile => Assert.False(game.FarmPlot[tile.Position].Watered));
    }

    [Fact]
    public void AC5_with_1_energy_and_charges_left_watering_returns_TooTired_and_changes_nothing()
    {
        var game = TestGames.WithTiles(1, null, TestGames.TilledWith(Target, null));
        var before = TestGames.WaterSnapshot(game);

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), result);
        Assert.False(game.FarmPlot[Target].Watered);
        Assert.Equal(20, game.Player.WateringCan.Charges);
        Assert.Equal(1, game.Player.Energy);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void AC6_20_waterings_on_20_dry_tilled_tiles_empty_the_can()
    {
        var game = TestGames.NewSession().Game;
        var positions = game.FarmPlot.Tiles.Select(tile => tile.Position).Take(20).ToList();
        positions.ForEach(position => game.FarmPlot[position].Till());

        var results = positions.Select(position => game.Water(position, ToolEnergyCost)).ToList();

        Assert.All(results, result => Assert.Equal(ActionResult.Ok, result));
        Assert.All(positions, position => Assert.True(game.FarmPlot[position].Watered));
        Assert.Equal(0, game.Player.WateringCan.Charges);
        Assert.Equal(150 - (20 * ToolEnergyCost), game.Player.Energy);
    }

    [Fact]
    public void AC6_refilling_an_empty_can_sets_charges_to_capacity_and_leaves_energy_unchanged()
    {
        var game = TestGames.WithCan(110, TestGames.CanWith(0), null);

        var result = game.RefillCan();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(20, game.Player.WateringCan.Charges);
        Assert.Equal(game.Player.WateringCan.Capacity, game.Player.WateringCan.Charges);
        Assert.Equal(110, game.Player.Energy);
    }

    [Fact]
    public void AC6_refilling_a_full_can_returns_Ok_and_changes_nothing()
    {
        var game = TestGames.NewSession().Game;
        var before = TestGames.WaterSnapshot(game);

        var result = game.RefillCan();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(20, game.Player.WateringCan.Charges);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void AC6_after_emptying_and_refilling_the_can_can_water_again()
    {
        var game = TestGames.NewSession().Game;
        var positions = game.FarmPlot.Tiles.Select(tile => tile.Position).Take(21).ToList();
        positions.ForEach(position => game.FarmPlot[position].Till());
        positions.Take(20).ToList().ForEach(position => game.Water(position, ToolEnergyCost));

        var empty = game.Water(positions[20], ToolEnergyCost);
        game.RefillCan();
        var refilled = game.Water(positions[20], ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.CanEmpty), empty);
        Assert.Equal(ActionResult.Ok, refilled);
        Assert.Equal(game.Player.WateringCan.Capacity - 1, game.Player.WateringCan.Charges);
    }

    [Fact]
    public void AC7_a_tile_with_a_planting_can_be_watered_and_keeps_its_planting()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Emberroot, 1);
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, planting));

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Ok, result);
        Assert.True(game.FarmPlot[Target].Watered);
        Assert.Equal(planting, game.FarmPlot[Target].Planting);
        Assert.Equal(19, game.Player.WateringCan.Charges);
        Assert.Equal(148, game.Player.Energy);
    }

    [Fact]
    public void BR_T9_watering_an_already_watered_tile_costs_nothing()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, null));
        game.Water(Target, ToolEnergyCost);
        var before = TestGames.WaterSnapshot(game);

        var second = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyWatered), second);
        Assert.Equal(19, game.Player.WateringCan.Charges);
        Assert.Equal(148, game.Player.Energy);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void BR_T9_checks_NotTilled_then_AlreadyWatered_then_CanEmpty_then_TooTired()
    {
        var watered = new GridPosition(1, 1);
        var dry = new GridPosition(2, 1);

        // Every later condition also fails, so each rejection proves it is checked before the ones after it.
        var empty = TestGames.WithCan(
            0, TestGames.CanWith(0), null, TestGames.TilledWith(watered, null, watered: true), TestGames.TilledWith(dry, null));
        var tired = TestGames.WithCan(0, TestGames.CanWith(5), null, TestGames.TilledWith(dry, null));

        Assert.Equal(ActionResult.Rejected(Rejection.NotTilled), empty.Water(Target, ToolEnergyCost));
        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyWatered), empty.Water(watered, ToolEnergyCost));
        Assert.Equal(ActionResult.Rejected(Rejection.CanEmpty), empty.Water(dry, ToolEnergyCost));
        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), tired.Water(dry, ToolEnergyCost));
    }

    [Fact]
    public void BR_T9_water_rejection_gives_the_same_rejections_in_the_same_order()
    {
        var watered = new GridPosition(1, 1);
        var dry = new GridPosition(2, 1);
        var empty = TestGames.WithCan(
            0, TestGames.CanWith(0), null, TestGames.TilledWith(watered, null, watered: true), TestGames.TilledWith(dry, null));
        var tired = TestGames.WithCan(0, TestGames.CanWith(5), null, TestGames.TilledWith(dry, null));
        var before = (TestGames.WaterSnapshot(empty), TestGames.WaterSnapshot(tired));

        Assert.Equal(Rejection.NotTilled, empty.WaterRejection(Target, ToolEnergyCost));
        Assert.Equal(Rejection.AlreadyWatered, empty.WaterRejection(watered, ToolEnergyCost));
        Assert.Equal(Rejection.CanEmpty, empty.WaterRejection(dry, ToolEnergyCost));
        Assert.Equal(Rejection.TooTired, tired.WaterRejection(dry, ToolEnergyCost));
        Assert.Equal(before, (TestGames.WaterSnapshot(empty), TestGames.WaterSnapshot(tired)));
    }

    [Fact]
    public void BR_T9_water_rejection_is_null_when_watering_is_allowed_and_changes_nothing()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, null));
        var before = TestGames.WaterSnapshot(game);

        var rejection = game.WaterRejection(Target, ToolEnergyCost);

        Assert.Null(rejection);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void BR_T9_tile_water_sets_watered_and_keeps_state_and_planting()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Dusktuber, 2);
        var tile = TestGames.TilledWith(Target, planting);

        tile.Water();

        Assert.True(tile.Watered);
        Assert.Equal(TileState.Tilled, tile.State);
        Assert.Equal(planting, tile.Planting);
    }

    [Fact]
    public void BR_T9_watering_one_tile_leaves_every_other_tile_dry()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, null), TestGames.TilledWith(new(0, 0), null));

        game.Water(Target, ToolEnergyCost);

        Assert.All(
            game.FarmPlot.Tiles.Where(tile => tile.Position != Target),
            tile => Assert.False(tile.Watered));
    }

    [Fact]
    public void BR_P1_watering_with_exactly_2_energy_succeeds_and_leaves_0()
    {
        var game = TestGames.WithTiles(2, null, TestGames.TilledWith(Target, null));

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Ok, result);
        Assert.True(game.FarmPlot[Target].Watered);
        Assert.Equal(0, game.Player.Energy);
    }

    [Fact]
    public void BR_P1_watering_at_0_energy_never_takes_energy_below_0()
    {
        var game = TestGames.WithTiles(0, null, TestGames.TilledWith(Target, null));

        var result = game.Water(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), result);
        Assert.Equal(0, game.Player.Energy);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void BR_P2_watering_costs_the_tool_energy_cost(int cost)
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, null));

        var result = game.Water(Target, cost);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(150 - cost, game.Player.Energy);
    }

    [Fact]
    public void BR_P2_watering_is_refused_below_the_tool_energy_cost()
    {
        var game = TestGames.WithTiles(4, null, TestGames.TilledWith(Target, null));
        var before = TestGames.WaterSnapshot(game);

        var result = game.Water(Target, 5);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), result);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void BR_P2_refilling_the_can_costs_no_energy_even_at_0_energy()
    {
        var game = TestGames.WithCan(0, TestGames.CanWith(3), null);

        var result = game.RefillCan();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(0, game.Player.Energy);
        Assert.Equal(game.Player.WateringCan.Capacity, game.Player.WateringCan.Charges);
    }
}
