using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>The canonical display labels of Crop types (canonical-values.md), for crops with no data row yet.</summary>
public static class CropLabels
{
    /// <summary>"Emberroot", "Dusktuber", "Moonmelon", "Sunbell" or "Starcorn".</summary>
    public static string For(CropTypeId cropTypeId) => cropTypeId switch
    {
        CropTypeId.Emberroot => "Emberroot",
        CropTypeId.Dusktuber => "Dusktuber",
        CropTypeId.Moonmelon => "Moonmelon",
        CropTypeId.Sunbell => "Sunbell",
        CropTypeId.Starcorn => "Starcorn",
        _ => throw new ArgumentOutOfRangeException(nameof(cropTypeId), cropTypeId, "Unknown Crop type."),
    };
}
