#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Art;

/// <summary>
/// One Area's trees, stumps and rocks (#151, REQ-16; the forest path's since #154), drawn from the
/// <see cref="GameRoot"/> session's <see cref="GameSession.ResourceNodes"/> in <see cref="Area"/> (an AreaId code, the
/// farm by default) on their Tiles through <see cref="AreaGridLayout"/>: one <see cref="MultiMeshInstance3D"/> per
/// kind, stage and Season mesh (the Twinbough <c>Full</c> in today's Season, the <c>Stump</c>, and
/// <c>RockKind.Rock</c>), so the whole Area's nodes are three draws (REQ-14). Each tree and stump gets a trunk cylinder
/// and each rock a box, so the farmer walks under the canopies but not through a node. Rebuilt on
/// <see cref="GameSession.NodesChanged"/>, which a Night also raises, so the trees follow the Season; a node cleared by
/// a swing (#152) is gone from that rebuild, mesh and collider both. On <see cref="GameSession.NodeSwung"/> in this
/// view's Area, a swing that leaves the node standing, its MultiMesh instance gets a short side-to-side shake through a
/// <see cref="Tween"/>. Only reads the session; holds no rules.
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

    /// <summary>How long a struck node shakes, and how far its top sways each way at first, in metres.</summary>
    private const float ShakeSeconds = 0.25f;

    private const float ShakeAmplitude = 0.06f;

    private GameSession? _session;

    private AreaId _area = AreaId.Farm;

    /// <summary>The AreaId code of the Area whose nodes this view draws, e.g. <c>forest_path</c>; the farm by default.</summary>
    [Export]
    public string Area { get; set; } = "farm";

    /// <summary>Each of this Area's nodes' Tile to its instance in the last build's MultiMesh, for the shake.</summary>
    private readonly Dictionary<GridPosition, (MultiMesh Mesh, int Index, Transform3D Resting)> _instances = [];

    private Tween? _shake;

    /// <summary>The instance the running shake moves, so a new shake can put it back first.</summary>
    private (MultiMesh Mesh, int Index, Transform3D Resting)? _shaking;

    public override void _Ready()
    {
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting: there are no nodes to draw.
            return;
        }

        if (!AreaCodes.TryParse(Area, out _area))
        {
            GD.PushError($"{Name}: \"{Area}\" is not an AreaId code");
            _session = null;
            return;
        }

        _session.NodesChanged += Rebuild;
        _session.NodeSwung += Shake;
        Rebuild();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NodesChanged -= Rebuild;
            _session.NodeSwung -= Shake;
        }
    }

    /// <summary>Frees the last build and draws the session's nodes in this Area again, the trees in today's Season.</summary>
    private void Rebuild()
    {
        _shake?.Kill();
        _shake = null;
        _shaking = null;
        _instances.Clear();
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        var season = _session!.Game.Date.Season;
        var nodes = _session.ResourceNodes.Where(node => node.Area == _area).ToList();
        var trees = Props(_area, nodes, NodeKind.Tree);
        var stumps = Props(_area, nodes, NodeKind.Stump);
        var rocks = Props(_area, nodes, NodeKind.Rock);

        AddMany("Trees", NatureModels.Twinbough(TreeStage.Full, season), nodes, NodeKind.Tree, trees);
        AddMany("Stumps", NatureModels.Twinbough(TreeStage.Stump, season), nodes, NodeKind.Stump, stumps);
        AddMany("Rocks", NatureModels.Rock(RockKind.Rock), nodes, NodeKind.Rock, rocks);
        AddChild(AreaArt.Trunks("TreeTrunks", trees, TreeTrunkRadius));
        AddChild(AreaArt.Trunks("StumpTrunks", stumps, StumpRadius));
        AddChild(RockBodies(rocks));
    }

    /// <summary>Adds the kind's MultiMesh and remembers which instance stands on each node's Tile.</summary>
    private void AddMany(string name, Mesh mesh, IEnumerable<ResourceNode> nodes, NodeKind kind, List<AreaProp> props)
    {
        var instance = AreaArt.Many(name, mesh, props);
        var index = 0;
        foreach (var node in nodes.Where(node => node.Kind == kind))
        {
            _instances[node.Position] = (instance.Multimesh, index, instance.Multimesh.GetInstanceTransform(index));
            index++;
        }

        AddChild(instance);
    }

    /// <summary>A short sway of the struck node's instance, damped back to where it stood (#152); view-only.</summary>
    private void Shake(GridPosition position)
    {
        // NodeSwung names only the Tile: the swing was in the Area the Player is in.
        if (_session!.CurrentArea != _area || !_instances.TryGetValue(position, out var instance))
        {
            return;
        }

        // One shake at a time: a new swing puts the node still shaking back where it stood, then restarts from the
        // struck node's resting transform, the one the build placed.
        _shake?.Kill();
        if (_shaking is { } last)
        {
            last.Mesh.SetInstanceTransform(last.Index, last.Resting);
        }

        var (mesh, index, rest) = instance;
        _shaking = instance;
        mesh.SetInstanceTransform(index, rest);
        _shake = CreateTween();
        _shake.TweenMethod(
            Callable.From<float>(t =>
            {
                var sway = Mathf.Sin(t * Mathf.Tau * 3f) * (1f - t) * ShakeAmplitude;
                mesh.SetInstanceTransform(index, rest.Translated(new Vector3(sway, 0f, 0f)));
            }),
            0f,
            1f,
            ShakeSeconds);
        _shake.TweenCallback(Callable.From(() =>
        {
            mesh.SetInstanceTransform(index, rest);
            _shaking = null;
        }));
    }

    /// <summary>The nodes of <paramref name="kind"/> as props on their Tiles' centres, each turned a little differently.</summary>
    private static List<AreaProp> Props(AreaId area, IEnumerable<ResourceNode> nodes, NodeKind kind) =>
        nodes
            .Where(node => node.Kind == kind)
            .Select(node =>
            {
                var (x, z) = AreaGridLayout.ToWorld(area, node.Position);
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
