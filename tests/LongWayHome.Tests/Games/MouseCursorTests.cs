using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>Whether the mouse cursor shows: <see cref="MouseCursor"/> (issue #54, AC7).</summary>
public class MouseCursorTests
{
    [Fact]
    public void AC7_cursor_is_hidden_on_controller()
    {
        Assert.False(MouseCursor.IsVisible(InputDeviceKind.Controller));
    }

    [Fact]
    public void AC7_cursor_is_visible_on_keyboard_and_mouse()
    {
        Assert.True(MouseCursor.IsVisible(InputDeviceKind.KeyboardMouse));
    }
}
