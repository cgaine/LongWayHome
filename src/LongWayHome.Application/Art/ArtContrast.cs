using System.Globalization;

namespace LongWayHome.Application.Art;

/// <summary>WCAG 2.x relative luminance and contrast ratio for palette hexes (#78). Pure C#, no Godot.</summary>
public static class ArtContrast
{
    /// <summary>The WCAG 2.x relative luminance of a <c>#RRGGBB</c> colour, 0 (black) to 1 (white).</summary>
    /// <exception cref="ArgumentException"><paramref name="hex"/> is not <c>#RRGGBB</c>.</exception>
    public static double RelativeLuminance(string hex)
    {
        if (hex is not { Length: 7 } || hex[0] != '#')
        {
            throw new ArgumentException($"'{hex}' is not a #RRGGBB colour.", nameof(hex));
        }

        return (0.2126 * Linear(hex, 1)) + (0.7152 * Linear(hex, 3)) + (0.0722 * Linear(hex, 5));
    }

    /// <summary>The WCAG contrast ratio <c>(Lmax + 0.05) / (Lmin + 0.05)</c>, 1 to 21.</summary>
    /// <exception cref="ArgumentException">Either hex is not <c>#RRGGBB</c>.</exception>
    public static double Ratio(string hexA, string hexB)
    {
        var a = RelativeLuminance(hexA);
        var b = RelativeLuminance(hexB);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>One sRGB channel, the two hex digits at <paramref name="start"/>, converted to linear light.</summary>
    private static double Linear(string hex, int start)
    {
        if (!byte.TryParse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            throw new ArgumentException($"'{hex}' is not a #RRGGBB colour.", nameof(hex));
        }

        var c = value / 255.0;
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
