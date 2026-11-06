using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Art;

/// <summary>
/// How a Tile's slab looks in each state (#78, AC2–AC5, AC10), driven through the real domain Tile and Game so the
/// look follows BR-T2, BR-T7, BR-T8 and BR-T9.
/// </summary>
public class TileLookTests
{
    private static readonly GridPosition Position = new(3, 4);

    private static readonly TileLook Grass = new("grass-lit", true, false, "soil-tilled", 0f);
    private static readonly TileLook TilledDry = new("soil-tilled", false, true, "soil-tilled", 0f);
    private static readonly TileLook TilledWatered = new("soil-watered", false, true, "soil-watered", 1f);

    [Fact]
    public void AC2_grass_tile_shows_green_slab_with_tufts_and_no_furrows()
    {
        var look = TileLook.For(new Tile(Position));

        Assert.Equal("grass-lit", look.SlabRole);
        Assert.True(look.ShowTufts);
        Assert.False(look.ShowFurrows);
        Assert.Equal(0f, look.Wet);
    }

    [Fact]
    public void AC2_tilled_tile_shows_brown_furrowed_slab_without_tufts()
    {
        var look = TileLook.For(new Tile(Position, TileState.Tilled, false, null));

        Assert.Equal(TilledDry, look);
    }

    [Fact]
    public void BR_T2_tilling_a_grass_tile_switches_to_the_tilled_look()
    {
        var tile = new Tile(Position);
        Assert.Equal(Grass, TileLook.For(tile));

        Assert.True(tile.Till().IsOk);

        Assert.Equal(TilledDry, TileLook.For(tile));
    }

    [Fact]
    public void BR_T8_tilled_tile_never_shows_tufts_again()
    {
        var tile = new Tile(Position);
        tile.Till();

        var looks = new List<TileLook> { TileLook.For(tile) };
        tile.Water();
        looks.Add(TileLook.For(tile));
        tile.Dry();
        looks.Add(TileLook.For(tile));
        tile.Soak();
        looks.Add(TileLook.For(tile));
        tile.Dry();
        looks.Add(TileLook.For(tile));
        tile.Till(); // refused: already tilled
        looks.Add(TileLook.For(tile));

        Assert.All(looks, look => Assert.False(look.ShowTufts));
        Assert.All(looks, look => Assert.True(look.ShowFurrows));
    }

    [Fact]
    public void AC3_watered_tile_shows_dark_wet_slab()
    {
        var tile = new Tile(Position);
        tile.Till();
        tile.Water();

        var look = TileLook.For(tile);

        Assert.Equal("soil-watered", look.SlabRole);
        Assert.Equal("soil-watered", look.FurrowRole);
        Assert.True(look.ShowFurrows);
        Assert.False(look.ShowTufts);
        Assert.Equal(1f, look.Wet);
    }

    [Fact]
    public void BR_T7_after_night_watered_tile_shows_dry_tilled_soil()
    {
        var game = TestGames.WithEnergy(150);
        game.Till(Position, 0);
        game.Water(Position, 0);
        Assert.Equal(TilledWatered, TileLook.For(game.FarmPlot[Position]));

        // The Night's watering reset (BR-T7).
        game.DryAllTiles();

        var look = TileLook.For(game.FarmPlot[Position]);
        Assert.Equal("soil-tilled", look.SlabRole);
        Assert.Equal(0f, look.Wet);
        Assert.Equal(TilledDry, look);
    }

    [Fact]
    public void AC4_rain_day_soaked_tilled_tile_including_freshly_tilled_shows_watered_look()
    {
        var earlier = new GridPosition(1, 1);
        var fresh = new GridPosition(5, 5);
        var game = TestGames.WithEnergy(150);
        game.Till(earlier, 0);

        // The Night's rain step (BR-T7) soaks every tilled Tile.
        game.SetWeather(Weather.Rain);
        game.WaterAllTilledTiles();

        // A Tile tilled during the rain Day is watered at once (BR-T7).
        Assert.True(game.Till(fresh, 0).IsOk);

        Assert.Equal(TilledWatered, TileLook.For(game.FarmPlot[earlier]));
        Assert.Equal(TilledWatered, TileLook.For(game.FarmPlot[fresh]));
        Assert.Equal(Grass, TileLook.For(game.FarmPlot[new GridPosition(0, 0)]));
    }

    [Fact]
    public void AC5_watering_an_already_watered_tile_leaves_its_look_unchanged()
    {
        var game = TestGames.WithEnergy(150);
        game.Till(Position, 0);
        game.Water(Position, 0);
        var before = TileLook.For(game.FarmPlot[Position]);

        var result = game.Water(Position, 0);

        Assert.False(result.IsOk);
        Assert.Equal(before, TileLook.For(game.FarmPlot[Position]));
    }

    [Fact]
    public void BR_T9_refused_rewater_keeps_identical_look()
    {
        var game = TestGames.WithEnergy(150);
        game.SetWeather(Weather.Rain);
        game.Till(Position, 0);
        var before = TileLook.For(game.FarmPlot[Position]);

        Assert.Equal(Rejection.AlreadyWatered, game.WaterRejection(Position, 0));
        Assert.False(game.Water(Position, 0).IsOk);

        Assert.Equal(TilledWatered, before);
        Assert.Equal(before, TileLook.For(game.FarmPlot[Position]));
    }

    [Fact]
    public void AC10_planted_tile_keeps_its_soil_look()
    {
        var planting = new Planting(CropTypeId.Starcorn, new GameDate(1, Season.Spring, 1), 3);

        Assert.Equal(TilledWatered, TileLook.For(new Tile(Position, TileState.Tilled, true, planting)));
        Assert.Equal(TilledDry, TileLook.For(new Tile(Position, TileState.Tilled, false, planting)));
    }
}
