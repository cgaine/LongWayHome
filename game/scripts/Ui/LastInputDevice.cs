#nullable enable

using System;
using Godot;

namespace LongWayHome.Game.Ui;

/// <summary>The kind of device the Player last touched.</summary>
public enum InputDevice
{
    KeyboardMouse,
    Controller,
}

/// <summary>
/// Tracks the device the Player last used, registered as an autoload: key, mouse button and mouse motion events
/// mean keyboard and mouse; joypad buttons and stick motion beyond the deadzone mean a controller. Starts on keyboard
/// and mouse. Raises <see cref="DeviceChanged"/> when it changes, and gives the button glyph of an action for the
/// current device, so prompts follow the device in use (REQ-13). Holds no rules.
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
    public InputDevice Current { get; private set; } = InputDevice.KeyboardMouse;

    /// <summary>The glyph of <paramref name="action"/> on <paramref name="device"/>, e.g. "[Space]" or "(A)".</summary>
    public static string GlyphFor(string action, InputDevice device) => (action, device) switch
    {
        ("use_tool", InputDevice.KeyboardMouse) => "[Space]",
        ("use_tool", InputDevice.Controller) => "(A)",
        ("interact", InputDevice.KeyboardMouse) => "[F]",
        ("interact", InputDevice.Controller) => "(X)",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "No glyph for this action."),
    };

    /// <summary>The glyph of <paramref name="action"/> on the device the Player last used.</summary>
    public string GlyphFor(string action) => GlyphFor(action, Current);

    /// <summary>
    /// The glyph of <paramref name="action"/> on <paramref name="tracker"/>'s current device, or on keyboard and mouse
    /// when there is no tracker (e.g. a scene run without the autoload).
    /// </summary>
    public static string GlyphFor(string action, LastInputDevice? tracker) =>
        GlyphFor(action, tracker?.Current ?? InputDevice.KeyboardMouse);

    public override void _Input(InputEvent @event)
    {
        InputDevice? device = @event switch
        {
            InputEventKey or InputEventMouseButton or InputEventMouseMotion => InputDevice.KeyboardMouse,
            InputEventJoypadButton => InputDevice.Controller,
            InputEventJoypadMotion motion when Mathf.Abs(motion.AxisValue) > JoypadDeadzone => InputDevice.Controller,
            _ => null,
        };

        if (device is { } used && used != Current)
        {
            Current = used;
            DeviceChanged?.Invoke();
        }
    }
}
