namespace LongWayHome.Domain.Players;

/// <summary>
/// The Player's Backpack: 12, 24 or 36 Slots (BR-K1), each empty, 1–99 of one item, or one tool with its tier
/// (BR-K2). Gained items top up matching stacks below 99, then fill the lowest free Slot (BR-K3). Tools never leave
/// it (BR-K6).
/// </summary>
public sealed class Backpack
{
    /// <summary>The most of one item a Slot holds (BR-K2).</summary>
    public const int MaxStack = 99;

    /// <summary>The capacity a Backpack starts at (BR-K1).</summary>
    public const int StartingCapacity = 12;

    /// <summary>The Slots in one hotbar row.</summary>
    public const int RowLength = 6;

    /// <summary>A new Backpack of 12 empty Slots on hotbar row 0.</summary>
    public Backpack() => throw new NotImplementedException();

    /// <summary>
    /// Restores a Backpack: <paramref name="capacity"/> is 12, 24 or 36 (BR-K1); <paramref name="slots"/> holds exactly
    /// one Slot per index 0..capacity − 1, each valid (BR-K2); <paramref name="hotbarRow"/> is 0..capacity / 6 − 1; the
    /// hoe and the watering can are each held exactly once (BR-K6). Anything else throws.
    /// </summary>
    public Backpack(int capacity, int hotbarRow, IEnumerable<BackpackSlot> slots) => throw new NotImplementedException();

    public int Capacity => throw new NotImplementedException();

    /// <summary>Exactly <see cref="Capacity"/> Slots, by index.</summary>
    public IReadOnlyList<BackpackSlot> Slots => throw new NotImplementedException();

    public int HotbarRow => throw new NotImplementedException();

    /// <summary>
    /// Adds a non-tool item: tops up Slots holding it below 99 in index order, then fills free Slots in index order
    /// (BR-K3). Returns the quantity that fitted nowhere. A quantity below 1 throws.
    /// </summary>
    public int Add(ItemRef item, int quantity) => throw new NotImplementedException();

    /// <summary>Whether <see cref="Add"/> would place the whole quantity, changing nothing.</summary>
    public bool CanFit(ItemRef item, int quantity) => throw new NotImplementedException();

    /// <summary>Puts a tool alone in the lowest free Slot; false and no change when there is none (BR-K2).</summary>
    public bool AddTool(ToolId tool, ToolTier tier) => throw new NotImplementedException();

    /// <summary>The total held of an item across all Slots.</summary>
    public int Quantity(ItemRef item) => throw new NotImplementedException();

    /// <summary>
    /// Removes a non-tool item from the highest-index Slots first, emptying Slots that reach 0; false and no change
    /// when fewer are held. A tool throws <see cref="InvalidOperationException"/> (BR-K6).
    /// </summary>
    public bool Remove(ItemRef item, int quantity) => throw new NotImplementedException();

    /// <summary>The tier of a held tool, or null when it is not held.</summary>
    public ToolTier? TierOf(ToolId tool) => throw new NotImplementedException();

    /// <summary>Raises a held tool's tier; a lower tier throws.</summary>
    public void SetTier(ToolId tool, ToolTier tier) => throw new NotImplementedException();

    /// <summary>Raises the capacity to 24 or 36, appending empty Slots; never down (BR-K1).</summary>
    public void RaiseCapacity(int capacity) => throw new NotImplementedException();
}
