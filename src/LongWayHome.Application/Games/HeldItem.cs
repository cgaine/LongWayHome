using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>What the Player holds: a tool or a seed type.</summary>
public sealed record HeldItem(ToolId? Tool, CropTypeId? Seed)
{
    public static HeldItem Of(ToolId tool) => new(tool, null);

    public static HeldItem SeedOf(CropTypeId seed) => new(null, seed);

    /// <summary>The display label from canonical-values.md: "Hoe", "Watering can", or "&lt;Crop type&gt; seeds".</summary>
    public string DisplayLabel => (Tool, Seed) switch
    {
        (ToolId.Hoe, null) => "Hoe",
        (ToolId.WateringCan, null) => "Watering can",
        (null, { } seed) => $"{CropLabel(seed)} seeds",
        _ => throw new InvalidOperationException("A held item is exactly one tool or one seed type."),
    };

    private static string CropLabel(CropTypeId seed) => seed switch
    {
        CropTypeId.Emberroot => "Emberroot",
        CropTypeId.Dusktuber => "Dusktuber",
        CropTypeId.Moonmelon => "Moonmelon",
        CropTypeId.Sunbell => "Sunbell",
        CropTypeId.Starcorn => "Starcorn",
        _ => throw new ArgumentOutOfRangeException(nameof(seed), seed, "Unknown Crop type."),
    };
}
