using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Data;

/// <summary>All of the game's reference data: crop types keyed by id, shop listings, tuning and node spawn points.</summary>
public sealed record GameData(
    IReadOnlyDictionary<CropTypeId, CropType> CropTypes,
    IReadOnlyList<ShopListing> ShopListings,
    GameTuning Tuning)
{
    /// <summary>The node spawn points from <c>nodes.json</c>; never null.</summary>
    public IReadOnlyList<NodeSpawn> NodeSpawns { get; init; } = [];

    /// <summary>The material types from <c>materials.json</c>, keyed by id; never null.</summary>
    public IReadOnlyDictionary<MaterialId, MaterialType> MaterialTypes { get; init; } =
        new Dictionary<MaterialId, MaterialType>();
}
