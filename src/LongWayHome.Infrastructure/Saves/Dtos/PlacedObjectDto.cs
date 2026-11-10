using System.Text.Json.Serialization;
using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>
/// One fence or chest on the Farm plot (#164): <c>{ "position", "placeable", "slots"? }</c>; a chest's 36 slots are
/// written like Backpack Slots, a fence has none.
/// </summary>
internal sealed record PlacedObjectDto
{
    public required GridPositionDto? Position { get; init; }

    public required PlaceableId Placeable { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<BackpackSlotDto?>? Slots { get; init; }
}
