using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>When a menu takes focus back: <see cref="MenuFocus"/> (issue #54, AC6).</summary>
public class MenuFocusTests
{
    public static TheoryData<string> RecoveryActions =>
        ["ui_up", "ui_down", "ui_left", "ui_right", "ui_focus_next", "ui_accept"];

    [Theory]
    [MemberData(nameof(RecoveryActions))]
    public void AC6_navigation_or_accept_press_with_nothing_focused_recovers_focus(string action)
    {
        Assert.True(MenuFocus.ShouldRecover(somethingFocused: false, action));
    }

    [Theory]
    [InlineData("ui_cancel")]
    [InlineData("interact")]
    [InlineData("pause")]
    [InlineData("use_tool")]
    [InlineData("ui_focus_prev")]
    [InlineData("")]
    public void AC6_other_press_with_nothing_focused_does_not_recover(string action)
    {
        Assert.False(MenuFocus.ShouldRecover(somethingFocused: false, action));
    }

    [Theory]
    [MemberData(nameof(RecoveryActions))]
    [InlineData("ui_cancel")]
    public void AC6_press_while_a_control_is_focused_does_not_recover(string action)
    {
        Assert.False(MenuFocus.ShouldRecover(somethingFocused: true, action));
    }

    [Fact]
    public void AC6_recovery_actions_are_exactly_the_six_navigation_and_accept_actions()
    {
        Assert.Equal(
            new[] { "ui_accept", "ui_down", "ui_focus_next", "ui_left", "ui_right", "ui_up" },
            MenuFocus.RecoveryActions.OrderBy(action => action, StringComparer.Ordinal).ToArray());
    }
}
