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

    /// <summary>Restores a can at a given charge level; charges must be within 0..capacity (BR-W1).</summary>
    public WateringCan(int capacity, int charges, bool upgraded)
    {
        // BR-W1
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(charges);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(charges, capacity);

        Capacity = capacity;
        Charges = charges;
        Upgraded = upgraded;
    }

    public int Capacity { get; }

    public int Charges { get; private set; }

    public bool Upgraded { get; }

    /// <summary>Uses one charge (BR-W2); false and no change when the can is empty.</summary>
    public bool TryUseCharge()
    {
        // BR-W2, BR-W1: an empty can is refused, so Charges never goes below 0.
        if (Charges == 0)
        {
            return false;
        }

        Charges--;
        return true;
    }

    /// <summary>Fills the can to capacity (BR-W3).</summary>
    public void Refill() => Charges = Capacity;

    public void Upgrade(int upgradedCapacity) => throw new NotImplementedException();
}
