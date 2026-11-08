using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// Maps between forest path grid positions and world ground coordinates (#154): tile (X, Y) is centred at world
/// (X − 7 + 0.5, Y − 20 + 0.5), so the 14 x 40 grid covers x −7..7 and z −20..20.
/// </summary>
public static class ForestPathGrid
{
    /// <summary>How far west of world x 0 the grid's west edge lies: half the strip's 14 m width.</summary>
    private const float OriginX = AreaGrid.ForestPathWidth / 2f;

    /// <summary>How far north of world z 0 the grid's north edge lies: half the strip's 40 m length.</summary>
    private const float OriginZ = AreaGrid.ForestPathHeight / 2f;

    /// <summary>The world ground centre (x, z) of the tile at <paramref name="p"/>.</summary>
    public static (float X, float Z) ToWorld(GridPosition p) => (p.X - OriginX + 0.5f, p.Y - OriginZ + 0.5f);

    /// <summary>The grid cell under world (x, z), without checking it is on the grid.</summary>
    public static (int X, int Y) CellAt(float x, float z) =>
        ((int)MathF.Floor(x + OriginX), (int)MathF.Floor(z + OriginZ));
}
