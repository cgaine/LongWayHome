using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The rolling 10-second minimum behind the FPS counter (#55): <see cref="FpsWindow"/>.</summary>
public class FpsWindowTests
{
    [Fact]
    public void AC6_lowest_is_0_before_any_sample()
    {
        var w = new FpsWindow();

        Assert.Equal(0, w.Lowest);
    }

    [Fact]
    public void AC6_a_single_sample_is_the_lowest()
    {
        var w = new FpsWindow();

        w.Add(0, 29);

        Assert.Equal(29, w.Lowest);
    }

    [Fact]
    public void AC6_lowest_is_the_smallest_sample_within_the_last_10_seconds()
    {
        var w = new FpsWindow();

        w.Add(0, 30);
        w.Add(2, 27);
        w.Add(4, 31);

        Assert.Equal(27, w.Lowest);
    }

    [Fact]
    public void AC6_a_sample_older_than_10_seconds_drops_out()
    {
        var w = new FpsWindow();

        w.Add(0, 20);
        w.Add(5, 30);
        w.Add(10.5, 31);

        Assert.Equal(30, w.Lowest);
    }

    [Fact]
    public void AC6_every_sample_older_than_10_seconds_drops_out_and_the_lowest_rises()
    {
        var w = new FpsWindow();

        w.Add(0, 18);
        w.Add(1, 19);
        w.Add(2, 25);
        w.Add(12.5, 30);

        Assert.Equal(30, w.Lowest);
    }

    [Fact]
    public void AC6_a_sample_exactly_10_seconds_old_still_counts()
    {
        var w = new FpsWindow();

        w.Add(0, 20);
        w.Add(10, 30);

        Assert.Equal(20, w.Lowest);
    }
}
