using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Games;

/// <summary>
/// The Game aggregate root: one save's whole state — calendar (BR-G1), clock, weather, the Player and the Farm plot.
/// </summary>
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
        FarmPlot farmPlot)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(farmPlot);

        Id = id;
        Date = date;
        ClockMinutes = clockMinutes;
        Weather = weather;
        LastDayEnd = lastDayEnd;
        CreatedAt = createdAt;
        SavedAt = savedAt;
        Player = player;
        FarmPlot = farmPlot;
    }

    public Guid Id { get; }

    public GameDate Date { get; }

    public int ClockMinutes { get; }

    public Weather Weather { get; }

    public DayEndReason? LastDayEnd { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset SavedAt { get; }

    public Player Player { get; }

    public FarmPlot FarmPlot { get; }
}
