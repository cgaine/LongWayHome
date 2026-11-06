namespace LongWayHome.Application.Games;

/// <summary>The text glyph that names the button for an input action on each device. Holds no rules.</summary>
public static class InputGlyphs
{
    /// <summary>
    /// The glyph for <paramref name="action"/> on <paramref name="device"/>, e.g. "[Space]" / "(A)" for
    /// <c>use_tool</c>. <c>move</c> and each <c>move_*</c> action give "[WASD]" / "(L stick)". An unknown action
    /// throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public static string For(string action, InputDeviceKind device) => throw new NotImplementedException();

    /// <summary>The hotbar hint: "&lt;tool_prev glyph&gt; &lt;tool_next glyph&gt; Change item", e.g. "[Q] [E] Change item".</summary>
    public static string ChangeItemHint(InputDeviceKind device) => throw new NotImplementedException();

    /// <summary>The shop's Close button: "&lt;ui_cancel glyph&gt; Close", e.g. "[Esc] Close".</summary>
    public static string CloseLabel(InputDeviceKind device) => throw new NotImplementedException();
}
