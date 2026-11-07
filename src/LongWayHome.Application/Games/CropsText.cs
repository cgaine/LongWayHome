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

    /// <summary>The crop stacks (not seeds) in CropTypeId order, each as "&lt;name&gt; × &lt;quantity&gt;".</summary>
    public static string For(IReadOnlyList<InventoryStack> stacks, IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        var entries = Entries(stacks, cropTypes);
        return entries.Count == 0
            ? Heading(entries)
            : $"{Heading(entries)} {string.Join(EntrySeparator, entries.Select(entry => entry.Text))}";
    }

    /// <summary>The crops row's leading label (#95): "Crops:" ahead of the entries, or "Crops: none" with none.</summary>
    public static string Heading(IReadOnlyCollection<(CropTypeId Crop, string Text)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.Count == 0 ? "Crops: none" : "Crops:";
    }

    /// <summary>The crop stacks (not seeds) in CropTypeId order, each as (crop, "&lt;name&gt; × &lt;quantity&gt;").</summary>
    public static IReadOnlyList<(CropTypeId Crop, string Text)> Entries(
        IReadOnlyList<InventoryStack> stacks,
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(stacks);
        ArgumentNullException.ThrowIfNull(cropTypes);

        return stacks
            .Where(stack => stack.Kind == ItemKind.Crop)
            .OrderBy(stack => stack.CropTypeId)
            .Select(stack => (stack.CropTypeId, $"{NameOf(stack.CropTypeId, cropTypes)} × {stack.Quantity}"))
            .ToList();
    }

    /// <summary>The HUD crops line from the Backpack: per-Crop-type totals across its Slots, text as for stacks.</summary>
    public static string For(Backpack backpack, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        throw new NotImplementedException();

    /// <summary>The crops row's entries from the Backpack: one per Crop type held, its total across Slots.</summary>
    public static IReadOnlyList<(CropTypeId Crop, string Text)> Entries(
        Backpack backpack,
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        throw new NotImplementedException();

    private static string NameOf(CropTypeId id, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        cropTypes.TryGetValue(id, out var cropType) ? cropType.Name : CropLabels.For(id);
}
