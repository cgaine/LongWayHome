using LongWayHome.Application.Ports;

namespace LongWayHome.Application.Games;

/// <summary>
/// An <see cref="IRandomSource"/> that always rolls 99, the highest roll, so the weather is never rain at any rain
/// chance below 100 (BR-G4). Used where no random source is given.
/// </summary>
internal sealed class NeverRainSource : IRandomSource
{
    public static readonly NeverRainSource Instance = new();

    private NeverRainSource()
    {
    }

    public int NextPercent() => 99;
}
