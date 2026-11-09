using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Farming;

/// <summary>
/// A fence or chest set down on the Farm plot (child of FarmPlot, BR-F1): its Tile, what it is, and for a chest
/// exactly 36 Slots shaped like Backpack Slots (BR-F2, BR-K2, no tools); a fence has none.
/// </summary>
public sealed class PlacedObject
{
    public PlacedObject(GridPosition position, PlaceableId placeable, IEnumerable<BackpackSlot>? slots = null)
    {
        throw new NotImplementedException();
    }

    public GridPosition Position { get; }

    public PlaceableId Placeable { get; }

    /// <summary>A chest's 36 Slots by index; empty for a fence.</summary>
    public IReadOnlyList<BackpackSlot> Slots => throw new NotImplementedException();
}
