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
    private static readonly SeasonGrass Spring = new("grass-mown", "grass-spring-mown", "grass-mown", "grass-shade", 1f, true);

    private static readonly SeasonGrass Summer = new("grass-lit", "grass-mown", "grass-lit", "grass-shade", 1f, true);

    private static readonly SeasonGrass Fall = new("grass-fall", "grass-fall-mown", "grass-fall", "grass-shade", 1f, true);

    /// <summary>Snow, with short grass tips showing through it and the shade patches hidden under it.</summary>
    private static readonly SeasonGrass Winter = new("snow", "snow", "grass-shade", "grass-shade", 0.5f, false);

    /// <summary>
    /// The grass look for <paramref name="season"/>: fresh green in spring, meadow green in summer, golden in fall and
    /// snow with grass tips in winter. The mown margin is a lighter shade of each Season's meadow.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="season"/> is not a defined Season.</exception>
    public static SeasonGrass For(Season season) => season switch
    {
        Season.Spring => Spring,
        Season.Summer => Summer,
        Season.Fall => Fall,
        Season.Winter => Winter,
        _ => throw new ArgumentOutOfRangeException(nameof(season), season, "not a Season"),
    };
}
