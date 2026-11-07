using System.Globalization;
using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>game/project.godot</c>, linked into the test output by the test project, and checks its
/// Input Map: every action the game uses is bound to keyboard and mouse and to a controller (REQ-13).
/// </summary>
public class InputMapTests
{
    private static readonly IReadOnlyDictionary<string, InputAction> Actions = ReadInputMap();

    public static TheoryData<string> AllActions =>
    [
        "move_up", "move_down", "move_left", "move_right", "run", "use_tool",
        "interact", "tool_prev", "tool_next", "open_menu", "pause",
        "camera_rotate_left", "camera_rotate_right", "camera_zoom_in", "camera_zoom_out", "hotbar_cycle",
    ];

    private static readonly string[] MoveActionNames = ["move_up", "move_down", "move_left", "move_right"];

    public static TheoryData<string> MoveActions => new(MoveActionNames);

    [Theory]
    [MemberData(nameof(AllActions))]
    public void AC1_action_is_bound_to_keyboard_or_mouse_and_to_a_controller(string action)
    {
        Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");
        var events = Actions[action].EventTypes;

        Assert.Contains(events, type => type is "InputEventKey" or "InputEventMouseButton");
        Assert.Contains(events, type => type is "InputEventJoypadButton" or "InputEventJoypadMotion");
    }

    [Theory]
    [MemberData(nameof(MoveActions))]
    public void Move_action_has_a_stick_deadzone_of_0_2(string action)
    {
        Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");
        Assert.Equal(0.2, Actions[action].Deadzone, precision: 3);
    }

    [Fact]
    public void Move_actions_are_bound_to_both_the_left_stick_and_the_d_pad()
    {
        foreach (var action in MoveActionNames)
        {
            Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");
            Assert.Contains("InputEventJoypadMotion", Actions[action].EventTypes);
            Assert.Contains("InputEventJoypadButton", Actions[action].EventTypes);
        }
    }

    [Theory]
    [InlineData("move_up", 1, "-1.0", 11)]
    [InlineData("move_down", 1, "1.0", 12)]
    [InlineData("move_left", 0, "-1.0", 13)]
    [InlineData("move_right", 0, "1.0", 14)]
    public void Move_action_uses_the_matching_left_stick_direction_and_d_pad_button(string action, int axis, string axisValue, int dpadButton)
    {
        Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");
        var body = Actions[action].Body;

        Assert.Contains($"\"axis\":{axis},\"axis_value\":{axisValue},", body);
        Assert.Matches($@"InputEventJoypadButton,[^)]*""button_index"":{dpadButton},", body);
    }

    [Theory]
    [InlineData("tool_prev", 81)]
    [InlineData("tool_next", 69)]
    [InlineData("use_tool", 32)]
    public void AC9_tool_action_is_bound_to_its_key(string action, int physicalKeycode)
    {
        Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");

        Assert.Matches($@"InputEventKey,(?:(?!Object\().)*""physical_keycode"":{physicalKeycode},", Actions[action].Body);
    }

    [Theory]
    [InlineData("tool_prev", 4)]
    [InlineData("tool_next", 5)]
    [InlineData("use_tool", 1)]
    public void AC9_tool_action_is_bound_to_its_mouse_button(string action, int buttonIndex)
    {
        Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");

        Assert.Matches($@"InputEventMouseButton,(?:(?!Object\().)*""button_index"":{buttonIndex},", Actions[action].Body);
    }

    [Theory]
    [InlineData("tool_prev", 9)]
    [InlineData("tool_next", 10)]
    [InlineData("use_tool", 0)]
    public void AC10_tool_action_is_bound_to_its_controller_button(string action, int buttonIndex)
    {
        Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");

        Assert.Matches($@"InputEventJoypadButton,(?:(?!Object\().)*""button_index"":{buttonIndex},", Actions[action].Body);
    }

    [Theory]
    [InlineData("camera_rotate_left", 90, 2, "-1.0")]
    [InlineData("camera_rotate_right", 67, 2, "1.0")]
    [InlineData("camera_zoom_in", 82, 3, "-1.0")]
    [InlineData("camera_zoom_out", 86, 3, "1.0")]
    public void AC2_camera_action_is_bound_to_its_key_and_right_stick_direction_with_deadzone_0_2(
        string action, int physicalKeycode, int axis, string axisValue)
    {
        Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");
        var body = Actions[action].Body;

        Assert.Matches($@"InputEventKey,(?:(?!Object\().)*""physical_keycode"":{physicalKeycode},", body);
        Assert.Contains($"\"axis\":{axis},\"axis_value\":{axisValue},", body);
        Assert.Equal(0.2, Actions[action].Deadzone, precision: 3);
    }

    [Fact]
    public void AC9_hotbar_cycle_is_bound_to_the_X_key()
    {
        Assert.True(Actions.ContainsKey("hotbar_cycle"), "project.godot has no input action 'hotbar_cycle'");

        Assert.Matches(@"InputEventKey,(?:(?!Object\().)*""physical_keycode"":88,", Actions["hotbar_cycle"].Body);
    }

    [Fact]
    public void AC9_hotbar_cycle_is_bound_to_the_controller_View_button()
    {
        Assert.True(Actions.ContainsKey("hotbar_cycle"), "project.godot has no input action 'hotbar_cycle'");

        Assert.Matches(@"InputEventJoypadButton,(?:(?!Object\().)*""button_index"":4,", Actions["hotbar_cycle"].Body);
    }

    [Fact]
    public void Camera_actions_leave_the_mouse_wheel_on_the_held_item_cycle()
    {
        foreach (var action in new[] { "camera_rotate_left", "camera_rotate_right", "camera_zoom_in", "camera_zoom_out" })
        {
            Assert.True(Actions.ContainsKey(action), $"project.godot has no input action '{action}'");
            Assert.DoesNotContain("InputEventMouseButton", Actions[action].EventTypes);
        }
    }

    private sealed record InputAction(double Deadzone, IReadOnlyList<string> EventTypes, string Body);

    private static Dictionary<string, InputAction> ReadInputMap()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game", "project.godot");
        var lines = File.ReadAllLines(path);

        var section = lines
            .SkipWhile(line => line.Trim() != "[input]")
            .Skip(1)
            .TakeWhile(line => !Regex.IsMatch(line, @"^\[[a-z_]+\]\s*$"));
        var text = string.Join("\n", section);

        var actions = new Dictionary<string, InputAction>();
        foreach (Match entry in Regex.Matches(text, @"^(?<name>[A-Za-z0-9_]+)=\{(?<body>.*?)^\}", RegexOptions.Multiline | RegexOptions.Singleline))
        {
            var body = entry.Groups["body"].Value;
            var deadzone = Regex.Match(body, @"""deadzone""\s*:\s*(?<value>[0-9.]+)");
            var types = Regex.Matches(body, @"Object\((?<type>[A-Za-z0-9_]+),")
                .Select(match => match.Groups["type"].Value)
                .ToList();

            actions[entry.Groups["name"].Value] = new InputAction(
                deadzone.Success ? double.Parse(deadzone.Groups["value"].Value, CultureInfo.InvariantCulture) : double.NaN,
                types,
                body);
        }

        return actions;
    }
}
