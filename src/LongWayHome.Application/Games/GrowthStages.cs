using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Games;

/// <summary>Maps a Planting's days grown to the <see cref="GrowthStage"/> it is drawn as.</summary>
public static class GrowthStages
{
    /// <summary>
    /// With d = DaysGrown and G = GrowDays: Ripe when d == G; else Seed when d == 0; else Sprout when 2d &lt; G;
    /// else Young. Throws <see cref="ArgumentException"/> when the Crop type is not the Planting's.
    /// </summary>
    public static GrowthStage For(Planting planting, CropType cropType) => throw new NotImplementedException();
}
