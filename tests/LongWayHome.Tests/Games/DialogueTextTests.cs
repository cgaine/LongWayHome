using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>#138: the Talk hint and the dialogue box's hint line, with the glyphs of <see cref="InputGlyphs"/> (AC7,
/// AC8).</summary>
public class DialogueTextTests
{
    [Theory]
    [InlineData(InputDeviceKind.KeyboardMouse, "[F] Talk")]
    [InlineData(InputDeviceKind.Controller, "(X) Talk")]
    public void AC7_Talk_hint_is_the_interact_glyph_and_Talk(InputDeviceKind device, string expected)
    {
        Assert.Equal(expected, DialogueText.TalkHint(device));
    }

    [Theory]
    [InlineData(InputDeviceKind.KeyboardMouse, false, "[F] Next")]
    [InlineData(InputDeviceKind.Controller, false, "(X) Next")]
    [InlineData(InputDeviceKind.KeyboardMouse, true, "[F] Close")]
    [InlineData(InputDeviceKind.Controller, true, "(X) Close")]
    public void AC8_Advance_hint_reads_Next_or_Close_on_the_last_line(InputDeviceKind device, bool last, string expected)
    {
        Assert.Equal(expected, DialogueText.AdvanceHint(device, last));
    }

    [Theory]
    [InlineData(InputDeviceKind.KeyboardMouse, "[Tab] Skip")]
    [InlineData(InputDeviceKind.Controller, "(Y) Skip")]
    public void AC10_Skip_hint_is_the_open_menu_glyph_and_Skip(InputDeviceKind device, string expected)
    {
        Assert.Equal(expected, DialogueText.SkipHint(device));
    }
}
