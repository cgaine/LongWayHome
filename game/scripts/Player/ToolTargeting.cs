#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Farming;
using LongWayHome.Game.Farm;

namespace LongWayHome.Game.Player;

/// <summary>
/// The tile the Player's tools act on: the Player's grid cell plus <see cref="PlayerController.Facing"/>. Draws the
/// Use target marker of ART_DIRECTION §7.2 on it (#78): a soft cream <c>Highlight</c> over the slab and four
/// ember-orange L-shaped corner <c>Brackets</c> standing above it, so it reads at the camera's lowest pitch. Hidden
/// when the tile in front is off the plot. A child of the Player. Holds no rules: whether a position is on the plot is
/// decided by <see cref="GridPosition.TryCreate"/>.
/// </summary>
public partial class ToolTargeting : Node3D
{
    /// <summary>Height of the cursor node; its parts carry their own heights above the slab top (y 0.06).</summary>
    private const float CursorHeight = 0f;

    private const float HighlightHeight = 0.065f;

    private const float HighlightSize = 0.96f;

    private const float BracketTop = 0.085f;

    private const float BracketLength = 0.25f;

    private const float BracketWidth = 0.05f;

    private const float BracketThickness = 0.02f;

    /// <summary>The highlight: <c>target-highlight</c> from the palette at 45 % opacity.</summary>
    private static readonly Color CursorColor = new(ArtPalette.Hex("target-highlight"), 0.45f);

    private static readonly Color BracketColor = new(ArtPalette.Hex("ember"));

    private PlayerController _player = null!;

    private Node3D _cursor = null!;

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
        _cursor = BuildCursor();
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

    /// <summary>
    /// The marker: a top-level <c>Cursor</c>, so it stays on the grid and does not move or turn with the Player,
    /// holding the <c>Highlight</c> and <c>Brackets</c>. Both use small unshaded materials of their own, not the lit
    /// shared one, so they read the same under any light, and skip the depth test so the farmer, crops and scenery
    /// never hide them (issue #71). The brackets draw after the translucent highlight, so it never dims them.
    /// </summary>
    private static Node3D BuildCursor()
    {
        var highlightMaterial = new StandardMaterial3D
        {
            AlbedoColor = CursorColor,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            NoDepthTest = true,
            RenderPriority = 1,
        };
        var bracketMaterial = new StandardMaterial3D
        {
            AlbedoColor = BracketColor,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            NoDepthTest = true,
            RenderPriority = 2,
        };

        var cursor = new Node3D { Name = "Cursor", TopLevel = true, Visible = false };
        cursor.AddChild(new MeshInstance3D
        {
            Name = "Highlight",
            Mesh = new PlaneMesh { Size = new Vector2(HighlightSize, HighlightSize) },
            MaterialOverride = highlightMaterial,
            Position = new Vector3(0f, HighlightHeight, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        cursor.AddChild(new MeshInstance3D
        {
            Name = "Brackets",
            Mesh = BuildBrackets(),
            MaterialOverride = bracketMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        return cursor;
    }

    /// <summary>
    /// Four L-shaped brackets in one mesh, one at each corner of the slab: two arms each, one along X and one along Z,
    /// meeting at the corner, with their tops at <see cref="BracketTop"/>.
    /// </summary>
    private static ArrayMesh BuildBrackets()
    {
        const float corner = HighlightSize / 2f;
        const float y = BracketTop - (BracketThickness / 2f);
        var alongX = new BoxMesh { Size = new Vector3(BracketLength, BracketThickness, BracketWidth) };
        var alongZ = new BoxMesh { Size = new Vector3(BracketWidth, BracketThickness, BracketLength) };

        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var sx in new[] { -1f, 1f })
        {
            foreach (var sz in new[] { -1f, 1f })
            {
                // Translation only, so the boxes' normals carry over unchanged.
                var x = sx * (corner - (BracketLength / 2f));
                var z = sz * (corner - (BracketWidth / 2f));
                surface.AppendFrom(alongX, 0, new Transform3D(Basis.Identity, new Vector3(x, y, z)));
                x = sx * (corner - (BracketWidth / 2f));
                z = sz * (corner - (BracketLength / 2f));
                surface.AppendFrom(alongZ, 0, new Transform3D(Basis.Identity, new Vector3(x, y, z)));
            }
        }

        return surface.Commit();
    }
}
