using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>Builds a new Game from the game data (BR-P6, BR-T1, BR-G1). Starting values come from the tuning.</summary>
public static class NewGameFactory
{
    public static Game Create(GameData data, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(data);
        var tuning = data.Tuning;

        // BR-P6: starting gold, energy, tools, a full can and the starting Seeds, all from the tuning.
        var inventory = new Inventory();
        foreach (var seeds in tuning.StartingSeeds)
        {
            // BR-P7: a crop listed twice merges into one stack.
            inventory.Add(ItemKind.Seed, seeds.CropTypeId, seeds.Quantity);
        }

        var player = new Player(
            new Gold(tuning.StartingGold),
            tuning.MaxEnergy,
            new HashSet<ToolId> { ToolId.Hoe, ToolId.WateringCan },
            new WateringCan(tuning.CanCapacity),
            inventory);

        // BR-G1: a new Game starts on Year 1, Spring 1. BR-T1: a fresh 12 x 9 plot of grass.
        return new Game(
            Guid.NewGuid(),
            new GameDate(1, Season.Spring, 1),
            tuning.DayStartMinutes,
            Weather.Clear,
            lastDayEnd: null,
            createdAt: now,
            savedAt: now,
            player,
            new FarmPlot());
    }
}
