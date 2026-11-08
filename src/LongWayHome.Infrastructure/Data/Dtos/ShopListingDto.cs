using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record ShopListingDto
{
    public required string Id { get; init; }

    public required ShopItemKind Kind { get; init; }

    /// <summary>Nullable, so not required: <c>null</c> and an absent field both mean "no crop type".</summary>
    public CropTypeId? CropTypeId { get; init; }

    public required int Price { get; init; }

    // TODO(#145): vendor, requiresStage and oncePerGame become required; none is read or validated yet.
    public BuildingId? Vendor { get; init; }

    public int? RequiresStage { get; init; }

    public ToolId? ToolId { get; init; }

    public bool? OncePerGame { get; init; }
}
