namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>
/// One Building in the save (#163):
/// <c>{ "id": "&lt;BuildingId code&gt;", "stage": n, "delivered": [{ "materialId", "quantity" }], "deliveredGold": n }</c>.
/// </summary>
internal sealed record BuildingDto
{
    public required string Id { get; init; }

    public required int Stage { get; init; }

    public required IReadOnlyList<DeliveredItemDto?> Delivered { get; init; }

    public required int DeliveredGold { get; init; }
}

/// <summary>One delivered item line of a <see cref="BuildingDto"/>: <c>{ "materialId", "quantity" }</c>.</summary>
internal sealed record DeliveredItemDto
{
    public required string MaterialId { get; init; }

    public required int Quantity { get; init; }
}
