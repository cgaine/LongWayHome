using LongWayHome.Application.Data;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

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

        // BR-P3: a full night's sleep restores MaxEnergy; passing out wakes with PassOutEnergy. Worked out and
        // checked (BR-P1) before any step changes the Game, so a bad reason or energy leaves the Game as it was.
        var tuning = data.Tuning;
        var energy = reason switch
        {
            DayEndReason.Slept => tuning.MaxEnergy,
            DayEndReason.PassedOut => tuning.PassOutEnergy,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown day end reason."),
        };
        if (energy is < 0 or > Player.MaxEnergy)
        {
            throw new ArgumentOutOfRangeException(
                nameof(data), energy, $"Wake energy must be between 0 and {Player.MaxEnergy} (BR-P1).");
        }

        // BR-T4: growth reads the watered flags, so it runs before the watering reset clears them.
        game.GrowPlantings(data.CropTypes);

        // BR-S2: shipping payout goes here (epic #11).
        // BR-G1: date advance goes here (epic #10).
        // BR-G4: weather roll goes here (epic #10).

        // BR-T7: the watering reset.
        game.DryAllTiles();

        // BR-T7: rain watering every tilled Tile goes here (epic #10).

        // BR-P3: wake with the energy worked out above.
        game.StartNextDay(reason, energy, tuning.DayStartMinutes);

        // BR-G5: save at the end of the Night goes here (epic #12).
    }
}
