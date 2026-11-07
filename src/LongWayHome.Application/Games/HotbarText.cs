namespace LongWayHome.Application.Games;

/// <summary>
/// The hotbar's compact slot labels (#95), used only when the full labels with their seed icons don't fit the screen:
/// a seed reads "Emberroot × 99" (its packet icon already says it is a seed), a tool keeps its label.
/// </summary>
public static class HotbarText
{
    /// <summary>
    /// "&lt;Crop type&gt; × &lt;quantity&gt;" for a seed, named as <see cref="HeldItem.DisplayLabel"/> names it
    /// (<see cref="CropLabels"/>), so it is the full hotbar label without " seeds"; a tool's
    /// <see cref="HeldItem.DisplayLabel"/> unchanged, ignoring <paramref name="quantity"/>.
    /// </summary>
    public static string Compact(HeldItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Seed is { } seed ? $"{CropLabels.For(seed)} × {quantity}" : item.DisplayLabel;
    }

    /// <summary>The hotbar row, 1-based, of the rows a Backpack of <paramref name="capacity"/> has: "Row 1/2".</summary>
    public static string Row(int hotbarRow, int capacity) => throw new NotImplementedException();
}
