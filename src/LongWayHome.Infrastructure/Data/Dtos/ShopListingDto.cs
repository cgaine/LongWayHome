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

    /// <summary>The Building that sells the listing (BR-L5).</summary>
    public required BuildingId Vendor { get; init; }

    /// <summary>The vendor's stage from which the listing is offered (BR-L5).</summary>
    public required int RequiresStage { get; init; }

    /// <summary>Nullable, so not required: set only for a tool listing.</summary>
    public ToolId? ToolId { get; init; }

    /// <summary>Whether the listing sells only once per Game (BR-L5).</summary>
    public required bool OncePerGame { get; init; }
}
