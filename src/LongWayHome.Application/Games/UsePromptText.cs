using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

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
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(cropTypes);

        if (preview.Action is { } action)
        {
            return action switch
            {
                UseActionKind.Till => ("Till", true),
                UseActionKind.Water => ("Water", true),
                UseActionKind.Plant when held.Seed is { } seed => ($"Plant {CropName(seed, cropTypes)}", true),
                UseActionKind.Harvest when targetPlanting is not null =>
                    ($"Harvest {CropName(targetPlanting.CropTypeId, cropTypes)}", true),
                _ => null,
            };
        }

        string? reason = preview.Rejection switch
        {
            Rejection.AlreadyTilled => "Already tilled",
            Rejection.TooTired => "Too tired",
            Rejection.NotTilled => "Till the soil first",
            Rejection.TileOccupied => "Something is growing here",
            Rejection.NoSeed => "No seeds left",
            Rejection.NotRipe => "Not ready yet",
            Rejection.AlreadyWatered => "Already watered",
            Rejection.CanEmpty => "Can is empty — refill at the pond",
            _ => null,
        };

        return reason is null ? null : (reason, false);
    }

    private static string CropName(CropTypeId cropTypeId, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        cropTypes.TryGetValue(cropTypeId, out var cropType) ? cropType.Name : CropLabels.For(cropTypeId);
}
