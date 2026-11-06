using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Farming;

/// <summary>Planting a Seed on a Tile and a Planting's status: BR-T3, BR-P2, BR-P7 (#24).</summary>
public class PlantingTests
{
    private static readonly GridPosition Target = new(3, 4);

    [Fact]
    public void AC1_planting_emberroot_on_tilled_empty_tile_in_a_new_game_sets_the_planting_leaves_9_seeds_and_150_energy()
    {
        var game = TestGames.NewSession().Game;
        Assert.Equal(150, game.Player.Energy);
        Assert.Equal(10, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
        Assert.Equal(new GameDate(1, Season.Spring, 1), game.Date);
        game.FarmPlot[Target].Till();

        var result = game.Plant(Target, CropTypeId.Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(
            new Planting(CropTypeId.Emberroot, new GameDate(1, Season.Spring, 1), 0),
            game.FarmPlot[Target].Planting);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.Equal(9, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
        Assert.Equal(150, game.Player.Energy);
    }

    [Fact]
    public void AC2_planting_on_grass_returns_NotTilled_and_changes_nothing()
    {
        var game = TestGames.NewSession().Game;
        var before = TestGames.FullSnapshot(game);

        var result = game.Plant(Target, CropTypeId.Emberroot);

        Assert.Equal(ActionResult.Rejected(Rejection.NotTilled), result);
        Assert.Equal(TileState.Grass, game.FarmPlot[Target].State);
        Assert.Null(game.FarmPlot[Target].Planting);
        Assert.Equal(10, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void AC3_planting_on_a_tile_with_a_planting_returns_TileOccupied_and_changes_nothing()
    {
        var game = TestGames.NewSession().Game;
        game.FarmPlot[Target].Till();
        game.Plant(Target, CropTypeId.Emberroot);
        var existing = game.FarmPlot[Target].Planting;
        var before = TestGames.FullSnapshot(game);

        var result = game.Plant(Target, CropTypeId.Dusktuber);

        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), result);
        Assert.Equal(existing, game.FarmPlot[Target].Planting);
        Assert.Equal(9, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
        Assert.Equal(5, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Dusktuber));
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void AC4_planting_all_5_dusktuber_seeds_removes_the_stack_and_a_sixth_plant_returns_NoSeed()
    {
        var game = TestGames.NewSession().Game;
        var positions = Enumerable.Range(0, 6).Select(x => new GridPosition(x, 0)).ToList();
        positions.ForEach(position => game.FarmPlot[position].Till());

        var results = positions.Take(5).Select(position => game.Plant(position, CropTypeId.Dusktuber)).ToList();

        Assert.All(results, result => Assert.Equal(ActionResult.Ok, result));
        Assert.DoesNotContain(
            game.Player.Inventory.Stacks,
            stack => stack.Kind == ItemKind.Seed && stack.CropTypeId == CropTypeId.Dusktuber);
        var before = TestGames.FullSnapshot(game);

        var sixth = game.Plant(positions[5], CropTypeId.Dusktuber);

        Assert.Equal(ActionResult.Rejected(Rejection.NoSeed), sixth);
        Assert.Null(game.FarmPlot[positions[5]].Planting);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, 0, PlantingStatus.Growing)]
    [InlineData(CropTypeId.Emberroot, 1, PlantingStatus.Growing)]
    [InlineData(CropTypeId.Emberroot, 2, PlantingStatus.Growing)]
    [InlineData(CropTypeId.Emberroot, 3, PlantingStatus.Ripe)]
    [InlineData(CropTypeId.Dusktuber, 4, PlantingStatus.Growing)]
    [InlineData(CropTypeId.Dusktuber, 5, PlantingStatus.Ripe)]
    [InlineData(CropTypeId.Moonmelon, 0, PlantingStatus.Growing)]
    [InlineData(CropTypeId.Moonmelon, 7, PlantingStatus.Growing)]
    [InlineData(CropTypeId.Moonmelon, 8, PlantingStatus.Ripe)]
    public void AC8_status_is_Ripe_exactly_when_days_grown_equals_grow_days(
        CropTypeId crop, int daysGrown, PlantingStatus expected)
    {
        var planting = TestGames.PlantingOf(crop, daysGrown);

        Assert.Equal(expected, planting.StatusFor(TestGames.Crop(crop)));
    }

    [Fact]
    public void AC8_status_for_a_different_crop_type_throws()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Emberroot, 3);

        Assert.Throws<ArgumentException>(() => planting.StatusFor(TestGames.Crop(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void Planting_rejects_negative_days_grown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Planting(CropTypeId.Emberroot, TestGames.FirstDay, -1));
    }

    [Fact]
    public void BR_T3_checks_NotTilled_then_TileOccupied_then_NoSeed()
    {
        var growing = TestGames.PlantingOf(CropTypeId.Dusktuber, 1);
        var occupied = new GridPosition(1, 1);
        var empty = new GridPosition(2, 1);
        var game = TestGames.WithTiles(
            150,
            new Inventory(),
            TestGames.TilledWith(occupied, growing),
            TestGames.TilledWith(empty, null));

        Assert.Equal(Rejection.NotTilled, game.PlantRejection(Target, CropTypeId.Emberroot));
        Assert.Equal(Rejection.TileOccupied, game.PlantRejection(occupied, CropTypeId.Emberroot));
        Assert.Equal(Rejection.NoSeed, game.PlantRejection(empty, CropTypeId.Emberroot));
        Assert.Equal(ActionResult.Rejected(Rejection.NotTilled), game.Plant(Target, CropTypeId.Emberroot));
        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), game.Plant(occupied, CropTypeId.Emberroot));
        Assert.Equal(ActionResult.Rejected(Rejection.NoSeed), game.Plant(empty, CropTypeId.Emberroot));
    }

    [Fact]
    public void BR_T3_plant_rejection_is_null_when_planting_is_allowed_and_changes_nothing()
    {
        var game = TestGames.NewSession().Game;
        game.FarmPlot[Target].Till();
        var before = TestGames.FullSnapshot(game);

        var rejection = game.PlantRejection(Target, CropTypeId.Emberroot);

        Assert.Null(rejection);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void BR_T3_planting_needs_a_seed_of_that_crop_type()
    {
        var game = TestGames.NewSession().Game;
        game.FarmPlot[Target].Till();
        var before = TestGames.FullSnapshot(game);

        var result = game.Plant(Target, CropTypeId.Moonmelon);

        Assert.Equal(ActionResult.Rejected(Rejection.NoSeed), result);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void BR_T3_planting_uses_exactly_one_seed_of_that_crop_type()
    {
        var game = TestGames.NewSession().Game;
        game.FarmPlot[Target].Till();

        game.Plant(Target, CropTypeId.Dusktuber);

        Assert.Equal(4, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Dusktuber));
        Assert.Equal(10, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
        Assert.Equal(CropTypeId.Dusktuber, game.FarmPlot[Target].Planting?.CropTypeId);
    }

    [Fact]
    public void BR_T3_tile_plant_on_grass_is_NotTilled_and_on_a_planting_is_TileOccupied()
    {
        var grass = new Tile(Target);
        var first = TestGames.PlantingOf(CropTypeId.Emberroot, 0);
        var occupied = TestGames.TilledWith(Target, first);

        Assert.Equal(ActionResult.Rejected(Rejection.NotTilled), grass.Plant(first));
        Assert.Null(grass.Planting);
        Assert.Equal(
            ActionResult.Rejected(Rejection.TileOccupied),
            occupied.Plant(TestGames.PlantingOf(CropTypeId.Dusktuber, 0)));
        Assert.Equal(first, occupied.Planting);
    }

    [Fact]
    public void BR_T3_tile_plant_on_tilled_empty_soil_sets_the_planting()
    {
        var tile = new Tile(Target);
        tile.Till();
        var planting = TestGames.PlantingOf(CropTypeId.Emberroot, 0);

        var result = tile.Plant(planting);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(planting, tile.Planting);
        Assert.Equal(TileState.Tilled, tile.State);
    }

    [Fact]
    public void BR_T3_a_restored_tile_cannot_hold_a_planting_on_grass()
    {
        Assert.Throws<ArgumentException>(
            () => new Tile(Target, TileState.Grass, false, TestGames.PlantingOf(CropTypeId.Emberroot, 0)));
    }

    [Theory]
    [InlineData(TileState.Grass, false, false)]
    [InlineData(TileState.Tilled, true, false)]
    [InlineData(TileState.Tilled, false, true)]
    [InlineData(TileState.Tilled, true, true)]
    public void Restored_tile_keeps_its_state_watered_flag_and_planting(TileState state, bool watered, bool planted)
    {
        var planting = planted ? TestGames.PlantingOf(CropTypeId.Moonmelon, 2) : null;

        var tile = new Tile(Target, state, watered, planting);

        Assert.Equal(Target, tile.Position);
        Assert.Equal(state, tile.State);
        Assert.Equal(watered, tile.Watered);
        Assert.Equal(planting, tile.Planting);
    }

    [Fact]
    public void BR_T1_restored_farm_plot_keeps_one_tile_per_position()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Emberroot, 1);

        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Target, planting, watered: true));

