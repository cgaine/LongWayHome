using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Shipping;

/// <summary>One Crop type's quantity in the Shipping Bin. Quantity is at least 1.</summary>
public sealed record ShippedItem(CropTypeId CropTypeId, int Quantity)
{
    public int Quantity { get; } = Validate(Quantity);

    private static int Validate(int quantity) => throw new NotImplementedException();
}
