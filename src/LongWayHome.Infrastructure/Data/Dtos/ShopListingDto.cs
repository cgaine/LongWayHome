using LongWayHome.Domain.Catalog;

namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record ShopListingDto
{
    public required string Id { get; init; }

    public required ShopItemKind Kind { get; init; }

    /// <summary>Nullable, so not required: <c>null</c> and an absent field both mean "no crop type".</summary>
    public CropTypeId? CropTypeId { get; init; }

    public required int Price { get; init; }
}
