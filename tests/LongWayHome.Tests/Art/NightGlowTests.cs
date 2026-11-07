using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// AC2 of #94: <see cref="NightGlow.At"/>, the clock-to-glow curve behind the <c>art_night</c> shader global. 0 up to
/// 19:30 (1170), rising linearly to 1 at 20:30 (1230), 1 until the day ends (1560, 02:00); clamped outside 360..1560.
/// </summary>
public class NightGlowTests
{
    private const float Tolerance = 1e-4f;

    [Theory]
    [InlineData(360, 0f)]
    [InlineData(720, 0f)]
    [InlineData(1170, 0f)]
    [InlineData(1200, 0.5f)]
    [InlineData(1230, 1f)]
    [InlineData(1260, 1f)]
    [InlineData(1560, 1f)]
    public void AC2_At_follows_the_curve_at_the_named_times(int clockMinutes, float expected)
    {
        Assert.Equal(expected, NightGlow.At(clockMinutes), Tolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(120)]
    [InlineData(359)]
    [InlineData(int.MinValue)]
    public void AC2_At_below_the_day_start_is_zero(int clockMinutes)
    {
        Assert.Equal(0f, NightGlow.At(clockMinutes), Tolerance);
    }

    [Theory]
    [InlineData(1561)]
    [InlineData(1800)]
    [InlineData(int.MaxValue)]
    public void AC2_At_above_the_day_end_is_one(int clockMinutes)
    {
        Assert.Equal(1f, NightGlow.At(clockMinutes), Tolerance);
    }

    [Fact]
    public void AC2_At_rises_strictly_between_1930_and_2030()
    {
        for (var minute = 1170; minute < 1230; minute++)
        {
            var now = NightGlow.At(minute);
            var next = NightGlow.At(minute + 1);
            Assert.True(next > now, $"At({minute + 1}) = {next} is not above At({minute}) = {now}");
        }
    }

    [Fact]
    public void AC2_At_never_falls_across_the_whole_day()
    {
        for (var minute = 360; minute < 1560; minute++)
        {
            var now = NightGlow.At(minute);
            var next = NightGlow.At(minute + 1);
            Assert.True(next >= now, $"At({minute + 1}) = {next} is below At({minute}) = {now}");
        }
    }

    [Fact]
    public void AC2_At_stays_between_zero_and_one()
    {
        for (var minute = -100; minute <= 1800; minute++)
        {
            Assert.InRange(NightGlow.At(minute), 0f, 1f);
        }
    }
}
