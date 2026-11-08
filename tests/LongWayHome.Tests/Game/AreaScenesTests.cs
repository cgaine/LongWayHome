using System.Globalization;
using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files from the repository and checks the glue of #136's areas: the shared
/// <c>AreaCommon.tscn</c>, the four area scenes, the <c>AreaScenes</c> map, the exit/arrival pairs between neighbouring
/// areas and the gap in the farm's south wall. Each check looks for the essential tokens, not exact formatting.
/// </summary>
public class AreaScenesTests
{
    private const string AreaExitScript = "res://scripts/World/AreaExit.cs";

    private const string AreaArrivalScript = "res://scripts/World/AreaArrival.cs";

    /// <summary>The open areas' codes (canonical-values.md) and the scene each lives in.</summary>
    private static readonly Dictionary<string, string> SceneByArea = new()
    {
        ["farm"] = "Farm",
        ["forest_path"] = "ForestPath",
        ["town"] = "Town",
        ["lakeshore"] = "Lakeshore",
    };

    private static readonly Regex NodeBlock = new(
        @"^\[node\s+(?<attrs>[^\]]*)\]\s*$(?<body>.*?)(?=^\[|\z)",
        RegexOptions.Multiline | RegexOptions.Singleline);

    // ---- AC7: the shared gameplay nodes live in one scene ----

    [Theory]
    [InlineData("Player")]
    [InlineData("Hud")]
    [InlineData("PauseMenu")]
    [InlineData("NightTransition")]
    [InlineData("ShopMenu")]
    [InlineData("BackpackMenu")]
    [InlineData("DayClock")]
    [InlineData("RainView")]
    [InlineData("NightGlowView")]
    public void AC7_area_common_scene_holds_the_shared_gameplay_node(string name)
    {
        var scene = ReadRepoFile("game", "scenes", "AreaCommon.tscn");

        Assert.Contains(Nodes(scene), node => node.Name == name);
    }

    [Theory]
    [InlineData("Farm")]
    [InlineData("ForestPath")]
    [InlineData("Town")]
    [InlineData("Lakeshore")]
    public void AC7_each_area_scene_instances_area_common(string sceneName)
    {
        var scene = ReadRepoFile("game", "scenes", $"{sceneName}.tscn");

        var id = ExtResourceId(scene, "PackedScene", "res://scenes/AreaCommon.tscn");
        Assert.True(id is not null, $"{sceneName}.tscn has no PackedScene ext_resource for res://scenes/AreaCommon.tscn");
        Assert.Matches(
            @"\[node\s+name=""[^""]+""\s+parent=""\.""\s+instance=ExtResource\(""" + Regex.Escape(id!) + @"""\)\]",
            scene);
    }

    [Theory]
    [InlineData("Farm")]
    [InlineData("ForestPath")]
    [InlineData("Town")]
    [InlineData("Lakeshore")]
    public void AC7_each_area_scene_has_its_own_environment_and_light(string sceneName)
    {
        var scene = ReadRepoFile("game", "scenes", $"{sceneName}.tscn");

        Assert.Contains(Nodes(scene), node => node.Type == "WorldEnvironment");
        Assert.Contains(Nodes(scene), node => node.Type == "DirectionalLight3D");
    }

    // ---- AC7: the AreaScenes map ----

    [Theory]
    [InlineData("farm", "Farm", "Farm")]
    [InlineData("forest_path", "ForestPath", "ForestPath")]
    [InlineData("town", "Town", "Town")]
    [InlineData("lakeshore", "Lakeshore", "Lakeshore")]
    public void AC7_area_scenes_maps_the_area_to_its_scene(string code, string enumName, string sceneName)
    {
        var source = ReadRepoFile("game", "scripts", "World", "AreaScenes.cs");

        // Keyed by the code string or the AreaId member, with the scene path on the same line.
        Assert.Matches(
            @"(?:""" + Regex.Escape(code) + @"""|AreaId\." + enumName + @"\b)[^\n]*""res://scenes/" + sceneName + @"\.tscn""",
            source);
    }

    // ---- AC7: each exit has an arrival beside it on the other side ----

    [Theory]
    [InlineData("Farm", "forest_path")]
    [InlineData("ForestPath", "farm")]
    [InlineData("ForestPath", "town")]
    [InlineData("Town", "forest_path")]
    [InlineData("Town", "lakeshore")]
    [InlineData("Lakeshore", "town")]
    public void AC7_exit_leads_to_an_arrival_in_the_neighbouring_area(string fromScene, string targetArea)
    {
        var exits = ScriptedNodes(ReadRepoFile("game", "scenes", $"{fromScene}.tscn"), AreaExitScript);
        var exit = exits.FirstOrDefault(node => Property(node, "TargetArea") == targetArea);
        Assert.True(exit is not null, $"{fromScene}.tscn has no AreaExit with TargetArea = \"{targetArea}\"");
        var arrivalId = Property(exit!, "ArrivalId");
        Assert.False(string.IsNullOrEmpty(arrivalId), $"{fromScene}.tscn's exit to {targetArea} has no ArrivalId");

        var targetScene = SceneByArea[targetArea];
        var arrivals = ScriptedNodes(ReadRepoFile("game", "scenes", $"{targetScene}.tscn"), AreaArrivalScript);
        Assert.Contains(arrivals, node => Property(node, "ArrivalId") == arrivalId);
    }

