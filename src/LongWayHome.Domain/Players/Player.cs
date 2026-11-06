using System.Collections.Frozen;

namespace LongWayHome.Domain.Players;

/// <summary>The Player. Energy is always between 0 and 150 (BR-P1); gold is never negative (BR-P5, via <see cref="Gold"/>).</summary>
public sealed class Player
{
    /// <summary>The highest Energy a Player can have (BR-P1).</summary>
    public const int MaxEnergy = 150;

    public Player(Gold gold, int energy, IReadOnlySet<ToolId> tools, WateringCan wateringCan, Inventory inventory)
    {
        // BR-P1
        if (energy is < 0 or > MaxEnergy)
        {
            throw new ArgumentOutOfRangeException(
                nameof(energy), energy, $"Energy must be between 0 and {MaxEnergy} (BR-P1).");
        }

        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(wateringCan);
        ArgumentNullException.ThrowIfNull(inventory);

        Gold = gold;
        Energy = energy;
        Tools = tools.ToFrozenSet();
        WateringCan = wateringCan;
        Inventory = inventory;
    }

    public Gold Gold { get; private set; }

    public int Energy { get; private set; }

    public IReadOnlySet<ToolId> Tools { get; }

    public WateringCan WateringCan { get; }

    public Inventory Inventory { get; }

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
    public bool TrySpendGold(int amount) => throw new NotImplementedException();
}
