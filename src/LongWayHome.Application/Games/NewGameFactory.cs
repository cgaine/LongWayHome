using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>
/// Builds a new Game from the game data (BR-P6, BR-T1, BR-G1, BR-I1, BR-N7). Starting values come from the tuning; the
/// Opening is not done yet.
/// </summary>
public static class NewGameFactory
{
    public static Game Create(GameData data, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(data);
        var tuning = data.Tuning;

        // BR-P6, BR-K1: starting gold, energy, a 12-Slot Backpack with the Basic hoe (Slot 0) and the full Basic can
        // (Slot 1), all from the tuning. BR-P6, BR-I1: no Seeds; Maggie gives the tuning's starting Seeds during the
        // Opening (GameSession.GiveStarterSeeds).
        var backpack = new Backpack();
        backpack.AddTool(ToolId.Hoe, ToolTier.Basic);
        backpack.AddTool(ToolId.WateringCan, ToolTier.Basic);

        var player = new Player(
            new Gold(tuning.StartingGold),
            tuning.MaxEnergy,
            new WateringCan(tuning.CanCapacity),
            backpack);

        // BR-G1: a new Game starts on Year 1, Spring 1. BR-T1: a fresh 72 x 36 plot of grass. BR-R1: Maggie and Clyde
        // are present from the start; Tom, June, Jesse and Mira are not. BR-I1: the Opening has not played yet.
        // BR-N7: a node with no swings taken at every atStart spawn point.
        return new Game(
            Guid.NewGuid(),
            new GameDate(1, Season.Spring, 1),
            tuning.DayStartMinutes,
            Weather.Clear,
            lastDayEnd: null,
            createdAt: now,
            savedAt: now,
            player,
            new FarmPlot(),
            residents: Game.DefaultResidents(),
            openingDone: false,
            resourceNodes: data.NodeSpawns.Where(spawn => spawn.AtStart).Select(ResourceNode.FromSpawn).ToList());
    }
}
