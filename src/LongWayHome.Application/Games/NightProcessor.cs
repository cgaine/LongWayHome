using LongWayHome.Application.Data;
using LongWayHome.Application.Ports;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>
/// Runs the Night between two Days, in the domain model's order. The steps of later features have a comment at
/// their place in that order, so each is inserted where it belongs.
/// </summary>
public static class NightProcessor
{
    /// <summary>
    /// Runs the Night steps on a Game whose Day ended for the given reason, with a source that never rolls rain
    /// (<see cref="NeverRainSource"/>: always 99).
    /// </summary>
    public static void Run(Game game, GameData data, DayEndReason reason) =>
        Run(game, data, reason, NeverRainSource.Instance);

    /// <summary>
    /// Runs the Night steps on a Game whose Day ended for the given reason, rolling the next Day's weather once from
    /// the random source (BR-G4).
    /// </summary>
    public static void Run(Game game, GameData data, DayEndReason reason, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(random);

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

        // BR-S2, BR-P5: an overflowing payout throws here, before growth changes the Game, not halfway through.
        game.ShippingPayout(data.CropTypes);

        // BR-T4: growth reads the watered flags, so it runs before the watering reset clears them.
        game.GrowPlantings(data.CropTypes);

        // BR-S2: the shipping payout, after growth and before the date step.
        game.PayOutShippingBin(data.CropTypes);

        // BR-G1: the date moves on one day. After growth, which never reads the Season, so a Planting keeps growing
        // across a season change (BR-T5).
        game.AdvanceDate();

        // BR-G4: the next Day's weather, rolled exactly once: rain when the roll (0–99) is below the rain chance.
        var weather = random.NextPercent() < tuning.RainChancePercent ? Weather.Rain : Weather.Clear;
        game.SetWeather(weather);

        // BR-T7: the watering reset.
        game.DryAllTiles();

        // BR-T7: on a rain Day every tilled Tile is watered from the start of the Day.
        if (weather == Weather.Rain)
        {
            game.WaterAllTilledTiles();
        }

        // BR-P3: wake with the energy worked out above.
        game.StartNextDay(reason, energy, tuning.DayStartMinutes);

        // BR-G5: save at the end of the Night goes here (epic #12).
    }
}
