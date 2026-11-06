using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Shipping;

/// <summary>
/// The Shipping Bin: at most one item per Crop type, ordered by <see cref="CropTypeId"/>. Nothing can be taken back
/// out (BR-S1); the Night pays for everything in it and empties it (BR-S2).
/// </summary>
public sealed class ShippingBin
{
    public IReadOnlyList<ShippedItem> Items => throw new NotImplementedException();

    public void Add(CropTypeId cropTypeId, int quantity) => throw new NotImplementedException();

    public void Empty() => throw new NotImplementedException();
}
