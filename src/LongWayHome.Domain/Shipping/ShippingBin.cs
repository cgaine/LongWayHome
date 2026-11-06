using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Shipping;

/// <summary>
/// The Shipping Bin: at most one item per Crop type, ordered by <see cref="CropTypeId"/>. Nothing can be taken back
/// out (BR-S1); the Night pays for everything in it and empties it (BR-S2).
/// </summary>
public sealed class ShippingBin
{
    // Keyed by Crop type, so there is at most one item per type; the sorted key gives the Items order.
    private readonly SortedDictionary<CropTypeId, ShippedItem> _items = new();

    /// <summary>The shipped items, one per Crop type, ordered by <see cref="CropTypeId"/>.</summary>
    public IReadOnlyList<ShippedItem> Items => _items.Values.ToList().AsReadOnly();

    /// <summary>
    /// Adds a quantity of a Crop type, merging into its existing item (BR-S1). A quantity below 1 or a total past
    /// <see cref="int.MaxValue"/> throws and changes nothing.
    /// </summary>
    public void Add(CropTypeId cropTypeId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        var held = _items.TryGetValue(cropTypeId, out var item) ? item.Quantity : 0;
        _items[cropTypeId] = new ShippedItem(cropTypeId, checked(held + quantity));
    }

    /// <summary>Removes every item; only the Night's payout calls this (BR-S2).</summary>
    public void Empty() => _items.Clear();
}
