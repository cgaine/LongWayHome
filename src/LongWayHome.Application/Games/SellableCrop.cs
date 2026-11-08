using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>One row of the shop's Sell section: a carried Crop type, its name, the quantity carried and what selling it pays.</summary>
public sealed record SellableCrop(CropTypeId CropTypeId, string Name, int Quantity, int Payout);
