using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The Shop: what it offers today (BR-L1, BR-L4, BR-L5) and buying from it (BR-L2).</summary>
public static class ShopService
{
    /// <summary>
    /// The listings offered today, in listing order within each kind: the in-season seeds (BR-L1), then the axe and
    /// pickaxe while not in the Backpack (BR-L4), then the Can upgrade while the Watering can is not upgraded. A listing
    /// is offered only when its vendor is at or above its <see cref="ShopListing.RequiresStage"/>, and a once-per-game
    /// listing only until bought (BR-L5).
    /// </summary>
    public static IReadOnlyList<ShopListing> Offers(Game game, GameData data)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);

        // BR-L1: a seed listing whose Crop type has no data row has no Season, so it is never offered. OrderBy is
        // stable, so listing order holds within each kind.
        return data.ShopListings
            .Where(listing => IsOffered(listing, game, data))
            .OrderBy(listing => KindOrder(listing.Kind))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// The vendor Building's stage, read from the Game's Building (BR-L5). The single place a vendor's stage is read.
    /// </summary>
    public static int VendorStage(Game game, BuildingId vendor)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.BuildingStage(vendor);
    }

    /// <summary>
    /// Buys a quantity of an offered listing: takes the price and gains the seeds, or applies the Can upgrade, in one
    /// step (BR-L2, BR-W4), or puts a tool in the lowest free Slot at Basic (BR-L4). Seeds that fit nowhere drop at
    /// <paramref name="feet"/>, or at plot tile (0, 0) when it is null, so a seed purchase is never refused for room
    /// (BR-K3). A quantity below 1, or other than 1 for the Can upgrade or a tool, throws; a listing not offered today
    /// is NotOffered; less gold than the total is NotEnoughGold; a tool with no free Slot is BackpackFull. A
    /// once-per-game listing bought is recorded (BR-L5). A rejection or an overflowing total changes nothing, and
    /// buying costs no energy (BR-P2).
    /// </summary>
    public static ActionResult Buy(
        Game game, GameData data, string listingId, int quantity = 1, GridPosition? feet = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        // BR-L1, BR-L5: only what is offered today can be bought.
        if (data.ShopListings.FirstOrDefault(listing => listing.Id == listingId) is not { } offered
            || !IsOffered(offered, game, data))
        {
            return ActionResult.Rejected(Rejection.NotOffered);
        }

        var result = offered.Kind switch
        {
            ShopItemKind.CanUpgrade => BuyCanUpgrade(game, data, offered, quantity),
            ShopItemKind.Tool => BuyTool(game, offered, offered.ToolId!.Value, quantity),
            _ => BuySeeds(game, offered, offered.CropTypeId!.Value, quantity, feet ?? new GridPosition(0, 0)),
        };

        // BR-L5: a once-per-game listing bought is never offered again.
        if (result.IsOk && offered.OncePerGame)
        {
            game.RecordPurchase(offered.Id);
        }

        return result;
    }

    private static ActionResult BuySeeds(
        Game game, ShopListing offered, CropTypeId cropTypeId, int quantity, GridPosition feet)
    {
        var player = game.Player;

        var seeds = ItemRef.Seed(cropTypeId);

        // The price is checked before anything changes, so an overflow leaves gold and Backpack as they were.
        var total = checked(offered.Price.Value * quantity);

        // BR-L2, BR-P5: the gold is taken and the seeds gained in the same step, or nothing changes.
        if (!player.TrySpendGold(total))
        {
            return ActionResult.Rejected(Rejection.NotEnoughGold);
        }

        // BR-K3: what fits goes in the Backpack and the rest drops at the feet; never refused for room.
        game.Gain(seeds, quantity, feet);
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

    private static ActionResult BuyTool(Game game, ShopListing offered, ToolId tool, int quantity)
    {
        // BR-L4: a tool is bought once, never several at a time.
        ArgumentOutOfRangeException.ThrowIfNotEqual(quantity, 1);

        var player = game.Player;

        // BR-L2: gold is checked first, so too little gold is NotEnoughGold even with a full Backpack.
        if (player.Gold.Value < offered.Price.Value)
        {
            return ActionResult.Rejected(Rejection.NotEnoughGold);
        }

        // BR-L4: the free Slot is checked before any gold is taken, so a refused purchase costs nothing.
        if (player.Backpack.Slots.All(slot => slot.Item is not null))
        {
            return ActionResult.Rejected(Rejection.BackpackFull);
        }

        // BR-L2, BR-P5, BR-L4: both steps are known to succeed now; the gold is taken and the tool added at Basic.
        player.TrySpendGold(offered.Price.Value);
        player.Backpack.AddTool(tool, ToolTier.Basic);
        return ActionResult.Ok;
    }

    /// <summary>Seeds, then tools, then the Can upgrade.</summary>
    private static int KindOrder(ShopItemKind kind) => kind switch
    {
        ShopItemKind.Seed => 0,
        ShopItemKind.Tool => 1,
        _ => 2,
    };

    private static bool IsOffered(ShopListing listing, Game game, GameData data) =>
        // BR-L5: the vendor must be at or above the listing's stage, and a once-per-game listing goes once bought.
        VendorStage(game, listing.Vendor) >= listing.RequiresStage
        && !(listing.OncePerGame && game.PurchasedListings.Contains(listing.Id))
        && IsKindOffered(listing, game, data);

    private static bool IsKindOffered(ShopListing listing, Game game, GameData data) => listing.Kind switch
    {
        // BR-L1: seeds only for Crop types of the current Season.
        ShopItemKind.Seed => listing.CropTypeId is { } cropTypeId
            && data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            && cropType.Season == game.Date.Season,

        // BR-L1, BR-W4: the Can upgrade is offered until bought.
        ShopItemKind.CanUpgrade => !game.Player.WateringCan.Upgraded,

        // BR-L4: a tool is offered while it isn't already in the Backpack.
        ShopItemKind.Tool => listing.ToolId is { } tool && game.Player.Backpack.TierOf(tool) is null,
        _ => false,
    };
}
