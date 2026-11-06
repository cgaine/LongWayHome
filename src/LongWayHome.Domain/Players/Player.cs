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
        Tools = new HashSet<ToolId>(tools);
        WateringCan = wateringCan;
        Inventory = inventory;
    }

    public Gold Gold { get; }

    public int Energy { get; }

    public IReadOnlySet<ToolId> Tools { get; }

    public WateringCan WateringCan { get; }

    public Inventory Inventory { get; }
}
