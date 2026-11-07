#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Game.Art;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws one Tile of the Farm plot as the 1 x 1 m slab of ART_DIRECTION §7.2 (#78), in the state
/// <see cref="TileLook.For(Tile, Season)"/> picks: grass (a slab in the Season's ground colour, <see cref="SeasonGrass"/>
/// (#79), with three small tufts in its tuft colour and height and, on about one Tile in six, a native frond or
/// puffball), tilled (a brown slab with three east–west furrows) or watered (the same, much darker, with the shader's
/// wet sheen); tilled and watered look the same all year (ART_DIRECTION §7.2, BR-T8). The slab is 0.96 m square, so
/// the dark plot base under the plot shows through the 4 cm gaps as grooves and the grid reads without drawn lines. Any Planting on it stands on the slab top as a low-poly model for
/// its <see cref="GrowthStage"/> (REQ-12): the art-direction model of <see cref="CropArt"/>, one mesh from
/// <see cref="CropModels"/> under the shared palette material (#90, #91, #92).
/// <para>
/// The slab and furrows are built once when ready; the tufts and native plant on the first <see cref="Refresh"/>,
/// which brings the Tile's grid position for their <see cref="TileDecoration"/>, and only if the Tile is still grass
/// (a tilled Tile never reverts, BR-T8). After that <see cref="Refresh"/> only swaps palette roles, sets the
/// <c>wet</c> instance parameter, scales the tufts' height and shows or hides parts, and only when the look or the
/// Season's tufts change. Each (stage, crop) model is
/// built once, the first time it is shown. Every lit tile mesh uses the one shared palette material. Holds no rules.
/// </para>
/// </summary>
public partial class TileView : Node3D
{
    /// <summary>Height of the slab top, which Planting models stand on.</summary>
    private const float SurfaceHeight = 0.06f;

    private const float FurrowHeight = 0.015f;

    /// <summary>The furrows' z offsets from the Tile's centre: three ridges running east–west (along X).</summary>
    private static readonly float[] FurrowRows = [-0.3f, 0f, 0.3f];

    private static readonly Vector3 FurrowSize = new(0.9f, FurrowHeight, 0.12f);

    private static BoxMesh? _slabMesh;

    private static ArrayMesh? _furrowMesh;

    private readonly Dictionary<(GrowthStage Stage, CropTypeId Crop), Node3D> _models = new();

    private MeshInstance3D? _slab;

    private MeshInstance3D? _furrows;

    private MeshInstance3D? _tufts;

    private MeshInstance3D? _nativePlant;

    /// <summary>The palette role last given to the tufts, so a Season's role is applied only when it changes.</summary>
    private string? _tuftRole;

    private bool _decorated;

    private TileLook? _look;

    private Node3D? _shown;

    public override void _Ready() => EnsureBuilt();

    /// <summary>
    /// Redraws this view from <paramref name="tile"/>'s state and its Planting's <paramref name="stage"/> (null when
    /// the Tile has no Planting), as decided by <c>GameSession.GrowthStageAt</c>, in the current
    /// <paramref name="season"/>, whose grass colours a grass Tile's slab and tufts (#79).
    /// </summary>
    public void Refresh(Tile tile, GrowthStage? stage, Season season)
    {
        EnsureBuilt();
        var look = TileLook.For(tile, season);
        EnsureDecorated(tile.Position, look);
        ApplyLook(look);
        ApplyTufts(SeasonGrass.For(season));

        var model = tile.Planting is { } planting && stage is { } shownStage
            ? ModelFor(shownStage, planting.CropTypeId)
            : null;
        if (model == _shown)
        {
            return;
        }

        if (_shown is not null)
        {
            _shown.Visible = false;
        }

        if (model is not null)
        {
            model.Visible = true;
        }

        _shown = model;
    }

