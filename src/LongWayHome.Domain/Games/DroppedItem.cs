using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Games;

/// <summary>
/// Items on the ground at a position (child of <see cref="Game"/>): never a tool (BR-K6), 1–99 of one item (BR-K2).
/// Picked up while there is room; every one is removed in the Night (BR-K4).
/// </summary>
public sealed class DroppedItem
{
    public DroppedItem(AreaId area, int? mineLevel, GridPosition position, ItemRef item, int quantity)
    {
        throw new NotImplementedException();
    }

    public AreaId Area { get; }

    /// <summary>Always null until the mine exists.</summary>
    public int? MineLevel { get; }

    public GridPosition Position { get; }

    public ItemRef Item { get; }

    public int Quantity { get; internal set; }
}
