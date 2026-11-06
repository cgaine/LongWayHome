namespace LongWayHome.Application.Games;

/// <summary>
/// When a menu takes focus back: with nothing focused, a navigation or accept press focuses the first button.
/// </summary>
public static class MenuFocus
{
    /// <summary>The actions that recover focus: <c>ui_up</c>, <c>ui_down</c>, <c>ui_left</c>, <c>ui_right</c>,
    /// <c>ui_focus_next</c> and <c>ui_accept</c>.</summary>
    public static IReadOnlyList<string> RecoveryActions { get; } =
        ["ui_up", "ui_down", "ui_left", "ui_right", "ui_focus_next", "ui_accept"];

    /// <summary>
    /// True when nothing is focused and <paramref name="action"/> is one of <see cref="RecoveryActions"/>; the menu
    /// then focuses its first button and consumes the press.
    /// </summary>
    public static bool ShouldRecover(bool somethingFocused, string action) =>
        !somethingFocused && RecoveryActions.Contains(action);
}
