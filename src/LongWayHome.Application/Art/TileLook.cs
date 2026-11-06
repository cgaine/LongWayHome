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

    /// <summary>
    /// The look for a Tile's current state and watered flag: grass is a green slab with tufts; tilled soil is a brown
    /// furrowed slab, much darker and wet while watered. A tilled Tile never shows tufts again, as it never reverts
    /// (BR-T8). A grass Tile's hidden furrows keep the dry soil role.
    /// </summary>
    public static TileLook For(Tile tile) => tile.State switch
    {
        TileState.Tilled => tile.Watered ? TilledWatered : TilledDry,
        _ => Grass,
    };
}
