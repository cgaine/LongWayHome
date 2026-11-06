using LongWayHome.Application.Data;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>Holds the current Game and the game data, and runs the Player's actions as commands.</summary>
public sealed class GameSession
{
    public GameSession(Game game, GameData data)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);

        Game = game;
        Data = data;
    }

#pragma warning disable CS0067 // Raised once the commands are implemented.
    /// <summary>Raised after a successful action changes a Tile.</summary>
    public event Action<GridPosition>? TileChanged;

    /// <summary>Raised after a successful action changes the Player (energy, can or tools).</summary>
    public event Action? PlayerChanged;
#pragma warning restore CS0067

    public Game Game { get; }

    public GameData Data { get; }

    /// <summary>The held-item cycle: Hoe, Watering can, then one entry per Seed stack in CropTypeId order.</summary>
    public IReadOnlyList<HeldItem> HeldItems() => throw new NotImplementedException();

    /// <summary>Decides what Use would do, without changing state.</summary>
    public UsePreview PreviewUse(GridPosition? target, HeldItem held) => throw new NotImplementedException();

    /// <summary>Performs what <see cref="PreviewUse"/> names, or returns its rejection.</summary>
    public ActionResult Use(GridPosition? target, HeldItem held) => throw new NotImplementedException();

    /// <summary>Tills a Tile, spending the tuning's tool energy cost.</summary>
    public ActionResult Till(GridPosition position) => throw new NotImplementedException();
}
