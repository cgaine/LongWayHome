namespace LongWayHome.Domain.Games;

/// <summary>The outcome of a Player action: <see cref="Ok"/>, or the <see cref="Games.Rejection"/> that refused it.</summary>
public readonly record struct ActionResult(Rejection? Rejection)
{
    public static ActionResult Ok => new(null);

    public bool IsOk => Rejection is null;

    public static ActionResult Rejected(Rejection rejection) => new(rejection);
}
