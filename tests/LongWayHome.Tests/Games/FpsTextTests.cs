using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The FPS counter text (#55): <see cref="FpsText.For"/>.</summary>
public class FpsTextTests
{
    [Fact]
    public void AC5_fps_text_shows_the_current_fps_and_the_lowest_with_a_middle_dot()
    {
        Assert.Equal("30 fps · min 28", FpsText.For(30, 28));
    }
}
