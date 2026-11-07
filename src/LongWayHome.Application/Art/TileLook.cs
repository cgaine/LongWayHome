using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Art;

/// <summary>
/// How a Tile's slab looks (#78): the palette role of the slab, whether its grass tufts and soil furrows show, the
/// furrows' role and the shader's <c>wet</c> sheen (0 dry, 1 watered). Pure C#, no Godot.
/// </summary>
public sealed record TileLook(string SlabRole, bool ShowTufts, bool ShowFurrows, string FurrowRole, float Wet)
{
    private static readonly TileLook Grass = new("grass-lit", true, false, "soil-tilled", 0f);
    private static readonly TileLook TilledDry = new("soil-tilled", false, true, "soil-tilled", 0f);
    private static readonly TileLook TilledWatered = new("soil-watered", false, true, "soil-watered", 1f);

    /// <summary>The grass look in each Season, indexed by <see cref="Season"/>, built once so a Refresh allocates nothing.</summary>
    private static readonly TileLook[] GrassBySeason = Enum.GetValues<Season>()
        .Select(season => Grass with { SlabRole = SeasonGrass.For(season).Ground })
        .ToArray();

    /// <summary>
    /// The look for a Tile's current state and watered flag in Summer: grass is a green slab with tufts; tilled soil is
    /// a brown furrowed slab, much darker and wet while watered. A tilled Tile never shows tufts again, as it never
    /// reverts (BR-T8). A grass Tile's hidden furrows keep the dry soil role.
    /// </summary>
    public static TileLook For(Tile tile) => For(tile, Season.Summer);

    /// <summary>
    /// The look for a Tile in <paramref name="season"/> (#79): a grass Tile takes the Season's ground role
    /// (<see cref="SeasonGrass.Ground"/>); tilled and watered Tiles look the same all year (ART_DIRECTION §7.2) and never take the grass colour (BR-T8).
    /// </summary>
    public static TileLook For(Tile tile, Season season) => tile.State switch
    {
        TileState.Tilled => tile.Watered ? TilledWatered : TilledDry,
        _ => Enum.IsDefined(season) ? GrassBySeason[(int)season] : throw new ArgumentOutOfRangeException(nameof(season), season, "not a Season"),
    };
}
