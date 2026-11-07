using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Games;

/// <summary>
/// Items on the ground at a position (child of <see cref="Game"/>): never a tool (BR-K6), 1–99 of one item (BR-K2).
/// Picked up while there is room; every one is removed in the Night (BR-K4).
/// </summary>
public sealed class DroppedItem
{
    /// <summary>The deepest mine level.</summary>
    public const int DeepestMineLevel = 5;

    private int _quantity;

    /// <summary>
    /// A DroppedItem: <paramref name="item"/> is a valid non-tool item (BR-K6), <paramref name="quantity"/> is 1–99
    /// (BR-K2), and <paramref name="mineLevel"/> is 1–5 when <paramref name="area"/> is the mine and null otherwise.
    /// Anything else throws.
    /// </summary>
    public DroppedItem(AreaId area, int? mineLevel, GridPosition position, ItemRef item, int quantity)
    {
        if (!Enum.IsDefined(area))
        {
            throw new ArgumentOutOfRangeException(nameof(area), area, "Unknown area.");
        }

        if (area == AreaId.Mine
            ? mineLevel is not (>= 1 and <= DeepestMineLevel)
            : mineLevel is not null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mineLevel),
                mineLevel,
                $"A mine level is 1–{DeepestMineLevel} in the mine and empty everywhere else.");
        }

        // BR-K6: a tool never leaves the Backpack, so it never lies on the ground.
        if (item.Kind == ItemKind.Tool)
        {
            throw new ArgumentException("A tool can't be dropped (BR-K6).", nameof(item));
        }

        if (!item.IsValid)
        {
            throw new ArgumentException($"The item's id doesn't match its kind {item.Kind}.", nameof(item));
        }

        Area = area;
        MineLevel = mineLevel;
        Position = position;
        Item = item;
        Quantity = quantity;
    }

    public AreaId Area { get; }

    /// <summary>1–5 when dropped in the mine; always null until the mine exists.</summary>
    public int? MineLevel { get; }

    public GridPosition Position { get; }

    public ItemRef Item { get; }

    /// <summary>1–99 (BR-K2); lowered only by a partial pick-up (BR-K4). Outside 1–99 throws.</summary>
    public int Quantity
    {
        get => _quantity;
        internal set
        {
            // BR-K2
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(Quantity));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Backpack.MaxStack, nameof(Quantity));
            _quantity = value;
        }
    }
}
