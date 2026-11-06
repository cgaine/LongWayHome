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
    public static GrowthStage For(Planting planting, CropType cropType)
    {
        ArgumentNullException.ThrowIfNull(planting);

        // Ripe comes from the Planting's own status, so the drawing can't drift from BR-T6's ripe test.
        if (planting.StatusFor(cropType) == PlantingStatus.Ripe)
        {
            return GrowthStage.Ripe;
        }

        var daysGrown = planting.DaysGrown;
        if (daysGrown == 0)
        {
            return GrowthStage.Seed;
        }

        return 2 * daysGrown < cropType.GrowDays ? GrowthStage.Sprout : GrowthStage.Young;
    }
}
