using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>The canonical display labels of Seasons (canonical-values.md).</summary>
public static class SeasonLabels
{
    /// <summary>"Spring", "Summer", "Fall" or "Winter".</summary>
    public static string For(Season season) => season switch
    {
        Season.Spring => "Spring",
        Season.Summer => "Summer",
        Season.Fall => "Fall",
        Season.Winter => "Winter",
        _ => throw new ArgumentOutOfRangeException(nameof(season), season, "Unknown Season."),
    };
}
