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
    /// <summary>
    /// Whether the axe and pickaxe have a target in <paramref name="area"/>: the farm and the forest path, whose grids
    /// hold their own ResourceNodes (#154). <see cref="GameSession.HasNodeGrid"/> reads this too, so the target and
    /// the session's Use decision cannot drift apart.
    /// </summary>
    public static bool HasNodeGrid(AreaId area) => area is AreaId.Farm or AreaId.ForestPath;

    /// <summary>The target in <paramref name="area"/> for the cell in front, or null when there is none.</summary>
    public static GridPosition? InFront(AreaId area, int cellX, int cellY, HeldItem? held)
    {
        var hasTarget = area == AreaId.Farm || (HasNodeGrid(area) && held?.Tool is ToolId.Axe or ToolId.Pickaxe);
        return hasTarget && AreaGrid.TryCreate(area, cellX, cellY, out var position) ? position : null;
    }
}
