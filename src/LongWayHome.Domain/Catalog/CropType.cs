using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Catalog;

/// <summary>Reference data for one kind of crop (BR-C2: growDays ≥ 1, seedCost and sellPrice above 0).</summary>
public sealed record CropType(
    CropTypeId Id,
    string Name,
    int GrowDays,
    Gold SeedCost,
    Gold SellPrice,
    Season Season,
    bool Regrows);