    /// <summary>Swaps the slab and furrow roles, the wet sheen and the parts shown, when the look has changed.</summary>
    private void ApplyLook(TileLook look)
    {
        if (look == _look)
        {
            return;
        }

        if (look.SlabRole != _look?.SlabRole)
        {
            ArtMaterials.Apply(_slab!, look.SlabRole);
        }

        if (look.FurrowRole != _look?.FurrowRole)
        {
            ArtMaterials.Apply(_furrows!, look.FurrowRole);
        }

        _slab!.SetInstanceShaderParameter("wet", look.Wet);
        _furrows!.SetInstanceShaderParameter("wet", look.Wet);
        _furrows.Visible = look.ShowFurrows;
        if (_tufts is not null)
        {
            _tufts.Visible = look.ShowTufts;
        }

        if (_nativePlant is not null)
        {
            _nativePlant.Visible = look.ShowTufts;
        }

        _look = look;
    }

    /// <summary>
    /// Gives the tufts the Season's role and height, when either has changed. The blades stand on the slab top, so the
    /// node is scaled on Y about that height (not about y 0) to keep their feet on the slab.
    /// </summary>
    private void ApplyTufts(SeasonGrass grass)
    {
        if (_tufts is null)
        {
            return;
        }

        if (grass.Tufts != _tuftRole)
        {
            ArtMaterials.Apply(_tufts, grass.Tufts);
            _tuftRole = grass.Tufts;
        }

        var scale = grass.TuftHeightScale;
        if (!Mathf.IsEqualApprox(_tufts.Scale.Y, scale))
        {
            _tufts.Scale = new Vector3(1f, scale, 1f);
            _tufts.Position = new Vector3(0f, SurfaceHeight * (1f - scale), 0f);
        }
    }

    private Node3D ModelFor(GrowthStage stage, CropTypeId crop)
    {
        if (_models.TryGetValue((stage, crop), out var model))
        {
            return model;
        }

        // Every crop model is one merged mesh, shared by every Tile at that (crop, stage), in world metres and coloured
        // by its UVs under the shared material: one draw (#90).
        model = new MeshInstance3D
        {
            Mesh = CropModels.MeshFor(crop, stage),
            MaterialOverride = ArtMaterials.Shared,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
        };
        model.Name = $"{stage}_{crop}";
        model.Visible = false;
        model.Position = new Vector3(0f, SurfaceHeight, 0f);
        AddChild(model);
        _models[(stage, crop)] = model;
        return model;
    }

