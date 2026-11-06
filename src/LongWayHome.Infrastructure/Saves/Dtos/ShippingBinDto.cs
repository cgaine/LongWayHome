namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record ShippingBinDto
{
    public required IReadOnlyList<ShippedItemDto> Items { get; init; }
}
