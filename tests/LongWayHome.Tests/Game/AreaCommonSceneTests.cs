using System.Globalization;
using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files to check how #136's areas are put together beyond the exit pairs: NightTransition is
/// still the last input node in every area (it is the last child of <c>AreaCommon.tscn</c>, which is the last child of
/// each area scene), every arrival sits outside its exit's trigger and faces into the area, the night's return to the
/// farm is in the shared scene, and Town uses <c>TownModels</c> for its buildings with collision around each.
/// </summary>
public class AreaCommonSceneTests
{
    private static readonly Regex NodeHeader = new(@"^\[node\s+(?<attrs>[^\]]*)\]", RegexOptions.Multiline);

    [Fact]
    public void Night_transition_is_the_last_child_of_area_common()
    {
        var top = TopLevelNodes(ReadScene("AreaCommon"));

        Assert.Equal("NightTransition", top[^1]);
        Assert.Contains("NightReturn", top);
    }

    [Theory]
    [InlineData("Farm")]
    [InlineData("ForestPath")]
    [InlineData("Town")]
    [InlineData("Lakeshore")]
    public void Area_common_is_the_last_child_of_each_area_scene(string scene)
    {
        var top = TopLevelNodes(ReadScene(scene));

        Assert.Equal("Common", top[^1]);
    }

    [Theory]
    [InlineData("Farm")]
    [InlineData("ForestPath")]
    [InlineData("Town")]
    [InlineData("Lakeshore")]
    public void Each_arrival_is_outside_every_exit_trigger_and_faces_away_from_the_nearest_exit(string scene)
    {
        var text = ReadScene(scene);
        var exits = Blocks(text).Where(b => b.Type == "Area3D").ToList();
        // The arrivals are the Marker3Ds naming an ArrivalId; the farm's Opening waypoints (#139) are Marker3Ds too.
        var arrivals = Blocks(text).Where(b => b.Type == "Marker3D" && b.Body.Contains("ArrivalId")).ToList();
        Assert.NotEmpty(exits);
        Assert.Equal(exits.Count, arrivals.Count);

        foreach (var arrival in arrivals)
        {
            var (basis, at) = Transform(arrival.Body);
            foreach (var exit in exits)
            {
                var (_, trigger) = Transform(exit.Body);

                // The trigger is 3 x 1 tiles; the Player's capsule (radius 0.35) at the arrival must not touch it.
                var clear = Math.Abs(at.X - trigger.X) >= 1.5 + 0.35 || Math.Abs(at.Z - trigger.Z) >= 0.5 + 0.35;
                Assert.True(clear, $"{scene}.tscn's {arrival.Name} is inside {exit.Name}'s trigger");
            }

            var nearest = exits.Select(e => Transform(e.Body).Origin).MinBy(o => Math.Abs(o.Z - at.Z) + Math.Abs(o.X - at.X));
            Assert.Equal(1.5, Math.Abs(nearest.Z - at.Z) - 0.5, 3);

            // Forward is the marker's −Z: it must point away from the exit, into the area.
            var forwardZ = -basis[8];
            Assert.True(Math.Sign(forwardZ) == Math.Sign(at.Z - nearest.Z), $"{scene}.tscn's {arrival.Name} faces out of the area");
        }
    }

    [Fact]
    public void Town_builds_its_buildings_from_town_models_with_a_body_round_each()
    {
        var view = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "game", "art", "areas", "TownView.cs"));
        Assert.Contains("TownModels.Store(StoreStage.MaggiesCorner)", view);
        Assert.Contains("TownModels.Carpenter(CarpenterStage.Empty)", view);
        Assert.Contains("TownModels.Blacksmith(BlacksmithStage.Empty)", view);

        var bodies = Blocks(ReadScene("Town")).Where(b => b.Type == "StaticBody3D").Select(b => b.Name).ToList();
        Assert.Contains("Store", bodies);
        Assert.Contains("Carpenter", bodies);
        Assert.Contains("Blacksmith", bodies);
    }

    private sealed record Block(string Name, string? Type, string? Parent, string Body);

    private static List<Block> Blocks(string scene)
    {
        var headers = NodeHeader.Matches(scene).ToList();
        var blocks = new List<Block>();
        for (var i = 0; i < headers.Count; i++)
        {
            var start = headers[i].Index + headers[i].Length;
            var next = scene.IndexOf("\n[", start, StringComparison.Ordinal);
            var body = next < 0 ? scene[start..] : scene[start..next];
            var attrs = headers[i].Groups["attrs"].Value;
            blocks.Add(new Block(Attr(attrs, "name")!, Attr(attrs, "type"), Attr(attrs, "parent"), body));
        }

        return blocks;
    }

    private static List<string> TopLevelNodes(string scene) =>
        Blocks(scene).Where(b => b.Parent == ".").Select(b => b.Name).ToList();

    private static string? Attr(string attrs, string key)
    {
        var match = Regex.Match(attrs, @"(?:^|\s)" + key + @"=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static (double[] Basis, (double X, double Z) Origin) Transform(string body)
    {
        var match = Regex.Match(body, @"^transform\s*=\s*Transform3D\(([^)]*)\)", RegexOptions.Multiline);
        Assert.True(match.Success, "a node has no transform");
        var n = match.Groups[1].Value.Split(',').Select(p => double.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();
        return (n[..9], (n[9], n[11]));
    }

    private static string ReadScene(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "game", "scenes", $"{name}.tscn"));
}
