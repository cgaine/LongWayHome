using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>
/// The HUD crops line: "Crops: Emberroot × 9, Dusktuber × 2" or "Crops: none", and its parts for the crops row
/// (#95), which shows each entry beside its crop icon.
/// </summary>
public static class CropsText
{
    /// <summary>What follows every entry but the last on the HUD crops row: ", ".</summary>
    public const string EntrySeparator = ", ";

    /// <summary>The crops row's leading label (#95): "Crops:" ahead of the entries, or "Crops: none" with none.</summary>
    public static string Heading(IReadOnlyCollection<(CropTypeId Crop, string Text)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.Count == 0 ? "Crops: none" : "Crops:";
    }

    /// <summary>The HUD crops line from the Backpack: per-Crop-type totals across its Slots in CropTypeId order, each as "&lt;name&gt; × &lt;quantity&gt;".</summary>
    public static string For(Backpack backpack, IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        var entries = Entries(backpack, cropTypes);
        return entries.Count == 0
            ? Heading(entries)
            : $"{Heading(entries)} {string.Join(EntrySeparator, entries.Select(entry => entry.Text))}";
    }

    /// <summary>The crops row's entries from the Backpack: one per Crop type held, its total across Slots.</summary>
    public static IReadOnlyList<(CropTypeId Crop, string Text)> Entries(
        Backpack backpack,
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(backpack);
        ArgumentNullException.ThrowIfNull(cropTypes);

        // BR-P7, BR-K2: a Crop split over several Slots is one entry with its total; Totals is in CropTypeId order.
        return backpack.Totals(ItemKind.Crop)
            .Select(total => (total.CropTypeId, $"{NameOf(total.CropTypeId, cropTypes)} × {total.Quantity}"))
            .ToList();
    }

    private static string NameOf(CropTypeId id, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        cropTypes.TryGetValue(id, out var cropType) ? cropType.Name : CropLabels.For(id);
}
