using LongWayHome.Domain.Catalog;

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

    /// <summary>The menu's close button.</summary>
    public const string CloseButton = "Close";

    /// <summary>"Rebuild the general store — stage 2".</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined Building.</exception>
    public static string Title(BuildingId building, int stage) => $"Rebuild {Name(building)} — stage {stage}";

    /// <summary>
    /// "Wood 12 / 50 (you have 8)" or "Gold 0 / 300 (you have 120)"; a complete line shows <see cref="CompleteMark"/>
    /// instead of what is held: "Stone 25 / 25 ✓".
    /// </summary>
    public static string Row(RebuildLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var progress = $"{line.Label} {line.Delivered} / {line.Needed}";
        return line.Complete ? $"{progress} {CompleteMark}" : $"{progress} (you have {line.Held})";
    }

    /// <summary>The line shown after a delivery: "Delivered 30 Wood", or "Delivered 100g" for gold.</summary>
    public static string AfterDeliver(RebuildLine line, int delivered)
    {
        ArgumentNullException.ThrowIfNull(line);

        return line.IsGold ? $"Delivered {delivered}g" : $"Delivered {delivered} {line.Label}";
    }

    /// <summary>
    /// "Not needed for this stage", "That's all delivered" or "You have none to deliver"; any other refusal "Can't
    /// deliver that".
    /// </summary>
    public static string Rejection(Domain.Games.Rejection rejection) => rejection switch
    {
        Domain.Games.Rejection.NotOffered => "Not needed for this stage",
        Domain.Games.Rejection.AlreadyDelivered => "That's all delivered",
        Domain.Games.Rejection.NothingToDeliver => "You have none to deliver",
        _ => "Can't deliver that",
    };

    /// <summary>"the general store", "the carpenter", "Mira's house" or "the blacksmith", as a title names them.</summary>
    private static string Name(BuildingId building) => building switch
    {
        BuildingId.GeneralStore => "the general store",
        BuildingId.Carpenter => "the carpenter",
        BuildingId.MirasHouse => "Mira's house",
        BuildingId.Blacksmith => "the blacksmith",
        _ => throw new ArgumentOutOfRangeException(nameof(building), building, "Unknown Building."),
    };
}
