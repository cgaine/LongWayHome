using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files for #166 AC7 and AC8: the farm Shipping Bin moves out of <c>Farm.tscn</c> into
/// <c>FarmShippingBin.tscn</c>, which <c>ShippingBinGate</c> instances once the general store is at stage 4 (BR-S3),
/// and the Hud finds a <c>bin</c> point added after its <c>_Ready</c>.
/// </summary>
public class FarmShippingBinSceneTests
{
    private const string BinScenePath = "res://scenes/FarmShippingBin.tscn";

    private const string GateScript = "res://scripts/World/ShippingBinGate.cs";

    private const string PointScript = "res://scripts/World/InteractionPoint.cs";

    private const string BinShipScript = "res://scripts/World/BinShip.cs";

    private static string BinScene() => ReadRepoFile("game", "scenes", "FarmShippingBin.tscn");

    private static string Farm() => ReadRepoFile("game", "scenes", "Farm.tscn");

    private static string Script(params string[] parts) => Code(ReadRepoFile(new[] { "game", "scripts" }.Concat(parts).ToArray()));

    /// <summary>The value of a <c>size = Vector3(…)</c> line in the sub_resource with the given id.</summary>
    private static string? SubResourceSize(string scene, string id)
    {
        var block = Regex.Match(
            scene,
            @"^\[sub_resource\s+[^\]]*id=""" + Regex.Escape(id) + @"""[^\]]*\]\s*$(?<body>.*?)(?=^\[|\z)",
            RegexOptions.Multiline | RegexOptions.Singleline);
        if (!block.Success)
        {
            return null;
        }

        var size = Regex.Match(block.Groups["body"].Value, @"^size\s*=\s*Vector3\(([^)]*)\)", RegexOptions.Multiline);
        return size.Success ? Regex.Replace(size.Groups[1].Value, @"\s", "") : null;
    }

    private static string? SubResourceRef(Node node, string key)
    {
        var match = Regex.Match(node.Body, @"^" + key + @"\s*=\s*SubResource\(""([^""]+)""\)", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }

    // ---- AC7: FarmShippingBin.tscn ----

    [Fact]
    public void AC7_FarmShippingBin_has_a_bin_InteractionPoint_at_3_0_minus_6_with_range_1_7()
    {
        var scene = BinScene();

        var point = Assert.Single(ScriptedNodes(scene, PointScript), node => Property(node, "PointId") == "bin");
        Assert.Equal("BinPoint", point.Name);
        Assert.Equal((3.0, 0.0, -6.0), Position(scene, point));
        Assert.Equal(1.7, Number(point, "Range"));
    }

    [Fact]
    public void AC7_FarmShippingBin_bin_point_has_a_BinShip_child()
    {
        var scene = BinScene();
        var point = Assert.Single(ScriptedNodes(scene, PointScript), node => Property(node, "PointId") == "bin");

        var binShip = Assert.Single(ScriptedNodes(scene, BinShipScript));
        Assert.Equal(point.Path, binShip.Parent);
    }

    [Fact]
    public void AC7_FarmShippingBin_has_a_ShippingBin_StaticBody3D_at_3_0_4_minus_6()
    {
        var scene = BinScene();

        var body = Assert.Single(Nodes(scene), node => node.Name == "ShippingBin");
        Assert.Equal("StaticBody3D", body.Type);
        Assert.Equal((3.0, 0.4, -6.0), Position(scene, body));
    }

    [Fact]
    public void AC7_FarmShippingBin_body_has_a_wood_box_mesh_and_collision_of_1_2_by_0_8_by_0_8()
    {
        var scene = BinScene();
        var nodes = Nodes(scene);
        var body = Assert.Single(nodes, node => node.Name == "ShippingBin");

        var mesh = Assert.Single(nodes, node => node.Parent == body.Path && node.Type == "MeshInstance3D");
        var collision = Assert.Single(nodes, node => node.Parent == body.Path && node.Type == "CollisionShape3D");
        var meshId = SubResourceRef(mesh, "mesh");
        var shapeId = SubResourceRef(collision, "shape");
        Assert.NotNull(meshId);
        Assert.NotNull(shapeId);
        Assert.Equal("1.2,0.8,0.8", SubResourceSize(scene, meshId!));
        Assert.Equal("1.2,0.8,0.8", SubResourceSize(scene, shapeId!));
        Assert.Matches(
            @"albedo_color\s*=\s*Color\(0\.48,\s*0\.32,\s*0\.18,\s*1\)",
            scene);
    }

    // ---- AC7: Farm.tscn ----

    [Fact]
    public void AC7_Farm_has_a_ShippingBinGate_node_at_the_origin()
    {
        var scene = Farm();

        var gate = Assert.Single(ScriptedNodes(scene, GateScript));
        Assert.Equal("ShippingBinGate", gate.Name);
        Assert.Equal((0.0, 0.0, 0.0), Position(scene, gate));
    }

    [Fact]
    public void AC7_Farm_no_longer_has_its_own_ShippingBin_body_or_bin_point()
    {
        var scene = Farm();
        var nodes = Nodes(scene);

        Assert.DoesNotContain(nodes, node => node.Name == "ShippingBin");
        Assert.DoesNotContain(nodes, node => node.Path == "InteractionPoints/BinPoint");
        Assert.DoesNotContain(ScriptedNodes(scene, PointScript), node => Property(node, "PointId") == "bin");
        Assert.Empty(ScriptedNodes(scene, BinShipScript));
    }

    [Fact]
    public void AC7_Farm_does_not_instance_FarmShippingBin_statically()
    {
        Assert.Empty(InstancesOf(Farm(), BinScenePath));
    }

    // ---- BR-S3: the gate ----

    [Fact]
    public void BR_S3_ShippingBinGate_instances_FarmShippingBin_when_the_game_has_a_shipping_bin()
    {
        var source = Script("World", "ShippingBinGate.cs");

        Assert.Contains($"\"{BinScenePath}\"", source);
        Assert.Contains("HasShippingBin", source);
        Assert.Contains("Instantiate", source);
        Assert.Contains("_Ready", source);
    }

    [Fact]
    public void BR_S3_ShippingBinGate_checks_again_after_each_Night_and_unsubscribes()
    {
        var source = Script("World", "ShippingBinGate.cs");

        Assert.Contains("NightCompleted +=", source);
        Assert.Contains("NightCompleted -=", source);
    }

    // ---- AC8: the Hud's ship hint for a bin added after _Ready ----

    [Fact]
    public void AC8_Hud_finds_the_bin_point_again_on_NightCompleted()
    {
        var source = Script("Ui", "Hud.cs");

        // Every handler Hud subscribes to NightCompleted, and the methods each calls directly.
        var handlers = Regex.Matches(source, @"NightCompleted\s*\+=\s*(\w+)\s*;")
            .Select(match => match.Groups[1].Value)
            .ToList();
        Assert.NotEmpty(handlers);

        var reached = handlers.SelectMany(handler =>
        {
            var body = MethodBody(source, handler);
            var called = Regex.Matches(body, @"\b(\w+)\s*\(").Select(match => match.Groups[1].Value);
            return new[] { body }.Concat(called.Select(name => MethodBody(source, name)));
        });

        Assert.Contains(reached, body => body.Contains("BinPointId", StringComparison.Ordinal));
    }

    /// <summary>The braces-balanced body of the first method named <paramref name="name"/>, or "" when none.</summary>
    private static string MethodBody(string source, string name)
    {
        var header = Regex.Match(source, @"\bvoid\s+" + Regex.Escape(name) + @"\s*\([^)]*\)\s*(=>)?");
        if (!header.Success)
        {
            return "";
        }

        var start = header.Index + header.Length;
        if (header.Groups[1].Success)
        {
            var end = source.IndexOf(';', start);
            return source[start..end];
        }

        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            depth += source[i] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
            {
                return source[open..(i + 1)];
            }
        }

        return source[open..];
    }
}
