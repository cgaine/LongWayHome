using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Art;

/// <summary>
/// How a Tile's slab looks (#78): the palette role of the slab, whether its grass tufts and soil furrows show, the
/// furrows' role and the shader's <c>wet</c> sheen (0 dry, 1 watered). Pure C#, no Godot.
/// </summary>
public sealed record TileLook(string SlabRole, bool ShowTufts, bool ShowFurrows, string FurrowRole, float Wet)
{
    /// <summary>The look for a Tile's current state and watered flag.</summary>
    public static TileLook For(Tile tile) => throw new NotImplementedException();
}
