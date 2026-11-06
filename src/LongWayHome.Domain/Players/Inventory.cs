using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Players;

/// <summary>The Player's inventory stacks: at most one stack per (kind, Crop type), each of quantity ≥ 1 (BR-P7).</summary>
public sealed class Inventory
{
    /// <summary>Stacks ordered by <see cref="ItemKind"/> then <see cref="CropTypeId"/>.</summary>
    public IReadOnlyList<InventoryStack> Stacks => throw new NotImplementedException();

    public int Quantity(ItemKind kind, CropTypeId cropTypeId) => throw new NotImplementedException();

    public void Add(ItemKind kind, CropTypeId cropTypeId, int quantity) => throw new NotImplementedException();

    public bool Remove(ItemKind kind, CropTypeId cropTypeId, int quantity) => throw new NotImplementedException();
}
