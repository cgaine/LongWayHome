namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record ShopFileDto
{
    public required IReadOnlyList<ShopListingDto> Listings { get; init; }
}
