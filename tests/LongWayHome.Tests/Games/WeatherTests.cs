using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Application.Ports;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Random;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>The weather: rolled once each Night (BR-G4), rain waters every tilled Tile all Day (BR-T7), so watering
/// by hand is refused (BR-T9) and rain-watered Plantings grow (BR-T4).</summary>
public class WeatherTests
{
    private static readonly GridPosition Spot = new(3, 2);
    private static readonly GridPosition Other = new(4, 2);
    private static readonly GridPosition Grass = new(0, 0);

    private static readonly HeldItem Can = HeldItem.Of(ToolId.WateringCan);
    private static readonly HeldItem Hoe = HeldItem.Of(ToolId.Hoe);

    private static int ToolCost => TestGames.Shipped.Tuning.ToolEnergyCost;

    /// <summary>A random source that returns a scripted sequence (repeating the last value) and counts its calls.</summary>
    private sealed class ScriptedRandom(params int[] rolls) : IRandomSource
    {
        public int Calls { get; private set; }

        public int NextPercent()
        {
            var roll = rolls[Math.Min(Calls, rolls.Length - 1)];
            Calls++;
            return roll;
        }
    }

    private static void Night(GameState game, int roll, GameData? data = null) =>
        NightProcessor.Run(game, data ?? TestGames.Shipped, DayEndReason.Slept, new ScriptedRandom(roll));

    private static GameState RainDay(GameState game)
    {
        game.SetWeather(Weather.Rain);
        return game;
    }

    private static int TilledCount(GameState game) => game.FarmPlot.Tiles.Count(tile => tile.State == TileState.Tilled);

    // AC1 — the roll decides the next Day's weather.

    [Theory]
    [InlineData(21, Weather.Rain)]
    [InlineData(22, Weather.Clear)]
    [InlineData(0, Weather.Rain)]
    [InlineData(99, Weather.Clear)]
    public void AC1_the_night_roll_decides_the_next_days_weather(int roll, Weather expected)
    {
        var game = TestGames.WithEnergy(150);

        Night(game, roll);

        Assert.Equal(expected, game.Weather);
    }

    [Fact]
    public void BR_G4_a_roll_below_22_makes_the_next_day_rain()
    {
        var game = TestGames.WithEnergy(150);

        Night(game, 21);

        Assert.Equal(Weather.Rain, game.Weather);
    }

    [Fact]
    public void BR_G4_a_rain_day_after_a_roll_of_22_or_more_is_clear_again()
    {
        var game = RainDay(TestGames.WithEnergy(150));

        Night(game, 22);

        Assert.Equal(Weather.Clear, game.Weather);
    }

    [Theory]
    [InlineData(30, Weather.Rain)]
    [InlineData(49, Weather.Rain)]
    [InlineData(50, Weather.Clear)]
    public void BR_G4_the_rain_threshold_comes_from_the_tuning_RainChancePercent(int roll, Weather expected)
    {
        var data = TestGames.Shipped with { Tuning = TestGames.Shipped.Tuning with { RainChancePercent = 50 } };
        var game = TestGames.WithEnergy(150);

        Night(game, roll, data);

        Assert.Equal(expected, game.Weather);
    }

    [Fact]
    public void BR_G4_a_session_with_a_random_source_rolls_the_weather_when_the_player_sleeps()
    {
        var session = new GameSession(TestGames.WithEnergy(150), TestGames.Shipped, new ScriptedRandom(0));

        Assert.True(session.Sleep().IsOk);

        Assert.Equal(Weather.Rain, session.Game.Weather);
    }

    [Fact]
    public void BR_G4_a_session_with_a_random_source_rolls_the_weather_when_the_player_passes_out()
    {
        var session = new GameSession(TestGames.WithEnergy(150), TestGames.Shipped, new ScriptedRandom(0));

        session.AdvanceClock(TestGames.Shipped.Tuning.DayLengthRealSeconds);

        Assert.Equal(DayEndReason.PassedOut, session.Game.LastDayEnd);
        Assert.Equal(Weather.Rain, session.Game.Weather);
    }

    // AC2 — exactly one roll per Night.

    [Fact]
    public void AC2_the_source_is_asked_exactly_once_per_night()
    {
        var game = TestGames.WithEnergy(150);
        var random = new ScriptedRandom(50);

        NightProcessor.Run(game, TestGames.Shipped, DayEndReason.Slept, random);

        Assert.Equal(1, random.Calls);
    }

