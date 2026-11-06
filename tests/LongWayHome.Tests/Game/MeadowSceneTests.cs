using System.Globalization;
using System.Text.RegularExpressions;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real game files to check the meadow (#77): <c>Farm.tscn</c> instances <c>res://art/farm/Meadow.tscn</c>
/// in place of the old flat ground, the meadow's mesh nodes match <see cref="MeadowLayout.PartRoles"/> and carry no
/// collision, and the pond is split into palette-coloured shallows and a deeper centre. Each test loads the files it
/// needs, so a missing file fails as an assertion in that test only.
/// </summary>
public class MeadowSceneTests
{
    private const float Eps = 1e-4f;

    private static string GamePath(params string[] parts) =>
        Path.Combine(new[] { AppContext.BaseDirectory, "game" }.Concat(parts).ToArray());

    private static string FarmPath => GamePath("scenes", "Farm.tscn");
    private static string MeadowPath => GamePath("art", "farm", "Meadow.tscn");
    private static string MeadowViewPath => GamePath("art", "farm", "MeadowView.cs");

    private static readonly string[] PhysicsTypes =
    [
        "CollisionShape3D", "CollisionPolygon3D", "StaticBody3D", "RigidBody3D", "CharacterBody3D",
        "AnimatableBody3D", "Area3D",
    ];

    // ---- a tiny .tscn reader ----

    /// <summary>One <c>[tag key="value" ...]</c> section and the <c>key = value</c> lines after it.</summary>
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

        public required string Path { get; init; }
        public required IReadOnlyList<Section> Sections { get; init; }

        public IEnumerable<Section> Nodes => Sections.Where(s => s.Tag == "node");
        public IEnumerable<Section> ExtResources => Sections.Where(s => s.Tag == "ext_resource");
        public IEnumerable<Section> SubResources => Sections.Where(s => s.Tag == "sub_resource");

        public Section? Root => Nodes.FirstOrDefault(n => n.Attr("parent") is null);

        /// <summary>The node's path from the root: "." for the root, "Pond/Shallows" for a grandchild.</summary>
        public static string PathOf(Section node) => node.Attr("parent") switch
        {
            null => ".",
            "." => node.Attr("name")!,
            var parent => $"{parent}/{node.Attr("name")}",
        };

        public Section? Node(string path) => Nodes.FirstOrDefault(n => PathOf(n) == path);

        public Section? SubResource(string id) => SubResources.FirstOrDefault(s => s.Attr("id") == id);

        public string? ExtResourceId(string resPath) =>
            ExtResources.FirstOrDefault(r => r.Attr("path") == resPath)?.Attr("id");

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
            return new Scene { Path = path, Sections = sections };
        }

        private static string Unquote(string value) =>
            value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;
    }

    /// <summary>The numbers inside a value's parentheses, e.g. the 12 of a <c>Transform3D(...)</c>.</summary>
    private static float[] Numbers(string value) =>
        Regex.Matches(value[(value.IndexOf('(') + 1)..], @"-?\d+(\.\d+)?(e[-+]?\d+)?")
            .Select(m => float.Parse(m.Value, CultureInfo.InvariantCulture))
            .ToArray();

    /// <summary>The origin of a node's <c>transform</c>, or (0, 0, 0) when it has none.</summary>
    private static (float X, float Y, float Z) Origin(Section node)
    {
        var transform = node.Prop("transform");
        if (transform is null) return (0f, 0f, 0f);
        var n = Numbers(transform);
        Assert.Equal(12, n.Length);
        return (n[9], n[10], n[11]);
    }

    private static void AssertOrigin(Section node, float x, float y, float z)
    {
        var origin = Origin(node);
        Assert.True(
            MathF.Abs(origin.X - x) <= Eps && MathF.Abs(origin.Y - y) <= Eps && MathF.Abs(origin.Z - z) <= Eps,
            $"{Scene.PathOf(node)} is at {origin}, not ({x}, {y}, {z})");
    }

    private static string? ReferencedId(string? value, string kind)
    {
        if (value is null) return null;
        var match = Regex.Match(value, kind + @"\(""([^""]*)""\)");
        return match.Success ? match.Groups[1].Value : null;
    }

    // ---- AC1 ----

    [Fact]
    public void AC1_farm_drops_the_old_ground_nodes_and_their_sub_resources()
    {
        var farm = Scene.Load(FarmPath);

        foreach (var name in new[] { "Ground", "FarmPlotBorder", "FarmPlotArea" })
        {
            Assert.True(farm.Node(name) is null, $"Farm.tscn still has the node {name}");
        }

        foreach (var id in new[] { "Mat_grass", "PlaneMesh_grass", "Mat_plot", "PlaneMesh_plot", "Mat_plot_border", "PlaneMesh_plot_border" })
        {
            Assert.True(farm.SubResource(id) is null, $"Farm.tscn still has the sub_resource {id}");
        }
    }

    [Fact]
    public void AC1_farm_instances_the_meadow_at_the_origin()
    {
        var farm = Scene.Load(FarmPath);

        var id = farm.ExtResourceId("res://art/farm/Meadow.tscn");
        Assert.True(id is not null, "Farm.tscn has no ext_resource for res://art/farm/Meadow.tscn");

        var instance = farm.Nodes.FirstOrDefault(n => ReferencedId(n.Attr("instance"), "ExtResource") == id);
        Assert.True(instance is not null, "Farm.tscn has no node instancing Meadow.tscn");
        Assert.Equal(".", instance!.Attr("parent"));

        var transform = instance.Prop("transform");
        if (transform is not null)
        {
            var expected = new[] { 1f, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
            var actual = Numbers(transform);
            Assert.Equal(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.True(MathF.Abs(expected[i] - actual[i]) <= Eps, $"the Meadow instance's transform is not identity: {transform}");
            }
        }
    }

    [Fact]
    public void AC1_meadow_scene_has_a_meadow_root_with_its_view_script()
    {
        var meadow = Scene.Load(MeadowPath);

        var root = meadow.Root;
        Assert.True(root is not null, "Meadow.tscn has no root node");
        Assert.Equal("Meadow", root!.Attr("name"));
        Assert.Equal("Node3D", root.Attr("type"));

        var scriptId = meadow.ExtResources
            .FirstOrDefault(r => r.Attr("type") == "Script" && r.Attr("path") == "res://art/farm/MeadowView.cs")
            ?.Attr("id");
        Assert.True(scriptId is not null, "Meadow.tscn has no Script ext_resource for res://art/farm/MeadowView.cs");
        Assert.Equal(scriptId, ReferencedId(root.Prop("script"), "ExtResource"));
    }

    [Fact]
    public void AC1_meadow_scene_has_the_six_ground_layers()
    {
        var meadow = Scene.Load(MeadowPath);

        foreach (var name in new[] { "Ground", "ShadePatches", "MownMargin", "Path", "PathStones", "Edging" })
        {
            Assert.True(meadow.Node(name) is not null, $"Meadow.tscn has no child node {name}");
        }
    }

    // ---- AC6 ----

    [Fact]
    public void AC6_pond_splits_into_shallows_and_deep_and_keeps_its_collision_and_refill_point()
    {
        var farm = Scene.Load(FarmPath);

        Assert.True(farm.Node("Pond/Water") is null, "Farm.tscn still has Pond/Water");
        var shallows = farm.Node("Pond/Shallows");
        var deep = farm.Node("Pond/Deep");
        Assert.True(shallows is not null, "Farm.tscn has no Pond/Shallows");
        Assert.True(deep is not null, "Farm.tscn has no Pond/Deep");

        AssertCylinderMesh(farm, shallows!, 2.5f);
        AssertCylinderMesh(farm, deep!, 1.5f);
        Assert.Equal(Origin(shallows!).Y + 0.005f, Origin(deep!).Y, Eps);

        var pond = farm.Node("Pond");
        Assert.True(pond is not null, "Farm.tscn has no Pond");
        AssertOrigin(pond!, 10f, 0f, 0f);

        var collision = farm.Node("Pond/CollisionShape3D");
        Assert.True(collision is not null, "Farm.tscn has no Pond/CollisionShape3D");
        var shape = farm.SubResource(ReferencedId(collision!.Prop("shape"), "SubResource") ?? "");
        Assert.True(shape is not null, "Pond/CollisionShape3D has no shape sub_resource");
        Assert.Equal("CylinderShape3D", shape!.Attr("type"));
        Assert.Equal(2.5f, float.Parse(shape.Prop("radius")!, CultureInfo.InvariantCulture), Eps);

        var point = farm.Node("InteractionPoints/PondPoint");
        Assert.True(point is not null, "Farm.tscn has no InteractionPoints/PondPoint");
        AssertOrigin(point!, 10f, 0f, 0f);
        Assert.Equal(3.8f, float.Parse(point!.Prop("Range")!, CultureInfo.InvariantCulture), Eps);
    }

    private static void AssertCylinderMesh(Scene scene, Section node, float radius)
    {
        var name = Scene.PathOf(node);
        var mesh = scene.SubResource(ReferencedId(node.Prop("mesh"), "SubResource") ?? "");
        Assert.True(mesh is not null, $"{name} has no mesh sub_resource");
        Assert.Equal("CylinderMesh", mesh!.Attr("type"));
        Assert.True(mesh.Prop("top_radius") is not null, $"{name}'s CylinderMesh has no top_radius");
        Assert.True(mesh.Prop("bottom_radius") is not null, $"{name}'s CylinderMesh has no bottom_radius");
        Assert.Equal(radius, float.Parse(mesh.Prop("top_radius")!, CultureInfo.InvariantCulture), Eps);
        Assert.Equal(radius, float.Parse(mesh.Prop("bottom_radius")!, CultureInfo.InvariantCulture), Eps);
    }

    // ---- AC8 ----

    [Fact]
    public void AC8_meadow_scene_has_no_collision_or_physics_body()
    {
        var meadow = Scene.Load(MeadowPath);
        Assert.NotEmpty(meadow.Nodes);

        foreach (var node in meadow.Nodes)
        {
            Assert.False(
                PhysicsTypes.Contains(node.Attr("type")),
                $"Meadow.tscn node {Scene.PathOf(node)} is a {node.Attr("type")}");
        }
    }

    [Fact]
    public void AC8_meadow_view_creates_no_collision_or_physics_body()
    {
        Assert.True(File.Exists(MeadowViewPath), $"{MeadowViewPath} is missing");
        var text = File.ReadAllText(MeadowViewPath);

        foreach (var type in PhysicsTypes)
        {
            Assert.False(text.Contains(type), $"MeadowView.cs mentions {type}");
        }
    }

    // ---- AC9 ----

    [Fact]
    public void AC9_every_meadow_mesh_node_has_a_part_role_and_every_role_has_a_node()
    {
        var meadow = Scene.Load(MeadowPath);

        var meshNodes = meadow.Nodes
            .Where(n => n.Attr("type") is "MeshInstance3D" or "MultiMeshInstance3D")
            .Select(n => n.Attr("name")!)
            .ToList();
        Assert.NotEmpty(meshNodes);
        Assert.Equal(meshNodes.Count, meshNodes.Distinct().Count());

        var roles = MeadowLayout.PartRoles;
        var missingRole = meshNodes.Where(n => !roles.ContainsKey(n)).ToList();
        var missingNode = roles.Keys.Where(k => !meshNodes.Contains(k)).ToList();
        Assert.True(missingRole.Count == 0, $"Meadow.tscn mesh nodes with no PartRoles entry: {string.Join(", ", missingRole)}");
        Assert.True(missingNode.Count == 0, $"PartRoles entries with no mesh node in Meadow.tscn: {string.Join(", ", missingNode)}");
    }

    [Fact]
    public void AC9_meadow_view_applies_the_part_roles_through_the_shared_material()
    {
        Assert.True(File.Exists(MeadowViewPath), $"{MeadowViewPath} is missing");
        var text = File.ReadAllText(MeadowViewPath);

        Assert.Contains("ArtMaterials.Apply", text);
        Assert.Contains("PartRoles", text);
    }

    [Theory]
    [InlineData("Pond/Shallows", "lake-shallow")]
    [InlineData("Pond/Deep", "lake-deep")]
    public void AC9_pond_meshes_use_the_palette_material_with_their_swatch(string path, string role)
    {
        var farm = Scene.Load(FarmPath);

        var node = farm.Node(path);
        Assert.True(node is not null, $"Farm.tscn has no {path}");

        var paletteId = farm.ExtResourceId("res://art/materials/palette.tres");
        Assert.True(paletteId is not null, "Farm.tscn has no ext_resource for res://art/materials/palette.tres");
        Assert.Equal(paletteId, ReferencedId(node!.Prop("material_override"), "ExtResource"));

        var swatch = node.Prop("instance_shader_parameters/swatch");
        Assert.True(swatch is not null, $"{path} has no instance_shader_parameters/swatch");
        Assert.StartsWith("Vector2(", swatch);
        var uv = Numbers(swatch!);
        Assert.Equal(2, uv.Length);
        var (u, v) = ArtPalette.UvCentre(role);
        Assert.Equal(u, uv[0], Eps);
        Assert.Equal(v, uv[1], Eps);
    }
}
