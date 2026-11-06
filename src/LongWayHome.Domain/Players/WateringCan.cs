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

    public int Capacity { get; private set; }

    public int Charges { get; private set; }

    public bool Upgraded { get; private set; }

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

    /// <summary>
    /// Applies the Can upgrade (BR-W4): raises the capacity to <paramref name="upgradedCapacity"/> and fills the can. An
    /// already upgraded can throws <see cref="InvalidOperationException"/>, since the Shop never offers it twice; a
    /// capacity not above the current one throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public void Upgrade(int upgradedCapacity)
    {
        // BR-W4: the Can upgrade happens once per Game.
        if (Upgraded)
        {
            throw new InvalidOperationException("The Watering can is already upgraded.");
        }

        // BR-W1: the upgrade only ever raises the capacity, and filling to it keeps 0 ≤ Charges ≤ Capacity.
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(upgradedCapacity, Capacity);

        Capacity = upgradedCapacity;
        Charges = upgradedCapacity;
        Upgraded = true;
    }
}