    /// <summary>Builds the slab, 0.96 x 0.06 x 0.96 m with its bottom at y 0, and the (hidden) furrows on its top.</summary>
    private void EnsureBuilt()
    {
        if (_slab is not null)
        {
            return;
        }

        _slab = new MeshInstance3D
        {
            Name = "Slab",
            Position = new Vector3(0f, SurfaceHeight / 2f, 0f),
            Mesh = SlabMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(_slab);

        _furrows = new MeshInstance3D
        {
            Name = "Furrows",
            Mesh = FurrowMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        AddChild(_furrows);
    }

    /// <summary>
    /// Builds the tufts and native plant of the Tile at <paramref name="position"/>, once. Skipped for a Tile first
    /// seen tilled, which never shows them (BR-T8).
    /// </summary>
    private void EnsureDecorated(GridPosition position, TileLook look)
    {
        if (_decorated)
        {
            return;
        }

        _decorated = true;
        if (!look.ShowTufts)
        {
            return;
        }

        var decoration = TileDecoration.For(position);
        var blades = new List<MeshPart>();
        foreach (var (x, z) in decoration.TuftOffsets)
        {
            blades.AddRange(Decorations.Tuft(x, z, SurfaceHeight));
        }

        _tufts = new MeshInstance3D
        {
            Name = "Tufts",
            Mesh = ArtMeshes.Merge(blades),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        AddChild(_tufts);

        if (decoration.NativePlant == NativePlantKind.None)
        {
            return;
        }

        // Two colours in one mesh: each part's UVs point at its swatch and `swatch` stays unset, as on imported models.
        _nativePlant = new MeshInstance3D
        {
            Name = "NativePlant",
            Mesh = ArtMeshes.Merge(decoration.NativePlant == NativePlantKind.Frond
                ? Decorations.Frond(SurfaceHeight)
                : Decorations.Puffball(SurfaceHeight)),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = ArtMaterials.Shared,
            RotationDegrees = new Vector3(0f, TileDecoration.Hash(position) % 360, 0f),
            Visible = false,
        };
        AddChild(_nativePlant);
    }

    private static Transform3D At(float x, float y, float z) => new(Basis.Identity, new Vector3(x, y, z));

    /// <summary>The slab mesh, the same on every Tile, so all 108 share one resource.</summary>
    private static BoxMesh SlabMesh => _slabMesh ??= new BoxMesh { Size = new Vector3(0.96f, 0.06f, 0.96f) };

    /// <summary>The three furrow ridges in one mesh, the same on every Tile, so all 108 share one resource.</summary>
    private static ArrayMesh FurrowMesh => _furrowMesh ??= ArtMeshes.Merge(FurrowRows.Select(z =>
        new MeshPart(new BoxMesh { Size = FurrowSize }, At(0f, SurfaceHeight + (FurrowHeight / 2f), z))));

    /// <summary>The grass tile's tufts and native plants, each standing on the slab top at <c>top</c>.</summary>
    private static class Decorations
    {
        /// <summary>Three thin blades, 0.08–0.12 m tall, fanned out and leaning away from the tuft's centre.</summary>
        public static IEnumerable<MeshPart> Tuft(float x, float z, float top)
        {
            for (var i = 0; i < 3; i++)
            {
                var height = 0.08f + (0.02f * i);
                yield return new MeshPart(
                    new PrismMesh { Size = new Vector3(0.03f, height, 0.008f) },
                    Lean(x, top, z, (i * 120f) + 15f, 20f, height / 2f));
            }
        }

        /// <summary>Two flat fronds leaning out on opposite sides, about 0.22 m tall.</summary>
        public static IEnumerable<MeshPart> Frond(float top) =>
        [
            new(new PrismMesh { Size = new Vector3(0.07f, 0.26f, 0.015f) }, Lean(0f, top, 0f, 0f, 30f, 0.13f), "foliage"),
            new(new PrismMesh { Size = new Vector3(0.06f, 0.22f, 0.015f) }, Lean(0f, top, 0f, 160f, 36f, 0.11f), "foliage"),
        ];

        /// <summary>A thin stem with a round lilac head, about 0.22 m tall.</summary>
        public static IEnumerable<MeshPart> Puffball(float top) =>
        [
            new(
                new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.012f, Height = 0.18f, RadialSegments = 5, Rings = 1 },
                At(0f, top + 0.09f, 0f),
                "grass-shade"),
            new(
                new SphereMesh { Radius = 0.05f, Height = 0.09f, RadialSegments = 6, Rings = 3 },
                At(0f, top + 0.2f, 0f),
                "leaf-underside"),
        ];

        /// <summary>
        /// A part whose centre is <paramref name="lift"/> up its own length, standing at (<paramref name="x"/>,
        /// <paramref name="y"/>, <paramref name="z"/>) and leaning <paramref name="tiltDegrees"/> from upright towards
        /// the heading <paramref name="yawDegrees"/>.
        /// </summary>
        private static Transform3D Lean(float x, float y, float z, float yawDegrees, float tiltDegrees, float lift) =>
            new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(yawDegrees)), new Vector3(x, y, z))
            * new Transform3D(new Basis(Vector3.Right, Mathf.DegToRad(tiltDegrees)), Vector3.Zero)
            * new Transform3D(Basis.Identity, new Vector3(0f, lift, 0f));
    }
}
