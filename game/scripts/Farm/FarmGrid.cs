#nullable enable

using Godot;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Maps between Farm plot grid positions and world space. The plot spans x -6..6 and z -4.5..4.5 with its centre at
/// the origin and 1 tile = 1 unit; Y = 0 is the north row (-Z, nearest the house) and X = 0 the west column.
/// Holds no rules: whether a position is on the plot is decided by <see cref="GridPosition.TryCreate"/>.
/// </summary>
public static class FarmGrid
{
    private const float HalfWidth = FarmPlot.Width / 2f;

    private const float HalfHeight = FarmPlot.Height / 2f;

    /// <summary>The world-space centre of the tile at <paramref name="position"/>, on the ground (y = 0).</summary>
    public static Vector3 ToWorld(GridPosition position) =>
        new(position.X - HalfWidth + 0.5f, 0f, position.Y - HalfHeight + 0.5f);

    /// <summary>The tile under <paramref name="world"/> (height ignored); <c>false</c> when it is off the plot.</summary>
    public static bool TryToGrid(Vector3 world, out GridPosition position) =>
        GridPosition.TryCreate(
            Mathf.FloorToInt(world.X + HalfWidth),
            Mathf.FloorToInt(world.Z + HalfHeight),
            out position);
}
