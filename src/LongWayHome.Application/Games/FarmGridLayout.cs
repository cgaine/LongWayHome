using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Games;

/// <summary>
/// Maps between Farm plot grid positions and world ground coordinates (#150): tile (X, Y) is centred at world
/// (X − 36 + 0.5, Y − 4.5 + 0.5), so the grid covers x −36..36 and z −4.5..31.5. Pure; Godot's <c>FarmGrid</c> delegates
/// here.
/// </summary>
public static class FarmGridLayout
{
    /// <summary>
    /// How far west of world x 0 the grid's west edge lies: the Phase 1 block stays centred on world x 0, where the old
    /// 12 x 9 plot was, so its west edge stays at x −6.
    /// </summary>
    private static readonly float OriginX = FarmPlot.Phase1Block.OriginX + (FarmPlot.Phase1Block.Width / 2f);

    /// <summary>How far north of world z 0 the grid's north edge lies: the Phase 1 block stays centred on world z 0.</summary>
    private static readonly float OriginZ = FarmPlot.Phase1Block.OriginY + (FarmPlot.Phase1Block.Height / 2f);

    /// <summary>The world ground centre (x, z) of the tile at <paramref name="p"/>.</summary>
    public static (float X, float Z) ToWorld(GridPosition p) => (p.X - OriginX + 0.5f, p.Y - OriginZ + 0.5f);

    /// <summary>The grid cell under world (x, z), without checking it is on the plot.</summary>
    public static (int X, int Y) CellAt(float x, float z) =>
        ((int)MathF.Floor(x + OriginX), (int)MathF.Floor(z + OriginZ));
}
