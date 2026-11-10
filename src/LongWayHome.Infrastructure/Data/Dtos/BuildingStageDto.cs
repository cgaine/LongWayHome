using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Data.Dtos;

/// <summary>
/// One row of <c>buildings.json</c> (#163):
/// <c>{ "buildingId", "stage", "costItems": [{ "materialId", "quantity" }], "costGold", "unlocks" }</c>.
/// </summary>
internal sealed record BuildingStageDto
{
    public required BuildingId BuildingId { get; init; }

    public required int Stage { get; init; }

    public required IReadOnlyList<BuildingCostItemDto?> CostItems { get; init; }

    public required int CostGold { get; init; }

    public required string Unlocks { get; init; }
}

/// <summary>One cost item of a <see cref="BuildingStageDto"/>: <c>{ "materialId", "quantity" }</c>.</summary>
internal sealed record BuildingCostItemDto
{
    public required MaterialId MaterialId { get; init; }

    public required int Quantity { get; init; }
}
