using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Players;

/// <summary>A stack of one kind of item of one Crop type. Quantity is at least 1 (BR-P7).</summary>
public sealed record InventoryStack
{
    public InventoryStack(ItemKind kind, CropTypeId cropTypeId, int quantity)
    {
        // BR-P7
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        Kind = kind;
        CropTypeId = cropTypeId;
        Quantity = quantity;
    }

    public ItemKind Kind { get; }

    public CropTypeId CropTypeId { get; }

    public int Quantity { get; }
}
