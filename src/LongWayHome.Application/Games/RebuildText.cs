using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>The Rebuild menu's text (#163). Holds no rules.</summary>
public static class RebuildText
{
    /// <summary>The mark a complete line shows.</summary>
    public const string CompleteMark = "✓";

    /// <summary>An item line's button.</summary>
    public const string DeliverButton = "Deliver";

    /// <summary>The gold line's button.</summary>
    public const string DeliverGoldButton = "Deliver 100g";

    /// <summary>"Rebuild the general store — stage 2".</summary>
    public static string Title(BuildingId building, int stage) => throw new NotImplementedException();

    /// <summary>
    /// "Wood 12 / 50 (you have 8)" or "Gold 0 / 300 (you have 120)"; a complete line shows <see cref="CompleteMark"/>.
    /// </summary>
    public static string Row(RebuildLine line) => throw new NotImplementedException();

    /// <summary>The line shown after a delivery: what was delivered, e.g. "Delivered 30 Wood".</summary>
    public static string AfterDeliver(RebuildLine line, int delivered) => throw new NotImplementedException();

    /// <summary>The line shown when a delivery is refused.</summary>
    public static string Rejection(Rejection rejection) => throw new NotImplementedException();
}
