#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Core;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Art;

/// <summary>
/// Ember Bay's scenery (#136, ART_DIRECTION §7.3), from <see cref="TownLayout"/>: the grass ground, the road from the
/// north exit to the south exit, the round square, the general store at <see cref="StoreStage.MaggiesCorner"/>, the
/// carpenter at <see cref="CarpenterStage.Empty"/> and the blacksmith at <see cref="BlacksmithStage.Empty"/> (meshes
/// from <see cref="TownModels"/>), a few Twinbough trees as one <see cref="MultiMeshInstance3D"/> with trunk collision,
/// the river down the east side and the broken bridge stub on it (scenery only: across the river stays closed). Each
/// building's box collision and the river bank are in the scene. Built once on start, except the Rebuild sign in front
/// of the store (#163), drawn at its <c>"sign"</c> point while the session has one and redrawn on
/// <see cref="GameSession.BuildingsChanged"/>, its pips from <see cref="RebuildSignView"/>; holds no rules.
/// </summary>
public partial class TownView : Node3D
{
    private const float HalfWidth = TownLayout.HalfWidth;
    private const float HalfDepth = TownLayout.HalfDepth;
    private const float RiverWestX = TownLayout.RiverWestX;

    private const float TrunkRadius = 0.42f;

    /// <summary>The Rebuild sign's collision box: about its board's width, its height and a post's depth.</summary>
    private static readonly Vector3 SignCollisionSize = new(1.3f, 1.6f, 0.3f);

    private readonly List<Node3D> _signs = [];

    private GameSession? _session;

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

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is not null)
        {
            _session.BuildingsChanged += RebuildSigns;
            RebuildSigns();
        }
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.BuildingsChanged -= RebuildSigns;
        }
    }

    /// <summary>
    /// Draws a Rebuild sign at each <c>"sign"</c> <see cref="InteractionPoint"/> whose Building has one
    /// (<see cref="GameSession.RebuildSign"/> not null, BR-B4), facing +Z, with its pips from the view and a small box
    /// collision; redrawn on <see cref="GameSession.BuildingsChanged"/>.
    /// </summary>
    private void RebuildSigns()
    {
        foreach (var old in _signs)
        {
            RemoveChild(old);
            old.QueueFree();
        }

        _signs.Clear();
        foreach (var point in InteractionPoint.FindAll(GetTree().Root, "sign"))
        {
            if (RebuildSignPoint.Of(point) is not { } signPoint
                || signPoint.BuildingId is not { } building
                || _session!.RebuildSign(building) is not { } view)
            {
                continue;
            }

            var sign = new StaticBody3D
            {
                Name = $"RebuildSign_{signPoint.Building}",
                Position = ToLocal(point.GlobalPosition),
            };
            sign.AddChild(AreaArt.Model("Sign", TownModels.RebuildSign(view.WoodPip, view.StonePip, view.GoldPip), 0f, 0f));
            sign.AddChild(new CollisionShape3D
            {
                Name = "CollisionShape3D",
                Shape = new BoxShape3D { Size = SignCollisionSize },
                Position = new Vector3(0f, SignCollisionSize.Y / 2f, 0f),
            });
            AddChild(sign);
            _signs.Add(sign);
        }
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
