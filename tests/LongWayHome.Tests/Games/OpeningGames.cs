using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>Games for #139's Opening tests (BR-I1, BR-P6).</summary>
internal static class OpeningGames
{
    /// <summary>The same Game rebuilt through the constructor with the given <c>openingDone</c>.</summary>
    public static GameState WithOpening(GameState game, bool openingDone) =>
        new(
            game.Id,
            game.Date,
            game.ClockMinutes,
            game.Weather,
            game.LastDayEnd,
            game.CreatedAt,
            game.SavedAt,
            game.Player,
            game.FarmPlot,
            game.ShippingBin,
            game.Residents,
            openingDone);

    /// <summary>A new Game from the shipped data (Spring 1, the Opening not done, no Seeds once #139 lands).</summary>
    public static GameState New() => NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

    /// <summary>A two-tools-only Game on Spring 1 at the given energy whose Opening is not done.</summary>
    public static GameState NotDone(int energy = 150) => WithOpening(TestGames.WithEnergy(energy), openingDone: false);

    /// <summary>A Game whose Opening is not done, holding the given Backpack, on the given date (Spring 1).</summary>
    public static GameState NotDone(Backpack backpack, GameDate? date = null) =>
        WithOpening(TestGames.WithBackpack(backpack, date: date), openingDone: false);

    /// <summary>How many Seeds of <paramref name="crop"/> the Player carries.</summary>
    public static int SeedCount(GameState game, CropTypeId crop) =>
        game.Player.Backpack.Quantity(ItemRef.Seed(crop));
}
