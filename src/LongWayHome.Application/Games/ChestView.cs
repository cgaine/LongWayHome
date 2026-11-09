using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Games;

/// <summary>A placed chest for the Chest screen: its Tile and its 36 slots in index order, labelled like the Backpack's.</summary>
public sealed record ChestView(GridPosition Position, IReadOnlyList<BackpackViewSlot> Slots);
