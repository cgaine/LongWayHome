using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The Shop: what it offers today (BR-L1) and buying from it (BR-L2).</summary>
public static class ShopService
{
    /// <summary>
    /// The seed listings whose Crop type's Season is the current Season, in listing order, then the Can upgrade while
    /// the Watering can is not upgraded, in every Season (BR-L1).
    /// </summary>
    public static IReadOnlyList<ShopListing> Offers(Game game, GameData data)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);

        // BR-L1: a seed listing whose Crop type has no data row has no Season, so it is never offered. The Can
        // upgrade always comes after the seeds.
        return data.ShopListings
            .Where(listing => IsOffered(listing, game, data))
            .OrderBy(listing => listing.Kind == ShopItemKind.CanUpgrade)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Buys a quantity of an offered listing: takes the price and adds the seeds, or applies the Can upgrade, in one step
    /// (BR-L2, BR-W4). A quantity below 1, or other than 1 for the Can upgrade, throws; a listing not offered today is
    /// NotOffered; seeds that don't all fit in the Backpack are BackpackFull; less gold than the total is NotEnoughGold.
    /// A rejection or an overflowing total changes nothing, and buying costs no energy (BR-P2).
    /// </summary>
    public static ActionResult Buy(Game game, GameData data, string listingId, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        // BR-L1: only what is offered today can be bought.
        if (data.ShopListings.FirstOrDefault(listing => listing.Id == listingId) is not { } offered
            || !IsOffered(offered, game, data))
        {
            return ActionResult.Rejected(Rejection.NotOffered);
        }

        return offered.Kind == ShopItemKind.CanUpgrade
            ? BuyCanUpgrade(game, data, offered, quantity)
            : BuySeeds(game, offered, offered.CropTypeId!.Value, quantity);
    }

    private static ActionResult BuySeeds(Game game, ShopListing offered, CropTypeId cropTypeId, int quantity)
    {
        var player = game.Player;

        var seeds = ItemRef.Seed(cropTypeId);

        // The price is checked before anything changes, so an overflow leaves gold and Backpack as they were.
        var total = checked(offered.Price.Value * quantity);

        // BR-K3, BR-L2: until dropped items exist, seeds that fit nowhere are refused before any gold is taken.
        if (!player.Backpack.CanFit(seeds, quantity))
        {
            return ActionResult.Rejected(Rejection.BackpackFull);
        }

        // BR-L2, BR-P5: the gold is taken and the seeds added in the same step, or nothing changes.
        if (!player.TrySpendGold(total))
        {
            return ActionResult.Rejected(Rejection.NotEnoughGold);
        }

        player.Backpack.Add(seeds, quantity);
        return ActionResult.Ok;
    }

    private static ActionResult BuyCanUpgrade(Game game, GameData data, ShopListing offered, int quantity)
    {
        // BR-W4: the Can upgrade is bought once, never several at a time.
        ArgumentOutOfRangeException.ThrowIfNotEqual(quantity, 1);

        var player = game.Player;

        // The upgrade is checked before any gold is taken, so a can that cannot be raised never costs gold.
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(data.Tuning.CanUpgradedCapacity, player.WateringCan.Capacity);

        // BR-L2, BR-P5: the gold is taken and the can upgraded in the same step, or nothing changes.
        if (!player.TrySpendGold(offered.Price.Value))
        {
            return ActionResult.Rejected(Rejection.NotEnoughGold);
        }

        // BR-W4, BR-W1: capacity becomes the upgraded capacity and the can is filled; the can's tier becomes Rosk, so
        // the tier and the 40 capacity agree.
        player.WateringCan.Upgrade(data.Tuning.CanUpgradedCapacity);
        player.Backpack.SetTier(ToolId.WateringCan, ToolTier.Rosk);
        return ActionResult.Ok;
    }

    private static bool IsOffered(ShopListing listing, Game game, GameData data) => listing.Kind switch
    {
        // BR-L1: seeds only for Crop types of the current Season.
        ShopItemKind.Seed => listing.CropTypeId is { } cropTypeId
            && data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            && cropType.Season == game.Date.Season,

        // BR-L1, BR-W4: the Can upgrade is offered until bought.
        ShopItemKind.CanUpgrade => !game.Player.WateringCan.Upgraded,
        _ => false,
    };
}
