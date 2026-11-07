using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The HUD crops line: "Crops: Emberroot × 9, Dusktuber × 2" or "Crops: none".</summary>
public static class CropsText
{
    /// <summary>The crop stacks (not seeds) in CropTypeId order, each as "&lt;name&gt; × &lt;quantity&gt;".</summary>
    public static string For(IReadOnlyList<InventoryStack> stacks, IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(stacks);
        ArgumentNullException.ThrowIfNull(cropTypes);

        var entries = stacks
            .Where(stack => stack.Kind == ItemKind.Crop)
            .OrderBy(stack => stack.CropTypeId)
            .Select(stack => $"{NameOf(stack.CropTypeId, cropTypes)} × {stack.Quantity}")
            .ToList();

        return entries.Count == 0 ? "Crops: none" : $"Crops: {string.Join(", ", entries)}";
    }

    /// <summary>The crop stacks (not seeds) in CropTypeId order, each as (crop, "&lt;name&gt; × &lt;quantity&gt;").</summary>
    public static IReadOnlyList<(CropTypeId Crop, string Text)> Entries(
        IReadOnlyList<InventoryStack> stacks,
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        throw new NotImplementedException();

    private static string NameOf(CropTypeId id, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        cropTypes.TryGetValue(id, out var cropType) ? cropType.Name : CropLabels.For(id);
}
