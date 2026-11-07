namespace LongWayHome.Domain.Games;

/// <summary>What <see cref="Game.Gain"/> did: the quantity added to the Backpack and the DroppedItems it made (BR-K3).</summary>
public sealed record GainResult(int Added, IReadOnlyList<DroppedItem> Dropped);
