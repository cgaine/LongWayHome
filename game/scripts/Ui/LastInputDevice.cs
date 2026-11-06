#nullable enable

using System;
using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Ui;

/// <summary>
/// Tracks the device the Player last used, registered as an autoload: key, mouse button and mouse motion events
/// mean keyboard and mouse; joypad buttons and stick motion beyond the deadzone mean a controller. Starts on keyboard
/// and mouse. Raises <see cref="DeviceChanged"/> when it changes, and gives the button glyph of an action for the
/// current device from <see cref="InputGlyphs"/>, so prompts follow the device in use (REQ-13). When the device
/// changes it also shows or hides the mouse cursor as <see cref="MouseCursor.IsVisible"/> says (hidden during
/// controller play); it always processes, so this works while a menu has paused the tree. Holds no rules.
/// </summary>
public partial class LastInputDevice : Node
{
    /// <summary>The path of the autoload.</summary>
    public const string Path = "/root/LastInputDevice";

    /// <summary>Stick motion below this is drift, not the Player touching the controller (the input map's deadzone).</summary>
    private const float JoypadDeadzone = 0.2f;

    /// <summary>Raised when <see cref="Current"/> changes.</summary>
    public event Action? DeviceChanged;

    /// <summary>The device the Player last used.</summary>
    public InputDeviceKind Current { get; private set; } = InputDeviceKind.KeyboardMouse;

    public override void _Ready()
    {
        // Keeps tracking while a menu has paused the tree, so the menu's prompts follow the device too.
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>The glyph of <paramref name="action"/> on <paramref name="device"/>, e.g. "[Space]" or "(A)"; see
    /// <see cref="InputGlyphs.For"/>.</summary>
    public static string GlyphFor(string action, InputDeviceKind device) => InputGlyphs.For(action, device);

    /// <summary>The glyph of <paramref name="action"/> on the device the Player last used.</summary>
    public string GlyphFor(string action) => GlyphFor(action, Current);

    /// <summary>
    /// The glyph of <paramref name="action"/> on <paramref name="tracker"/>'s current device, or on keyboard and mouse
    /// when there is no tracker (e.g. a scene run without the autoload).
    /// </summary>
    public static string GlyphFor(string action, LastInputDevice? tracker) => GlyphFor(action, DeviceOf(tracker));

    /// <summary>
    /// <paramref name="tracker"/>'s current device, or keyboard and mouse when there is no tracker (e.g. a scene run
    /// without the autoload).
    /// </summary>
    public static InputDeviceKind DeviceOf(LastInputDevice? tracker) => tracker?.Current ?? InputDeviceKind.KeyboardMouse;

    public override void _Input(InputEvent @event)
    {
        InputDeviceKind? device = @event switch
        {
            InputEventKey or InputEventMouseButton or InputEventMouseMotion => InputDeviceKind.KeyboardMouse,
            InputEventJoypadButton => InputDeviceKind.Controller,
            InputEventJoypadMotion motion when Mathf.Abs(motion.AxisValue) > JoypadDeadzone => InputDeviceKind.Controller,
            _ => null,
        };

        if (device is { } used && used != Current)
        {
            Current = used;
            Input.MouseMode = MouseCursor.IsVisible(used) ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;
            DeviceChanged?.Invoke();
        }
    }
}
