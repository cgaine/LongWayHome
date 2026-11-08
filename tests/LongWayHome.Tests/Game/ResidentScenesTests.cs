using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files and checks #138's residents in the game: <c>Resident.tscn</c> with its
/// <c>InteractionPoint</c> (range 1.5) and <c>ResidentTalk</c>; Maggie placed by the general store in
/// <c>Town.tscn</c>, Clyde at the end of the dock in <c>Lakeshore.tscn</c>; <c>ResidentView</c>, <c>ResidentTalk</c> and
/// <c>ResidentModels</c>; and the Hud's Talk hint (AC7, AC8).
/// </summary>
public class ResidentScenesTests
{
    private const string ResidentScene = "res://scenes/Resident.tscn";

    private const string ViewScript = "res://scripts/World/ResidentView.cs";

    private const string PointScript = "res://scripts/World/InteractionPoint.cs";

    private const string TalkScript = "res://scripts/World/ResidentTalk.cs";

    /// <summary>TownLayout.Buildings: the general store's drum centre in town, its door facing +Z (the square).</summary>
    private const double StoreX = -10.0;

    private const double StoreZ = -6.0;

    /// <summary>GeneralStoreArt.WallRadius.</summary>
    private const double StoreWallRadius = 3.2;

    /// <summary>LakeshoreLayout: the dock runs x -7 to -5 from the shore (z 1) out to z 7.</summary>
    private const double DockWestX = -7.0;

    private const double DockEastX = -5.0;

    private const double DockSouthZ = 7.0;

    private static SceneText.Node ResidentIn(string sceneName, string code)
    {
        var scene = ReadRepoFile("game", "scenes", $"{sceneName}.tscn");
        var residents = InstancesOf(scene, ResidentScene);
        Assert.True(residents.Count > 0, $"{sceneName}.tscn instances no {ResidentScene}");
        var matching = residents.Where(node => Property(node, "ResidentId") == code).ToList();
        Assert.True(matching.Count == 1, $"{sceneName}.tscn has {matching.Count} Resident instance(s) with ResidentId = \"{code}\", not 1");
        return matching[0];
    }

    // ---- AC7: Resident.tscn ----

    [Fact]
    public void AC7_Resident_scene_root_runs_ResidentView()
    {
        var scene = ReadRepoFile("game", "scenes", "Resident.tscn");

        var views = ScriptedNodes(scene, ViewScript);
        var view = Assert.Single(views);
        Assert.Null(view.Parent);
    }

    [Fact]
    public void AC7_Resident_scene_has_an_InteractionPoint_child_with_range_1_5()
    {
        var scene = ReadRepoFile("game", "scenes", "Resident.tscn");

        var point = Assert.Single(ScriptedNodes(scene, PointScript));
        Assert.Equal(".", point.Parent);
        Assert.Equal(1.5, Number(point, "Range") ?? 1.0, 3);
    }

    [Fact]
    public void AC8_Resident_scenes_point_has_a_ResidentTalk_child()
    {
        var scene = ReadRepoFile("game", "scenes", "Resident.tscn");
        var point = Assert.Single(ScriptedNodes(scene, PointScript));

        var talk = Assert.Single(ScriptedNodes(scene, TalkScript));
        Assert.Equal(point.Path, talk.Parent);
    }

    // ---- AC7: placement ----

    [Fact]
    public void AC7_Town_has_maggie_in_front_of_the_general_store()
    {
        var scene = ReadRepoFile("game", "scenes", "Town.tscn");
        var maggie = ResidentIn("Town", "maggie");

        var (x, _, z) = Position(scene, maggie);
        var distance = Math.Sqrt(Math.Pow(x - StoreX, 2) + Math.Pow(z - StoreZ, 2));
        Assert.True(
            distance > StoreWallRadius && distance <= StoreWallRadius + 3.0,
            $"Maggie stands {distance:0.##} m from the store's centre at ({x}, {z}); she belongs just outside its wall");
        Assert.True(z > StoreZ, $"Maggie at z = {z} is behind the store, not in front of it (its door faces +Z)");
        Assert.True(x <= StoreX + 0.5, $"Maggie at x = {x} is on the door's right, not by the shop window on its left (-X)");
    }

    [Fact]
    public void AC7_Lakeshore_has_clyde_at_the_end_of_the_dock()
    {
        var scene = ReadRepoFile("game", "scenes", "Lakeshore.tscn");
        var clyde = ResidentIn("Lakeshore", "clyde");

        var (x, _, z) = Position(scene, clyde);
        Assert.InRange(x, DockWestX, DockEastX);
        Assert.InRange(z, DockSouthZ - 2.0, DockSouthZ);
    }

