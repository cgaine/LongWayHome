#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Art;

/// <summary>
/// The fences and chests on the farm (#164, REQ-48), drawn from the <see cref="GameRoot"/> session's
/// <see cref="FarmPlot.PlacedObjects"/> on their Tiles through <see cref="AreaGridLayout"/>: one
/// <see cref="MeshInstance3D"/> of <see cref="PlaceableArt"/>'s parts per object under the shared palette material,
/// and a <see cref="StaticBody3D"/> with a box per object, so the farmer can't walk through it. Rebuilt on
/// <see cref="GameSession.PlacedObjectsChanged"/>. Only reads the session; holds no rules.
/// </summary>
public partial class PlacedObjectsView : Node3D
{
    private const string GameRootPath = "/root/GameRoot";

    /// <summary>The collision box of a fence section, a little taller than it so the farmer can't step over.</summary>
    private static readonly Vector3 FenceBox = new(1f, 1.2f, 0.3f);

    /// <summary>The collision box of a chest.</summary>
    private static readonly Vector3 ChestBox = new(0.8f, 1f, 0.6f);

    private static readonly Dictionary<PlaceableId, Mesh> Meshes = [];

    private GameSession? _session;

    public override void _Ready()
    {
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting: there is nothing to draw.
            return;
        }

        _session.PlacedObjectsChanged += Rebuild;
        Rebuild();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.PlacedObjectsChanged -= Rebuild;
        }
    }

    /// <summary>Frees the last build and draws the session's placed objects again.</summary>
    private void Rebuild()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        var bodies = new StaticBody3D { Name = "PlacedBoxes" };
        var index = 0;
        foreach (var placed in _session!.Game.FarmPlot.PlacedObjects)
        {
            var (x, z) = AreaGridLayout.ToWorld(AreaId.Farm, placed.Position);
            AddChild(new MeshInstance3D
            {
                Name = $"{placed.Placeable}{index}",
                Mesh = MeshFor(placed.Placeable),
                MaterialOverride = ArtMaterials.Shared,
                Position = new Vector3(x, 0f, z),
            });

            var box = placed.Placeable == PlaceableId.Chest ? ChestBox : FenceBox;
            bodies.AddChild(new CollisionShape3D
            {
                Name = $"Box{index}",
                Shape = new BoxShape3D { Size = box },
                Position = new Vector3(x, box.Y / 2f, z),
            });
            index++;
        }

        AddChild(bodies);
    }

    private static Mesh MeshFor(PlaceableId placeable)
    {
        if (!Meshes.TryGetValue(placeable, out var mesh))
        {
            mesh = PartMeshes.Merge(PlaceableArt.For(placeable));
            Meshes[placeable] = mesh;
        }

        return mesh;
    }
}
