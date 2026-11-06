using LongWayHome.Domain.Catalog;

namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record CropTypeDto
{
    public required CropTypeId Id { get; init; }

    public required string Name { get; init; }

    public required int GrowDays { get; init; }

    public required int SeedCost { get; init; }

    public required int SellPrice { get; init; }

    public required Season Season { get; init; }

    public required bool Regrows { get; init; }
}
