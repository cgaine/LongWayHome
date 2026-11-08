using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>
/// The tile the Player's tools act on, given the cell in front of the farmer (#154): on the farm, that cell when it is
/// on the plot (BR-T1); on the forest path, that cell of its 14 x 40 grid only while the axe or pickaxe is held, as
/// only they have a target there; anywhere else, none. Pure; Godot's <c>ToolTargeting</c> supplies the cell.
/// </summary>
public static class UseTarget
{
    /// <summary>The target in <paramref name="area"/> for the cell in front, or null when there is none.</summary>
    public static GridPosition? InFront(AreaId area, int cellX, int cellY, HeldItem? held)
    {
        if (area == AreaId.Farm)
        {
            return GridPosition.TryCreate(cellX, cellY, out var onPlot) ? onPlot : null;
        }

        if (area == AreaId.ForestPath && held?.Tool is ToolId.Axe or ToolId.Pickaxe)
        {
            return AreaGrid.TryCreate(area, cellX, cellY, out var onPath) ? onPath : null;
        }

        return null;
    }
}
