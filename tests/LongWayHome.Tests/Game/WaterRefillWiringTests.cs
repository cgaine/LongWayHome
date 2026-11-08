using System.Text.RegularExpressions;
using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Game;

/// <summary>
/// #137: the code paths the scene tests don't drive. A <c>WaterRefill</c> with an unknown <c>Source</c> reports it and
/// never refills; every scene's <c>WaterRefill</c> names a real source matching its point's id, so the Hud's hint and
/// the refill agree; <c>InteractionPoint.FindAll</c> searches the whole tree; and the Hud disconnects every water point.
/// </summary>
public class WaterRefillWiringTests
{
    private const string RefillScriptPath = "res://scripts/World/WaterRefill.cs";

    private const string PointScriptPath = "res://scripts/World/InteractionPoint.cs";

    [Fact]
    public void Water_refill_with_an_unknown_source_reports_it_and_stops_listening_before_finding_the_session()
    {
        var source = ReadRepoFile("game", "scripts", "World", "WaterRefill.cs");

        var match = Regex.Match(
            source,
            @"if\s*\(\s*!WaterSourceCodes\.TryParse\(\s*Source\s*,\s*out\s+(?<field>\w+)\s*\)\s*\)\s*\{(?<body>.*?)\n        \}",
            RegexOptions.Singleline);
        Assert.True(match.Success, "WaterRefill doesn't check its Source with WaterSourceCodes.TryParse");
        var body = match.Groups["body"].Value;
        Assert.Contains("GD.PushError(", body, StringComparison.Ordinal);
        Assert.Contains("SetProcessUnhandledInput(false)", body, StringComparison.Ordinal);
        Assert.Contains("return;", body, StringComparison.Ordinal);
        Assert.True(
            match.Index < source.IndexOf(".Session", StringComparison.Ordinal),
            "WaterRefill finds the session before checking its Source");
        Assert.Matches(@"RefillCan\s*\(\s*" + match.Groups["field"].Value + @"\s*\)", source);
    }

    [Fact]
    public void Every_scene_water_refill_names_a_water_source_matching_its_point_id()
    {
        var scenes = Directory.GetFiles(Path.Combine(RepoRoot(), "game", "scenes"), "*.tscn");
        var refills = 0;
        foreach (var path in scenes)
        {
            var scene = File.ReadAllText(path);
            var refillId = ExtResourceId(scene, RefillScriptPath);
            if (refillId is null)
            {
                continue;
            }

            var pointId = ExtResourceId(scene, PointScriptPath);
            var nodes = Nodes(scene);
            var byPath = nodes.ToDictionary(node => node.Path);
            foreach (var node in nodes.Where(node => HasScript(node, refillId)))
            {
                refills++;
                var code = Property(node, "Source") ?? "pond";
                Assert.True(WaterSourceCodes.TryParse(code, out _), $"{Path.GetFileName(path)}'s {node.Path} has Source = \"{code}\"");
                Assert.True(
                    node.Parent is not null && byPath.TryGetValue(node.Parent, out var parent) && pointId is not null &&
                    HasScript(parent, pointId) && Property(parent, "PointId") == code,
                    $"{Path.GetFileName(path)}'s {node.Path} is not under an InteractionPoint with PointId = \"{code}\"");
            }
        }

        // 1 pond + 13 farm river + 12 town river + 19 lake.
        Assert.Equal(45, refills);
    }

    [Fact]
    public void Find_all_collects_every_matching_point_through_the_whole_tree()
    {
        var source = ReadRepoFile("game", "scripts", "World", "InteractionPoint.cs");
        var findAll = Regex.Match(source, @"FindAll\s*\(\s*Node\s+node.*?\n    }\n", RegexOptions.Singleline);
        Assert.True(findAll.Success, "InteractionPoint has no FindAll");

        // It keeps going after a match (returns a list it fills), and recurses into every child.
        Assert.DoesNotMatch(@"return\s+point\s*;", findAll.Value);
        Assert.Matches(@"foreach\s*\(\s*var\s+child\s+in\s+node\.GetChildren\(\)\s*\)\s*\{\s*Collect\(", source);
        var collect = Regex.Match(source, @"void\s+Collect\s*\(.*?\n    }\n", RegexOptions.Singleline);
        Assert.Matches(@"node\s+is\s+InteractionPoint\s+point\s*&&\s*Array\.IndexOf\(\s*pointIds\s*,\s*point\.PointId\s*\)\s*>=\s*0", collect.Value);
        Assert.Contains("found.Add(point)", collect.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Hud_disconnects_every_water_point_on_exit()
    {
        var source = ReadRepoFile("game", "scripts", "Ui", "Hud.cs");
        var exitTree = source[source.IndexOf("public override void _ExitTree()", StringComparison.Ordinal)..];

        Assert.Matches(@"foreach\s*\(\s*var\s+point\s+in\s+_waterPoints\s*\)", exitTree);
        Assert.Contains("PlayerEntered -= OnWaterPointEntered", exitTree, StringComparison.Ordinal);
        Assert.Contains("PlayerExited -= OnWaterPointExited", exitTree, StringComparison.Ordinal);
    }

    [Fact]
    public void Hud_hides_the_refill_hint_only_when_no_water_point_is_in_range()
    {
        var source = ReadRepoFile("game", "scripts", "Ui", "Hud.cs");
        var exited = Regex.Match(source, @"void\s+OnWaterPointExited\s*\(\s*\)\s*\{.*?\n    }\n", RegexOptions.Singleline);
        Assert.True(exited.Success, "Hud has no OnWaterPointExited");

        Assert.Matches(@"if\s*\(\s*_waterPointsInRange\s*==\s*0\s*\)\s*\{\s*HideRefillHint\(\);", exited.Value);
        Assert.Contains("_refillHint.Visible = _waterPointsInRange > 0;", source, StringComparison.Ordinal);
    }

    private sealed record Node(string Name, string? Parent, string Body)
    {
        public string Path => Parent is null ? "." : Parent == "." ? Name : $"{Parent}/{Name}";
    }

    private static List<Node> Nodes(string scene) =>
        Regex.Matches(
                scene,
                @"^\[node\s+(?<attrs>[^\]]*)\]\s*$(?<body>.*?)(?=^\[|\z)",
                RegexOptions.Multiline | RegexOptions.Singleline)
            .Select(match => new Node(
                Attribute(match.Groups["attrs"].Value, "name") ?? "",
                Attribute(match.Groups["attrs"].Value, "parent"),
                match.Groups["body"].Value))
            .ToList();

    private static string? Attribute(string attrs, string key)
    {
        var match = Regex.Match(attrs, @"(?:^|\s)" + key + @"=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtResourceId(string scene, string path)
    {
        var match = Regex.Match(
            scene, @"\[ext_resource\s+type=""Script""[^\]]*path=""" + Regex.Escape(path) + @"""[^\]]*id=""([^""]+)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool HasScript(Node node, string id) =>
        Regex.IsMatch(node.Body, @"^script\s*=\s*ExtResource\(""" + Regex.Escape(id) + @"""\)\s*$", RegexOptions.Multiline);

    private static string? Property(Node node, string key)
    {
        var match = Regex.Match(node.Body, @"^" + key + @"\s*=\s*""([^""]*)""\s*$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }

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
