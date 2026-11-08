#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Art;

/// <summary>
/// The farm's trees, stumps and rocks (#151, REQ-16), drawn from the <see cref="GameRoot"/> session's
/// <see cref="GameSession.ResourceNodes"/> on their Tiles through <see cref="FarmGridLayout"/>, as
/// <see cref="ForestPathView"/> draws its trees: one <see cref="MultiMeshInstance3D"/> per kind, stage and Season mesh
/// (the Twinbough <c>Full</c> in today's Season, the <c>Stump</c>, and <c>RockKind.Rock</c>), so the whole meadow's
/// nodes are three draws (REQ-14). Each tree and stump gets a trunk cylinder and each rock a box, so the farmer walks
/// under the canopies but not through a node. Rebuilt on <see cref="GameSession.NodesChanged"/>, which a Night also
/// raises, so the trees follow the Season. Only reads the session; holds no rules.
/// </summary>
public partial class ResourceNodesView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    /// <summary>A full Twinbough's trunk radius at its foot, as on the forest path.</summary>
    private const float TreeTrunkRadius = 0.42f;

    /// <summary>The stump's bark radius (its 0.9 m diameter).</summary>
    private const float StumpRadius = 0.45f;

    /// <summary>A rock is about 0.9 m across; its box stands taller than the rock so the farmer can't step over it.</summary>
    private static readonly Vector3 RockBox = new(0.9f, 1f, 0.9f);

    private GameSession? _session;

    public override void _Ready()
    {
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting: there are no nodes to draw.
            return;
        }

        _session.NodesChanged += Rebuild;
        Rebuild();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NodesChanged -= Rebuild;
        }
    }

    /// <summary>Frees the last build and draws the session's farm nodes again, the trees in today's Season.</summary>
    private void Rebuild()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        var season = _session!.Game.Date.Season;
        var farmNodes = _session.ResourceNodes.Where(node => node.Area == AreaId.Farm).ToList();
        var trees = Props(farmNodes, NodeKind.Tree);
        var stumps = Props(farmNodes, NodeKind.Stump);
        var rocks = Props(farmNodes, NodeKind.Rock);

        AddChild(AreaArt.Many("Trees", NatureModels.Twinbough(TreeStage.Full, season), trees));
        AddChild(AreaArt.Many("Stumps", NatureModels.Twinbough(TreeStage.Stump, season), stumps));
        AddChild(AreaArt.Many("Rocks", NatureModels.Rock(RockKind.Rock), rocks));
        AddChild(AreaArt.Trunks("TreeTrunks", trees, TreeTrunkRadius));
        AddChild(AreaArt.Trunks("StumpTrunks", stumps, StumpRadius));
        AddChild(RockBodies(rocks));
    }

    /// <summary>The nodes of <paramref name="kind"/> as props on their Tiles' centres, each turned a little differently.</summary>
    private static List<AreaProp> Props(IEnumerable<ResourceNode> nodes, NodeKind kind) =>
        nodes
            .Where(node => node.Kind == kind)
            .Select(node =>
            {
                var (x, z) = FarmGridLayout.ToWorld(node.Position);
                // A fixed yaw per Tile, so a node looks the same every time the view rebuilds.
                var yaw = ((node.Position.X * 47) + (node.Position.Y * 83)) % 360;
                return new AreaProp(x, z, yaw, 1f);
            })
            .ToList();

    /// <summary>One body with a box per rock, standing on the ground.</summary>
    private static StaticBody3D RockBodies(IReadOnlyList<AreaProp> rocks)
    {
        var body = new StaticBody3D { Name = "RockBoxes" };
        for (var i = 0; i < rocks.Count; i++)
        {
            var rock = rocks[i];
            body.AddChild(new CollisionShape3D
            {
                Name = $"Rock{i}",
                Shape = new BoxShape3D { Size = RockBox },
                Position = new Vector3(rock.X, RockBox.Y / 2f, rock.Z),
                RotationDegrees = new Vector3(0f, rock.YawDegrees, 0f),
            });
        }

        return body;
    }
}
