namespace LongWayHome.Application.Games;

/// <summary>The HUD clock text for a clock time: 12-hour, minutes rounded down to 10, e.g. "6:00 am".</summary>
public static class ClockText
{
    private const int MinutesPerDay = 24 * 60;

    /// <summary>
    /// "6:00 am", "12:50 pm", "2:00 am": 12-hour, no leading zero, minutes rounded down to the nearest 10. Values of
    /// 1440 and above are past midnight.
    /// </summary>
    public static string For(int clockMinutes)
    {
        var minuteOfDay = ((clockMinutes % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
        var hour24 = minuteOfDay / 60;
        var tens = minuteOfDay % 60 / 10 * 10;
        var hour12 = hour24 % 12 == 0 ? 12 : hour24 % 12;
        var suffix = hour24 < 12 ? "am" : "pm";
        return $"{hour12}:{tens:00} {suffix}";
    }
}
