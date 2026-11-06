#nullable enable

using System;
using System.Collections.Generic;

namespace LongWayHome.Game.Art;

/// <summary>One palette colour in the art sandbox: its role and its uppercase hex, e.g. "#7FA65A".</summary>
public readonly record struct Swatch(string Role, string Hex);

/// <summary>The colours the art sandbox's palette row shows: ART_DIRECTION §2, then the §7.2 ground colours.</summary>
public static class SandboxPalette
{
    /// <summary>Every swatch, in palette-row order.</summary>
    public static IReadOnlyList<Swatch> Swatches => throw new NotImplementedException();
}
