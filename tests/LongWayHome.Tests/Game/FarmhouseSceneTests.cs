using System.Globalization;
using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real game files to check the run-down farmhouse (#104): <c>Farm.tscn</c>'s <c>House</c> keeps its body,
/// transform and collision box, loses its three prototype meshes and their sub-resources, and instances
/// <c>res://art/farm/Farmhouse.tscn</c>, which is one merged <c>Model</c> mesh with no physics. The sleep door point is
/// pinned where it was (BR-G3). Each test loads the files it needs, so a missing file fails as an assertion in that
/// test only.
/// </summary>
public class FarmhouseSceneTests
{
    private const float Eps = 1e-4f;

    private static string GamePath(params string[] parts) =>
        Path.Combine(new[] { AppContext.BaseDirectory, "game" }.Concat(parts).ToArray());

    private static string FarmPath => GamePath("scenes", "Farm.tscn");
    private static string FarmhousePath => GamePath("art", "farm", "Farmhouse.tscn");
    private static string FarmhouseViewPath => GamePath("art", "farm", "FarmhouseView.cs");

    private static readonly string[] PhysicsTypes =
    [
        "CollisionShape3D", "CollisionPolygon3D", "StaticBody3D", "RigidBody3D", "CharacterBody3D",
        "AnimatableBody3D", "Area3D",
    ];

    // ---- a tiny .tscn reader (as MeadowSceneTests) ----

    private sealed record Section(
        string Tag, IReadOnlyDictionary<string, string> Attributes, IReadOnlyDictionary<string, string> Properties)
    {
        public string? Attr(string key) => Attributes.TryGetValue(key, out var v) ? v : null;
        public string? Prop(string key) => Properties.TryGetValue(key, out var v) ? v : null;
    }

    private sealed class Scene
    {
        private static readonly Regex HeaderRegex = new(@"^\[(\w+)(.*)\]\s*$");
        private static readonly Regex AttributeRegex = new(@"(\w+)=(\w+\(""[^""]*""\)|""[^""]*""|[^\s\]]+)");
        private static readonly Regex PropertyRegex = new(@"^([\w/]+)\s*=\s*(.*)$");

        public required IReadOnlyList<Section> Sections { get; init; }

        public IEnumerable<Section> Nodes => Sections.Where(s => s.Tag == "node");
        public IEnumerable<Section> ExtResources => Sections.Where(s => s.Tag == "ext_resource");
        public IEnumerable<Section> SubResources => Sections.Where(s => s.Tag == "sub_resource");

        public Section? Root => Nodes.FirstOrDefault(n => n.Attr("parent") is null);

        public static string PathOf(Section node) => node.Attr("parent") switch
        {
            null => ".",
            "." => node.Attr("name")!,
            var parent => $"{parent}/{node.Attr("name")}",
        };

        public Section? Node(string path) => Nodes.FirstOrDefault(n => PathOf(n) == path);

        public Section? SubResource(string id) => SubResources.FirstOrDefault(s => s.Attr("id") == id);

        public static Scene Load(string path)
        {
            Assert.True(File.Exists(path), $"{path} is missing");

            var sections = new List<Section>();
            string? tag = null;
            Dictionary<string, string> attributes = new();
            Dictionary<string, string> properties = new();
            foreach (var line in File.ReadLines(path))
            {
                var header = HeaderRegex.Match(line);
                if (header.Success)
                {
                    if (tag is not null) sections.Add(new Section(tag, attributes, properties));
                    tag = header.Groups[1].Value;
                    attributes = AttributeRegex.Matches(header.Groups[2].Value)
                        .ToDictionary(m => m.Groups[1].Value, m => Unquote(m.Groups[2].Value));
                    properties = new Dictionary<string, string>();
                    continue;
                }

                var property = PropertyRegex.Match(line);
                if (tag is not null && property.Success)
                {
                    properties[property.Groups[1].Value] = property.Groups[2].Value.Trim();
                }
            }

            if (tag is not null) sections.Add(new Section(tag, attributes, properties));
            return new Scene { Sections = sections };
        }

        private static string Unquote(string value) =>
            value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;
    }

    private static float[] Numbers(string value) =>
        Regex.Matches(value[(value.IndexOf('(') + 1)..], @"-?\d+(\.\d+)?(e[-+]?\d+)?")
            .Select(m => float.Parse(m.Value, CultureInfo.InvariantCulture))
            .ToArray();

    /// <summary>Asserts the node's transform is the identity basis at (x, y, z); no transform means the origin.</summary>
    private static void AssertTransform(Section node, float x, float y, float z)
    {
        var transform = node.Prop("transform");
        var actual = transform is null ? new[] { 1f, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 } : Numbers(transform);
        var expected = new[] { 1f, 0, 0, 0, 1, 0, 0, 0, 1, x, y, z };
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.True(
                MathF.Abs(expected[i] - actual[i]) <= Eps,
                $"{Scene.PathOf(node)}'s transform is {transform ?? "(none)"}, not the identity at ({x}, {y}, {z})");
        }
    }

    private static string? ReferencedId(string? value, string kind)
    {
        if (value is null) return null;
        var match = Regex.Match(value, kind + @"\(""([^""]*)""\)");
        return match.Success ? match.Groups[1].Value : null;
    }

    // ---- AC5: Farm.tscn's House ----

    [Fact]
    public void AC5_house_keeps_its_static_body_and_transform()
    {
        var farm = Scene.Load(FarmPath);

        var house = farm.Node("House");
        Assert.True(house is not null, "Farm.tscn has no House");
        Assert.Equal("StaticBody3D", house!.Attr("type"));
        AssertTransform(house, 0f, 1.5f, -9f);
    }

    [Fact]
    public void AC5_house_keeps_its_6_by_3_by_4_collision_box()
    {
        var farm = Scene.Load(FarmPath);

        var collision = farm.Node("House/CollisionShape3D");
        Assert.True(collision is not null, "Farm.tscn has no House/CollisionShape3D");
        Assert.Equal("CollisionShape3D", collision!.Attr("type"));
        AssertTransform(collision, 0f, 0f, 0f);

        var shape = farm.SubResource(ReferencedId(collision.Prop("shape"), "SubResource") ?? "");
        Assert.True(shape is not null, "House/CollisionShape3D has no shape sub_resource");
        Assert.Equal("BoxShape3D", shape!.Attr("type"));
        Assert.Equal(new[] { 6f, 3f, 4f }, Numbers(shape.Prop("size") ?? "()"));
    }

    [Fact]
    public void AC5_house_drops_the_prototype_meshes_and_their_sub_resources()
    {
        var farm = Scene.Load(FarmPath);

        foreach (var path in new[] { "House/Walls", "House/Roof", "House/Door" })
        {
            Assert.True(farm.Node(path) is null, $"Farm.tscn still has the node {path}");
        }

        foreach (var id in new[] { "BoxMesh_house", "Mat_wall", "PrismMesh_roof", "Mat_roof", "BoxMesh_door" })
        {
            Assert.True(farm.SubResource(id) is null, $"Farm.tscn still has the sub_resource {id}");
        }

        Assert.True(farm.SubResource("Mat_wood") is not null, "Farm.tscn lost Mat_wood, which the bin and the counter use");
    }

    [Fact]
    public void AC5_house_instances_the_farmhouse_scene_at_ground_level()
    {
        var farm = Scene.Load(FarmPath);

        var id = farm.ExtResources.FirstOrDefault(r => r.Attr("path") == "res://art/farm/Farmhouse.tscn")?.Attr("id");
        Assert.True(id is not null, "Farm.tscn has no ext_resource for res://art/farm/Farmhouse.tscn");

        var instance = farm.Node("House/Farmhouse");
        Assert.True(instance is not null, "Farm.tscn has no House/Farmhouse");
        Assert.Equal(id, ReferencedId(instance!.Attr("instance"), "ExtResource"));
        AssertTransform(instance, 0f, -1.5f, 0f);
    }

    // ---- AC5: Farmhouse.tscn and FarmhouseView.cs ----

    [Fact]
    public void AC5_farmhouse_scene_has_a_farmhouse_root_with_its_view_script()
    {
        var scene = Scene.Load(FarmhousePath);

        var root = scene.Root;
        Assert.True(root is not null, "Farmhouse.tscn has no root node");
        Assert.Equal("Farmhouse", root!.Attr("name"));
        Assert.Equal("Node3D", root.Attr("type"));

        var scriptId = scene.ExtResources
            .FirstOrDefault(r => r.Attr("type") == "Script" && r.Attr("path") == "res://art/farm/FarmhouseView.cs")
            ?.Attr("id");
        Assert.True(scriptId is not null, "Farmhouse.tscn has no Script ext_resource for res://art/farm/FarmhouseView.cs");
        Assert.Equal(scriptId, ReferencedId(root.Prop("script"), "ExtResource"));
    }

    [Fact]
    public void AC5_farmhouse_scene_is_one_model_mesh()
    {
        var scene = Scene.Load(FarmhousePath);

        var meshes = scene.Nodes.Where(n => n.Attr("type") is "MeshInstance3D" or "MultiMeshInstance3D").ToList();
        Assert.Single(meshes);
        Assert.Equal("Model", Scene.PathOf(meshes[0]));
        Assert.Equal("MeshInstance3D", meshes[0].Attr("type"));
    }

    [Fact]
    public void AC5_farmhouse_scene_has_no_collision_or_physics_body()
    {
        var scene = Scene.Load(FarmhousePath);
        Assert.NotEmpty(scene.Nodes);

        foreach (var node in scene.Nodes)
        {
            Assert.False(
                PhysicsTypes.Contains(node.Attr("type")),
                $"Farmhouse.tscn node {Scene.PathOf(node)} is a {node.Attr("type")}");
        }
    }

    [Fact]
    public void AC5_farmhouse_view_merges_the_run_down_parts_under_the_shared_material()
    {
        Assert.True(File.Exists(FarmhouseViewPath), $"{FarmhouseViewPath} is missing");
        var text = File.ReadAllText(FarmhouseViewPath);

        Assert.Matches(@"namespace\s+LongWayHome\.Game\.Art\s*;", text);
        Assert.Matches(@"\bclass\s+FarmhouseView\b", text);
        Assert.Contains("FarmhouseArt.RunDown", text);
        Assert.Contains("ArtMeshes.Merge", text);
        Assert.Contains("ArtMaterials.Shared", text);
        Assert.Matches(@"RadialSegments\s*=\s*6\b", text);
    }

    [Fact]
    public void AC5_farmhouse_view_creates_no_collision_or_physics_body()
    {
        Assert.True(File.Exists(FarmhouseViewPath), $"{FarmhouseViewPath} is missing");
        var text = File.ReadAllText(FarmhouseViewPath);

        foreach (var type in PhysicsTypes)
        {
            Assert.False(text.Contains(type), $"FarmhouseView.cs mentions {type}");
        }
    }

    // ---- BR-G3: the sleep door is unchanged ----

    [Fact]
    public void BR_G3_door_point_stays_at_the_house_door_with_its_range_and_sleep()
    {
        var farm = Scene.Load(FarmPath);

        var point = farm.Node("InteractionPoints/DoorPoint");
        Assert.True(point is not null, "Farm.tscn has no InteractionPoints/DoorPoint");
        AssertTransform(point!, 0f, 0f, -7f);
        Assert.Equal("door", point!.Prop("PointId")?.Trim('"'));
        Assert.Equal(1.25f, float.Parse(point.Prop("Range")!, CultureInfo.InvariantCulture), Eps);

        var sleep = farm.Node("InteractionPoints/DoorPoint/DoorSleep");
        Assert.True(sleep is not null, "InteractionPoints/DoorPoint has no DoorSleep child");
    }
}
