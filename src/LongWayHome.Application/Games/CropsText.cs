using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The HUD crops line: "Crops: Emberroot × 9, Dusktuber × 2" or "Crops: none".</summary>
public static class CropsText
{
    /// <summary>The crop stacks (not seeds) in CropTypeId order, each as "&lt;name&gt; × &lt;quantity&gt;".</summary>
    public static string For(IReadOnlyList<InventoryStack> stacks, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        throw new NotImplementedException();
}
