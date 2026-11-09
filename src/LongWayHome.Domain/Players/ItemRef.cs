using LongWayHome.Domain.Catalog;

namespace LongWayHome.Domain.Players;

/// <summary>
/// What a Backpack Slot holds: a kind plus exactly one id that matches it — a Crop type for <c>seed</c> and
/// <c>crop</c>, a tool for <c>tool</c>, a material for <c>material</c>.
/// </summary>
public readonly record struct ItemRef(ItemKind Kind, CropTypeId? CropTypeId, ToolId? ToolId, MaterialId? MaterialId = null, PlaceableId? PlaceableId = null)
{
    public static ItemRef Seed(CropTypeId cropTypeId) => new(ItemKind.Seed, cropTypeId, null);

    public static ItemRef Crop(CropTypeId cropTypeId) => new(ItemKind.Crop, cropTypeId, null);

    public static ItemRef Tool(ToolId toolId) => new(ItemKind.Tool, null, toolId);

    public static ItemRef Material(MaterialId materialId) => new(ItemKind.Material, null, null, materialId);

    public static ItemRef Placeable(PlaceableId placeableId) => new(ItemKind.Placeable, null, null, null, placeableId);

    /// <summary>Whether exactly one id is set and it matches the kind.</summary>
    public bool IsValid => Kind switch
    {
        ItemKind.Seed or ItemKind.Crop => CropTypeId is not null && ToolId is null && MaterialId is null,
        ItemKind.Tool => ToolId is not null && CropTypeId is null && MaterialId is null,
        ItemKind.Material => MaterialId is not null && CropTypeId is null && ToolId is null,
        ItemKind.Placeable => PlaceableId is not null && CropTypeId is null && ToolId is null && MaterialId is null,
        _ => false,
    };
}
