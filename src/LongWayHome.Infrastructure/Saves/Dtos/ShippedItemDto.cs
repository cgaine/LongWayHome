using LongWayHome.Domain.Catalog;

namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record ShippedItemDto
{
    public required CropTypeId CropTypeId { get; init; }

    public required int Quantity { get; init; }
}
