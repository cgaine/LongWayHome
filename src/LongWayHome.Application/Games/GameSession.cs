using LongWayHome.Application.Data;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>Holds the current Game and the game data. Player actions are added as commands by later features.</summary>
public sealed class GameSession
{
    public GameSession(Game game, GameData data)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);

        Game = game;
        Data = data;
    }

    public Game Game { get; }

    public GameData Data { get; }
}
