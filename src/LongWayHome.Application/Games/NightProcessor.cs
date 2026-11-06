using LongWayHome.Application.Data;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// Runs the Night between two Days, in the domain model's order. The steps of later features have a comment at
/// their place in that order, so each is inserted where it belongs.
/// </summary>
public static class NightProcessor
{
    /// <summary>Runs the Night steps on a Game whose Day ended for the given reason.</summary>
    public static void Run(Game game, GameData data, DayEndReason reason)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);

        // BR-T4: growth reads the watered flags, so it runs before the watering reset clears them.
        game.GrowPlantings(data.CropTypes);

        // BR-S2: shipping payout goes here (epic #11).
        // BR-G1: date advance goes here (epic #10).
        // BR-G4: weather roll goes here (epic #10).

        // BR-T7: the watering reset.
        game.DryAllTiles();

        // BR-T7: rain watering every tilled Tile goes here (epic #10).

        // BR-P3: a full night's sleep restores MaxEnergy; passing out wakes with PassOutEnergy.
        var tuning = data.Tuning;
        var energy = reason switch
        {
            DayEndReason.Slept => tuning.MaxEnergy,
            DayEndReason.PassedOut => tuning.PassOutEnergy,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown day end reason."),
        };
        game.StartNextDay(reason, energy, tuning.DayStartMinutes);

        // BR-G5: save at the end of the Night goes here (epic #12).
    }
}
