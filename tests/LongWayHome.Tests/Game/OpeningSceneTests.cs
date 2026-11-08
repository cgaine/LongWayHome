using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files and checks #139's Opening glue: <c>Farm.tscn</c> runs <c>OpeningSequence.cs</c>, holds
/// Maggie and the walk's waypoints; the sequence drives <c>OpeningFlow</c> through the <c>DialogueBox</c> and lets
/// <c>open_menu</c> skip the crash (AC7, AC8); the south exit and the door's Sleep are gated on it (AC10).
/// </summary>
public class OpeningSceneTests
{
    private const string SequenceScript = "res://scripts/Opening/OpeningSequence.cs";

    private const string ResidentScene = "res://scenes/Resident.tscn";

    private const string ArrivalScript = "res://scripts/World/AreaArrival.cs";

    private static string Farm() => ReadRepoFile("game", "scenes", "Farm.tscn");

    private static string SequenceSource() => Code(ReadRepoFile("game", "scripts", "Opening", "OpeningSequence.cs"));

    // ---- AC7: the farm scene ----

    [Fact]
    public void AC7_Farm_has_one_node_running_the_opening_sequence()
    {
        Assert.Single(ScriptedNodes(Farm(), SequenceScript));
    }

    [Fact]
    public void AC7_Farm_has_maggie_for_the_opening()
    {
        var maggies = InstancesOf(Farm(), ResidentScene)
            .Where(node => Property(node, "ResidentId") == "maggie")
            .ToList();

        Assert.Single(maggies);
    }

    [Fact]
    public void AC7_Farm_has_waypoints_for_maggies_walk_to_the_farmhouse()
    {
        var scene = Farm();
        var arrivals = ScriptedNodes(scene, ArrivalScript).Select(node => node.Path).ToHashSet();

        var waypoints = Nodes(scene)
            .Where(node => node.Type == "Marker3D" && !arrivals.Contains(node.Path))
            .ToList();

        Assert.True(waypoints.Count >= 2, $"Farm.tscn has {waypoints.Count} Marker3D waypoints besides the arrivals; Maggie's walk needs at least 2");
    }

    // ---- AC7, AC8: the sequence script ----

    [Fact]
    public void AC7_The_sequence_follows_the_application_opening_flow()
    {
        Assert.Contains("OpeningFlow", SequenceSource());
    }

    [Fact]
    public void AC7_The_sequence_shows_maggies_lines_through_the_dialogue_box()
    {
        var source = SequenceSource();

        Assert.Contains("DialogueBox", source);
        Assert.Matches(new Regex(@"\.Show\s*\("), source);
    }

    [Fact]
    public void AC7_open_menu_skips_the_crash_scene()
    {
        Assert.Contains("\"open_menu\"", SequenceSource());
    }

    [Fact]
    public void AC8_The_sequence_stops_the_clock_by_the_flows_ClockRuns()
    {
        Assert.Contains("ClockRuns", SequenceSource());
    }

    // ---- AC10: the south exit and the door wait for the Opening ----

    [Fact]
    public void AC10_The_area_exit_is_gated_on_the_opening()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "AreaExit.cs"));

        Assert.Contains("OpeningSequence", source);
    }

    [Fact]
    public void AC10_The_doors_sleep_is_gated_on_the_opening()
    {
        var source = Code(ReadRepoFile("game", "scripts", "World", "DoorSleep.cs"));

        Assert.Contains("OpeningSequence", source);
    }
}
