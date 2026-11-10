namespace LongWayHome.Domain.Games;

/// <summary>
/// What a delivery at a Rebuild sign did (BR-B2): the amount delivered, or the <see cref="Games.Rejection"/> that
/// refused it (with 0 delivered).
/// </summary>
public readonly record struct DeliveryResult(int Delivered, Rejection? Rejection)
{
    public bool IsOk => Rejection is null;

    public static DeliveryResult Ok(int delivered) => new(delivered, null);

    public static DeliveryResult Rejected(Rejection rejection) => new(0, rejection);
}
