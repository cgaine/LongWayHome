using LongWayHome.Application.Data;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>Holds the current Game and the game data.</summary>
public sealed class GameSession
{
    public GameSession(Game game, GameData data) => throw new NotImplementedException();

    public Game Game { get; } = null!;

    public GameData Data { get; } = null!;
}
