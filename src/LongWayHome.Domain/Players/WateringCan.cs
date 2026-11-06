namespace LongWayHome.Domain.Players;

/// <summary>The Watering can. Charges are always between 0 and capacity (BR-W1). Starts full and not upgraded.</summary>
public sealed class WateringCan
{
    public WateringCan(int capacity)
    {
        // BR-W1: a can holds at least one charge; charges start at capacity, so 0 ≤ Charges ≤ Capacity.
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        Capacity = capacity;
        Charges = capacity;
        Upgraded = false;
    }

    public int Capacity { get; }

    public int Charges { get; }

    public bool Upgraded { get; }
}
