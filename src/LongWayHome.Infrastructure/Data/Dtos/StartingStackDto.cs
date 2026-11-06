using LongWayHome.Domain.Catalog;

namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record StartingStackDto
{
    public required CropTypeId CropTypeId { get; init; }

    public required int Quantity { get; init; }
}
