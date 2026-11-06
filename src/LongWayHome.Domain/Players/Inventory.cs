using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Players;

/// <summary>The Player's inventory stacks: at most one stack per (kind, Crop type), each of quantity ≥ 1 (BR-P7).</summary>
public sealed class Inventory
{
    // BR-P7: keyed by (kind, Crop type), so there is at most one stack per key. The sorted key gives the
    // Stacks order: ItemKind, then CropTypeId, both in enum order.
    private readonly SortedDictionary<(ItemKind Kind, CropTypeId CropTypeId), InventoryStack> _stacks = new();

    /// <summary>Stacks ordered by <see cref="ItemKind"/> then <see cref="CropTypeId"/>.</summary>
    public IReadOnlyList<InventoryStack> Stacks => _stacks.Values.ToList().AsReadOnly();

    /// <summary>The quantity held of a kind and Crop type; 0 when there is no such stack.</summary>
    public int Quantity(ItemKind kind, CropTypeId cropTypeId) =>
        _stacks.TryGetValue((kind, cropTypeId), out var stack) ? stack.Quantity : 0;

    /// <summary>Adds items, merging into the existing stack of that kind and Crop type or creating one (BR-P7).</summary>
    public void Add(ItemKind kind, CropTypeId cropTypeId, int quantity)
    {
        // BR-P7
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        var total = checked(Quantity(kind, cropTypeId) + quantity);
        _stacks[(kind, cropTypeId)] = new InventoryStack(kind, cropTypeId, total);
    }

    /// <summary>
    /// Removes items. Returns <c>false</c> and changes nothing when fewer than <paramref name="quantity"/> are held;
    /// removes the stack when it reaches 0 (BR-P7).
    /// </summary>
    public bool Remove(ItemKind kind, CropTypeId cropTypeId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        var held = Quantity(kind, cropTypeId);
        if (held < quantity)
        {
            return false;
        }

        // BR-P7: a stack at 0 is removed.
        var remaining = held - quantity;
        if (remaining == 0)
        {
            _stacks.Remove((kind, cropTypeId));
        }
        else
        {
            _stacks[(kind, cropTypeId)] = new InventoryStack(kind, cropTypeId, remaining);
        }

        return true;
    }
}
