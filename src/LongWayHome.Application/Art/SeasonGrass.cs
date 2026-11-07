using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

/// <summary>
/// The grass look for a Season (#79, ART_DIRECTION §7.2): the palette roles of the meadow ground, the mown margin, the
/// grass tufts and the shade patches, how tall the tufts stand and whether the shade patches show. Pure C#, no Godot.
/// </summary>
public sealed record SeasonGrass(
    string Ground,
    string Margin,
    string Tufts,
    string ShadePatches,
    float TuftHeightScale,
    bool ShowShadePatches)
{
    /// <summary>The grass look for <paramref name="season"/>.</summary>
    public static SeasonGrass For(Season season) => throw new NotImplementedException();
}
