using System.Globalization;
using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// #138 wiring the red tests didn't drive: which way Maggie and Clyde face, the Resident instances' place in their
/// area's tree (it decides who handles an <c>interact</c> press when a water point is also in range, and lets the Hud
/// find the named points), the Hud's Talk hint standing in for the Refill hint, and the dialogue box keeping the pause
/// menu from resuming the tree under it.
/// </summary>
public class ResidentWiringTests
{
    private const string ResidentScene = "res://scenes/Resident.tscn";

    /// <summary>The node's facing (its basis's +Z column) on the ground, from the tscn's row-major Transform3D.</summary>
    private static (double X, double Z) Forward(SceneText.Node node)
    {
        var match = Regex.Match(node.Body, @"^transform\s*=\s*Transform3D\(([^)]*)\)", RegexOptions.Multiline);
        Assert.True(match.Success, $"{node.Name} has no transform");
        var m = match.Groups[1].Value.Split(',').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();

        // Rows (xx, xy, xz), (yx, yy, yz), (zx, zy, zz): the +Z column is (xz, yz, zz).
        return (m[2], m[8]);
    }

    private static SceneText.Node Resident(string scene, string code) =>
        Assert.Single(InstancesOf(scene, ResidentScene), node => Property(node, "ResidentId") == code);

    [Fact]
    public void Maggie_faces_the_square()
    {
        var scene = ReadRepoFile("game", "scenes", "Town.tscn");
        var maggie = Resident(scene, "maggie");
        var (x, _, z) = Position(scene, maggie);
        var (fx, fz) = Forward(maggie);

        // The closest her line of sight comes to the square's centre (0, 0) is inside the square (radius 5).
        var along = -(x * fx + z * fz);
        Assert.True(along > 0, $"Maggie at ({x}, {z}) faces ({fx}, {fz}), away from the square");
        var closest = Math.Sqrt(Math.Pow(x + fx * along, 2) + Math.Pow(z + fz * along, 2));
        Assert.True(closest < 5.0, $"Maggie's line of sight passes {closest:0.##} m from the square's centre");
    }

    [Fact]
    public void Clyde_faces_the_lake_south_of_the_dock()
    {
        var scene = ReadRepoFile("game", "scenes", "Lakeshore.tscn");
        var (fx, fz) = Forward(Resident(scene, "clyde"));

        Assert.True(fz > 0.9 && Math.Abs(fx) < 0.45, $"Clyde faces ({fx}, {fz}), not out over the lake (+Z)");
    }

    [Theory]
    [InlineData("Town", "maggie")]
    [InlineData("Lakeshore", "clyde")]
    public void The_resident_comes_after_the_water_points_and_before_Common(string sceneName, string code)
    {
        // Unhandled input reaches later nodes first, so the Resident's ResidentTalk takes an interact press before a
        // water point's WaterRefill in range of the same spot; and before Common, its ResidentView names its point
        // before the Hud looks for it.
        var top = Nodes(ReadRepoFile("game", "scenes", $"{sceneName}.tscn")).Where(node => node.Parent == ".").ToList();
        var resident = top.FindIndex(node => Property(node, "ResidentId") == code);

        Assert.True(resident > top.FindIndex(node => node.Name == "InteractionPoints"), $"{code} is before InteractionPoints");
        Assert.True(resident < top.FindIndex(node => node.Name == "Common"), $"{code} is after Common");
    }

    [Fact]
    public void Hud_hides_the_refill_hint_while_a_resident_is_in_range()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Ui", "Hud.cs"));

        Assert.Contains("_refillHint.Visible = _residentPointsInRange == 0;", source, StringComparison.Ordinal);
        var entered = Regex.Match(source, @"void\s+OnResidentPointEntered\s*\(\s*\)\s*\{.*?\n    }\n", RegexOptions.Singleline);
        Assert.True(entered.Success, "Hud has no OnResidentPointEntered");
        Assert.Contains("_talkHint.Visible = true;", entered.Value, StringComparison.Ordinal);
        Assert.Contains("_refillHint.Visible = false;", entered.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Hud_finds_only_present_residents_points()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Ui", "Hud.cs"));

        Assert.Contains(".IsPresent", source, StringComparison.Ordinal);
        Assert.Matches(@"InteractionPoint\.FindAll\s*\(\s*GetTree\(\)\.Root\s*,\s*residentCodes\s*\)", source);
    }

    [Fact]
    public void Dialogue_box_keeps_pause_and_cancel_from_the_pause_menu_while_open()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Ui", "DialogueBox.cs"));

        Assert.Matches(@"IsActionPressed\s*\(\s*""pause""", source);
        Assert.Matches(@"IsActionPressed\s*\(\s*""ui_cancel""", source);
    }

    [Fact]
    public void ResidentTalk_only_talks_to_a_point_that_names_a_resident()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "ResidentTalk.cs"));

        Assert.Contains("ResidentCodes.TryParse(_point.PointId", source, StringComparison.Ordinal);
    }
}
