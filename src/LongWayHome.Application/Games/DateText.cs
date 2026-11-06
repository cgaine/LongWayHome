using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>The HUD date text for a <see cref="GameDate"/>, e.g. "Spring 1, Year 1".</summary>
public static class DateText
{
    /// <summary>"&lt;Season label&gt; &lt;day&gt;, Year &lt;year&gt;", e.g. "Spring 1, Year 1".</summary>
    public static string For(GameDate date) => $"{SeasonLabels.For(date.Season)} {date.Day}, Year {date.Year}";
}
