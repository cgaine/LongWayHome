#nullable enable

using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The lakeshore's scenery (#136, ART_DIRECTION §7), from <see cref="LakeshoreLayout"/>: the grass, the road down from
/// the north exit, the sandy shore
/// down to the waterline, the lake in the palette's lake colours (shallows along the shore, deep water beyond), the
/// river coming down the east side into it, a plank dock running out over the water (one
/// <see cref="MultiMeshInstance3D"/> of planks, on posts) and a few rocks on the shore (one more). The collision at the
/// waterline, the river bank and the dock's sides is in the scene. Built once on start; holds no rules.
/// </summary>
public partial class LakeshoreView : Node3D
{
    private const float HalfWidth = LakeshoreLayout.HalfWidth;
    private const float HalfDepth = LakeshoreLayout.HalfDepth;
    private const float WaterlineZ = LakeshoreLayout.WaterlineZ;
    private const float RiverWestX = LakeshoreLayout.RiverWestX;

    private const float DeckY = 0.08f;

    public override void _Ready()
    {
        AddChild(AreaArt.Flat("Ground", -HalfWidth - 6f, RiverWestX, -HalfDepth - 3f, LakeshoreLayout.ShoreNorthZ, 0f, "grass-lit"));
        AddChild(AreaArt.Flat("Road", -1.5f, 1.5f, -HalfDepth - 3f, LakeshoreLayout.ShoreNorthZ, 0.01f, "dirt-path"));
        AddChild(AreaArt.Flat("Shore",-HalfWidth - 6f, RiverWestX, LakeshoreLayout.ShoreNorthZ, WaterlineZ, 0f, "dirt-path"));
        AddChild(AreaArt.Flat("RiverShallows", RiverWestX, RiverWestX + 0.8f, -HalfDepth - 3f, WaterlineZ, -0.02f, "lake-shallow"));
        AddChild(AreaArt.Flat("River", RiverWestX + 0.8f, HalfWidth + 8f, -HalfDepth - 3f, WaterlineZ, -0.03f, "lake-deep"));
        AddChild(AreaArt.Flat("LakeShallows", -HalfWidth - 6f, HalfWidth + 8f, WaterlineZ, WaterlineZ + 1.5f, -0.02f, "lake-shallow"));
        AddChild(AreaArt.Flat("Lake", -HalfWidth - 6f, HalfWidth + 8f, WaterlineZ + 1.5f, HalfDepth + 12f, -0.03f, "lake-deep"));

        AddChild(Dock());
        AddChild(AreaArt.Many("Rocks", NatureModels.Rock(RockKind.Rock), LakeshoreLayout.Rocks()));
    }

    /// <summary>The dock: its planks as one MultiMesh, and a post at each corner and halfway along.</summary>
    private static Node3D Dock()
    {
        var dock = new Node3D { Name = "Dock" };
        var planks = LakeshoreLayout.DockPlanks();
        var width = LakeshoreLayout.DockEastX - LakeshoreLayout.DockWestX;
        var mesh = new BoxMesh { Size = new Vector3(width + 0.2f, 0.06f, LakeshoreLayout.PlankPitch - 0.05f) };
        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = planks.Count,
        };
        for (var i = 0; i < planks.Count; i++)
        {
            multimesh.SetInstanceTransform(i, new Transform3D(Basis.Identity, new Vector3(planks[i].X, DeckY, planks[i].Z)));
        }

        var deck = new MultiMeshInstance3D { Name = "Planks", Multimesh = multimesh };
        ArtMaterials.Apply(deck, "timber");
        dock.AddChild(deck);

        var middle = (LakeshoreLayout.DockNorthZ + LakeshoreLayout.DockSouthZ) / 2f;
        foreach (var x in new[] { LakeshoreLayout.DockWestX, LakeshoreLayout.DockEastX })
        {
            foreach (var z in new[] { WaterlineZ, middle, LakeshoreLayout.DockSouthZ })
            {
                var post = new MeshInstance3D
                {
                    Name = $"Post{dock.GetChildCount()}",
                    Mesh = new BoxMesh { Size = new Vector3(0.2f, 0.6f, 0.2f) },
                    Position = new Vector3(x, 0.05f, z - 0.1f),
                };
                ArtMaterials.Apply(post, "timber-trim");
                dock.AddChild(post);
            }
        }

        return dock;
    }
}
