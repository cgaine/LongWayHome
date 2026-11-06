#nullable enable

using Godot;
using LongWayHome.Domain.Farming;
using LongWayHome.Game.Farm;

namespace LongWayHome.Game.Player;

/// <summary>
/// The tile the Player's tools act on: the Player's grid cell plus <see cref="PlayerController.Facing"/>. Draws a
/// bright 1 x 1 outline on it, hidden when the tile in front is off the plot. A child of the Player. Holds no rules:
/// whether a position is on the plot is decided by <see cref="GridPosition.TryCreate"/>.
/// </summary>
public partial class ToolTargeting : Node3D
{
    /// <summary>Height of the cursor, above the tile surface and its outline so it is never hidden.</summary>
    private const float CursorHeight = 0.03f;

    private static readonly Color CursorColor = new(1f, 0.95f, 0.45f);

    private PlayerController _player = null!;

    private MeshInstance3D _cursor = null!;

    /// <summary>The tile in front of the Player, or null when that is off the plot.</summary>
    public GridPosition? Target
    {
        get
        {
            var cell = FarmGrid.CellAt(_player.GlobalPosition) + _player.Facing;
            return GridPosition.TryCreate(cell.X, cell.Y, out var position) ? position : null;
        }
    }

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();

        // Top level, so the cursor stays on the grid and does not move or turn with the Player.
        _cursor = new MeshInstance3D
        {
            Name = "Cursor",
            Mesh = BuildCursor(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            TopLevel = true,
            Visible = false,
        };
        AddChild(_cursor);
    }

    public override void _Process(double delta)
    {
        if (Target is { } target)
        {
            _cursor.GlobalPosition = FarmGrid.ToWorld(target) with { Y = CursorHeight };
            _cursor.Visible = true;
        }
        else
        {
            _cursor.Visible = false;
        }
    }

    private static ImmediateMesh BuildCursor()
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = CursorColor,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        const float half = 0.5f;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, material);
        mesh.SurfaceAddVertex(new Vector3(-half, 0f, -half));
        mesh.SurfaceAddVertex(new Vector3(half, 0f, -half));
        mesh.SurfaceAddVertex(new Vector3(half, 0f, half));
        mesh.SurfaceAddVertex(new Vector3(-half, 0f, half));
        mesh.SurfaceAddVertex(new Vector3(-half, 0f, -half));
        mesh.SurfaceEnd();
        return mesh;
    }
}
