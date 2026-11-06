using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>The shop menu's text. Holds no rules.</summary>
public static class ShopText
{
    /// <summary>The line shown in winter instead of seed rows.</summary>
    public const string NoSeeds = "";

    /// <summary>"Emberroot seeds — 15g".</summary>
    public static string Row(ShopListing listing, GameData data) => throw new NotImplementedException();

    /// <summary>"Gold 100".</summary>
    public static string Gold(int gold) => throw new NotImplementedException();

    /// <summary>"Not enough gold" or "Not sold this season".</summary>
    public static string Rejection(Domain.Games.Rejection rejection) => throw new NotImplementedException();
}
