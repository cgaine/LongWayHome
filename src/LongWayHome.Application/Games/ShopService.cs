using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The Shop: what it offers today (BR-L1) and buying from it (BR-L2).</summary>
public static class ShopService
{
    /// <summary>The seed listings whose Crop type's Season is the current Season, in listing order (BR-L1).</summary>
    public static IReadOnlyList<ShopListing> Offers(Game game, GameData data)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);

        // BR-L1: a seed listing whose Crop type has no data row has no Season, so it is never offered. The Can
        // upgrade is offered by the Watering can upgrade feature, not here.
        return data.ShopListings
            .Where(listing => listing.Kind == ShopItemKind.Seed
                && listing.CropTypeId is { } cropTypeId
                && data.CropTypes.TryGetValue(cropTypeId, out var cropType)
                && cropType.Season == game.Date.Season)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Buys a quantity of an offered listing: takes the price and adds the seeds in one step (BR-L2). A quantity below
    /// 1 throws; a listing not offered today is NotOffered; less gold than the total is NotEnoughGold. A rejection or
    /// an overflowing total changes nothing, and buying costs no energy (BR-P2).
    /// </summary>
    public static ActionResult Buy(Game game, GameData data, string listingId, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        // BR-L1: only what is offered today can be bought.
        if (Offers(game, data).FirstOrDefault(listing => listing.Id == listingId) is not
            { CropTypeId: { } cropTypeId } offered)
        {
            return ActionResult.Rejected(Rejection.NotOffered);
        }

        var player = game.Player;

        // Both sums are checked before anything changes, so an overflow leaves gold and inventory as they were.
        var total = checked(offered.Price.Value * quantity);
        _ = checked(player.Inventory.Quantity(ItemKind.Seed, cropTypeId) + quantity);

        // BR-L2, BR-P5: the gold is taken and the seeds added in the same step, or nothing changes.
        if (!player.TrySpendGold(total))
        {
            return ActionResult.Rejected(Rejection.NotEnoughGold);
        }

        player.Inventory.Add(ItemKind.Seed, cropTypeId, quantity);
        return ActionResult.Ok;
    }
}
