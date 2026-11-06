using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The HUD clock text: <see cref="ClockText.For"/>.</summary>
public class ClockTextTests
{
    [Theory]
    [InlineData(360, "6:00 am")]
    [InlineData(725, "12:00 pm")]
    [InlineData(779, "12:50 pm")]
    [InlineData(1439, "11:50 pm")]
    [InlineData(1440, "12:00 am")]
    [InlineData(1500, "1:00 am")]
    [InlineData(1560, "2:00 am")]
    public void AC7_clock_text_is_12_hour_rounded_down_to_10_minutes(int clockMinutes, string expected)
    {
        var text = ClockText.For(clockMinutes);

        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(0, "12:00 am")]
    [InlineData(59, "12:50 am")]
    [InlineData(719, "11:50 am")]
    [InlineData(720, "12:00 pm")]
    [InlineData(1265, "9:00 pm")]
    [InlineData(366, "6:00 am")]
    [InlineData(370, "6:10 am")]
    public void Clock_text_covers_midnight_noon_and_the_10_minute_steps(int clockMinutes, string expected)
    {
        Assert.Equal(expected, ClockText.For(clockMinutes));
    }
}