    [Theory]
    [InlineData("Town", "clyde")]
    [InlineData("Lakeshore", "maggie")]
    [InlineData("Farm", "maggie")]
    [InlineData("Farm", "clyde")]
    public void AC7_Each_resident_stands_only_in_their_own_area(string sceneName, string code)
    {
        var scene = ReadRepoFile("game", "scenes", $"{sceneName}.tscn");

        Assert.DoesNotContain(InstancesOf(scene, ResidentScene), node => Property(node, "ResidentId") == code);
    }

    // ---- AC7: ResidentView and ResidentModels ----

    [Fact]
    public void AC7_ResidentView_exports_its_resident_code_and_names_its_point_after_it()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "ResidentView.cs"));

        Assert.Matches(@"partial\s+class\s+ResidentView\b", source);
        Assert.Matches(@"\[Export\]\s*public\s+string\s+ResidentId\s*\{\s*get;\s*set;\s*\}", source);
        Assert.Contains("ResidentCodes.TryParse(", source, StringComparison.Ordinal);
        Assert.Matches(@"\.PointId\s*=", source);
    }

    [Fact]
    public void AC7_ResidentView_shows_the_model_and_a_capsule_collider_only_when_present()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "ResidentView.cs"));

        Assert.Contains(".IsPresent(", source, StringComparison.Ordinal);
        Assert.Contains("ResidentModels.", source, StringComparison.Ordinal);
        Assert.Contains("StaticBody3D", source, StringComparison.Ordinal);
        Assert.Contains("CapsuleShape3D", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC7_ResidentView_turns_to_face_the_player_while_in_range()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "ResidentView.cs"));

        Assert.Matches(@"IsPlayerInRange|PlayerEntered", source);
        Assert.Matches(@"LookAt\s*\(|Rotation\s*=|Rotation\.Y\s*=|Atan2\s*\(", source);
    }

    [Fact]
    public void AC6_ResidentModels_builds_one_cached_mesh_per_resident_from_ResidentArt_with_the_outline()
    {
        var raw = ReadRepoFile("game", "art", "town", "ResidentModels.cs");
        var source = Code(raw);

        Assert.Matches(@"static\s+class\s+ResidentModels\b", source);
        Assert.Contains("ResidentArt.For(", source, StringComparison.Ordinal);
        Assert.Contains("Dictionary<", source, StringComparison.Ordinal);
        // Code() strips everything after "//", so the res:// path is looked for as a string literal in the raw source.
        Assert.Contains("\"res://art/shaders/outline.gdshader\"", raw, StringComparison.Ordinal);
        Assert.Contains("NextPass", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Skeleton3D", source, StringComparison.Ordinal);
    }

    // ---- AC8: ResidentTalk ----

    [Fact]
    public void AC8_ResidentTalk_opens_the_dialogue_box_with_the_display_name_and_greeting_on_interact_in_range()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "ResidentTalk.cs"));

        Assert.Matches(@"partial\s+class\s+ResidentTalk\b", source);
        Assert.Matches(@"IsActionPressed\s*\(\s*""interact""\s*,\s*allowEcho:\s*false\s*\)", source);
        Assert.Contains("IsPlayerInRange", source, StringComparison.Ordinal);
        Assert.Contains("ResidentLines.DisplayName(", source, StringComparison.Ordinal);
        Assert.Contains("ResidentLines.Greeting(", source, StringComparison.Ordinal);
        Assert.Contains("DialogueBox", source, StringComparison.Ordinal);
        Assert.Matches(@"\.Show\s*\(", source);
    }

    [Fact]
    public void AC11_ResidentTalk_does_nothing_while_the_tree_is_paused_and_marks_its_press_handled()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "ResidentTalk.cs"));

        Assert.Matches(@"GetTree\(\)\.Paused", source);
        Assert.DoesNotMatch(@"GetTree\(\)\.Paused\s*=", source);
        Assert.DoesNotContain("ProcessModeEnum.Always", source, StringComparison.Ordinal);
        Assert.Contains("SetInputAsHandled()", source, StringComparison.Ordinal);
    }

    // ---- AC7: the Hud's Talk hint ----

    [Fact]
    public void AC7_Hud_scene_has_a_TalkHint_label()
    {
        var scene = ReadRepoFile("game", "ui", "Hud.tscn");

        var hint = Nodes(scene).SingleOrDefault(node => node.Name == "TalkHint");
        Assert.True(hint is not null, "Hud.tscn has no TalkHint node");
        Assert.Equal("Label", hint!.Type);
    }

    [Fact]
    public void AC7_Hud_shows_the_talk_hint_over_the_residents_points()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Ui", "Hud.cs"));

        Assert.Contains("\"TalkHint\"", source, StringComparison.Ordinal);
        Assert.Contains("DialogueText.TalkHint(", source, StringComparison.Ordinal);
        Assert.Matches(@"""maggie""|""clyde""|ResidentCodes\.", source);
    }
}
