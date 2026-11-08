using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Catalog;

/// <summary>
/// One item the Shop can sell. <see cref="CropTypeId"/> is set only for Seeds and <see cref="ToolId"/> only for a tool;
/// both are null for the Can upgrade.
/// </summary>
public sealed record ShopListing(
    string Id,
    ShopItemKind Kind,
    CropTypeId? CropTypeId,
    Gold Price,
    BuildingId Vendor,
    int RequiresStage,
    ToolId? ToolId,
    bool OncePerGame);
