using LongWayHome.Application.Ports;

namespace LongWayHome.Infrastructure.Random;

/// <summary>An <see cref="IRandomSource"/> over <see cref="System.Random.Shared"/>.</summary>
public sealed class SystemRandomSource : IRandomSource
{
    public int NextPercent() => throw new NotImplementedException();
}
