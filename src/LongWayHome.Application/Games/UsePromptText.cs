using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Games;

/// <summary>The words of the Use prompt for a <see cref="UsePreview"/>; it reads the preview and decides nothing.</summary>
public static class UsePromptText
{
    /// <summary>
    /// The prompt text and whether it names an action (true) or a rejection reason (false), or null to show no
    /// prompt.
    /// </summary>
    public static (string Text, bool IsAction)? For(
        UsePreview preview,
        HeldItem held,
        Planting? targetPlanting,
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes) => throw new NotImplementedException();
}
