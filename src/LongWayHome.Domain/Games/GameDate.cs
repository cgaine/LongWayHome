using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Games;

/// <summary>A date in the game calendar: Year ≥ 1, Day 1–28.</summary>
public readonly record struct GameDate(int Year, Season Season, int Day)
{
    public int Year { get; } = Validate(Year);

    public Season Season { get; } = Season;

    public int Day { get; } = Validate(Day);

    private static int Validate(int value) => throw new NotImplementedException();
}
