#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Maps between Farm plot grid positions and world space through the pure <see cref="FarmGridLayout"/> (#150): the
/// 72 x 36 grid spans x −36..36 and z −4.5..31.5 with 1 tile = 1 unit, laid out so the Phase 1 block (X 30–41, Y 0–8)
/// stays where the old 12 x 9 plot was; Y = 0 is the north row (−Z, nearest the house) and X = 0 the west column.
/// Holds no rules: whether a position is on the plot is decided by <see cref="GridPosition.TryCreate"/>.
/// </summary>
public static class FarmGrid
{
    /// <summary>The world-space centre of the tile at <paramref name="position"/>, on the ground (y = 0).</summary>
    public static Vector3 ToWorld(GridPosition position)
    {
        var (x, z) = FarmGridLayout.ToWorld(position);
        return new Vector3(x, 0f, z);
    }

    /// <summary>The tile under <paramref name="world"/> (height ignored); <c>false</c> when it is off the plot.</summary>
    public static bool TryToGrid(Vector3 world, out GridPosition position)
    {
        var cell = CellAt(world);
        return GridPosition.TryCreate(cell.X, cell.Y, out position);
    }

    /// <summary>The grid cell under <paramref name="world"/> (height ignored), without checking it is on the plot.</summary>
    public static Vector2I CellAt(Vector3 world)
    {
        var (x, y) = FarmGridLayout.CellAt(world.X, world.Z);
        return new Vector2I(x, y);
    }
}
