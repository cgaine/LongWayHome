namespace LongWayHome.Application.Games;

/// <summary>Whether the mouse cursor shows: hidden during controller play, visible on keyboard and mouse.</summary>
public static class MouseCursor
{
    /// <summary>False for <see cref="InputDeviceKind.Controller"/>, true for <see cref="InputDeviceKind.KeyboardMouse"/>.</summary>
    public static bool IsVisible(InputDeviceKind device) => device != InputDeviceKind.Controller;
}
