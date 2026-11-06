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

    /// <summary>The shipped data row for a Crop type (emberroot growDays 3, dusktuber 5, moonmelon 8).</summary>
    public static CropType Crop(CropTypeId id) => Shipped.CropTypes[id];

    /// <summary>The date a new Game starts on: Year 1, Spring, Day 1.</summary>
    public static GameDate FirstDay => new(1, Season.Spring, 1);

    /// <summary>A Planting of a Crop type, planted on the first day, that has grown for the given days.</summary>
    public static Planting PlantingOf(CropTypeId id, int daysGrown) => new(id, FirstDay, daysGrown);

    /// <summary>A Planting that is Ripe for its shipped Crop type.</summary>
    public static Planting Ripe(CropTypeId id) => PlantingOf(id, Crop(id).GrowDays);

    /// <summary>A tilled Tile holding a Planting.</summary>
    public static Tile TilledWith(GridPosition position, Planting? planting, bool watered = false) =>
        new(position, TileState.Tilled, watered, planting);

    /// <summary>The shipped starting seeds: 10 emberroot and 5 dusktuber.</summary>
    public static Inventory StartingSeeds()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 10);
        inventory.Add(ItemKind.Seed, CropTypeId.Dusktuber, 5);
        return inventory;
    }

    /// <summary>A Game whose plot is grass except for the given Tiles (restored through FarmPlot(IEnumerable&lt;Tile&gt;)).</summary>
    public static GameState WithTiles(int energy, Inventory? inventory, params Tile[] tiles)
    {
        var overrides = tiles.ToDictionary(tile => tile.Position);
        var all = new List<Tile>();
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                var position = new GridPosition(x, y);
                all.Add(overrides.TryGetValue(position, out var tile) ? tile : new Tile(position));
            }
        }

        return new GameState(
            Guid.NewGuid(),
            FirstDay,
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
                inventory ?? StartingSeeds()),
            new FarmPlot(all));
    }

    /// <summary>A session at 150 energy with the starting seeds and the given Tiles.</summary>
    public static GameSession SessionWithTiles(params Tile[] tiles) => new(WithTiles(150, null, tiles), Shipped);

    /// <summary>Energy, every Tile (state, watered, planting) and every inventory stack, to prove an action changed nothing.</summary>
    public static (int Energy, string Tiles, string Inventory) FullSnapshot(GameState game) =>
        (game.Player.Energy,
            string.Join(
                ",",
                game.FarmPlot.Tiles.Select(tile =>
                    $"{tile.Position.X}:{tile.Position.Y}:{tile.State}:{tile.Watered}:{tile.Planting}")),
            string.Join(",", game.Player.Inventory.Stacks.Select(stack => $"{stack.Kind}:{stack.CropTypeId}:{stack.Quantity}")));

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
