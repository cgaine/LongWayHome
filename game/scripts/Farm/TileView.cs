#nullable enable

using Godot;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws one Tile of the Farm plot: a 1 x 1 quad level with the ground and a faint outline, so the 12 x 9 grid is
/// visible. <see cref="Refresh"/> redraws it from the Tile's state. Holds no rules.
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

    private readonly StandardMaterial3D _surfaceMaterial = new();

    private MeshInstance3D? _surface;

    public override void _Ready() => EnsureBuilt();

    /// <summary>Redraws this view from <paramref name="tile"/>'s state.</summary>
    public void Refresh(Tile tile)
    {
        EnsureBuilt();
        _surfaceMaterial.AlbedoColor = tile.State == TileState.Tilled ? TilledColor : GrassColor;
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
    }

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
