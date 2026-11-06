using LongWayHome.Application.Data;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>Runs the Night between two Days, in the domain model's order.</summary>
public static class NightProcessor
{
    /// <summary>Runs the Night steps on a Game whose Day ended for the given reason.</summary>
    public static void Run(Game game, GameData data, DayEndReason reason) => throw new NotImplementedException();
}
