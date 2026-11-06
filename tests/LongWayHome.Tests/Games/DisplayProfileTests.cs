using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The device display profile (#55): <see cref="DisplayProfile"/>.</summary>
public class DisplayProfileTests
{
    [Fact]
    public void AC1_IsSteamDeck_is_true_when_the_SteamDeck_variable_is_1()
    {
        Assert.True(DisplayProfile.IsSteamDeck("1"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    public void AC1_IsSteamDeck_is_false_when_the_SteamDeck_variable_is_missing_or_not_1(string? value)
    {
        Assert.False(DisplayProfile.IsSteamDeck(value));
    }

    [Fact]
    public void AC2_MaxFps_is_30_on_a_Steam_Deck()
    {
        Assert.Equal(30, DisplayProfile.MaxFps(true));
    }

    [Fact]
    public void AC2_MaxFps_is_0_uncapped_elsewhere()
    {
        Assert.Equal(0, DisplayProfile.MaxFps(false));
    }

    public static TheoryData<string[]> ArgsWithShowFps => new()
    {
        new[] { "--show-fps" },
        new[] { "--foo", "--show-fps" },
    };

    public static TheoryData<string[]> ArgsWithoutShowFps => new()
    {
        Array.Empty<string>(),
        new[] { "--fps" },
    };

    [Theory]
    [MemberData(nameof(ArgsWithShowFps))]
    public void AC6_ShowFps_is_true_when_the_user_args_contain_show_fps(string[] args)
    {
        Assert.True(DisplayProfile.ShowFps(args));
    }

    [Theory]
    [MemberData(nameof(ArgsWithoutShowFps))]
    public void AC6_ShowFps_is_false_without_the_show_fps_arg(string[] args)
    {
        Assert.False(DisplayProfile.ShowFps(args));
    }
}
