using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The words of the Use prompt for a <see cref="UsePreview"/>; it reads the preview and decides nothing.</summary>
public static class UsePromptText
{
    /// <summary>
    /// The prompt text and whether it names an action (true) or a rejection reason (false), or null to show no
    /// prompt. <paramref name="held"/> is null when the held hotbar slot is empty or holds a crop;
    /// <paramref name="targetPlaceable"/> is the fence or chest on the target Tile, if any.
    /// </summary>
    public static (string Text, bool IsAction)? For(
        UsePreview preview,
        HeldItem? held,
        Planting? targetPlanting,
        IReadOnlyDictionary<CropTypeId, CropType> cropTypes,
        PlaceableId? targetPlaceable = null)
    {
        ArgumentNullException.ThrowIfNull(cropTypes);

        if (preview.Action is { } action)
        {
            return action switch
            {
                UseActionKind.Till => ("Till", true),
                UseActionKind.Water => ("Water", true),
                UseActionKind.Chop => ("Chop", true),
                UseActionKind.Break => ("Break", true),
                UseActionKind.Place when held?.Placeable is { } placed =>
                    ($"Place {HeldItem.PlaceableLabel(placed).ToLowerInvariant()}", true),
                UseActionKind.PickUp when targetPlaceable is { } target =>
                    ($"Pick up {HeldItem.PlaceableLabel(target).ToLowerInvariant()}", true),
                UseActionKind.Plant when held?.Seed is { } seed => ($"Plant {CropName(seed, cropTypes)}", true),
                UseActionKind.Harvest when targetPlanting is not null =>
                    ($"Harvest {CropName(targetPlanting.CropTypeId, cropTypes)}", true),
                _ => null,
            };
        }

        string? reason = preview.Rejection switch
        {
            Rejection.AlreadyTilled => "Already tilled",
            Rejection.NodeInTheWay => "Something's in the way",
            Rejection.TooTired => "Too tired",
            Rejection.NeedsGrothAxe => "Needs a Groth axe",
            Rejection.NotTilled => "Till the soil first",
            Rejection.TileOccupied when targetPlaceable is not null => "Something's in the way",
            Rejection.TileOccupied => "Something is growing here",
            Rejection.ChestNotEmpty => "Empty the chest first",
            Rejection.NoSeed => "No seeds left",
            Rejection.NotRipe => "Not ready yet",
            Rejection.AlreadyWatered => "Already watered",
            Rejection.CanEmpty => "Can is empty — refill at the pond",
            Rejection.OutOfSeason => OutOfSeason(held, cropTypes),
            _ => null,
        };

        return reason is null ? null : (reason, false);
    }

    // BR-C1: names the Season of the held Seed's Crop type; a Seed with no data row has no Season to name.
    private static string OutOfSeason(HeldItem? held, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        held?.Seed is { } seed && cropTypes.TryGetValue(seed, out var cropType)
            ? $"Out of season — plant in {SeasonLabels.For(cropType.Season)}"
            : "Out of season";

    private static string CropName(CropTypeId cropTypeId, IReadOnlyDictionary<CropTypeId, CropType> cropTypes) =>
        cropTypes.TryGetValue(cropTypeId, out var cropType) ? cropType.Name : CropLabels.For(cropTypeId);
}
