using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Catalog;

/// <summary>
/// One row of <c>buildings.json</c>: what raising <see cref="BuildingId"/> to <see cref="Stage"/> costs, and what it
/// unlocks (display text).
/// </summary>
public sealed record BuildingStage(
    BuildingId BuildingId,
    int Stage,
    IReadOnlyList<MaterialStack> CostItems,
    Gold CostGold,
    string Unlocks);
