namespace LongWayHome.Domain.Players;

/// <summary>The Player. Energy is always between 0 and 150 (BR-P1); gold is never negative (BR-P5, via <see cref="Gold"/>).</summary>
public sealed class Player
{
    /// <summary>The highest Energy a Player can have (BR-P1).</summary>
    public const int MaxEnergy = 150;

    /// <summary>A Player carrying the given Backpack (REQ-47); the tools are in its Slots.</summary>
    public Player(Gold gold, int energy, WateringCan wateringCan, Backpack backpack)
    {
        // BR-P1
        if (energy is < 0 or > MaxEnergy)
        {
            throw new ArgumentOutOfRangeException(
                nameof(energy), energy, $"Energy must be between 0 and {MaxEnergy} (BR-P1).");
        }

        ArgumentNullException.ThrowIfNull(wateringCan);
        ArgumentNullException.ThrowIfNull(backpack);

        Gold = gold;
        Energy = energy;
        WateringCan = wateringCan;
        Backpack = backpack;
    }

    public Gold Gold { get; private set; }

    public int Energy { get; private set; }

    public WateringCan WateringCan { get; }

    /// <summary>The Player's Backpack: tools, seeds and crops in Slots (REQ-47).</summary>
    public Backpack Backpack { get; }

    /// <summary>Starts a new Day with the given energy (BR-P1, BR-P3).</summary>
    public void StartDay(int energy)
    {
        // BR-P1
        if (energy is < 0 or > MaxEnergy)
        {
            throw new ArgumentOutOfRangeException(
                nameof(energy), energy, $"Energy must be between 0 and {MaxEnergy} (BR-P1).");
        }

        Energy = energy;
    }

    /// <summary>Spends energy; returns false and changes nothing when less than the amount is held (BR-P1, BR-P2).</summary>
    public bool TrySpendEnergy(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        // BR-P1, BR-P2: an action is refused rather than taking Energy below 0.
        if (Energy < amount)
        {
            return false;
        }

        Energy -= amount;
        return true;
    }

    /// <summary>Adds gold: a negative amount throws, and the sum is checked (BR-P5).</summary>
    public void EarnGold(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        // BR-P5: an overflowing sum throws before Gold is assigned, so gold is unchanged.
        Gold = new Gold(checked(Gold.Value + amount));
    }

    /// <summary>Spends gold; returns false and changes nothing when less than the amount is held (BR-L2, BR-P5).</summary>
    public bool TrySpendGold(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        // BR-L2, BR-P5: a purchase is refused rather than taking gold below 0.
        if (Gold.Value < amount)
        {
            return false;
        }

        Gold = new Gold(Gold.Value - amount);
        return true;
    }
}
