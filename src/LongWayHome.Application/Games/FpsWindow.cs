namespace LongWayHome.Application.Games;

/// <summary>The lowest frames-per-second sample over the last 10 seconds.</summary>
public sealed class FpsWindow
{
    /// <summary>How far back from the latest sample, in seconds, a sample still counts (inclusive).</summary>
    public const double WindowSeconds = 10;

    private readonly Queue<(double At, int Fps)> _samples = new();

    /// <summary>Records a sample taken at <paramref name="nowSeconds"/>.</summary>
    public void Add(double nowSeconds, int fps)
    {
        _samples.Enqueue((nowSeconds, fps));
        while (_samples.Peek().At < nowSeconds - WindowSeconds)
        {
            _samples.Dequeue();
        }
    }

    /// <summary>The lowest sample within 10 seconds of the latest one; 0 before any sample.</summary>
    public int Lowest => _samples.Count == 0 ? 0 : _samples.Min(s => s.Fps);
}
