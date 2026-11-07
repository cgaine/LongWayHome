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

        // BR-P6, BR-K1: starting gold, energy, a 12-Slot Backpack with the Basic hoe (Slot 0) and the full Basic can
        // (Slot 1), then the starting Seeds, all from the tuning.
        var backpack = new Backpack();
        backpack.AddTool(ToolId.Hoe, ToolTier.Basic);
        backpack.AddTool(ToolId.WateringCan, ToolTier.Basic);
        foreach (var seeds in tuning.StartingSeeds)
        {
            // BR-K3: a crop listed twice tops up its stack. Starting Seeds that fit nowhere are bad tuning data.
            if (backpack.Add(ItemRef.Seed(seeds.CropTypeId), seeds.Quantity) != 0)
            {
                throw new InvalidOperationException("The tuning's starting seeds don't fit in the Backpack.");
            }
        }

        var player = new Player(
            new Gold(tuning.StartingGold),
            tuning.MaxEnergy,
            new WateringCan(tuning.CanCapacity),
            backpack);

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
