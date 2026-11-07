using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>
/// One Slot of the Backpack screen: its index, item, quantity, tool tier, label (<see cref="BackpackText.Label"/>) and
/// icon path (the seed or crop icon; null for a tool or an empty Slot).
/// </summary>
public sealed record BackpackViewSlot(int Index, ItemRef? Item, int Quantity, ToolTier? ToolTier, string Label, string? IconPath);

/// <summary>The Backpack screen: every Slot in index order, the hotbar row and the capacity.</summary>
public sealed record BackpackView(IReadOnlyList<BackpackViewSlot> Slots, int HotbarRow, int Capacity);
