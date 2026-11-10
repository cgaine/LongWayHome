using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Buildings;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>Games, Buildings and stage rows for the Rebuild sign tests (#163).</summary>
internal static class BuildingGames
{
    public const string Stage2Unlocks = "The whole store reopens: the 24-slot backpack";

    public const string Stage3Unlocks = "A storeroom: the 36-slot backpack and chests";

    public const string Stage4Unlocks = "An expanded shop: a shipping bin for the farm";

    /// <summary>
    /// The general store's stage 2–4 rows from the design doc, built here rather than read from <c>buildings.json</c>
    /// so domain and session tests don't depend on the shipped file.
    /// </summary>
    public static readonly IReadOnlyList<BuildingStage> StoreStages =
    [
        new(BuildingId.GeneralStore, 2, [new(MaterialId.Wood, 50), new(MaterialId.Stone, 25)], new Gold(300), Stage2Unlocks),
        new(BuildingId.GeneralStore, 3, [new(MaterialId.Plank, 40), new(MaterialId.Stone, 60)], new Gold(1200), Stage3Unlocks),
        new(
            BuildingId.GeneralStore,
            4,
            [new(MaterialId.Plank, 80), new(MaterialId.Stone, 120), new(MaterialId.RoskOre, 20)],
            new Gold(3000),
            Stage4Unlocks),
    ];

    /// <summary>The shipped data with <see cref="StoreStages"/> as its Building stage rows.</summary>
    public static readonly GameData Data = TestGames.Shipped with { BuildingStages = StoreStages };

    /// <summary>The new-Game Buildings as <see cref="Describe"/> writes them.</summary>
    public const string NewGameBuildings = "GeneralStore:1:-:0,Carpenter:0:-:0,MirasHouse:0:-:0,Blacksmith:0:-:0";

    public static ItemRef Material(MaterialId id) => ItemRef.Material(id);

    /// <summary>All four Buildings in enum order: the general store as given, the rest at their new-Game stage 0.</summary>
    public static List<Building> All(
        int storeStage = 1, int storeGold = 0, params (MaterialId Material, int Quantity)[] storeDelivered) =>
    [
        new Building(
            BuildingId.GeneralStore,
            storeStage,
            storeDelivered.ToDictionary(pair => pair.Material, pair => pair.Quantity),
            storeGold),
        new Building(BuildingId.Carpenter, 0),
        new Building(BuildingId.MirasHouse, 0),
        new Building(BuildingId.Blacksmith, 0),
    ];

    /// <summary>A Game whose Player carries the given Backpack and gold, with the given Buildings (passed to the constructor).</summary>
    public static GameState WithBuildings(IReadOnlyList<Building>? buildings, Backpack? backpack = null, int gold = 100)
    {
        var game = TestGames.WithBackpack(backpack ?? TestGames.ToolsOnly(), gold: gold);
        return new GameState(
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
            game.OpeningDone,
            game.PurchasedListings,
            game.ResourceNodes,
            game.NodeRegrowths,
            buildings);
    }

    /// <summary>A Game with the default Buildings except the store, holding the given items and gold.</summary>
    public static GameState Store(
        int gold = 100, int storeStage = 1, int storeGold = 0, (MaterialId, int)[]? delivered = null,
        params (ItemRef Item, int Quantity)[] held) =>
        WithBuildings(All(storeStage, storeGold, delivered ?? []), TestGames.BackpackWith(held), gold);

    /// <summary>A session in town over <paramref name="game"/>, with <see cref="Data"/>.</summary>
    public static GameSession InTown(GameState game)
    {
        var session = new GameSession(game, Data);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        return session;
    }

    /// <summary>Every Building as "Id:stage:delivered:gold", delivered as "Wood=30;Stone=5" (by MaterialId, zeros left out) or "-".</summary>
    public static string Describe(IEnumerable<Building> buildings) =>
        string.Join(",", buildings.Select(Describe));

    public static string Describe(Building building)
    {
        var delivered = building.Delivered
            .Where(pair => pair.Value != 0)
            .OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}={pair.Value}")
            .ToList();
        return $"{building.Id}:{building.Stage}:{(delivered.Count == 0 ? "-" : string.Join(";", delivered))}:{building.DeliveredGold}";
    }

    /// <summary>Gold, every Backpack Slot, every Building, energy and clock: to prove a delivery changed nothing.</summary>
    public static (int Gold, string Backpack, string Buildings, int Energy, int Clock) Snapshot(GameState game) =>
        (game.Player.Gold.Value,
            TestGames.Describe(game.Player.Backpack),
            Describe(game.Buildings),
            game.Player.Energy,
            game.ClockMinutes);

    public static int Held(GameState game, MaterialId material) => game.Player.Backpack.Quantity(ItemRef.Material(material));

    public static Building StoreOf(GameState game) => game.Buildings.Single(building => building.Id == BuildingId.GeneralStore);
}
