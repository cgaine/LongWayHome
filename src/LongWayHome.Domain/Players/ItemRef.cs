using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Players;

/// <summary>
/// What a Backpack Slot holds: a kind plus exactly one id that matches it — a Crop type for <c>seed</c> and
/// <c>crop</c>, a tool for <c>tool</c>.
/// </summary>
public readonly record struct ItemRef(ItemKind Kind, CropTypeId? CropTypeId, ToolId? ToolId)
{
    public static ItemRef Seed(CropTypeId cropTypeId) => new(ItemKind.Seed, cropTypeId, null);

    public static ItemRef Crop(CropTypeId cropTypeId) => new(ItemKind.Crop, cropTypeId, null);

    public static ItemRef Tool(ToolId toolId) => new(ItemKind.Tool, null, toolId);
}
