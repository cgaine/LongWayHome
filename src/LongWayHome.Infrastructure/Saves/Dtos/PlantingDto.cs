using LongWayHome.Domain.Catalog;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>A Planting. Its status is derived from the Crop type's data and never stored.</summary>
internal sealed record PlantingDto
{
    public required CropTypeId CropTypeId { get; init; }

    public required GameDateDto PlantedOn { get; init; }

    public required int DaysGrown { get; init; }
}
