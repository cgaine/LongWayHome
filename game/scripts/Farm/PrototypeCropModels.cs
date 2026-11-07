#nullable enable

using Godot;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Game.Farm;

/// <summary>
/// The prototype's low-poly Planting models, built from primitive meshes with flat materials and standing on y = 0 of
/// their own node. No imported art. <c>TileView</c> falls back to these for a crop that <c>CropModels</c> does not
/// draw yet (moonmelon, sunbell and starcorn, #90); the summer and fall features replace them and the one that
/// empties this class deletes the file.
/// </summary>
internal static class PrototypeCropModels
{
    /// <summary>
    /// The prototype models are authored at about a tile's quarter and drawn larger, so each stage and crop reads from
    /// the default camera distance at 1280 x 800 (REQ-12).
    /// </summary>
    public static readonly Vector3 ModelScale = new(1.6f, 1.6f, 1.6f);

    private static readonly Color MoundColor = new(0.36f, 0.24f, 0.14f);

    private static readonly Color SeedColor = new(0.95f, 0.85f, 0.55f);

    private static readonly Color LeafColor = new(0.35f, 0.75f, 0.25f);

    private static readonly Color StemColor = new(0.25f, 0.55f, 0.18f);

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
        CropTypeId.Moonmelon => Moonmelon(),
        CropTypeId.Sunbell => Sunbell(),
        _ => Starcorn(),
    };

    /// <summary>The colour a crop is known by, used to tint its young plant.</summary>
    private static Color CropColor(CropTypeId crop) => crop switch
    {
        CropTypeId.Moonmelon => new Color(0.55f, 0.72f, 1f),
        CropTypeId.Sunbell => new Color(0.98f, 0.85f, 0.2f),
        _ => new Color(0.95f, 0.75f, 0.2f),
    };

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
