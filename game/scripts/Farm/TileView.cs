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
/// <see cref="TileLook.For"/> picks: grass (a green slab with three small tufts and, on about one Tile in six, a native
/// frond or puffball), tilled (a brown slab with three east–west furrows) or watered (the same, much darker, with the
/// shader's wet sheen). The slab is 0.96 m square, so the dark plot base under the plot shows through the 4 cm gaps as
/// grooves and the grid reads without drawn lines. Any Planting on it stands on the slab top as a low-poly model for
/// its <see cref="GrowthStage"/>: a seed mound, a sprout, a young plant tinted toward its crop's colour, or a
/// crop-specific ripe model (REQ-12).
/// <para>
/// The slab and furrows are built once when ready; the tufts and native plant on the first <see cref="Refresh"/>,
/// which brings the Tile's grid position for their <see cref="TileDecoration"/>, and only if the Tile is still grass
/// (a tilled Tile never reverts, BR-T8). After that <see cref="Refresh"/> only swaps palette roles, sets the
/// <c>wet</c> instance parameter and shows or hides parts, and only when the look changes. Each (stage, crop) model is
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

    /// <summary>
    /// The planting models are authored at about a tile's quarter and drawn larger, so each stage and crop reads from
    /// the default camera distance at 1280 x 800 (REQ-12).
    /// </summary>
    private static readonly Vector3 ModelScale = new(1.6f, 1.6f, 1.6f);

    private static readonly Color MoundColor = new(0.36f, 0.24f, 0.14f);

    private static readonly Color SeedColor = new(0.95f, 0.85f, 0.55f);

    private static readonly Color LeafColor = new(0.35f, 0.75f, 0.25f);

    private static readonly Color StemColor = new(0.25f, 0.55f, 0.18f);

    private readonly Dictionary<(GrowthStage Stage, CropTypeId Crop), Node3D> _models = new();

    private MeshInstance3D? _slab;

    private MeshInstance3D? _furrows;

    private MeshInstance3D? _tufts;

    private MeshInstance3D? _nativePlant;

    private bool _decorated;

    private TileLook? _look;

    private Node3D? _shown;

    public override void _Ready() => EnsureBuilt();

    /// <summary>
    /// Redraws this view from <paramref name="tile"/>'s state and its Planting's <paramref name="stage"/> (null when
    /// the Tile has no Planting), as decided by <c>GameSession.GrowthStageAt</c>.
    /// </summary>
    public void Refresh(Tile tile, GrowthStage? stage)
    {
        EnsureBuilt();
        var look = TileLook.For(tile);
        EnsureDecorated(tile.Position, look);
        ApplyLook(look);

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

    private Node3D ModelFor(GrowthStage stage, CropTypeId crop)
    {
        if (_models.TryGetValue((stage, crop), out var model))
        {
            return model;
        }

        model = stage switch
        {
            GrowthStage.Seed => CropModels.Seed(),
            GrowthStage.Sprout => CropModels.Sprout(),
            GrowthStage.Young => CropModels.Young(crop),
            _ => CropModels.Ripe(crop),
        };
        model.Name = $"{stage}_{crop}";
        model.Visible = false;
        model.Position = new Vector3(0f, SurfaceHeight, 0f);
        model.Scale = ModelScale;
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
        ArtMaterials.Apply(_tufts, "grass-lit");
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

    /// <summary>
    /// The low-poly models of a Planting, built from primitive meshes with flat materials and standing on y = 0 of
    /// their own node. No imported art.
    /// </summary>
    private static class CropModels
    {
        /// <summary>A small raised soil mound with a pale seed on top.</summary>
        public static Node3D Seed()
        {
            var model = new Node3D();
            Add(model, "Mound", Sphere(0.2f, 0.12f, MoundColor), new Vector3(0f, 0.02f, 0f));
            Add(model, "SeedDot", Sphere(0.05f, 0.1f, SeedColor), new Vector3(0f, 0.08f, 0f));
            return model;
        }

        /// <summary>Two small leaves on a short stem.</summary>
        public static Node3D Sprout()
        {
            var model = new Node3D();
            Add(model, "Stem", Cylinder(0.015f, 0.015f, 0.12f, StemColor), new Vector3(0f, 0.06f, 0f));
            var leaf = Box(new Vector3(0.14f, 0.02f, 0.07f), LeafColor);
            Add(model, "LeafLeft", leaf, new Vector3(-0.07f, 0.13f, 0f), new Vector3(0f, 0f, 25f));
            Add(model, "LeafRight", leaf, new Vector3(0.07f, 0.13f, 0f), new Vector3(0f, 0f, -25f));
            return model;
        }

        /// <summary>A taller leafy plant whose leaves are tinted toward the crop's colour.</summary>
        public static Node3D Young(CropTypeId crop)
        {
            var model = new Node3D();
            var tint = LeafColor.Lerp(CropColor(crop), 0.35f);
            Add(model, "Stem", Cylinder(0.025f, 0.03f, 0.32f, StemColor), new Vector3(0f, 0.16f, 0f));
            var leaf = Prism(new Vector3(0.12f, 0.26f, 0.04f), tint);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f;
                var direction = new Vector3(Mathf.Sin(Mathf.DegToRad(angle)), 0f, Mathf.Cos(Mathf.DegToRad(angle)));
                Add(
                    model,
                    $"Leaf{i}",
                    leaf,
                    (direction * 0.1f) + new Vector3(0f, 0.2f + (0.06f * (i % 2)), 0f),
                    new Vector3(35f, angle, 0f));
            }

            Add(model, "Crown", Sphere(0.07f, 0.12f, tint), new Vector3(0f, 0.34f, 0f));
            return model;
        }

        /// <summary>The ripe model of each crop: clearly different from Young and from each other.</summary>
        public static Node3D Ripe(CropTypeId crop) => crop switch
        {
            CropTypeId.Emberroot => Emberroot(),
            CropTypeId.Dusktuber => Dusktuber(),
            CropTypeId.Moonmelon => Moonmelon(),
            CropTypeId.Sunbell => Sunbell(),
            _ => Starcorn(),
        };

        /// <summary>The colour a crop is known by, used to tint its young plant.</summary>
        private static Color CropColor(CropTypeId crop) => crop switch
        {
            CropTypeId.Emberroot => new Color(0.9f, 0.35f, 0.15f),
            CropTypeId.Dusktuber => new Color(0.4f, 0.2f, 0.5f),
            CropTypeId.Moonmelon => new Color(0.55f, 0.72f, 1f),
            CropTypeId.Sunbell => new Color(0.98f, 0.85f, 0.2f),
            _ => new Color(0.95f, 0.75f, 0.2f),
        };

        /// <summary>An orange-red root crown showing above the soil, with a tuft of green leaves.</summary>
        private static Node3D Emberroot()
        {
            var model = new Node3D();
            var root = new Color(0.92f, 0.32f, 0.12f);
            Add(model, "Root", Cylinder(0.16f, 0.06f, 0.22f, root, 7), new Vector3(0f, 0.11f, 0f));
            Add(model, "Crown", Sphere(0.16f, 0.12f, root), new Vector3(0f, 0.22f, 0f));
            var leaf = Prism(new Vector3(0.1f, 0.34f, 0.04f), LeafColor);
            Add(model, "LeafA", leaf, new Vector3(-0.07f, 0.4f, 0f), new Vector3(0f, 0f, 20f));
            Add(model, "LeafB", leaf, new Vector3(0.07f, 0.4f, 0f), new Vector3(0f, 0f, -20f));
            Add(model, "LeafC", leaf, new Vector3(0f, 0.42f, 0.06f), new Vector3(-20f, 0f, 0f));
            return model;
        }

        /// <summary>A dark purple bulb sitting in the soil with a leafy top.</summary>
        private static Node3D Dusktuber()
        {
            var model = new Node3D();
            var bulb = new Color(0.32f, 0.14f, 0.42f);
            Add(model, "Bulb", Sphere(0.24f, 0.3f, bulb), new Vector3(0f, 0.13f, 0f));
            var leaf = Box(new Vector3(0.26f, 0.02f, 0.09f), new Color(0.25f, 0.6f, 0.22f));
            for (var i = 0; i < 3; i++)
            {
                Add(model, $"Leaf{i}", leaf, new Vector3(0f, 0.32f, 0f), new Vector3(0f, i * 60f, 25f));
            }

            return model;
        }

        /// <summary>A large pale blue-white round melon with a short vine.</summary>
        private static Node3D Moonmelon()
        {
            var model = new Node3D();
            Add(model, "Melon", Sphere(0.38f, 0.36f, new Color(0.66f, 0.8f, 1f)), new Vector3(0f, 0.18f, 0f));
            Add(model, "Vine", Box(new Vector3(0.22f, 0.03f, 0.04f), StemColor), new Vector3(0.14f, 0.03f, 0.12f));
            Add(model, "Leaf", Box(new Vector3(0.12f, 0.02f, 0.1f), LeafColor), new Vector3(0.24f, 0.04f, 0.16f));
            return model;
        }

        /// <summary>A leafy plant with yellow bell flowers hanging from it.</summary>
        private static Node3D Sunbell()
        {
            var model = new Node3D();
            Add(model, "Stem", Cylinder(0.025f, 0.03f, 0.44f, StemColor), new Vector3(0f, 0.22f, 0f));
            var leaf = Prism(new Vector3(0.12f, 0.24f, 0.04f), LeafColor);
            Add(model, "LeafA", leaf, new Vector3(-0.08f, 0.14f, 0f), new Vector3(0f, 0f, 40f));
            Add(model, "LeafB", leaf, new Vector3(0.08f, 0.14f, 0f), new Vector3(0f, 0f, -40f));
            var bell = Cylinder(0.02f, 0.09f, 0.12f, new Color(1f, 0.85f, 0.15f), 6);
            Add(model, "BellA", bell, new Vector3(-0.12f, 0.38f, 0f));
            Add(model, "BellB", bell, new Vector3(0.12f, 0.42f, 0.04f));
            Add(model, "BellC", bell, new Vector3(0f, 0.48f, -0.1f));
            return model;
        }

        /// <summary>A tall stalk with a golden cob.</summary>
        private static Node3D Starcorn()
        {
            var model = new Node3D();
            Add(model, "Stalk", Cylinder(0.035f, 0.045f, 0.8f, StemColor), new Vector3(0f, 0.4f, 0f));
            var leaf = Box(new Vector3(0.32f, 0.02f, 0.06f), LeafColor);
            Add(model, "LeafA", leaf, new Vector3(-0.14f, 0.3f, 0f), new Vector3(0f, 0f, -25f));
            Add(model, "LeafB", leaf, new Vector3(0.14f, 0.5f, 0f), new Vector3(0f, 0f, 25f));
            Add(
                model,
                "Cob",
                Cylinder(0.06f, 0.07f, 0.26f, new Color(1f, 0.78f, 0.12f), 8),
                new Vector3(0.09f, 0.62f, 0f),
                new Vector3(0f, 0f, -15f));
            return model;
        }

        private static void Add(Node3D parent, string name, Mesh mesh, Vector3 position, Vector3? rotation = null) =>
            parent.AddChild(new MeshInstance3D
            {
                Name = name,
                Mesh = mesh,
                Position = position,
                RotationDegrees = rotation ?? Vector3.Zero,
            });

        private static SphereMesh Sphere(float diameter, float height, Color color) => new()
        {
            Radius = diameter / 2f,
            Height = height,
            RadialSegments = 8,
            Rings = 4,
            Material = Flat(color),
        };

        private static CylinderMesh Cylinder(float top, float bottom, float height, Color color, int segments = 6) =>
            new()
            {
                TopRadius = top,
                BottomRadius = bottom,
                Height = height,
                RadialSegments = segments,
                Rings = 1,
                Material = Flat(color),
            };

        private static BoxMesh Box(Vector3 size, Color color) => new() { Size = size, Material = Flat(color) };

        private static PrismMesh Prism(Vector3 size, Color color) => new() { Size = size, Material = Flat(color) };

        private static StandardMaterial3D Flat(Color color) => new() { AlbedoColor = color };
    }
}