    [Theory]
    [InlineData("Farm")]
    [InlineData("ForestPath")]
    [InlineData("Town")]
    [InlineData("Lakeshore")]
    public void AC7_every_exit_names_an_open_area_and_an_arrival_that_exists_there(string sceneName)
    {
        var exits = ScriptedNodes(ReadRepoFile("game", "scenes", $"{sceneName}.tscn"), AreaExitScript);
        Assert.True(exits.Count > 0, $"{sceneName}.tscn has no AreaExit");

        foreach (var exit in exits)
        {
            var target = Property(exit, "TargetArea");
            Assert.True(
                target is not null && SceneByArea.ContainsKey(target),
                $"{sceneName}.tscn's exit {exit.Name} targets \"{target}\", not an open area");
            var targetScene = SceneByArea[target!];
            var arrivalId = Property(exit, "ArrivalId");
            var arrivals = ScriptedNodes(ReadRepoFile("game", "scenes", $"{targetScene}.tscn"), AreaArrivalScript);
            Assert.True(
                arrivals.Any(node => Property(node, "ArrivalId") == arrivalId),
                $"{sceneName}.tscn's exit {exit.Name} names arrival \"{arrivalId}\", which {targetScene}.tscn lacks");
        }
    }

    // ---- AC7: the farm's south wall opens at x = 0 ----

    [Fact]
    public void AC7_farm_south_wall_has_a_three_tile_gap_centred_on_x_0()
    {
        var scene = ReadRepoFile("game", "scenes", "Farm.tscn");
        var boxWidths = BoxShapeWidths(scene);

        // The south wall: the Boundary's collision shapes at the farm's south edge (z = 13.5).
        var south = Nodes(scene)
            .Where(node => node.Parent == "Boundary" && node.Type == "CollisionShape3D")
            .Select(node => (node.Name, Origin: Origin(node), Shape: ShapeId(node)))
            .Where(wall => wall.Origin is { } origin && Math.Abs(origin.Z - 13.5) < 0.75)
            .ToList();
        Assert.True(south.Count > 0, "Farm.tscn's Boundary has no wall at the south edge (z = 13.5)");

        foreach (var (name, origin, shape) in south)
        {
            Assert.True(shape is not null && boxWidths.ContainsKey(shape), $"Boundary/{name} has no BoxShape3D");
            var halfWidth = boxWidths[shape!] / 2;
            var left = origin!.Value.X - halfWidth;
            var right = origin.Value.X + halfWidth;

            // Nothing of the wall may reach into the 3-tile gap from x = -1.5 to x = 1.5.
            Assert.True(
                right <= -1.5 + 1e-6 || left >= 1.5 - 1e-6,
                $"Boundary/{name} spans x = {left} to {right}, closing the south exit at x = 0");
        }
    }

    // ---- Parsing helpers ----

    private sealed record Node(string Name, string? Type, string? Parent, string Body);

    private static List<Node> Nodes(string scene) =>
        NodeBlock.Matches(scene)
            .Select(match =>
            {
                var attrs = match.Groups["attrs"].Value;
                return new Node(
                    Attribute(attrs, "name") ?? "",
                    Attribute(attrs, "type"),
                    Attribute(attrs, "parent"),
                    match.Groups["body"].Value);
            })
            .ToList();

    private static string? Attribute(string attrs, string key)
    {
        var match = Regex.Match(attrs, @"(?:^|\s)" + key + @"=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtResourceId(string scene, string type, string path)
    {
        var match = Regex.Match(
            scene,
            @"\[ext_resource\s+type=""" + type + @"""[^\]]*path=""" + Regex.Escape(path) + @"""[^\]]*id=""([^""]+)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>The nodes whose script is the given ext_resource script; none when the scene doesn't load it.</summary>
    private static List<Node> ScriptedNodes(string scene, string scriptPath)
    {
        var id = ExtResourceId(scene, "Script", scriptPath);
        if (id is null)
        {
            return [];
        }

        var script = new Regex(@"^script\s*=\s*ExtResource\(""" + Regex.Escape(id) + @"""\)\s*$", RegexOptions.Multiline);
        return Nodes(scene).Where(node => script.IsMatch(node.Body)).ToList();
    }

    /// <summary>A string property set on the node, e.g. <c>TargetArea = "town"</c>.</summary>
    private static string? Property(Node node, string key)
    {
        var match = Regex.Match(node.Body, @"^" + key + @"\s*=\s*""([^""]*)""\s*$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>The origin of the node's <c>transform = Transform3D(...)</c>: its last three numbers.</summary>
    private static (double X, double Y, double Z)? Origin(Node node)
    {
        var match = Regex.Match(node.Body, @"^transform\s*=\s*Transform3D\(([^)]*)\)", RegexOptions.Multiline);
        if (!match.Success)
        {
            return null;
        }

        var numbers = match.Groups[1].Value
            .Split(',')
            .Select(part => double.Parse(part.Trim(), CultureInfo.InvariantCulture))
            .ToArray();
        return numbers.Length == 12 ? (numbers[9], numbers[10], numbers[11]) : null;
    }

    private static string? ShapeId(Node node)
    {
        var match = Regex.Match(node.Body, @"^shape\s*=\s*SubResource\(""([^""]+)""\)", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>The x size of every BoxShape3D sub_resource, by id.</summary>
    private static Dictionary<string, double> BoxShapeWidths(string scene) =>
        Regex.Matches(
                scene,
                @"^\[sub_resource\s+type=""BoxShape3D""\s+id=""(?<id>[^""]+)""\]\s*$(?<body>.*?)(?=^\[|\z)",
                RegexOptions.Multiline | RegexOptions.Singleline)
            .Select(match => (
                Id: match.Groups["id"].Value,
                Size: Regex.Match(match.Groups["body"].Value, @"^size\s*=\s*Vector3\(\s*([-0-9.eE]+)", RegexOptions.Multiline)))
            .Where(box => box.Size.Success)
            .ToDictionary(box => box.Id, box => double.Parse(box.Size.Groups[1].Value, CultureInfo.InvariantCulture));

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        return dir!.FullName;
    }
}
