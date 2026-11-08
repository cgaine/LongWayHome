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
    /// price; "Watering can upgrade — 200g" for the Can upgrade; "Axe — 50g" for a tool. A seed listing with no Crop type, or
    /// a tool listing with no tool, throws.
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

    /// <summary>The Sell section's header. Stub (#146): the real text is set by the implementation.</summary>
    public const string SellHeader = "";

    /// <summary>The line shown in the Sell section when no Crop can be sold. Stub (#146).</summary>
    public const string NothingToSell = "";

    /// <summary>"Sell all crops — 495g". Stub (#146).</summary>
    public static string SellAllRow(int total) => throw new NotImplementedException();

    /// <summary>"Emberroot × 12 — 420g". Stub (#146).</summary>
    public static string SellRow(string name, int quantity, int total) => throw new NotImplementedException();

    /// <summary>"Sold for 420g". Stub (#146).</summary>
    public static string AfterSell(int total) => throw new NotImplementedException();

    /// <summary>"Gold 100".</summary>
    public static string Gold(int gold) => GoldText.For(gold);

    /// <summary>"Not enough gold", "Backpack full" or "Not sold this season".</summary>
    public static string Rejection(Domain.Games.Rejection rejection) => rejection switch
    {
        Domain.Games.Rejection.NotEnoughGold => "Not enough gold",
        Domain.Games.Rejection.BackpackFull => "Backpack full",
        Domain.Games.Rejection.NotOffered => "Not sold this season",
        _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Not a shop rejection."),
    };
}
