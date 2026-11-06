using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Data;

/// <summary>All of the game's reference data: crop types keyed by id, shop listings and tuning.</summary>
public sealed record GameData(
    IReadOnlyDictionary<CropTypeId, CropType> CropTypes,
    IReadOnlyList<ShopListing> ShopListings,
    GameTuning Tuning);