        Assert.Equal(108, game.FarmPlot.Tiles.Count);
        Assert.Equal(planting, game.FarmPlot[Target].Planting);
        Assert.True(game.FarmPlot[Target].Watered);
        Assert.Equal(TileState.Grass, game.FarmPlot[new GridPosition(0, 0)].State);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("extra")]
    public void BR_T1_restored_farm_plot_rejects_anything_but_one_tile_per_position(string defect)
    {
        var tiles = new FarmPlot().Tiles.Select(tile => new Tile(tile.Position)).ToList();
        switch (defect)
        {
            case "missing":
                tiles.RemoveAt(10);
                break;
            case "duplicate":
                tiles[10] = new Tile(tiles[11].Position);
                break;
            default:
                tiles.Add(new Tile(new GridPosition(0, 0)));
                break;
        }

        Assert.Throws<ArgumentException>(() => new FarmPlot(tiles));
    }

    [Fact]
    public void BR_P2_planting_costs_no_energy_even_at_0_energy()
    {
        var game = TestGames.WithTiles(0, TestGames.StartingSeeds(), TestGames.TilledWith(Target, null));

        var result = game.Plant(Target, CropTypeId.Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(0, game.Player.Energy);
        Assert.NotNull(game.FarmPlot[Target].Planting);
    }

    [Fact]
    public void BR_P7_planting_the_last_seed_removes_its_stack()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 1);
        var game = TestGames.WithTiles(150, inventory, TestGames.TilledWith(Target, null));

        var result = game.Plant(Target, CropTypeId.Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Empty(game.Player.Inventory.Stacks);
        Assert.Equal(0, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Emberroot));
    }

    [Fact]
    public void Planting_records_the_game_date_as_planted_on()
    {
        var game = TestGames.NewSession().Game;
        game.FarmPlot[Target].Till();

        game.Plant(Target, CropTypeId.Emberroot);

        Assert.Equal(game.Date, game.FarmPlot[Target].Planting?.PlantedOn);
        Assert.Equal(0, game.FarmPlot[Target].Planting?.DaysGrown);
    }

    [Fact]
    public void Restored_farm_plot_rejects_null_tiles()
    {
        var tiles = new FarmPlot().Tiles.Select(tile => new Tile(tile.Position)).ToList();
        tiles[0] = null!;

        Assert.Throws<ArgumentNullException>(() => new FarmPlot(tiles));
        Assert.Throws<ArgumentNullException>(() => new FarmPlot(null!));
    }

    [Fact]
    public void Planting_with_days_grown_set_keeps_it_and_compares_by_value()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Dusktuber, 4);

        Assert.Equal(4, planting.DaysGrown);
        Assert.Equal(new Planting(CropTypeId.Dusktuber, TestGames.FirstDay, 4), planting);
        Assert.NotEqual(planting with { DaysGrown = 5 }, planting);
        Assert.Throws<ArgumentOutOfRangeException>(() => planting with { DaysGrown = -1 });
    }
}
