using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Games;

/// <summary>
/// The farmer's feet tile: the plot tile nearest a grid cell, clamped to 0..FarmPlot.Width − 1 and
/// 0..FarmPlot.Height − 1, so dropping and picking up beside the plot use the same tile. Godot supplies the cell.
/// </summary>
public static class FeetTile
{
    public static GridPosition Nearest(int cellX, int cellY) =>
        new(Math.Clamp(cellX, 0, FarmPlot.Width - 1), Math.Clamp(cellY, 0, FarmPlot.Height - 1));
}
