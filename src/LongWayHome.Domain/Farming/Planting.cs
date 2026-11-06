using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Domain.Farming;

/// <summary>A Crop growing on a tilled Tile.</summary>
public sealed record Planting(CropTypeId CropTypeId, GameDate PlantedOn, int DaysGrown)
{
    private readonly int _daysGrown = NotNegative(DaysGrown);

    /// <summary>Days the Planting has grown; never negative, also through <c>with</c>.</summary>
    public int DaysGrown
    {
        get => _daysGrown;
        init => _daysGrown = NotNegative(value);
    }

    /// <summary>Ripe when DaysGrown equals the Crop type's growDays, otherwise Growing.</summary>
    public PlantingStatus StatusFor(CropType cropType)
    {
        ArgumentNullException.ThrowIfNull(cropType);
        if (cropType.Id != CropTypeId)
        {
            throw new ArgumentException($"The Planting is {CropTypeId}, not {cropType.Id}.", nameof(cropType));
        }

        return DaysGrown == cropType.GrowDays ? PlantingStatus.Ripe : PlantingStatus.Growing;
    }

    private static int NotNegative(int daysGrown)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysGrown, nameof(DaysGrown));
        return daysGrown;
    }
}
