using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>Builds Games and sessions for action tests from the real <c>game/data/*.json</c> files.</summary>
internal static class TestGames
{
    public static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 30, 0, TimeSpan.Zero);

    public static readonly GameData Shipped = LoadShippedData();

    /// <summary>A Game on a fresh plot whose Player has the given energy and inventory.</summary>
    public static GameState WithEnergy(int energy, Inventory? inventory = null) =>
        new(
            Guid.NewGuid(),
            new GameDate(1, Season.Spring, 1),
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            new Player(
                new Gold(100),
                energy,
                new HashSet<ToolId> { ToolId.Hoe, ToolId.WateringCan },
                new WateringCan(20),
                inventory ?? new Inventory()),
            new FarmPlot());

    public static GameSession NewSession() => new(NewGameFactory.Create(Shipped, Now), Shipped);

    public static GameSession SessionWithEnergy(int energy, GameData? data = null) =>
        new(WithEnergy(energy), data ?? Shipped);

    /// <summary>Energy plus every Tile's state and watered flag, to prove an action changed nothing.</summary>
    public static (int Energy, string Tiles) Snapshot(GameState game) =>
        (game.Player.Energy,
            string.Join(",", game.FarmPlot.Tiles.Select(tile => $"{tile.Position.X}:{tile.Position.Y}:{tile.State}:{tile.Watered}")));

    private static GameData LoadShippedData()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "data");
        string Read(string fileName) => File.ReadAllText(Path.Combine(folder, fileName));

        return new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            Read(JsonGameDataSource.TuningFileName)).Load();
    }
}
