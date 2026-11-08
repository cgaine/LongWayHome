using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Games;

/// <summary>
/// Maps between Farm plot grid positions and world ground coordinates (#150): tile (X, Y) is centred at world
/// (X − 36 + 0.5, Y − 4.5 + 0.5), so the grid covers x −36..36 and z −4.5..31.5. Pure; Godot's <c>FarmGrid</c> delegates
/// here.
/// </summary>
public static class FarmGridLayout
{
    /// <summary>The world ground centre (x, z) of the tile at <paramref name="p"/>.</summary>
    public static (float X, float Z) ToWorld(GridPosition p) => throw new NotImplementedException();

    /// <summary>The grid cell under world (x, z), without checking it is on the plot.</summary>
    public static (int X, int Y) CellAt(float x, float z) => throw new NotImplementedException();
}
