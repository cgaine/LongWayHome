namespace LongWayHome.Domain.Players;

/// <summary>The Player. Energy is always between 0 and 150 (BR-P1).</summary>
public sealed class Player
{
    public Player(Gold gold, int energy, IReadOnlySet<ToolId> tools, WateringCan wateringCan, Inventory inventory) =>
        throw new NotImplementedException();

    public Gold Gold { get; }

    public int Energy { get; }

    public IReadOnlySet<ToolId> Tools { get; } = null!;

    public WateringCan WateringCan { get; } = null!;

    public Inventory Inventory { get; } = null!;
}
