using LongWayHome.Application.Ports;

namespace LongWayHome.Infrastructure.Random;

/// <summary>An <see cref="IRandomSource"/> over <see cref="System.Random.Shared"/>.</summary>
public sealed class SystemRandomSource : IRandomSource
{
    /// <summary>An integer from 0 to 99 inclusive, uniformly.</summary>
    public int NextPercent() => System.Random.Shared.Next(0, 100);
}