    [Fact]
    public void BR_G4_the_weather_is_rolled_once_for_each_of_several_nights()
    {
        var game = TestGames.WithEnergy(150);
        var random = new ScriptedRandom(0, 99, 21, 22, 5);
        var expected = new[] { Weather.Rain, Weather.Clear, Weather.Rain, Weather.Clear, Weather.Rain };

        for (var night = 0; night < expected.Length; night++)
        {
            NightProcessor.Run(game, TestGames.Shipped, night % 2 == 0 ? DayEndReason.Slept : DayEndReason.PassedOut, random);

            Assert.Equal(night + 1, random.Calls);
            Assert.Equal(expected[night], game.Weather);
        }
    }

    [Fact]
    public void AC2_a_session_asks_the_source_once_for_each_night_it_runs()
    {
        var random = new ScriptedRandom(50);
        var session = new GameSession(TestGames.WithEnergy(150), TestGames.Shipped, random);

        for (var night = 0; night < 4; night++)
        {
            Assert.True(session.Sleep().IsOk);
        }

        Assert.Equal(4, random.Calls);
    }

    // AC3 / AC4 — the rain step after the watering reset.

    [Fact]
    public void AC3_after_a_rain_night_every_tilled_tile_is_watered_and_every_grass_tile_is_not()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: false),
            TestGames.TilledWith(Other, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: true),
            TestGames.TilledWith(new GridPosition(5, 2), TestGames.Ripe(CropTypeId.Dusktuber), watered: false));

        Night(game, 0);

        Assert.Equal(3, TilledCount(game));
        Assert.All(
            game.FarmPlot.Tiles,
            tile => Assert.Equal(tile.State == TileState.Tilled, tile.Watered));
    }

    [Fact]
    public void BR_T7_a_rain_night_waters_tilled_tiles_that_were_dry()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, null, watered: false));

        Night(game, 0);

        Assert.True(game.FarmPlot[Spot].Watered);
        Assert.False(game.FarmPlot[Grass].Watered);
    }

    [Fact]
    public void AC4_after_a_clear_night_no_tile_is_watered()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: true),
            TestGames.TilledWith(Other, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: false));

        Night(game, 99);

        Assert.Equal(Weather.Clear, game.Weather);
        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
    }

    [Fact]
    public void BR_T7_a_rain_day_followed_by_a_clear_night_leaves_every_tile_dry()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, null, watered: false));
        Night(game, 0);
        Assert.True(game.FarmPlot[Spot].Watered);

        Night(game, 99);

        Assert.Equal(Weather.Clear, game.Weather);
        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
    }

    [Fact]
    public void BR_T7_water_all_tilled_tiles_waters_tilled_tiles_only()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: false),
            TestGames.TilledWith(Other, null, watered: true));

        game.WaterAllTilledTiles();

        Assert.True(game.FarmPlot[Spot].Watered);
        Assert.True(game.FarmPlot[Other].Watered);
        Assert.Equal(2, game.FarmPlot.Tiles.Count(tile => tile.Watered));
    }

    [Fact]
    public void BR_T7_soak_waters_a_dry_tilled_tile_and_keeps_its_planting()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: false);

        tile.Soak();

        Assert.True(tile.Watered);
        Assert.Equal(TileState.Tilled, tile.State);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 1), tile.Planting);
    }

    [Fact]
    public void BR_T7_soak_leaves_an_already_watered_tile_watered_without_throwing()
    {
        var tile = TestGames.TilledWith(Spot, null, watered: true);

        tile.Soak();

        Assert.True(tile.Watered);
    }

    [Fact]
    public void BR_T7_soak_does_nothing_to_grass()
    {
        var tile = new Tile(Grass);

        tile.Soak();

        Assert.False(tile.Watered);
        Assert.Equal(TileState.Grass, tile.State);
    }

    // AC5 — tilling on a rain day.

    [Fact]
    public void BR_T7_tile_tilled_on_a_rain_day_is_watered()
    {
        var game = RainDay(TestGames.WithEnergy(150));

        Assert.True(game.Till(Spot, ToolCost).IsOk);

        Assert.Equal(TileState.Tilled, game.FarmPlot[Spot].State);
        Assert.True(game.FarmPlot[Spot].Watered);
    }

    [Fact]
    public void AC5_tilling_on_a_rain_day_costs_exactly_the_tool_energy_cost()
    {
        var game = RainDay(TestGames.WithEnergy(150));

        game.Till(Spot, ToolCost);

        Assert.Equal(2, ToolCost);
        Assert.Equal(150 - 2, game.Player.Energy);
    }

    [Fact]
    public void AC5_tilling_through_a_rain_day_session_use_leaves_the_tile_tilled_and_watered()
    {
        var session = new GameSession(TestGames.WithEnergy(150), TestGames.Shipped, new ScriptedRandom(0));
        Assert.True(session.Sleep().IsOk);
        var energy = session.Game.Player.Energy;

        Assert.True(session.Use(Spot, Hoe).IsOk);

        Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Spot].State);
        Assert.True(session.Game.FarmPlot[Spot].Watered);
        Assert.Equal(energy - ToolCost, session.Game.Player.Energy);
    }

    [Fact]
    public void AC5_tilling_on_a_clear_day_leaves_the_tile_tilled_and_dry()
    {
        var game = TestGames.WithEnergy(150);
        Night(game, 99);

        Assert.True(game.Till(Spot, ToolCost).IsOk);

        Assert.Equal(TileState.Tilled, game.FarmPlot[Spot].State);
        Assert.False(game.FarmPlot[Spot].Watered);
        Assert.Equal(TestGames.Shipped.Tuning.MaxEnergy - ToolCost, game.Player.Energy);
    }

    [Fact]
    public void BR_P2_tilling_on_a_rain_day_below_2_energy_is_TooTired_and_leaves_the_tile_grass_and_dry()
    {
        var game = RainDay(TestGames.WithEnergy(1));

        var result = game.Till(Spot, ToolCost);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), result);
        Assert.Equal(TileState.Grass, game.FarmPlot[Spot].State);
        Assert.False(game.FarmPlot[Spot].Watered);
        Assert.Equal(1, game.Player.Energy);
    }

    // AC6 — watering on a rain day.

    [Fact]
    public void BR_T9_watering_a_rain_watered_tile_is_AlreadyWatered_and_uses_no_charge_or_energy()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, null, watered: false));
        Night(game, 0);
        var charges = game.Player.WateringCan.Charges;
        var energy = game.Player.Energy;

        var result = game.Water(Spot, ToolCost);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyWatered), result);
        Assert.Equal(charges, game.Player.WateringCan.Charges);
        Assert.Equal(energy, game.Player.Energy);
    }

    [Fact]
    public void AC6_watering_a_tile_tilled_on_a_rain_day_is_AlreadyWatered_and_uses_no_charge_or_energy()
    {
        var game = RainDay(TestGames.WithEnergy(150));
        game.Till(Spot, ToolCost);
        var before = TestGames.WaterSnapshot(game);

        var result = game.Water(Spot, ToolCost);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyWatered), result);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Fact]
    public void AC6_preview_with_the_can_on_a_rain_day_tilled_tile_is_AlreadyWatered()
    {
        var session = new GameSession(
            TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, null, watered: false)),
            TestGames.Shipped,
            new ScriptedRandom(0));
        Assert.True(session.Sleep().IsOk);
        var before = TestGames.WaterSnapshot(session.Game);

        var preview = session.PreviewUse(Spot, Can);
        var result = session.Use(Spot, Can);

        Assert.Equal(new UsePreview(null, Rejection.AlreadyWatered), preview);
        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyWatered), result);
        Assert.Equal(before, TestGames.WaterSnapshot(session.Game));
    }

    // AC7 — rain-watered Plantings grow.

    [Fact]
    public void BR_T4_a_growing_planting_through_a_rain_day_grows_one_day_in_the_next_night()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 1), watered: false));
        Night(game, 0);
        var daysBefore = game.FarmPlot[Spot].Planting!.DaysGrown;

        Night(game, 99);

        Assert.Equal(daysBefore + 1, game.FarmPlot[Spot].Planting!.DaysGrown);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void AC7_a_planting_not_watered_by_hand_grows_after_a_rain_day_whatever_the_next_roll(int nextRoll)
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Moonmelon, 2), watered: false));
        Night(game, 0);

        Night(game, nextRoll);

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Moonmelon, 3), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void BR_G4_weather_is_rolled_after_growth_so_a_dry_planting_does_not_grow_on_the_rain_night_itself()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 2), watered: false));

        Night(game, 0);

        // Growth read the dry flag before the rain step watered the Tile.
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 2), game.FarmPlot[Spot].Planting);
        Assert.True(game.FarmPlot[Spot].Watered);
        Assert.Equal(Weather.Rain, game.Weather);
        Assert.Equal(TestGames.Shipped.Tuning.MaxEnergy, game.Player.Energy);
        Assert.Equal(TestGames.Shipped.Tuning.DayStartMinutes, game.ClockMinutes);
    }

    // AC8 — nothing but the Night changes the weather.

    [Fact]
    public void AC8_weather_is_unchanged_by_every_day_time_action_on_a_rain_day()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: false),
            TestGames.TilledWith(Other, TestGames.Ripe(CropTypeId.Emberroot), watered: false));
        var session = new GameSession(game, TestGames.Shipped, new ScriptedRandom(0, 99));
        Assert.True(session.Sleep().IsOk);
        Assert.Equal(Weather.Rain, session.Game.Weather);

        void Unchanged() => Assert.Equal(Weather.Rain, session.Game.Weather);

        session.Till(new GridPosition(6, 2));
        Unchanged();
        session.Water(Spot);
        Unchanged();
        session.Plant(Spot, CropTypeId.Emberroot);
        Unchanged();
        Assert.True(session.Harvest(Other).IsOk);
        Unchanged();
        session.RefillCan();
        Unchanged();
        foreach (var held in session.HeldItems())
        {
            session.PreviewUse(new GridPosition(7, 2), held);
            session.Use(new GridPosition(7, 3), held);
        }

        Unchanged();
        session.AdvanceClock(TestGames.Shipped.Tuning.DayLengthRealSeconds / 2.0);
        Unchanged();
        session.AdvanceClock(TestGames.Shipped.Tuning.DayLengthRealSeconds / 4.0);
        Unchanged();
        Assert.True(session.Game.ClockMinutes < TestGames.Shipped.Tuning.DayEndMinutes);
    }

    [Fact]
    public void BR_G4_weather_is_unchanged_by_day_time_actions_on_a_rain_day_game()
    {
        var game = RainDay(TestGames.WithTiles(150, null, TestGames.TilledWith(Other, TestGames.Ripe(CropTypeId.Emberroot))));

        game.Till(Spot, ToolCost);
        game.Water(Spot, ToolCost);
        game.Plant(Spot, CropTypeId.Emberroot);
        game.Harvest(Other, TestGames.Crop(CropTypeId.Emberroot));
        game.RefillCan();
        game.AdvanceClock(600, TestGames.Shipped.Tuning.DayEndMinutes - 1);

        Assert.Equal(Weather.Rain, game.Weather);
    }

    [Fact]
    public void AC8_a_new_game_is_clear()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        Assert.Equal(Weather.Clear, game.Weather);
    }

    // AC9 — the two-argument session never rains.

    [Fact]
    public void AC9_the_two_argument_session_never_produces_rain_over_50_nights()
    {
        var session = new GameSession(
            TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, null, watered: false)),
            TestGames.Shipped);

        for (var night = 0; night < 50; night++)
        {
            Assert.True(session.Sleep().IsOk);

            Assert.Equal(Weather.Clear, session.Game.Weather);
            Assert.False(session.Game.FarmPlot[Spot].Watered);
        }
    }

    // AC10 — the system random source.

    [Fact]
    public void AC10_system_random_source_returns_only_0_to_99_over_10000_calls()
    {
        var random = new SystemRandomSource();
        var rolls = Enumerable.Range(0, 10_000).Select(_ => random.NextPercent()).ToList();

        Assert.All(rolls, roll => Assert.InRange(roll, 0, 99));
        Assert.Contains(rolls, roll => roll < 22);
        Assert.Contains(rolls, roll => roll >= 22);
    }

    // The HUD label.

    [Theory]
    [InlineData(Weather.Clear, "Clear")]
    [InlineData(Weather.Rain, "Rain")]
    public void Weather_text_is_the_display_label(Weather weather, string expected)
    {
        Assert.Equal(expected, WeatherText.For(weather));
    }

    [Fact]
    public void Set_weather_changes_the_games_weather()
    {
        var game = TestGames.WithEnergy(150);

        game.SetWeather(Weather.Rain);

        Assert.Equal(Weather.Rain, game.Weather);
    }
}
