using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Farming;

/// <summary>
/// A fence or chest set down on the Farm plot (child of FarmPlot, BR-F1): its Tile, what it is, and for a chest
/// exactly 36 Slots shaped like Backpack Slots (BR-F2, BR-K2, no tools); a fence has none.
/// </summary>
public sealed class PlacedObject
{
    /// <summary>The Slots a chest has (BR-F2).</summary>
    public const int ChestSlotCount = 36;

    // BR-K2, BR-K3: the same slot filling as the Backpack.
    private readonly SlotStore _store;

    /// <summary>
    /// A Placed object on <paramref name="position"/>. A chest takes exactly one valid, tool-free Slot per index 0–35,
    /// or 36 empty ones when <paramref name="slots"/> is null; a fence takes none. Anything else throws.
    /// </summary>
    public PlacedObject(GridPosition position, PlaceableId placeable, IEnumerable<BackpackSlot>? slots = null)
    {
        if (!Enum.IsDefined(placeable))
        {
            throw new ArgumentOutOfRangeException(nameof(placeable), placeable, "Unknown placeable.");
        }

        Position = position;
        Placeable = placeable;
        _store = new SlotStore(placeable == PlaceableId.Chest ? ChestSlots(slots) : FenceSlots(slots));
    }

    public GridPosition Position { get; }

    public PlaceableId Placeable { get; }

    /// <summary>A chest's 36 Slots by index; empty for a fence.</summary>
    public IReadOnlyList<BackpackSlot> Slots => _store.Slots.AsReadOnly();

    /// <summary>Whether every Slot is empty (always, for a fence).</summary>
    public bool IsEmpty => _store.Slots.TrueForAll(slot => slot.Item is null);

    /// <summary>Adds a non-tool stack to a chest's Slots (BR-K3); returns what fitted nowhere.</summary>
    internal int Store(ItemRef item, int quantity) => _store.Add(item, quantity);

    /// <summary>Takes <paramref name="quantity"/> from the stack in chest Slot <paramref name="index"/>.</summary>
    internal void TakeFrom(int index, int quantity) => _store.TakeFrom(index, quantity);

    private static List<BackpackSlot> FenceSlots(IEnumerable<BackpackSlot>? slots)
    {
        if (slots is not null && slots.Any())
        {
            throw new ArgumentException("A fence has no Slots (BR-F2).", nameof(slots));
        }

        return [];
    }

    private static List<BackpackSlot> ChestSlots(IEnumerable<BackpackSlot>? slots)
    {
        if (slots is null)
        {
            return Enumerable.Range(0, ChestSlotCount).Select(SlotStore.EmptySlot).ToList();
        }

        var byIndex = new BackpackSlot?[ChestSlotCount];
        foreach (var slot in slots)
        {
            ArgumentNullException.ThrowIfNull(slot, nameof(slots));
            if (slot.Index is < 0 or >= ChestSlotCount)
            {
                throw new ArgumentException(
                    $"Chest slot index {slot.Index} is outside 0..{ChestSlotCount - 1} (BR-F2).", nameof(slots));
            }

            if (byIndex[slot.Index] is not null)
            {
                throw new ArgumentException($"Chest slot index {slot.Index} appears more than once.", nameof(slots));
            }

            // BR-K2; BR-K6, BR-F2: a tool is never stored in a chest.
            SlotStore.ValidateSlot(slot);
            if (slot.Item?.Kind == ItemKind.Tool)
            {
                throw new ArgumentException($"Chest slot {slot.Index} holds a tool (BR-K6).", nameof(slots));
            }

            byIndex[slot.Index] = slot;
        }

        var missing = Array.FindIndex(byIndex, slot => slot is null);
        if (missing >= 0)
        {
            throw new ArgumentException(
                $"Chest slot index {missing} is missing; a chest has {ChestSlotCount} slots (BR-F2).", nameof(slots));
        }

        return byIndex.Select(slot => slot!).ToList();
    }
}
