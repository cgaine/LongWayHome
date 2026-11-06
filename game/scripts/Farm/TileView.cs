#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Game.Farm;

/// <summary>
/// Draws one Tile of the Farm plot: a 1 x 1 quad level with the ground and a faint outline, so the 12 x 9 grid is
/// visible, darker soil when watered, and any Planting on it as a low-poly model for its <see cref="GrowthStage"/>:
/// a seed mound, a sprout, a young plant tinted toward its crop's colour, or a crop-specific ripe model (REQ-12).
/// <see cref="Refresh"/> redraws it from the Tile's state. Each (stage, crop) model is built once, the first time it
/// is shown, and then only shown or hidden. Holds no rules.
/// </summary>
public partial class TileView : Node3D
{
    /// <summary>Height of the tile surface above the ground; just above the meadow's ground layers (the highest, the path, is at y 0.008).</summary>
    private const float SurfaceHeight = 0.015f;

    /// <summary>Height of the outline, just above the surface so the two never z-fight.</summary>
    private const float OutlineHeight = 0.02f;

    private const float Size = 1f;

    private static readonly Color GrassColor = new(0.45f, 0.62f, 0.3f);

    private static readonly Color TilledColor = new(0.45f, 0.3f, 0.18f);

    /// <summary>Watered tilled soil: clearly darker than dry soil, so the watered state is visible (REQ-12).</summary>
    private static readonly Color WateredColor = new(0.25f, 0.16f, 0.1f);

    /// <summary>
    /// The planting models are authored at about a tile's quarter and drawn larger, so each stage and crop reads from
    /// the default camera distance at 1280 x 800 (REQ-12).
    /// </summary>
    private static readonly Vector3 ModelScale = new(1.6f, 1.6f, 1.6f);

    private static readonly Color OutlineColor = new(0.18f, 0.28f, 0.1f, 0.45f);

    private static readonly Color MoundColor = new(0.36f, 0.24f, 0.14f);

    private static readonly Color SeedColor = new(0.95f, 0.85f, 0.55f);

    private static readonly Color LeafColor = new(0.35f, 0.75f, 0.25f);

    private static readonly Color StemColor = new(0.25f, 0.55f, 0.18f);

    private readonly StandardMaterial3D _surfaceMaterial = new();

    private readonly Dictionary<(GrowthStage Stage, CropTypeId Crop), Node3D> _models = new();

    private MeshInstance3D? _surface;

    private Node3D? _shown;

    public override void _Ready() => EnsureBuilt();

    /// <summary>
    /// Redraws this view from <paramref name="tile"/>'s state and its Planting's <paramref name="stage"/> (null when
    /// the Tile has no Planting), as decided by <c>GameSession.GrowthStageAt</c>.
    /// </summary>
    public void Refresh(Tile tile, GrowthStage? stage)
    {
        EnsureBuilt();
        _surfaceMaterial.AlbedoColor = tile.State != TileState.Tilled
            ? GrassColor
            : tile.Watered ? WateredColor : TilledColor;

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
