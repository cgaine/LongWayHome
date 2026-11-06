namespace LongWayHome.Domain.Players;

/// <summary>The Watering can. Charges are always between 0 and capacity (BR-W1).</summary>
public sealed class WateringCan
{
    public WateringCan(int capacity) => throw new NotImplementedException();

    public int Capacity { get; }

    public int Charges { get; }

    public bool Upgraded { get; }
}
