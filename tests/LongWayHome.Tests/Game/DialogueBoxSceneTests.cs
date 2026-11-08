using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files and checks #138's dialogue box: <c>game/ui/DialogueBox.tscn</c> in every area through
/// <c>AreaCommon.tscn</c>, and <c>DialogueBox.cs</c>, glue over <c>DialogueFlow</c> that pauses the tree while shown
/// (BR-G2, AC9), processes <c>Always</c>, and keeps its presses from reaching gameplay (AC11).
/// </summary>
public class DialogueBoxSceneTests
{
    private const string BoxScene = "res://ui/DialogueBox.tscn";

    private const string BoxScript = "res://scripts/Ui/DialogueBox.cs";

    private static string Source() => Code(ReadRepoFile("game", "scripts", "Ui", "DialogueBox.cs"));

    /// <summary>The body of the method declared with <paramref name="signature"/>, up to the next member at the same
    /// indentation; empty when there is none.</summary>
    private static string MethodBody(string source, string signature)
    {
        var match = Regex.Match(
            source,
            signature + @"[^{]*\{(?<body>.*?)^    \}",
            RegexOptions.Multiline | RegexOptions.Singleline);
        return match.Success ? match.Groups["body"].Value : "";
    }

    // ---- the scene ----

    [Fact]
    public void AC8_AreaCommon_instances_the_dialogue_box_so_every_area_has_one()
    {
        var scene = ReadRepoFile("game", "scenes", "AreaCommon.tscn");

        Assert.Single(InstancesOf(scene, BoxScene));
    }

    [Fact]
    public void AC8_DialogueBox_scene_root_runs_DialogueBox_cs()
    {
        var scene = ReadRepoFile("game", "ui", "DialogueBox.tscn");

        var box = Assert.Single(ScriptedNodes(scene, BoxScript));
        Assert.Null(box.Parent);
    }

    [Fact]
    public void AC8_DialogueBox_scene_has_labels_for_the_speaker_the_line_and_the_hints()
    {
        var scene = ReadRepoFile("game", "ui", "DialogueBox.tscn");

        var labels = Nodes(scene).Where(node => node.Type is "Label" or "RichTextLabel").ToList();
        Assert.True(labels.Count >= 3, $"DialogueBox.tscn has {labels.Count} labels; it needs the speaker, the line and the hints");
    }

    [Fact]
    public void AC8_DialogueBox_panel_sits_at_the_bottom_of_the_screen()
    {
        var scene = ReadRepoFile("game", "ui", "DialogueBox.tscn");

        Assert.Contains(
            Nodes(scene),
            node => node.Type is "PanelContainer" or "Panel"
                && Regex.IsMatch(node.Body, @"^anchor_top\s*=\s*1(\.0)?\s*$", RegexOptions.Multiline));
    }

    // ---- the script's API ----

    [Fact]
    public void AC8_DialogueBox_has_Show_with_a_speaker_and_lines_and_a_Finished_signal()
    {
        var source = Source();

        Assert.Matches(@"partial\s+class\s+DialogueBox\b", source);
        Assert.Matches(@"public\s+void\s+Show\s*\(\s*string\s+\w+\s*,\s*IReadOnlyList<string>\s+\w+\s*\)", source);
        Assert.Matches(@"\[Signal\]\s*public\s+delegate\s+void\s+FinishedEventHandler\s*\(\s*\)", source);
        Assert.Contains("DialogueFlow", source, StringComparison.Ordinal);
        Assert.Contains("SignalName.Finished", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC8_DialogueBox_hints_come_from_DialogueText_and_follow_the_last_input_device()
    {
        var source = Source();

        Assert.Contains("DialogueText.AdvanceHint(", source, StringComparison.Ordinal);
        Assert.Contains("DialogueText.SkipHint(", source, StringComparison.Ordinal);
        Assert.Contains("LastInputDevice", source, StringComparison.Ordinal);
        Assert.Contains("DeviceChanged", source, StringComparison.Ordinal);
    }

    // ---- AC9: the clock and the Player stop while it is open ----

    [Fact]
    public void AC9_Show_pauses_the_scene_tree()
    {
        var show = MethodBody(Source(), @"public\s+void\s+Show\s*\(\s*string");

        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*true", show);
    }

    [Fact]
    public void AC9_Closing_resumes_the_tree_and_emits_finished()
    {
        var source = Source();

        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*false", source);
        Assert.Matches(@"EmitSignal\s*\(\s*SignalName\.Finished\s*\)", source);
    }

    [Fact]
    public void AC9_DialogueBox_processes_always_so_it_runs_while_the_tree_is_paused()
    {
        Assert.Contains("ProcessMode = ProcessModeEnum.Always", Source(), StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_Leaving_the_scene_while_shown_does_not_leave_the_tree_paused()
    {
        var exit = MethodBody(Source(), @"public\s+override\s+void\s+_ExitTree\s*\(\s*\)");

        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*false", exit);
    }

    // ---- AC10, AC11: the buttons, and keeping them from gameplay ----

    [Fact]
    public void AC10_Interact_and_use_tool_advance_and_open_menu_skips()
    {
        var source = Source();

        Assert.Matches(@"IsActionPressed\s*\(\s*""interact""", source);
        Assert.Matches(@"IsActionPressed\s*\(\s*""use_tool""", source);
        Assert.Matches(@"IsActionPressed\s*\(\s*""open_menu""", source);
        Assert.Matches(@"\.Advance\s*\(\s*\)", source);
        Assert.Matches(@"\.Skip\s*\(\s*\)", source);
    }

    [Fact]
    public void AC11_The_press_that_closes_the_box_is_marked_handled_in_Input()
    {
        var input = MethodBody(Source(), @"public\s+override\s+void\s+_Input\s*\(\s*InputEvent");

        Assert.False(input.Length == 0, "DialogueBox.cs has no _Input override, so its presses could reach gameplay first");
        Assert.Contains("SetInputAsHandled()", input, StringComparison.Ordinal);
    }

    [Fact]
    public void AC11_Presses_while_the_box_is_not_shown_are_ignored()
    {
        var input = MethodBody(Source(), @"public\s+override\s+void\s+_Input\s*\(\s*InputEvent");

        Assert.Matches(@"if\s*\(\s*!\s*[\w.]*(IsOpen|Visible)\b[^)]*\)\s*\{?\s*return", input);
    }
}
