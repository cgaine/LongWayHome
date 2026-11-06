namespace LongWayHome.Application.Art;

/// <summary>WCAG 2.x relative luminance and contrast ratio for palette hexes (#78). Pure C#, no Godot.</summary>
public static class ArtContrast
{
    /// <summary>The WCAG 2.x relative luminance of a <c>#RRGGBB</c> colour, 0 (black) to 1 (white).</summary>
    public static double RelativeLuminance(string hex) => throw new NotImplementedException();

    /// <summary>The WCAG contrast ratio <c>(Lmax + 0.05) / (Lmin + 0.05)</c>, 1 to 21.</summary>
    public static double Ratio(string hexA, string hexB) => throw new NotImplementedException();
}
