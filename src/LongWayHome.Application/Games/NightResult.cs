using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>What a Night did that the wake-up shows: the Buildings rebuilt (BR-B3), in BuildingId order.</summary>
public sealed record NightResult(IReadOnlyList<BuildingId> Rebuilt);
