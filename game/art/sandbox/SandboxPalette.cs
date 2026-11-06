#nullable enable

using System.Collections.Generic;

namespace LongWayHome.Game.Art;

/// <summary>One palette colour in the art sandbox: its role and its uppercase hex, e.g. "#7FA65A".</summary>
public readonly record struct Swatch(string Role, string Hex);

/// <summary>The colours the art sandbox's palette row shows: ART_DIRECTION §2, then the §7.2 ground colours.</summary>
public static class SandboxPalette
{
    /// <summary>Every swatch, in palette-row order.</summary>
    public static IReadOnlyList<Swatch> Swatches { get; } =
    [
        // ART_DIRECTION §2 (the sky gradients are shown by the light presets instead; trunks come from the wood row).
        new("Meadow grass, lit", "#7FA65A"),
        new("Meadow grass, shade", "#3F7A6C"),
        new("Native foliage", "#3E8F82"),
        new("Leaf undersides", "#7A68A8"),
        new("Dry tilled soil", "#8A5A3C"),
        new("Watered soil", "#3E2A20"),
        new("Wood", "#9C7E8C"),
        new("Trunks", "#6B5A78"),
        new("Stone", "#8F8A96"),
        new("Lake, deep", "#2D6A86"),
        new("Lake, shallow", "#5FB3AA"),
        new("Town accent", "#E9773A"),
        new("Lantern light", "#FFB04A"),
        new("Glow: cool", "#6FF2E0"),
        new("Glow: rare", "#B48CFF"),
        new("Ship metal", "#C9D6E8"),

        // ART_DIRECTION §7.2, the farm plot and meadow ground.
        new("Mown margin", "#8CB862"),
        new("Rock", "#9A95A2"),
        new("Dirt path", "#B8946C"),
        new("Watered tile", "#4A3226"),
        new("Target highlight", "#FFF6E0"),
        new("Fall grass", "#A8A458"),
        new("Winter snow", "#E4ECF0"),
    ];
}
