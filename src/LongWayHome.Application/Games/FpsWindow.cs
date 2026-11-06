namespace LongWayHome.Application.Games;

/// <summary>The lowest frames-per-second sample over the last 10 seconds.</summary>
public sealed class FpsWindow
{
    /// <summary>Records a sample taken at <paramref name="nowSeconds"/>.</summary>
    public void Add(double nowSeconds, int fps) => throw new NotImplementedException();

    /// <summary>The lowest sample within 10 seconds of the latest one; 0 before any sample.</summary>
    public int Lowest => throw new NotImplementedException();
}
