#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Art;

/// <summary>
/// The forest path's scenery (#136, ART_DIRECTION §7), from <see cref="ForestPathLayout"/>: the grass ground, the
/// winding dirt path, the river down the east side, and the Twinbough trees in today's season as one
/// <see cref="MultiMeshInstance3D"/>, each with trunk-only collision so the Player walks under the canopies. The area's
/// edges and the river bank are the scene's <c>Boundary</c>. Built once on start; holds no rules.
/// </summary>
public partial class ForestPathView : Node3D
{
    private const float HalfWidth = ForestPathLayout.HalfWidth;
    private const float HalfLength = ForestPathLayout.HalfLength;
    private const float RiverWestX = ForestPathLayout.RiverWestX;

    /// <summary>A full Twinbough's trunk radius at its foot.</summary>
    private const float TrunkRadius = 0.42f;

    public override void _Ready()
    {
        var season = GetNode<GameRoot>("/root/GameRoot").Session?.Game.Date.Season ?? Season.Spring;
        var trees = ForestPathLayout.Trees();

        // The ground runs a little past the edges so the camera never sees past it.
        AddChild(AreaArt.Flat("Ground", -HalfWidth - 6f, RiverWestX, -HalfLength - 3f, HalfLength + 3f, 0f, "grass-lit"));
        AddChild(AreaArt.Ribbon("Path", ForestPathLayout.PathCentre(1f), ForestPathLayout.PathHalfWidth, 0.01f, "dirt-path"));
        AddChild(AreaArt.Flat("RiverShallows", RiverWestX, RiverWestX + 0.8f, -HalfLength - 3f, HalfLength + 3f, -0.02f, "lake-shallow"));
        AddChild(AreaArt.Flat("River", RiverWestX + 0.8f, HalfWidth + 8f, -HalfLength - 3f, HalfLength + 3f, -0.03f, "lake-deep"));
        AddChild(AreaArt.Many("Trees", NatureModels.Twinbough(TreeStage.Full, season), trees));
        AddChild(AreaArt.Trunks("TreeTrunks", trees, TrunkRadius));
    }
}
