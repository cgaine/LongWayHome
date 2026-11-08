using System.Globalization;
using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files and checks #137's water points: the farm's and the town's <c>river</c> points along the
/// bank, the lakeshore's <c>lake</c> points along the shore and at the dock end, each an <c>InteractionPoint</c> with a
/// <c>WaterRefill</c> child naming its source; the pond's point unchanged; and the Hud's refill hint over all of them.
/// </summary>
public class WaterPointsSceneTests
{
    private const string PointScript = "res://scripts/World/InteractionPoint.cs";

    private const string RefillScript = "res://scripts/World/WaterRefill.cs";

    private const double BankRange = 1.5;

    /// <summary>The farthest apart two neighbouring points along a bank may stand, in tiles.</summary>
    private const double MaxSpacing = 2.0;

    /// <summary>The town's river wall face is at x = 16; the Player (radius 0.35) reaches about 15.65.</summary>
    private const double BankMinX = 14.5;

    private const double BankMaxX = 15.65;

    /// <summary>The farm's river bank face moved to x = 36.5 with the 72 x 36 grid (#150); the Player reaches about 36.15.</summary>
    private const double FarmBankMinX = 35.0;

    private const double FarmBankMaxX = 36.15;

    /// <summary>TownLayout.BridgeZ: the broken bridge stub stands here on the town's bank.</summary>
    private const double BridgeZ = 0.0;

    private static readonly Regex NodeBlock = new(
        @"^\[node\s+(?<attrs>[^\]]*)\]\s*$(?<body>.*?)(?=^\[|\z)",
        RegexOptions.Multiline | RegexOptions.Singleline);

    // ---- AC4: the farm's river bank ----

    [Fact]
    public void AC4_farm_river_points_refill_from_the_river_at_range_1_5()
    {
        var points = WaterPoints("Farm", "river");

        Assert.True(points.Count >= 2, $"Farm.tscn has {points.Count} InteractionPoint(s) with PointId = \"river\"");
        AssertEachRefillsFrom(points, "Farm", "river");
        AssertEachRange(points, BankRange);
    }

    [Fact]
    public void AC4_farm_river_points_stand_on_the_walkable_side_of_the_bank()
    {
        var points = WaterPoints("Farm", "river");
        Assert.NotEmpty(points);

        foreach (var point in points)
        {
            Assert.InRange(point.X, FarmBankMinX, FarmBankMaxX);
            Assert.InRange(point.Z, -13.0, 31.5);
        }
    }

    [Fact]
    public void AC4_farm_river_points_cover_the_bank_no_more_than_2_apart()
    {
        var zs = WaterPoints("Farm", "river").Select(point => point.Z).Order().ToList();
        Assert.NotEmpty(zs);

        Assert.True(zs[0] <= -11, $"The farm's northernmost river point is at z = {zs[0]}, short of the north end");
        Assert.True(zs[^1] >= 30, $"The farm's southernmost river point is at z = {zs[^1]}, short of the south end");
        foreach (var (a, b) in zs.Zip(zs.Skip(1)))
        {
            Assert.True(b - a <= MaxSpacing, $"The farm's river points at z = {a} and z = {b} are more than 2 apart");
        }
    }

    // ---- AC5: the town's river bank ----

    [Fact]
    public void AC5_town_river_points_refill_from_the_river_at_range_1_5()
    {
        var points = WaterPoints("Town", "river");

        Assert.True(points.Count >= 2, $"Town.tscn has {points.Count} InteractionPoint(s) with PointId = \"river\"");
        AssertEachRefillsFrom(points, "Town", "river");
        AssertEachRange(points, BankRange);
    }

    [Fact]
    public void AC5_town_river_points_stand_on_the_walkable_side_of_the_bank_clear_of_the_bridge_stub()
    {
        var points = WaterPoints("Town", "river");
        Assert.NotEmpty(points);

        foreach (var point in points)
        {
            Assert.InRange(point.X, BankMinX, BankMaxX);
            Assert.True(
                Math.Abs(point.Z - BridgeZ) > 1.2,
                $"Town.tscn's river point {point.Path} at z = {point.Z} stands on the bridge stub");
        }
    }

