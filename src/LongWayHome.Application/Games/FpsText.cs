using System.Globalization;

namespace LongWayHome.Application.Games;

/// <summary>The FPS counter text, e.g. "30 fps · min 28".</summary>
public static class FpsText
{
    /// <summary>The current frames per second and the lowest in the last 10 seconds.</summary>
    public static string For(int fps, int lowest) =>
        string.Create(CultureInfo.InvariantCulture, $"{fps} fps · min {lowest}");
}
