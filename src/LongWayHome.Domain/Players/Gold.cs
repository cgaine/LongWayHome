namespace LongWayHome.Domain.Players;

/// <summary>An amount of gold. Gold is never negative (BR-P5).</summary>
public readonly record struct Gold(int Value)
{
    public int Value { get; } = Value >= 0
        ? Value
        : throw new ArgumentOutOfRangeException(nameof(Value), Value, "Gold is never negative (BR-P5).");
}
