#nullable enable

using System;

namespace LongWayHome.Game.Art;

/// <summary>The four light presets of the art sandbox (ART_DIRECTION §3 time-of-day table).</summary>
public enum LightPreset { Morning, Day, Dusk, Night }

/// <summary>Which shading styles the art sandbox shows: both side by side, flat only or stepped only.</summary>
public enum StyleMode { Both, FlatOnly, SteppedOnly }

/// <summary>What the art sandbox is showing: the light preset, the style mode and whether the styles' sides are swapped.</summary>
public readonly record struct SandboxLook(LightPreset Preset, StyleMode Mode, bool SidesSwapped)
{
    /// <summary>The look the sandbox opens with: Morning, both styles, flat on the left.</summary>
    public static SandboxLook Default { get; } = new(LightPreset.Morning, StyleMode.Both, false);

    /// <summary>The next preset, Morning → Day → Dusk → Night → Morning.</summary>
    public SandboxLook NextPreset() => this with { Preset = Step(Preset, 1) };

    /// <summary>The previous preset, wrapping from Morning to Night.</summary>
    public SandboxLook PreviousPreset() => this with { Preset = Step(Preset, -1) };

    /// <summary>The same look with the flat and stepped sides swapped.</summary>
    public SandboxLook Swapped() => this with { SidesSwapped = !SidesSwapped };

    /// <summary>The next style mode, Both → Flat only → Stepped only → Both.</summary>
    public SandboxLook NextMode() => this with { Mode = Step(Mode, 1) };

    /// <summary>The <c>art_night</c> shader global for this look (#94): 1 under the Night preset, 0 under the others.</summary>
    public float ArtNight => throw new NotImplementedException();

    /// <summary>The on-screen label, "&lt;Preset&gt; · &lt;mode&gt;", e.g. "Dusk · Flat only".</summary>
    public string Label => $"{Preset} · {ModeName(Mode)}";

    private static string ModeName(StyleMode mode) => mode switch
    {
        StyleMode.FlatOnly => "Flat only",
        StyleMode.SteppedOnly => "Stepped only",
        _ => "Both",
    };

    /// <summary>The enum value <paramref name="by"/> places on from <paramref name="value"/>, wrapping at both ends.</summary>
    private static T Step<T>(T value, int by)
        where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var index = Array.IndexOf(values, value);
        return values[((index + by) % values.Length + values.Length) % values.Length];
    }
}
