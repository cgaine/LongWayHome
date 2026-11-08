using LongWayHome.Domain.Catalog;

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

    private readonly List<BackpackSlot> _slots;

    /// <summary>A new Backpack of 12 empty Slots on hotbar row 0.</summary>
    public Backpack()
    {
        _slots = Enumerable.Range(0, StartingCapacity).Select(EmptySlot).ToList();
        HotbarRow = 0;
    }

    /// <summary>
    /// Restores a Backpack: <paramref name="capacity"/> is 12, 24 or 36 (BR-K1); <paramref name="slots"/> holds exactly
    /// one Slot per index 0..capacity − 1, each valid (BR-K2); <paramref name="hotbarRow"/> is 0..capacity / 6 − 1; the
    /// hoe and the watering can are each held exactly once (BR-K6). Anything else throws.
    /// </summary>
    public Backpack(int capacity, int hotbarRow, IEnumerable<BackpackSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);

        // BR-K1
        if (!IsCapacity(capacity))
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity), capacity, "A Backpack has 12, 24 or 36 Slots (BR-K1).");
        }

        if (hotbarRow < 0 || hotbarRow >= capacity / RowLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hotbarRow), hotbarRow, $"The hotbar row must be between 0 and {(capacity / RowLength) - 1}.");
        }

        var byIndex = new BackpackSlot?[capacity];
        foreach (var slot in slots)
        {
            ArgumentNullException.ThrowIfNull(slot, nameof(slots));
            if (slot.Index < 0 || slot.Index >= capacity)
            {
                throw new ArgumentException(
                    $"Slot index {slot.Index} is outside 0..{capacity - 1} (BR-K1).", nameof(slots));
            }

            if (byIndex[slot.Index] is not null)
            {
                throw new ArgumentException($"Slot index {slot.Index} appears more than once.", nameof(slots));
            }

            // BR-K2
            ValidateSlot(slot);
            byIndex[slot.Index] = slot;
        }

        var missing = Array.FindIndex(byIndex, slot => slot is null);
        if (missing >= 0)
        {
            throw new ArgumentException($"Slot index {missing} is missing; there must be {capacity} Slots.", nameof(slots));
        }

        _slots = byIndex.Select(slot => slot!).ToList();

        // BR-K6: the hoe and the watering can never leave the Backpack, so each is held exactly once; BR-K2: no tool
        // is held twice.
        foreach (var tool in Enum.GetValues<ToolId>())
        {
            var held = _slots.Count(slot => slot.Item == ItemRef.Tool(tool));
            var required = tool is ToolId.Hoe or ToolId.WateringCan;
            if (held > 1 || (required && held != 1))
            {
                throw new ArgumentException(
                    $"The Backpack must hold the {tool} exactly once, not {held} times (BR-K6).", nameof(slots));
            }
        }

        HotbarRow = hotbarRow;
    }

    public int Capacity => _slots.Count;

    /// <summary>Exactly <see cref="Capacity"/> Slots, by index.</summary>
    public IReadOnlyList<BackpackSlot> Slots => _slots.AsReadOnly();

    /// <summary>The row of <see cref="RowLength"/> Slots shown as the hotbar; changes only through <see cref="CycleHotbar"/>.</summary>
    public int HotbarRow { get; private set; }

    /// <summary>The six Slots of the current hotbar row: indexes HotbarRow × 6 to HotbarRow × 6 + 5 (BR-K5).</summary>
    public IReadOnlyList<BackpackSlot> HotbarSlots => _slots.GetRange(HotbarRow * RowLength, RowLength).AsReadOnly();

    /// <summary>
    /// The Slot at <paramref name="position"/> (0–5) of the current hotbar row (BR-K5), without copying the row; a
    /// position outside 0–5 throws.
    /// </summary>
    public BackpackSlot HotbarSlotAt(int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, RowLength);
        return _slots[(HotbarRow * RowLength) + position];
    }

    /// <summary>Moves the hotbar to the next row, wrapping back to row 0 after row Capacity / 6 − 1 (BR-K5).</summary>
    public void CycleHotbar() => HotbarRow = (HotbarRow + 1) % (Capacity / RowLength);

    /// <summary>
    /// Removes <paramref name="quantity"/> from one Slot, emptying it when it reaches 0; false and no change when the
    /// Slot holds fewer, or holds a tool (BR-K6).
    /// </summary>
    public bool RemoveFrom(int slotIndex, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slotIndex, Capacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        var slot = _slots[slotIndex];

        // BR-K6: a tool never leaves the Backpack; an empty Slot or one holding fewer gives nothing.
        if (slot.Item is not { } item || item.Kind == ItemKind.Tool || slot.Quantity < quantity)
        {
            return false;
        }

        // BR-K2: a Slot at 0 is empty.
        _slots[slotIndex] = slot.Quantity == quantity
            ? EmptySlot(slotIndex)
            : slot with { Quantity = slot.Quantity - quantity };
        return true;
    }

    /// <summary>
    /// Adds a non-tool item: tops up Slots holding it below 99 in index order, then fills free Slots in index order
    /// (BR-K3). Returns the quantity that fitted nowhere. A quantity below 1 throws.
    /// </summary>
    public int Add(ItemRef item, int quantity)
    {
        RequireItem(item, quantity);

        var left = quantity;

        // BR-K3: first onto Slots of the same item below 99, lowest index first.
        for (var index = 0; index < _slots.Count && left > 0; index++)
        {
            var slot = _slots[index];
            if (slot.Item == item && slot.Quantity < MaxStack)
            {
                var placed = Math.Min(MaxStack - slot.Quantity, left);
                _slots[index] = slot with { Quantity = slot.Quantity + placed };
                left -= placed;
            }
        }

        // BR-K3, BR-K2: then into the lowest free Slots, at most 99 each.
        for (var index = 0; index < _slots.Count && left > 0; index++)
        {
            if (_slots[index].Item is null)
            {
                var placed = Math.Min(MaxStack, left);
                _slots[index] = new BackpackSlot(index, item, placed, null);
                left -= placed;
            }
        }

        return left;
    }

    /// <summary>Whether <see cref="Add"/> would place the whole quantity, changing nothing.</summary>
    public bool CanFit(ItemRef item, int quantity)
    {
        RequireItem(item, quantity);

        var room = _slots.Sum(slot => slot.Item is null
            ? MaxStack
            : slot.Item == item ? MaxStack - slot.Quantity : 0);
        return room >= quantity;
    }

    /// <summary>
    /// Puts a tool alone in the lowest free Slot; false and no change when there is none (BR-K2). A tool already held
    /// throws, so the Backpack never holds it twice.
    /// </summary>
    public bool AddTool(ToolId tool, ToolTier tier)
    {
        if (TierOf(tool) is not null)
        {
            throw new InvalidOperationException($"The {tool} is already in the Backpack.");
        }

        // BR-K2: a tool is alone in its Slot, with quantity 1 and its tier.
        var free = _slots.FindIndex(slot => slot.Item is null);
        if (free < 0)
        {
            return false;
        }

        _slots[free] = new BackpackSlot(free, ItemRef.Tool(tool), 1, tier);
        return true;
    }

    /// <summary>The total held of an item across all Slots.</summary>
    public int Quantity(ItemRef item) => _slots.Where(slot => slot.Item == item).Sum(slot => slot.Quantity);

    /// <summary>
    /// The total held per Crop type of a seed or crop kind, across all Slots, in <see cref="CropTypeId"/> order; Crop
    /// types not held are absent.
    /// </summary>
    public IReadOnlyList<(CropTypeId CropTypeId, int Quantity)> Totals(ItemKind kind) =>
        _slots
            .Where(slot => slot.Item is { } item && item.Kind == kind && item.CropTypeId is not null)
            .GroupBy(slot => slot.Item!.Value.CropTypeId!.Value)
            .OrderBy(group => group.Key)
            .Select(group => (group.Key, group.Sum(slot => slot.Quantity)))
            .ToList();

    /// <summary>
    /// Removes a non-tool item from the highest-index Slots first, emptying Slots that reach 0; false and no change
    /// when fewer are held. A tool throws <see cref="InvalidOperationException"/> (BR-K6).
    /// </summary>
    public bool Remove(ItemRef item, int quantity)
    {
        // BR-K6: tools never leave the Backpack.
        if (item.Kind == ItemKind.Tool)
        {
            throw new InvalidOperationException("A tool can't be removed from the Backpack (BR-K6).");
        }

        RequireItem(item, quantity);

        if (Quantity(item) < quantity)
        {
            return false;
        }

        var left = quantity;
        for (var index = _slots.Count - 1; index >= 0 && left > 0; index--)
        {
            var slot = _slots[index];
            if (slot.Item != item)
            {
                continue;
            }

            var taken = Math.Min(slot.Quantity, left);
            left -= taken;

            // BR-K2: a Slot at 0 is empty.
            _slots[index] = slot.Quantity == taken ? EmptySlot(index) : slot with { Quantity = slot.Quantity - taken };
        }

        return true;
    }

    /// <summary>The tier of a held tool, or null when it is not held.</summary>
    public ToolTier? TierOf(ToolId tool) =>
        _slots.FirstOrDefault(slot => slot.Item == ItemRef.Tool(tool))?.ToolTier;

    /// <summary>Raises a held tool's tier; a lower tier throws.</summary>
    public void SetTier(ToolId tool, ToolTier tier)
    {
        var index = _slots.FindIndex(slot => slot.Item == ItemRef.Tool(tool));
        if (index < 0)
        {
            throw new InvalidOperationException($"The {tool} is not in the Backpack.");
        }

        var slot = _slots[index];
        if (tier < slot.ToolTier)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tier), tier, $"The {tool}'s tier only goes up; it is {slot.ToolTier}.");
        }

        _slots[index] = slot with { ToolTier = tier };
    }

    /// <summary>
    /// Moves the contents of Slot <paramref name="from"/> onto Slot <paramref name="to"/>: into an empty Slot (move),
    /// onto the same non-tool item up to 99 (merge), or else swapping the two (BR-K2, BR-K6). False and no change when
    /// nothing moved; an index outside 0..Capacity − 1 throws.
    /// </summary>
    public bool Move(int from, int to)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(from);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(from, Capacity);
        ArgumentOutOfRangeException.ThrowIfNegative(to);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(to, Capacity);

        var source = _slots[from];
        if (from == to || source.Item is not { } item)
        {
            return false;
        }

        var target = _slots[to];

        // BR-K2: onto the same non-tool item, the target fills to at most 99 and the source keeps the rest, emptying
        // at 0. Tools are never merged (a tool is always alone in its Slot).
        if (target.Item == item && item.Kind != ItemKind.Tool)
        {
            var moved = Math.Min(MaxStack - target.Quantity, source.Quantity);
            if (moved <= 0)
            {
                return false;
            }

            _slots[to] = target with { Quantity = target.Quantity + moved };
            _slots[from] = source.Quantity == moved
                ? EmptySlot(from)
                : source with { Quantity = source.Quantity - moved };
            return true;
        }

        // BR-K2, BR-K6: otherwise the contents (item, quantity, tool tier) move or swap; each Slot keeps its index, and
        // a tool keeps its tier and stays in the Backpack. An empty target is a plain move.
        _slots[to] = source with { Index = to };
        _slots[from] = target with { Index = from };
        return true;
    }

    /// <summary>Raises the capacity to 24 or 36, appending empty Slots; never down (BR-K1).</summary>
    public void RaiseCapacity(int capacity)
    {
        // BR-K1
        if (!IsCapacity(capacity) || capacity <= Capacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                $"The capacity only rises to 24 or 36, above the current {Capacity} (BR-K1).");
        }

        for (var index = Capacity; index < capacity; index++)
        {
            _slots.Add(EmptySlot(index));
        }
    }

    private static bool IsCapacity(int capacity) => capacity is 12 or 24 or 36;

    private static BackpackSlot EmptySlot(int index) => new(index, null, 0, null);

    private static void RequireItem(ItemRef item, int quantity)
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

    private static void ValidateSlot(BackpackSlot slot)
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
