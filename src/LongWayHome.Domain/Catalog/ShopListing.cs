using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Catalog;

/// <summary>
/// One item the Shop can sell. <see cref="CropTypeId"/> is set only for Seeds, <see cref="ToolId"/> only for a tool,
/// <see cref="PlaceableId"/> only for a placeable and <see cref="BackpackCapacity"/> (24 or 36) only for a backpack
/// upgrade; all are null for the Can upgrade.
/// </summary>
public sealed record ShopListing(
    string Id,
    ShopItemKind Kind,
    CropTypeId? CropTypeId,
    Gold Price,
    BuildingId Vendor,
    int RequiresStage,
    ToolId? ToolId,
    bool OncePerGame,
    PlaceableId? PlaceableId = null,
    int? BackpackCapacity = null);
