#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws one Tile of the Farm plot: a 1 x 1 quad level with the ground and a faint outline, so the 12 x 9 grid is
/// visible, and any Planting on it (a sprout while Growing, a taller plant while Ripe). <see cref="Refresh"/> redraws
/// it from the Tile's state. Holds no rules.
/// </summary>
public partial class TileView : Node3D
{
    /// <summary>Height of the tile surface above the ground; just above the plot area mesh (y 0.01) in Farm.tscn.</summary>
    private const float SurfaceHeight = 0.015f;

    /// <summary>Height of the outline, just above the surface so the two never z-fight.</summary>
    private const float OutlineHeight = 0.02f;

    private const float Size = 1f;

    private static readonly Color GrassColor = new(0.45f, 0.62f, 0.3f);

    private static readonly Color TilledColor = new(0.45f, 0.3f, 0.18f);

    private static readonly Color OutlineColor = new(0.18f, 0.28f, 0.1f, 0.45f);

    private static readonly Color LeafColor = new(0.35f, 0.75f, 0.25f);

    private static readonly Color StemColor = new(0.25f, 0.55f, 0.18f);

    private static readonly Color RipeTopColor = new(0.95f, 0.45f, 0.15f);

    private readonly StandardMaterial3D _surfaceMaterial = new();

    private MeshInstance3D? _surface;

    private Node3D _sprout = null!;

    private Node3D _ripePlant = null!;

    public override void _Ready() => EnsureBuilt();

    /// <summary>
    /// Redraws this view from <paramref name="tile"/>'s state and Planting. The Planting's status comes from its
    /// Crop type in <paramref name="cropTypes"/>; one with no data is drawn as Growing.
    /// </summary>
    public void Refresh(Tile tile, IReadOnlyDictionary<CropTypeId, CropType> cropTypes)
    {
        EnsureBuilt();
        _surfaceMaterial.AlbedoColor = tile.State == TileState.Tilled ? TilledColor : GrassColor;

        var status = tile.Planting is { } planting
            ? cropTypes.TryGetValue(planting.CropTypeId, out var cropType)
                ? planting.StatusFor(cropType)
                : PlantingStatus.Growing
            : (PlantingStatus?)null;
        _sprout.Visible = status == PlantingStatus.Growing;
        _ripePlant.Visible = status == PlantingStatus.Ripe;
    }

    private void EnsureBuilt()
    {
        if (_surface is not null)
        {
            return;
        }

        _surface = new MeshInstance3D
        {
            Name = "Surface",
            Position = new Vector3(0f, SurfaceHeight, 0f),
            Mesh = new PlaneMesh { Size = new Vector2(Size, Size), Material = _surfaceMaterial },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _surfaceMaterial.AlbedoColor = GrassColor;
        AddChild(_surface);

        AddChild(new MeshInstance3D
        {
            Name = "Outline",
            Mesh = BuildOutline(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        _sprout = BuildSprout();
        AddChild(_sprout);
        _ripePlant = BuildRipePlant();
        AddChild(_ripePlant);
    }

    /// <summary>A Growing Planting: two small green leaves. Placeholder until the per-stage models of #26.</summary>
    private static Node3D BuildSprout()
    {
        var sprout = new Node3D { Name = "Sprout", Visible = false };
        var leaf = new PrismMesh { Size = new Vector3(0.08f, 0.22f, 0.04f), Material = Flat(LeafColor) };
        sprout.AddChild(new MeshInstance3D
        {
            Name = "LeafLeft",
            Mesh = leaf,
            Position = new Vector3(-0.04f, SurfaceHeight + 0.11f, 0f),
            RotationDegrees = new Vector3(0f, 0f, 20f),
        });
        sprout.AddChild(new MeshInstance3D
        {
            Name = "LeafRight",
            Mesh = leaf,
            Position = new Vector3(0.04f, SurfaceHeight + 0.11f, 0f),
            RotationDegrees = new Vector3(0f, 0f, -20f),
        });
        return sprout;
    }

    /// <summary>A Ripe Planting: a taller stem with a coloured top. Placeholder until the per-crop models of #26.</summary>
    private static Node3D BuildRipePlant()
    {
        const float stemHeight = 0.45f;
        var plant = new Node3D { Name = "RipePlant", Visible = false };
        plant.AddChild(new MeshInstance3D
        {
            Name = "Stem",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.03f,
                BottomRadius = 0.04f,
                Height = stemHeight,
                RadialSegments = 6,
                Rings = 1,
                Material = Flat(StemColor),
            },
            Position = new Vector3(0f, SurfaceHeight + (stemHeight / 2f), 0f),
        });
        plant.AddChild(new MeshInstance3D
        {
            Name = "Top",
            Mesh = new SphereMesh
            {
                Radius = 0.14f,
                Height = 0.28f,
                RadialSegments = 8,
                Rings = 4,
                Material = Flat(RipeTopColor),
            },
            Position = new Vector3(0f, SurfaceHeight + stemHeight + 0.08f, 0f),
        });
        return plant;
    }

    private static StandardMaterial3D Flat(Color color) => new() { AlbedoColor = color };

    private static ImmediateMesh BuildOutline()
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = OutlineColor,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        const float half = Size / 2f;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, material);
        mesh.SurfaceAddVertex(new Vector3(-half, OutlineHeight, -half));
        mesh.SurfaceAddVertex(new Vector3(half, OutlineHeight, -half));
        mesh.SurfaceAddVertex(new Vector3(half, OutlineHeight, half));
        mesh.SurfaceAddVertex(new Vector3(-half, OutlineHeight, half));
        mesh.SurfaceAddVertex(new Vector3(-half, OutlineHeight, -half));
        mesh.SurfaceEnd();
        return mesh;
    }
}
