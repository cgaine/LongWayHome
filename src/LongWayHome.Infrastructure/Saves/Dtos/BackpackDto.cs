namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>The Player's Backpack (save version 2): its capacity, hotbar row and every Slot, empty ones included.</summary>
internal sealed record BackpackDto
{
    public required int Capacity { get; init; }

    public required int HotbarRow { get; init; }

    public required IReadOnlyList<BackpackSlotDto?> Slots { get; init; }
}
