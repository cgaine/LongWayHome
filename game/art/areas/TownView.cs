#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Art;

/// <summary>
/// Ember Bay's scenery (#136, ART_DIRECTION §7.3), from <see cref="TownLayout"/>: the grass ground, the road from the
/// north exit to the south exit, the round square, the general store at <see cref="StoreStage.MaggiesCorner"/>, the
/// carpenter at <see cref="CarpenterStage.Empty"/> and the blacksmith at <see cref="BlacksmithStage.Empty"/> (meshes
/// from <see cref="TownModels"/>), a few Twinbough trees as one <see cref="MultiMeshInstance3D"/> with trunk collision,
/// the river down the east side and the broken bridge stub on it (scenery only: across the river stays closed). Each
/// building's box collision and the river bank are in the scene. Built once on start; holds no rules.
/// </summary>
public partial class TownView : Node3D
{
    private const float HalfWidth = TownLayout.HalfWidth;
    private const float HalfDepth = TownLayout.HalfDepth;
    private const float RiverWestX = TownLayout.RiverWestX;

    private const float TrunkRadius = 0.42f;

    public override void _Ready()
    {
        var season = GetNode<GameRoot>("/root/GameRoot").Session?.Game.Date.Season ?? Season.Spring;
        var trees = TownLayout.Trees();

        AddChild(AreaArt.Flat("Ground", -HalfWidth - 6f, RiverWestX, -HalfDepth - 3f, HalfDepth + 3f, 0f, "grass-lit"));
        AddChild(AreaArt.Flat(
            "Road", -TownLayout.RoadHalfWidth, TownLayout.RoadHalfWidth, -HalfDepth - 3f, HalfDepth + 3f, 0.01f, "dirt-path"));
        AddChild(Square());
        AddChild(AreaArt.Flat("RiverShallows", RiverWestX, RiverWestX + 0.8f, -HalfDepth - 3f, HalfDepth + 3f, -0.02f, "lake-shallow"));
        AddChild(AreaArt.Flat("River", RiverWestX + 0.8f, HalfWidth + 8f, -HalfDepth - 3f, HalfDepth + 3f, -0.03f, "lake-deep"));
        AddChild(BridgeStub());

        foreach (var building in TownLayout.Buildings)
        {
            var mesh = building.Kind switch
            {
                TownBuildingKind.Store => TownModels.Store(StoreStage.MaggiesCorner),
                TownBuildingKind.Carpenter => TownModels.Carpenter(CarpenterStage.Empty),
                _ => TownModels.Blacksmith(BlacksmithStage.Empty),
            };
            AddChild(AreaArt.Model(building.Kind.ToString(), mesh, building.X, building.Z));
        }

        AddChild(AreaArt.Many("Trees", NatureModels.Twinbough(TreeStage.Full, season), trees));
        AddChild(AreaArt.Trunks("TreeTrunks", trees, TrunkRadius));
    }

    /// <summary>The square: a flat dirt disc around (0, 0), just above the road.</summary>
    private static MeshInstance3D Square()
    {
        var node = new MeshInstance3D
        {
            Name = "Square",
            Mesh = new CylinderMesh
            {
                TopRadius = TownLayout.SquareRadius,
                BottomRadius = TownLayout.SquareRadius,
                Height = 0.01f,
                RadialSegments = 24,
                Rings = 1,
            },
            Position = new Vector3(0f, 0.012f, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        ArtMaterials.Apply(node, "dirt-path");
        return node;
    }

    /// <summary>The broken bridge: a few planks reaching out from the west bank and stopping short, on two posts.</summary>
    private static Node3D BridgeStub()
    {
        var stub = new Node3D { Name = "BridgeStub", Position = new Vector3(RiverWestX, 0f, TownLayout.BridgeZ) };
        for (var i = 0; i < 5; i++)
        {
            // Each plank a little shorter than the last, so the end reads broken off.
            var width = 2.4f - (i % 2 * 0.5f);
            var plank = new MeshInstance3D
            {
                Name = $"Plank{i}",
                Mesh = new BoxMesh { Size = new Vector3(0.36f, 0.08f, width) },
                Position = new Vector3(-0.6f + (i * 0.42f), 0.25f, 0f),
            };
            ArtMaterials.Apply(plank, "timber");
            stub.AddChild(plank);
        }

        foreach (var z in new[] { -1.1f, 1.1f })
        {
            var post = new MeshInstance3D
            {
                Name = z < 0 ? "PostNorth" : "PostSouth",
                Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.9f, 0.22f) },
                Position = new Vector3(0.9f, 0.1f, z),
            };
            ArtMaterials.Apply(post, "timber-trim");
            stub.AddChild(post);
        }

        return stub;
    }
}
