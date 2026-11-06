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
    /// price; "Watering can upgrade — 200g" for the Can upgrade. A seed listing with no Crop type throws.
    /// </summary>
    public static string Row(ShopListing listing, GameData data)
    {
        ArgumentNullException.ThrowIfNull(listing);
        ArgumentNullException.ThrowIfNull(data);

        if (listing.Kind == ShopItemKind.CanUpgrade)
        {
            return $"{CanUpgradeLabel} — {listing.Price.Value}g";
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

    /// <summary>"Gold 100".</summary>
    public static string Gold(int gold) => GoldText.For(gold);

    /// <summary>"Not enough gold" or "Not sold this season".</summary>
    public static string Rejection(Domain.Games.Rejection rejection) => rejection switch
    {
        Domain.Games.Rejection.NotEnoughGold => "Not enough gold",
        Domain.Games.Rejection.NotOffered => "Not sold this season",
        _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, "Not a shop rejection."),
    };
}
