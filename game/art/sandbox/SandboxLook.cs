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
    public SandboxLook NextPreset() => throw new NotImplementedException();

    /// <summary>The previous preset, wrapping from Morning to Night.</summary>
    public SandboxLook PreviousPreset() => throw new NotImplementedException();

    /// <summary>The same look with the flat and stepped sides swapped.</summary>
    public SandboxLook Swapped() => throw new NotImplementedException();

    /// <summary>The next style mode, Both → Flat only → Stepped only → Both.</summary>
    public SandboxLook NextMode() => throw new NotImplementedException();

    /// <summary>The on-screen label, "&lt;Preset&gt; · &lt;mode&gt;", e.g. "Dusk · Flat only".</summary>
    public string Label => throw new NotImplementedException();
}
