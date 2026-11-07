using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>One Backpack Slot; an empty Slot is written with a null item, quantity 0 and a null tier.</summary>
internal sealed record BackpackSlotDto
{
    public required int Index { get; init; }

    public required ItemRefDto? Item { get; init; }

    public required int Quantity { get; init; }

    public required ToolTier? ToolTier { get; init; }
}
