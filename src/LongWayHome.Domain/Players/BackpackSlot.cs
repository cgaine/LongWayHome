namespace LongWayHome.Domain.Players;

/// <summary>
/// One Backpack Slot: empty (no item, quantity 0, no tier), 1–99 of one item, or exactly one tool with its tier
/// (BR-K2).
/// </summary>
public sealed record BackpackSlot(int Index, ItemRef? Item, int Quantity, ToolTier? ToolTier);
