using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// Maps between an Area's grid positions and its world ground coordinates (#154): the forest path through
/// <see cref="ForestPathGrid"/>, every other Area through the farm's <see cref="FarmGridLayout"/> (their grids are the
/// farm's size for now, <see cref="AreaGrid"/>). Pure; Godot's views, the feet tile and the tool target use it.
/// </summary>
public static class AreaGridLayout
{
    /// <summary>The world ground centre (x, z) of the tile at <paramref name="p"/> on the Area's grid.</summary>
    public static (float X, float Z) ToWorld(AreaId area, GridPosition p) =>
        area == AreaId.ForestPath ? ForestPathGrid.ToWorld(p) : FarmGridLayout.ToWorld(p);

    /// <summary>The cell of the Area's grid under world (x, z), without checking it is on the grid.</summary>
    public static (int X, int Y) CellAt(AreaId area, float x, float z) =>
        area == AreaId.ForestPath ? ForestPathGrid.CellAt(x, z) : FarmGridLayout.CellAt(x, z);
}
