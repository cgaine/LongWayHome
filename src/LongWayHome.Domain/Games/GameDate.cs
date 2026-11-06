using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Games;

/// <summary>A date in the game calendar: Year ≥ 1, Day 1–28 of a Season (BR-G1).</summary>
public readonly record struct GameDate(int Year, Season Season, int Day)
{
    /// <summary>The number of Days in a Season (BR-G1).</summary>
    public const int DaysPerSeason = 28;

    // BR-G1
    public int Year { get; } = Year >= 1
        ? Year
        : throw new ArgumentOutOfRangeException(nameof(Year), Year, "Year must be at least 1 (BR-G1).");

    public Season Season { get; } = Season;

    // BR-G1
    public int Day { get; } = Day is >= 1 and <= DaysPerSeason
        ? Day
        : throw new ArgumentOutOfRangeException(nameof(Day), Day, $"Day must be between 1 and {DaysPerSeason} (BR-G1).");

    /// <summary>The next day in the calendar (BR-G1).</summary>
    public GameDate Next()
    {
        if (Day < DaysPerSeason)
        {
            return new GameDate(Year, Season, Day + 1);
        }

        // After the last Season of the Year (the last value of Season) comes the first Season of the next Year.
        var seasons = Enum.GetValues<Season>();
        var next = Array.IndexOf(seasons, Season) + 1;
        return next < seasons.Length
            ? new GameDate(Year, seasons[next], 1)
            : new GameDate(Year + 1, seasons[0], 1);
    }
}
