using LongWayHome.Application.Data;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

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

    /// <summary>Raised after a successful action changes a Tile.</summary>
    public event Action<GridPosition>? TileChanged;

    /// <summary>Raised after a successful action changes the Player (energy, can or tools).</summary>
    public event Action? PlayerChanged;

    public Game Game { get; }

    public GameData Data { get; }

    /// <summary>The held-item cycle: Hoe, Watering can, then one entry per Seed stack in CropTypeId order.</summary>
    public IReadOnlyList<HeldItem> HeldItems()
    {
        var items = new List<HeldItem> { HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan) };

        // Inventory.Stacks is ordered by ItemKind then CropTypeId, so the seeds come out in CropTypeId order.
        items.AddRange(Game.Player.Inventory.Stacks
            .Where(stack => stack.Kind == ItemKind.Seed)
            .Select(stack => HeldItem.SeedOf(stack.CropTypeId)));

        return items.AsReadOnly();
    }

    /// <summary>Decides what Use would do, without changing state.</summary>
    public UsePreview PreviewUse(GridPosition? target, HeldItem held)
    {
        ArgumentNullException.ThrowIfNull(held);

        if (target is not { } position)
        {
            return Rejected(Rejection.NoTarget);
        }

        if (held.Tool == ToolId.Hoe)
        {
            // BR-T2, then BR-P2: the domain decides, so the preview and Game.Till cannot drift apart.
            if (Game.TillRejection(position, Data.Tuning.ToolEnergyCost) is { } rejection)
            {
                return Rejected(rejection);
            }

            return new UsePreview(UseActionKind.Till, null);
        }

        // The can and seeds get their actions in later features.
        return Rejected(Rejection.NothingToDo);
    }

    /// <summary>Performs what <see cref="PreviewUse"/> names, or returns its rejection.</summary>
    public ActionResult Use(GridPosition? target, HeldItem held)
    {
        var preview = PreviewUse(target, held);
        return preview.Action switch
        {
            UseActionKind.Till => Till(target!.Value),
            _ => ActionResult.Rejected(preview.Rejection!.Value),
        };
    }

    /// <summary>Tills a Tile, spending the tuning's tool energy cost (BR-P2). Raises the change events on success.</summary>
    public ActionResult Till(GridPosition position)
    {
        var result = Game.Till(position, Data.Tuning.ToolEnergyCost);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            PlayerChanged?.Invoke();
        }

        return result;
    }

    private static UsePreview Rejected(Rejection rejection) => new(null, rejection);
}
