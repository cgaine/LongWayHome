using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record InventoryStackDto
{
    public required ItemKind Kind { get; init; }

    public required CropTypeId CropTypeId { get; init; }

    public required int Quantity { get; init; }
}
