using System.Text.RegularExpressions;
using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The button glyphs shown in prompts: <see cref="InputGlyphs"/>, checked against the real
/// <c>game/project.godot</c> Input Map and the table in issue #54 (REQ-13).</summary>
public class InputGlyphsTests
{
    public static TheoryData<string, InputDeviceKind, string> GlyphTable => new()
    {
        { "move", InputDeviceKind.KeyboardMouse, "[WASD]" },
        { "move", InputDeviceKind.Controller, "(L stick)" },
        { "move_up", InputDeviceKind.KeyboardMouse, "[WASD]" },
        { "move_up", InputDeviceKind.Controller, "(L stick)" },
        { "move_down", InputDeviceKind.KeyboardMouse, "[WASD]" },
        { "move_down", InputDeviceKind.Controller, "(L stick)" },
        { "move_left", InputDeviceKind.KeyboardMouse, "[WASD]" },
        { "move_left", InputDeviceKind.Controller, "(L stick)" },
        { "move_right", InputDeviceKind.KeyboardMouse, "[WASD]" },
        { "move_right", InputDeviceKind.Controller, "(L stick)" },
        { "run", InputDeviceKind.KeyboardMouse, "[Shift]" },
        { "run", InputDeviceKind.Controller, "(B)" },
        { "use_tool", InputDeviceKind.KeyboardMouse, "[Space]" },
        { "use_tool", InputDeviceKind.Controller, "(A)" },
        { "interact", InputDeviceKind.KeyboardMouse, "[F]" },
        { "interact", InputDeviceKind.Controller, "(X)" },
        { "tool_prev", InputDeviceKind.KeyboardMouse, "[Q]" },
        { "tool_prev", InputDeviceKind.Controller, "(LB)" },
        { "tool_next", InputDeviceKind.KeyboardMouse, "[E]" },
        { "tool_next", InputDeviceKind.Controller, "(RB)" },
        { "open_menu", InputDeviceKind.KeyboardMouse, "[Tab]" },
        { "open_menu", InputDeviceKind.Controller, "(Y)" },
        { "pause", InputDeviceKind.KeyboardMouse, "[Esc]" },
        { "pause", InputDeviceKind.Controller, "(Start)" },
        { "ui_cancel", InputDeviceKind.KeyboardMouse, "[Esc]" },
        { "ui_cancel", InputDeviceKind.Controller, "(B)" },
    };

    [Fact]
    public void AC1_every_input_map_action_has_a_glyph_on_both_devices()
    {
        var actions = ReadInputActionNames();
        Assert.NotEmpty(actions);
        Assert.Contains("tool_next", actions);
        Assert.Contains("move_up", actions);

        foreach (var action in actions.Append("ui_cancel"))
        {
            foreach (var device in Enum.GetValues<InputDeviceKind>())
            {
                var glyph = InputGlyphs.For(action, device);

                Assert.False(string.IsNullOrWhiteSpace(glyph), $"No glyph for '{action}' on {device}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(GlyphTable))]
    public void AC2_action_has_the_specified_glyph(string action, InputDeviceKind device, string expected)
    {
        Assert.Equal(expected, InputGlyphs.For(action, device));
    }

    [Theory]
    [InlineData("jump", InputDeviceKind.KeyboardMouse)]
    [InlineData("jump", InputDeviceKind.Controller)]
    [InlineData("", InputDeviceKind.KeyboardMouse)]
    [InlineData("USE_TOOL", InputDeviceKind.Controller)]
    public void AC3_unknown_action_throws(string action, InputDeviceKind device)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InputGlyphs.For(action, device));
    }

    [Fact]
    public void AC3_undefined_device_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InputGlyphs.For("use_tool", (InputDeviceKind)99));
    }

    [Theory]
    [InlineData(InputDeviceKind.KeyboardMouse, "[Q] [E] Change item")]
    [InlineData(InputDeviceKind.Controller, "(LB) (RB) Change item")]
    public void AC4_change_item_hint_names_tool_prev_and_tool_next(InputDeviceKind device, string expected)
    {
        Assert.Equal(expected, InputGlyphs.ChangeItemHint(device));
    }

    [Theory]
    [InlineData(InputDeviceKind.KeyboardMouse, "[Esc] Close")]
    [InlineData(InputDeviceKind.Controller, "(B) Close")]
    public void AC5_close_label_names_ui_cancel(InputDeviceKind device, string expected)
    {
        Assert.Equal(expected, InputGlyphs.CloseLabel(device));
    }

    private static List<string> ReadInputActionNames()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game", "project.godot");
        var lines = File.ReadAllLines(path);

        var section = lines
            .SkipWhile(line => line.Trim() != "[input]")
            .Skip(1)
            .TakeWhile(line => !Regex.IsMatch(line, @"^\[[a-z_]+\]\s*$"));

        return section
            .Select(line => Regex.Match(line, @"^(?<name>[A-Za-z0-9_]+)=\{"))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value)
            .ToList();
    }
}
