namespace LongWayHome.Application.Games;

/// <summary>The text glyph that names the button for an input action on each device. Holds no rules.</summary>
public static class InputGlyphs
{
    /// <summary>
    /// The glyph for <paramref name="action"/> on <paramref name="device"/>, e.g. "[Space]" / "(A)" for
    /// <c>use_tool</c>. <c>move</c> and each <c>move_*</c> action give "[WASD]" / "(L stick)". An unknown action
    /// throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public static string For(string action, InputDeviceKind device)
    {
        var (keyboardMouse, controller) = action switch
        {
            "move" or "move_up" or "move_down" or "move_left" or "move_right" => ("[WASD]", "(L stick)"),
            "run" => ("[Shift]", "(B)"),
            "use_tool" => ("[Space]", "(A)"),
            "interact" => ("[F]", "(X)"),
            "tool_prev" => ("[Q]", "(LB)"),
            "tool_next" => ("[E]", "(RB)"),
            "open_menu" => ("[Tab]", "(Y)"),
            "pause" => ("[Esc]", "(Start)"),
            "ui_cancel" => ("[Esc]", "(B)"),
            "ui_accept" => ("[Space]", "(A)"),
            "camera_rotate_left" => ("[Z]", "(R stick)"),
            "camera_rotate_right" => ("[C]", "(R stick)"),
            "camera_zoom_in" => ("[R]", "(R stick)"),
            "camera_zoom_out" => ("[V]", "(R stick)"),
            "hotbar_cycle" => ("[X]", "(View)"),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "No glyph for this action."),
        };

        return device switch
        {
            InputDeviceKind.KeyboardMouse => keyboardMouse,
            InputDeviceKind.Controller => controller,
            _ => throw new ArgumentOutOfRangeException(nameof(device), device, "Unknown input device."),
        };
    }

    /// <summary>The hotbar hint: "&lt;tool_prev glyph&gt; &lt;tool_next glyph&gt; Change item", e.g. "[Q] [E] Change item".</summary>
    public static string ChangeItemHint(InputDeviceKind device) =>
        $"{For("tool_prev", device)} {For("tool_next", device)} Change item";

    /// <summary>The hotbar row hint: "&lt;hotbar_cycle glyph&gt; Next row", e.g. "[X] Next row".</summary>
    public static string CycleRowHint(InputDeviceKind device) => $"{For("hotbar_cycle", device)} Next row";

    /// <summary>The shop's Close button: "&lt;ui_cancel glyph&gt; Close", e.g. "[Esc] Close".</summary>
    public static string CloseLabel(InputDeviceKind device) => $"{For("ui_cancel", device)} Close";
}
