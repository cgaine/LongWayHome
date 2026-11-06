using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Games;

/// <summary>The Game aggregate root: one save's whole state.</summary>
public sealed class Game
{
    public Game(
        Guid id,
        GameDate date,
        int clockMinutes,
        Weather weather,
        DayEndReason? lastDayEnd,
        DateTimeOffset createdAt,
        DateTimeOffset savedAt,
        Player player,
        FarmPlot farmPlot) => throw new NotImplementedException();

    public Guid Id { get; }

    public GameDate Date { get; }

    public int ClockMinutes { get; }

    public Weather Weather { get; }

    public DayEndReason? LastDayEnd { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset SavedAt { get; }

    public Player Player { get; } = null!;

    public FarmPlot FarmPlot { get; } = null!;
}
