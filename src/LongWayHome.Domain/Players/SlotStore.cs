namespace LongWayHome.Domain.Players;

/// <summary>
/// The slot filling shared by the Backpack and a placed chest (BR-K2, BR-K3): a list of Slots, each empty, 1–99 of one
/// item, or one tool with its tier. Gained items top up Slots of the same item below 99 in index order, then fill the
/// lowest free Slots.
/// </summary>
internal sealed class SlotStore
{
    /// <summary>The most of one item a Slot holds (BR-K2).</summary>
    public const int MaxStack = 99;

    public SlotStore(List<BackpackSlot> slots) => Slots = slots;

    /// <summary>The Slots by index, changed in place.</summary>
    public List<BackpackSlot> Slots { get; }

    /// <summary>
    /// Adds a non-tool item: tops up Slots holding it below 99 in index order, then fills free Slots in index order
    /// (BR-K3). Returns the quantity that fitted nowhere.
    /// </summary>
    public int Add(ItemRef item, int quantity)
    {
        RequireItem(item, quantity);

        var left = quantity;

        // BR-K3: first onto Slots of the same item below 99, lowest index first.
        for (var index = 0; index < Slots.Count && left > 0; index++)
        {
            var slot = Slots[index];
            if (slot.Item == item && slot.Quantity < MaxStack)
            {
                var placed = Math.Min(MaxStack - slot.Quantity, left);
                Slots[index] = slot with { Quantity = slot.Quantity + placed };
                left -= placed;
            }
        }

        // BR-K3, BR-K2: then into the lowest free Slots, at most 99 each.
        for (var index = 0; index < Slots.Count && left > 0; index++)
        {
            if (Slots[index].Item is null)
            {
                var placed = Math.Min(MaxStack, left);
                Slots[index] = new BackpackSlot(index, item, placed, null);
                left -= placed;
            }
        }

        return left;
    }

    /// <summary>Whether <see cref="Add"/> would place the whole quantity, changing nothing.</summary>
    public bool CanFit(ItemRef item, int quantity)
    {
        RequireItem(item, quantity);

        var room = Slots.Sum(slot => slot.Item is null
            ? MaxStack
            : slot.Item == item ? MaxStack - slot.Quantity : 0);
        return room >= quantity;
    }

    /// <summary>
    /// Takes <paramref name="quantity"/> (1 to its quantity) from the non-tool stack in Slot <paramref name="index"/>,
    /// emptying the Slot at 0 (BR-K2).
    /// </summary>
    public void TakeFrom(int index, int quantity)
    {
        var slot = Slots[index];
        Slots[index] = slot.Quantity == quantity ? EmptySlot(index) : slot with { Quantity = slot.Quantity - quantity };
    }

    public static BackpackSlot EmptySlot(int index) => new(index, null, 0, null);

    /// <summary>Throws for a tool (it goes in alone, BR-K2), an item whose id doesn't match its kind, or a quantity below 1.</summary>
    public static void RequireItem(ItemRef item, int quantity)
    {
        if (item.Kind == ItemKind.Tool)
        {
            throw new ArgumentException("A tool goes in through AddTool, never as a stack (BR-K2).", nameof(item));
        }

        if (!item.IsValid)
        {
            throw new ArgumentException($"The item's id doesn't match its kind {item.Kind}.", nameof(item));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
    }

    /// <summary>Throws <see cref="ArgumentException"/> unless the Slot is empty, 1–99 of a valid item, or one tool with its tier (BR-K2).</summary>
    public static void ValidateSlot(BackpackSlot slot)
    {
        // BR-K2
        if (slot.Item is not { } item)
        {
            if (slot.Quantity != 0 || slot.ToolTier is not null)
            {
                throw new ArgumentException($"Empty Slot {slot.Index} must have quantity 0 and no tier (BR-K2).");
            }

            return;
        }

        if (!item.IsValid)
        {
            throw new ArgumentException($"Slot {slot.Index} holds an item whose id doesn't match its kind {item.Kind}.");
        }

        if (item.Kind == ItemKind.Tool)
        {
            if (slot.Quantity != 1 || slot.ToolTier is null)
            {
                throw new ArgumentException($"Tool Slot {slot.Index} must have quantity 1 and a tier (BR-K2).");
            }

            return;
        }

        if (slot.Quantity is < 1 or > MaxStack || slot.ToolTier is not null)
        {
            throw new ArgumentException(
                $"Slot {slot.Index} must hold 1 to {MaxStack} of its item and no tier (BR-K2).");
        }
    }
}
