namespace LongWayHome.Application.Games;

/// <summary>The kind of device the Player last used, which decides the button glyphs and the mouse cursor.</summary>
public enum InputDeviceKind
{
    /// <summary>Keyboard and mouse.</summary>
    KeyboardMouse,

    /// <summary>A controller (Xbox-style labels, as Steam Input presents them on Steam Deck).</summary>
    Controller,
}
