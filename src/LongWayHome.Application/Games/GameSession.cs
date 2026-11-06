using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
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

    /// <summary>Raised after a successful action changes the inventory.</summary>
    public event Action? InventoryChanged;

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

        // BR-T6: harvesting needs no tool, so a Ripe Planting is harvested whatever is held.
        if (IsRipe(Game.FarmPlot[position]))
        {
            return new UsePreview(UseActionKind.Harvest, null);
        }

        if (held.Seed is { } seed)
        {
            // BR-T3: the domain decides, so the preview and Game.Plant cannot drift apart.
            return Game.PlantRejection(position, seed) is { } rejection
                ? Rejected(rejection)
                : new UsePreview(UseActionKind.Plant, null);
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

        // The can gets its action in the watering feature.
        return Rejected(Rejection.NothingToDo);
    }

    /// <summary>Performs what <see cref="PreviewUse"/> names, or returns its rejection.</summary>
    public ActionResult Use(GridPosition? target, HeldItem held)
    {
        var preview = PreviewUse(target, held);
        return preview.Action switch
        {
            UseActionKind.Till => Till(target!.Value),
            UseActionKind.Plant => Plant(target!.Value, held.Seed!.Value),
            UseActionKind.Harvest => Harvest(target!.Value),
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

    /// <summary>Plants one Seed of a Crop type (BR-T3). Raises InventoryChanged and TileChanged on success.</summary>
    public ActionResult Plant(GridPosition position, CropTypeId cropTypeId)
    {
        var result = Game.Plant(position, cropTypeId);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            InventoryChanged?.Invoke();
        }

        return result;
    }

    /// <summary>
    /// Harvests the ripe Planting on a Tile (BR-T6), looking up its Crop type in <see cref="GameData.CropTypes"/>.
    /// Raises InventoryChanged and TileChanged on success. A Planting whose Crop type has no data is NotRipe.
    /// </summary>
    public ActionResult Harvest(GridPosition position)
    {
        if (Game.FarmPlot[position].Planting is not { } planting)
        {
            return ActionResult.Rejected(Rejection.NoPlanting);
        }

        if (!Data.CropTypes.TryGetValue(planting.CropTypeId, out var cropType))
        {
            return ActionResult.Rejected(Rejection.NotRipe);
        }

        var result = Game.Harvest(position, cropType);
        if (result.IsOk)
        {
            TileChanged?.Invoke(position);
            InventoryChanged?.Invoke();
        }

        return result;
    }

    /// <summary>The HUD label for a held item: "Hoe", "Watering can", or "Emberroot seeds × 9".</summary>
    public string HeldLabel(HeldItem held)
    {
        ArgumentNullException.ThrowIfNull(held);

        return held.Seed is { } seed
            ? $"{held.DisplayLabel} × {Game.Player.Inventory.Quantity(ItemKind.Seed, seed)}"
            : held.DisplayLabel;
    }

    /// <summary>Whether a Tile holds a Ripe Planting. A Crop type with no data row is never Ripe.</summary>
    private bool IsRipe(Tile tile) =>
        tile.Planting is { } planting
        && Data.CropTypes.TryGetValue(planting.CropTypeId, out var cropType)
        && planting.StatusFor(cropType) == PlantingStatus.Ripe;

    private static UsePreview Rejected(Rejection rejection) => new(null, rejection);
}
