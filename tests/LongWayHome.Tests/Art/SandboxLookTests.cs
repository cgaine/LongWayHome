using LongWayHome.Game.Art;

namespace LongWayHome.Tests.Art;

/// <summary>The art sandbox's light preset and style switching (#75): <see cref="SandboxLook"/>.</summary>
public class SandboxLookTests
{
    [Fact]
    public void AC5_NextPreset_cycles_Morning_Day_Dusk_Night_and_wraps()
    {
        var look = SandboxLook.Default;
        var seen = new List<LightPreset>();

        for (var i = 0; i < 5; i++)
        {
            look = look.NextPreset();
            seen.Add(look.Preset);
        }

        Assert.Equal(
            new[] { LightPreset.Day, LightPreset.Dusk, LightPreset.Night, LightPreset.Morning, LightPreset.Day },
            seen);
    }

    [Fact]
    public void AC5_PreviousPreset_cycles_backwards_and_wraps()
    {
        var look = SandboxLook.Default;
        var seen = new List<LightPreset>();

        for (var i = 0; i < 5; i++)
        {
            look = look.PreviousPreset();
            seen.Add(look.Preset);
        }

        Assert.Equal(
            new[] { LightPreset.Night, LightPreset.Dusk, LightPreset.Day, LightPreset.Morning, LightPreset.Night },
            seen);
    }

    [Theory]
    [InlineData(LightPreset.Morning, "Morning · Both")]
    [InlineData(LightPreset.Day, "Day · Both")]
    [InlineData(LightPreset.Dusk, "Dusk · Both")]
    [InlineData(LightPreset.Night, "Night · Both")]
    public void AC5_Label_names_the_preset(LightPreset preset, string expected)
    {
        var look = new SandboxLook(preset, StyleMode.Both, false);

        Assert.Equal(expected, look.Label);
    }

    [Fact]
    public void AC6_Swapped_flips_the_sides_and_back()
    {
        var once = SandboxLook.Default.Swapped();
        var twice = once.Swapped();

        Assert.True(once.SidesSwapped);
        Assert.False(twice.SidesSwapped);
        Assert.Equal(SandboxLook.Default, twice);
    }

    [Fact]
    public void AC6_NextMode_cycles_Both_FlatOnly_SteppedOnly_and_wraps()
    {
        var look = SandboxLook.Default;
        var seen = new List<StyleMode>();

        for (var i = 0; i < 4; i++)
        {
            look = look.NextMode();
            seen.Add(look.Mode);
        }

        Assert.Equal(
            new[] { StyleMode.FlatOnly, StyleMode.SteppedOnly, StyleMode.Both, StyleMode.FlatOnly },
            seen);
    }

    [Theory]
    [InlineData(LightPreset.Dusk, StyleMode.FlatOnly, "Dusk · Flat only")]
    [InlineData(LightPreset.Night, StyleMode.SteppedOnly, "Night · Stepped only")]
    [InlineData(LightPreset.Day, StyleMode.Both, "Day · Both")]
    public void AC6_Label_names_the_mode(LightPreset preset, StyleMode mode, string expected)
    {
        var look = new SandboxLook(preset, mode, false);

        Assert.Equal(expected, look.Label);
    }

    [Fact]
    public void AC6_preset_changes_keep_mode_and_sides()
    {
        var look = new SandboxLook(LightPreset.Day, StyleMode.SteppedOnly, true);

        var next = look.NextPreset();
        var previous = look.PreviousPreset();

        Assert.Equal(new SandboxLook(LightPreset.Dusk, StyleMode.SteppedOnly, true), next);
        Assert.Equal(new SandboxLook(LightPreset.Morning, StyleMode.SteppedOnly, true), previous);
    }
}
