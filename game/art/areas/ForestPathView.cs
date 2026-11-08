#nullable enable

using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The forest path's scenery (#136, ART_DIRECTION §7), from <see cref="ForestPathLayout"/>: the grass ground, the
/// winding dirt path and the river down the east side. The Twinbough trees and the bank's rocks are ResourceNodes since
/// #154, drawn with their colliders by the scene's <see cref="ResourceNodesView"/> for <c>forest_path</c>. The area's
/// edges and the river bank are the scene's <c>Boundary</c>. Built once on start; holds no rules.
/// </summary>
public partial class ForestPathView : Node3D
{
    private const float HalfWidth = ForestPathLayout.HalfWidth;
    private const float HalfLength = ForestPathLayout.HalfLength;
    private const float RiverWestX = ForestPathLayout.RiverWestX;

    public override void _Ready()
    {
        // The ground runs a little past the edges so the camera never sees past it.
        AddChild(AreaArt.Flat("Ground", -HalfWidth - 6f, RiverWestX, -HalfLength - 3f, HalfLength + 3f, 0f, "grass-lit"));
        AddChild(AreaArt.Ribbon("Path", ForestPathLayout.PathCentre(1f), ForestPathLayout.PathHalfWidth, 0.01f, "dirt-path"));
        AddChild(AreaArt.Flat("RiverShallows", RiverWestX, RiverWestX + 0.8f, -HalfLength - 3f, HalfLength + 3f, -0.02f, "lake-shallow"));
        AddChild(AreaArt.Flat("River", RiverWestX + 0.8f, HalfWidth + 8f, -HalfLength - 3f, HalfLength + 3f, -0.03f, "lake-deep"));
    }
}
