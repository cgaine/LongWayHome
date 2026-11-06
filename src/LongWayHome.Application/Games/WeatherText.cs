using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>The HUD display label for a <see cref="Weather"/>.</summary>
public static class WeatherText
{
    /// <summary>"Clear" or "Rain", the display labels in canonical-values.md.</summary>
    public static string For(Weather weather) => weather switch
    {
        Weather.Clear => "Clear",
        Weather.Rain => "Rain",
        _ => throw new ArgumentOutOfRangeException(nameof(weather), weather, "Unknown weather."),
    };
}
