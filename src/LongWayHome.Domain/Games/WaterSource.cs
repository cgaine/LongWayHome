namespace LongWayHome.Domain.Games;

/// <summary>
/// A place the Watering can is refilled (BR-W3, BR-A2). Serialized as <c>pond</c> (the farm), <c>river</c> (the meadow
/// edge and town), <c>lake</c> (the lakeshore).
/// </summary>
public enum WaterSource
{
    Pond,
    River,
    Lake,
}
