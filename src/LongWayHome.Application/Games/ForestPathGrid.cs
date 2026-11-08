using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Games;

/// <summary>
/// Maps between forest path grid positions and world ground coordinates (#154): tile (X, Y) is centred at world
/// (X − 7 + 0.5, Y − 20 + 0.5), so the 14 x 40 grid covers x −7..7 and z −20..20.
/// </summary>
public static class ForestPathGrid
{
    /// <summary>The world ground centre (x, z) of the tile at <paramref name="p"/>.</summary>
    public static (float X, float Z) ToWorld(GridPosition p) => throw new NotImplementedException();

    /// <summary>The grid cell under world (x, z), without checking it is on the grid.</summary>
    public static (int X, int Y) CellAt(float x, float z) => throw new NotImplementedException();
}
