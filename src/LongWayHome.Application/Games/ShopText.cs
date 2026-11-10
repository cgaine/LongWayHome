using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>The shop menu's text. Holds no rules.</summary>
public static class ShopText
{
    /// <summary>The line shown in winter instead of seed rows.</summary>
    public const string NoSeeds = "No seeds this season";

    /// <summary>The canonical label of the <c>can_upgrade</c> Shop item kind (canonical-values.md).</summary>
    public const string CanUpgradeLabel = "Watering can upgrade";

    /// <summary>
    /// "Emberroot seeds — 15g": the Crop type's name (its canonical label when it has no data row) and the listing's
    /// price; "Watering can upgrade — 200g" for the Can upgrade; "Axe — 50g" for a tool; "Backpack (24 slots) — 400g"
    /// for a backpack upgrade; "Chest — 100g" for a placeable. A seed listing with no Crop type, a tool listing with no
    /// tool, a backpack upgrade with no capacity or a placeable listing with no placeable throws.
    /// </summary>
    public static string Row(ShopListing listing, GameData data)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(data);

        if (listing.Kind == ShopItemKind.CanUpgrade)
        {
            return $"{CanUpgradeLabel} — {listing.Price.Value}g";
        }

        if (listing.Kind == ShopItemKind.Tool)
        {
            // canonical-values.md: the tool's display label, "Axe" or "Pickaxe".
            return listing.ToolId is { } tool
                ? $"{HeldItem.Of(tool).DisplayLabel} — {listing.Price.Value}g"
                : throw new ArgumentException($"Shop listing '{listing.Id}' is a tool listing with no tool.", nameof(listing));
        }

        if (listing.Kind == ShopItemKind.BackpackUpgrade)
        {
            return listing.BackpackCapacity is { } capacity
                ? $"Backpack ({capacity} slots) — {listing.Price.Value}g"
                : throw new ArgumentException($"Shop listing '{listing.Id}' is a backpack upgrade with no capacity.", nameof(listing));
        }

        if (listing.Kind == ShopItemKind.Placeable)
        {
            // canonical-values.md: the placeable's display label, "Fence" or "Chest".
            return listing.PlaceableId is { } placeable
                ? $"{HeldItem.PlaceableLabel(placeable)} — {listing.Price.Value}g"
                : throw new ArgumentException($"Shop listing '{listing.Id}' is a placeable listing with no placeable.", nameof(listing));
        }

        if (listing.CropTypeId is not { } cropTypeId)
        {
            throw new ArgumentException($"Shop listing '{listing.Id}' is not a seed listing.", nameof(listing));
        }

        var name = data.CropTypes.TryGetValue(cropTypeId, out var cropType)
            ? cropType.Name
            : CropLabels.For(cropTypeId);
        return $"{name} seeds — {listing.Price.Value}g";
    }

    /// <summary>The line shown after a purchase that dropped seeds at the farmer's feet (BR-K3).</summary>
    public const string SeedsDropped = "Some seeds dropped at your feet";

    /// <summary>The message after a successful purchase: <see cref="SeedsDropped"/> when any dropped, else "".</summary>
    public static string AfterBuy(bool dropped) => dropped ? SeedsDropped : "";

    /// <summary>
    /// The message after a successful purchase of <paramref name="listing"/>: "The chest dropped at your feet" for a
    /// placeable that dropped (BR-K3, #166), else as <see cref="AfterBuy(bool)"/>.
    /// </summary>
    public static string AfterBuy(ShopListing listing, bool dropped)
    {
        ArgumentNullException.ThrowIfNull(listing);

        return dropped && listing is { Kind: ShopItemKind.Placeable, PlaceableId: { } placeable }
            ? $"The {HeldItem.PlaceableLabel(placeable).ToLowerInvariant()} dropped at your feet"
            : AfterBuy(dropped);
    }

    /// <summary>The Sell section's header, below the buy rows in town (BR-L3).</summary>
    public const string SellHeader = "Sell";

    /// <summary>The line shown in the Sell section instead of its rows when no Crop can be sold.</summary>
    public const string NothingToSell = "No crops to sell";

    /// <summary>"Sell all crops — 495g": the sell-all row with what it would pay.</summary>
    public static string SellAllRow(int total) => $"Sell all crops — {total}g";

    /// <summary>"Emberroot × 12 — 420g": a Crop type's name from data, the quantity carried and what selling it pays.</summary>
    public static string SellRow(string name, int quantity, int total) => $"{name} × {quantity} — {total}g";

    /// <summary>"Sold for 420g": the message after a sale.</summary>
    public static string AfterSell(int total) => $"Sold for {total}g";

    /// <summary>"Gold 100".</summary>
    public static string Gold(int gold) => GoldText.For(gold);

    /// <summary>
    /// "Not enough gold", "Backpack full" or "Not sold this season" when buying; "No crops to sell" or "Maggie can't buy
    /// that" when selling.
    /// </summary>
    public static string Rejection(Domain.Games.Rejection rejection) => rejection switch
    {
        Domain.Games.Rejection.NotEnoughGold => "Not enough gold",
        Domain.Games.Rejection.BackpackFull => "Backpack full",
        Domain.Games.Rejection.NotOffered => "Not sold this season",
        Domain.Games.Rejection.NothingToSell => NothingToSell,
        Domain.Games.Rejection.NotSellable => "Maggie can't buy that",
        _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Not a shop rejection."),
    };
}
