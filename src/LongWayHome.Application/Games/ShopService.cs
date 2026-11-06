using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>The Shop: what it offers today (BR-L1) and buying from it (BR-L2).</summary>
public static class ShopService
{
    /// <summary>The seed listings whose Crop type's Season is the current Season, in listing order (BR-L1).</summary>
    public static IReadOnlyList<ShopListing> Offers(Game game, GameData data) => throw new NotImplementedException();

    /// <summary>Buys a quantity of an offered listing: takes the price and adds the seeds in one step (BR-L2).</summary>
    public static ActionResult Buy(Game game, GameData data, string listingId, int quantity = 1) =>
        throw new NotImplementedException();
}