    [Fact]
    public void AC5_town_river_points_cover_the_bank_with_a_gap_only_at_the_bridge_stub()
    {
        var zs = WaterPoints("Town", "river").Select(point => point.Z).Order().ToList();
        Assert.NotEmpty(zs);

        Assert.True(zs[0] <= -12, $"The town's northernmost river point is at z = {zs[0]}, short of the north end");
        Assert.True(zs[^1] >= 12, $"The town's southernmost river point is at z = {zs[^1]}, short of the south end");
        var gaps = zs.Zip(zs.Skip(1)).Where(pair => pair.Second - pair.First > MaxSpacing).ToList();
        Assert.True(gaps.Count <= 1, $"The town's river points have {gaps.Count} gaps wider than 2; only the bridge stub may break them");
        foreach (var (a, b) in gaps)
        {
            Assert.True(
                a < BridgeZ && b > BridgeZ && a >= BridgeZ - 3.5 && b <= BridgeZ + 3.5,
                $"The town's river points have a gap from z = {a} to z = {b}, not at the bridge stub (z = {BridgeZ})");
        }
    }

    // ---- AC6: the lake's shore and dock ----

    [Fact]
    public void AC6_lakeshore_lake_points_refill_from_the_lake_at_range_1_5()
    {
        var points = WaterPoints("Lakeshore", "lake");

        Assert.True(points.Count >= 2, $"Lakeshore.tscn has {points.Count} InteractionPoint(s) with PointId = \"lake\"");
        AssertEachRefillsFrom(points, "Lakeshore", "lake");
        AssertEachRange(points, BankRange);
    }

    [Fact]
    public void AC6_lakeshore_has_a_lake_point_at_the_end_of_the_dock()
    {
        var points = WaterPoints("Lakeshore", "lake");

        Assert.Contains(points, IsAtDockEnd);
    }

    [Fact]
    public void AC6_lakeshore_lake_points_stand_on_the_shore_or_the_dock_end()
    {
        var points = WaterPoints("Lakeshore", "lake");
        Assert.NotEmpty(points);

        foreach (var point in points)
        {
            Assert.True(
                IsOnShore(point) || IsAtDockEnd(point),
                $"Lakeshore.tscn's lake point {point.Path} at ({point.X}, {point.Z}) is neither on the shore nor at the dock end");
        }
    }

    [Fact]
    public void AC6_lakeshore_shore_points_cover_the_shoreline_no_more_than_2_apart()
    {
        var xs = WaterPoints("Lakeshore", "lake").Where(IsOnShore).Select(point => point.X).Order().ToList();
        Assert.NotEmpty(xs);

        Assert.True(xs[0] <= -18, $"The lake's westernmost shore point is at x = {xs[0]}, short of the west end");
        Assert.True(xs[^1] >= 14, $"The lake's easternmost shore point is at x = {xs[^1]}, short of the east end");
        foreach (var (a, b) in xs.Zip(xs.Skip(1)))
        {
            Assert.True(b - a <= MaxSpacing, $"The lake's shore points at x = {a} and x = {b} are more than 2 apart");
        }
    }

    // ---- AC7: the hint stays on along the bank ----

