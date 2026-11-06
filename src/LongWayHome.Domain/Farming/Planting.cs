using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Domain.Farming;

/// <summary>A Crop growing on a tilled Tile.</summary>
public sealed record Planting(CropTypeId CropTypeId, GameDate PlantedOn, int DaysGrown)
{
    /// <summary>Ripe when DaysGrown equals the Crop type's growDays, otherwise Growing.</summary>
    public PlantingStatus StatusFor(CropType cropType) => throw new NotImplementedException();
}