    [Theory]
    [InlineData("Farm", "river")]
    [InlineData("Town", "river")]
    [InlineData("Lakeshore", "lake")]
    public void AC7_neighbouring_water_points_ranges_overlap_along_the_bank(string sceneName, string pointId)
    {
        var points = WaterPoints(sceneName, pointId);
        var shore = sceneName == "Lakeshore"
            ? points.Where(IsOnShore).OrderBy(point => point.X).ToList()
            : points.OrderBy(point => point.Z).ToList();
        Assert.True(shore.Count >= 2, $"{sceneName}.tscn has fewer than two {pointId} points along its bank");

        foreach (var (a, b) in shore.Zip(shore.Skip(1)))
        {
            if (sceneName == "Town" && a.Z < BridgeZ && b.Z > BridgeZ)
            {
                continue; // the bridge stub breaks the town's bank
            }

            var distance = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Z - a.Z, 2));
            Assert.True(
                distance <= a.Range + b.Range,
                $"{sceneName}.tscn's {a.Path} and {b.Path} are {distance} apart; their ranges ({a.Range}, {b.Range}) leave a gap");
        }
    }

    [Fact]
    public void AC7_hud_finds_every_water_point_with_find_all()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Ui", "Hud.cs"));

        Assert.Matches(@"InteractionPoint\.FindAll\s*\(", source);
        foreach (var id in new[] { "pond", "river", "lake" })
        {
            Assert.Contains($"\"{id}\"", source, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"InteractionPoint\.Find\s*\(\s*GetTree\(\)\.Root\s*,\s*PondPointId\s*\)", source);
    }

    [Fact]
    public void AC7_hud_counts_water_point_enters_and_exits()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Ui", "Hud.cs"));

        Assert.Matches(@"\w\s*\+\+|\+\+\s*\w|\+=\s*1\b", source);
        Assert.Matches(@"\w\s*--|--\s*\w|-=\s*1\b", source);
    }

    [Fact]
    public void AC7_interaction_point_declares_find_all_over_several_ids()
    {
        var source = ReadRepoFile("game", "scripts", "World", "InteractionPoint.cs");

        Assert.Matches(
            @"public\s+static\s+[\w<>\[\]?, ]+\s+FindAll\s*\(\s*Node\s+node\s*,\s*params\s+string\[\]\s+pointIds\s*\)",
            source);
    }

    // ---- AC8: the pond is unchanged ----

    [Fact]
    public void AC8_farm_pond_point_moves_with_the_pond_and_refills_from_the_pond()
    {
        var points = WaterPoints("Farm", "pond");

        var pond = Assert.Single(points);
        Assert.Equal("InteractionPoints/PondPoint", pond.Path);
        Assert.Equal(3.8, pond.Range, 3);
        Assert.Equal((10.0, 0.0, -9.0), (pond.X, pond.Y, pond.Z));

        var refills = RefillChildren(ReadRepoFile("game", "scenes", "Farm.tscn"), pond.Path);
        var refill = Assert.Single(refills);
        var source = Property(refill, "Source");
        Assert.True(source is null or "pond", $"Farm.tscn's pond WaterRefill has Source = \"{source}\", not pond");
    }

    [Fact]
    public void AC8_pond_refill_is_renamed_to_water_refill()
    {
        var root = RepoRoot();

        Assert.False(
            File.Exists(Path.Combine(root, "game", "scripts", "World", "PondRefill.cs")),
            "game/scripts/World/PondRefill.cs still exists; it is renamed to WaterRefill.cs");
        Assert.False(
            File.Exists(Path.Combine(root, "game", "scripts", "World", "PondRefill.cs.uid")),
            "game/scripts/World/PondRefill.cs.uid still exists");
        Assert.True(
            File.Exists(Path.Combine(root, "game", "scripts", "World", "WaterRefill.cs")),
            "game/scripts/World/WaterRefill.cs not found");
    }

    [Fact]
    public void AC8_no_scene_loads_pond_refill()
    {
        foreach (var scene in new[] { "Farm", "Town", "Lakeshore" })
        {
            Assert.DoesNotContain("PondRefill.cs", ReadRepoFile("game", "scenes", $"{scene}.tscn"), StringComparison.Ordinal);
        }
    }

    // ---- Edge: WaterRefill behaves as PondRefill did, naming its source ----

    [Fact]
    public void Water_refill_exports_its_source_defaulting_to_pond_and_passes_it_to_refill_can()
    {
        var source = ReadRepoFile("game", "scripts", "World", "WaterRefill.cs");

        Assert.Matches(@"partial\s+class\s+WaterRefill\b", source);
        Assert.Matches(@"\[Export\]\s*public\s+string\s+Source\s*\{\s*get;\s*set;\s*\}\s*=\s*""pond""", source);
        Assert.Contains("WaterSourceCodes.TryParse(", source, StringComparison.Ordinal);
        Assert.Matches(@"RefillCan\s*\(\s*\w+\s*\)", source);
    }

    [Fact]
    public void Water_refill_acts_on_an_interact_press_in_range_and_marks_it_handled()
    {
        var source = ReadRepoFile("game", "scripts", "World", "WaterRefill.cs");

        Assert.Matches(@"IsActionPressed\s*\(\s*""interact""\s*,\s*allowEcho:\s*false\s*\)", source);
        Assert.Contains("IsPlayerInRange", source, StringComparison.Ordinal);
        Assert.Contains("SetInputAsHandled()", source, StringComparison.Ordinal);
    }

    // ---- Helpers ----

    private static bool IsOnShore(WaterPoint point) => point.Z is >= 0.5 and <= 1.65;

    private static bool IsAtDockEnd(WaterPoint point) =>
        point.X is >= -6.6 and <= -5.4 && point.Z is >= 5.5 and <= 6.65;

    private static void AssertEachRefillsFrom(List<WaterPoint> points, string sceneName, string sourceCode)
    {
        var scene = ReadRepoFile("game", "scenes", $"{sceneName}.tscn");
        foreach (var point in points)
        {
            var refills = RefillChildren(scene, point.Path);
            Assert.True(refills.Count == 1, $"{sceneName}.tscn's {point.Path} has {refills.Count} WaterRefill children, not 1");
            Assert.Equal(sourceCode, Property(refills[0], "Source"));
        }
    }

    private static void AssertEachRange(List<WaterPoint> points, double range)
    {
        foreach (var point in points)
        {
            Assert.Equal(range, point.Range, 3);
        }
    }

    private sealed record Node(string Name, string? Type, string? Parent, string Body)
    {
        public string Path => Parent is null ? "." : Parent == "." ? Name : $"{Parent}/{Name}";
    }

    private sealed record WaterPoint(string Path, double X, double Y, double Z, double Range);

    /// <summary>The scene's InteractionPoints with the given id, at their positions in the scene (their parents'
    /// translations added).</summary>
    private static List<WaterPoint> WaterPoints(string sceneName, string pointId)
    {
        var scene = ReadRepoFile("game", "scenes", $"{sceneName}.tscn");
        var byPath = Nodes(scene).ToDictionary(node => node.Path);
        return ScriptedNodes(scene, PointScript)
            .Where(node => Property(node, "PointId") == pointId)
            .Select(node =>
            {
                var (x, y, z) = (0.0, 0.0, 0.0);
                for (var current = node; current is not null;
                     current = current.Parent is { } parent && byPath.TryGetValue(parent, out var up) ? up : null)
                {
                    if (Origin(current) is { } origin)
                    {
                        (x, y, z) = (x + origin.X, y + origin.Y, z + origin.Z);
                    }
                }

                return new WaterPoint(node.Path, x, y, z, Number(node, "Range") ?? 1.0);
            })
            .ToList();
    }

    private static List<Node> RefillChildren(string scene, string pointPath) =>
        ScriptedNodes(scene, RefillScript).Where(node => node.Parent == pointPath).ToList();

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

    /// <summary>A string property set on the node, e.g. <c>Source = "river"</c>.</summary>
    private static string? Property(Node node, string key)
    {
        var match = Regex.Match(node.Body, @"^" + key + @"\s*=\s*""([^""]*)""\s*$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>A number property set on the node, e.g. <c>Range = 1.5</c>.</summary>
    private static double? Number(Node node, string key)
    {
        var match = Regex.Match(node.Body, @"^" + key + @"\s*=\s*([-0-9.eE]+)\s*$", RegexOptions.Multiline);
        return match.Success ? double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
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

    /// <summary>The source without its comments, so a doc comment's <c>--show-fps</c> isn't read as code.</summary>
    private static string Code(string source) =>
        Regex.Replace(source, @"//[^\n]*", "");

    private static string ReadRepoFile(params string[] parts)
    {
        var path = System.IO.Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        return dir!.FullName;
    }
}
