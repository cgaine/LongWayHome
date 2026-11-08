using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files from the repository and checks the Godot glue of #131's Backpack screen:
/// <c>game/ui/BackpackMenu.tscn</c> with its <c>BackpackMenu.cs</c> script, instanced in <c>Farm.tscn</c>, following
/// <c>ShopMenu</c>'s pattern. Each check looks for the essential tokens, not exact formatting.
/// </summary>
public class BackpackMenuSceneTests
{
    private static readonly string[] ScenePath = ["game", "ui", "BackpackMenu.tscn"];

    private static readonly string[] ScriptPath = ["game", "scripts", "Ui", "BackpackMenu.cs"];

    // ---- AC1: the scene, its grid, and the farm instance ----

    [Fact]
    public void AC1_backpack_menu_scene_has_a_CanvasLayer_root_with_the_BackpackMenu_script()
    {
        var scene = ReadRepoFile(ScenePath);

        var resource = Regex.Match(
            scene, @"\[ext_resource\s+type=""Script""[^\]]*path=""res://scripts/Ui/BackpackMenu\.cs""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, "BackpackMenu.tscn has no ext_resource for res://scripts/Ui/BackpackMenu.cs");
        var id = Regex.Escape(resource.Groups[1].Value);

        // The root is the first node and has no parent.
        var root = Regex.Match(scene, @"^\[node\s+(?<attrs>[^\]]*)\]\s*$(?<body>.*?)(?=^\[|\z)", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(root.Success, "BackpackMenu.tscn has no nodes");
        Assert.Contains(@"type=""CanvasLayer""", root.Groups["attrs"].Value, StringComparison.Ordinal);
        Assert.DoesNotContain("parent=", root.Groups["attrs"].Value, StringComparison.Ordinal);
        Assert.Matches(@"script\s*=\s*ExtResource\(""" + id + @"""\)", root.Groups["body"].Value);
    }

    [Fact]
    public void AC1_backpack_menu_scene_has_a_GridContainer_with_6_columns()
    {
        var scene = ReadRepoFile(ScenePath);

        Assert.Matches(
            new Regex(@"\[node\s+[^\]]*type=""GridContainer""[^\]]*\]\s*$(?:(?!^\[).)*?^columns\s*=\s*6\s*$",
                RegexOptions.Multiline | RegexOptions.Singleline),
            scene);
    }

    [Fact]
    public void AC1_farm_scene_instances_the_backpack_menu()
    {
        // #136 moved the shared gameplay nodes into AreaCommon.tscn, which Farm.tscn (and every area scene) instances.
        var common = ReadRepoFile("game", "scenes", "AreaCommon.tscn");

        var resource = Regex.Match(
            common, @"\[ext_resource\s+type=""PackedScene""[^\]]*path=""res://ui/BackpackMenu\.tscn""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, "AreaCommon.tscn has no PackedScene ext_resource for res://ui/BackpackMenu.tscn");
        var id = Regex.Escape(resource.Groups[1].Value);
        Assert.Matches(@"\[node\s+name=""[^""]+""\s+parent=""\.""\s+instance=ExtResource\(""" + id + @"""\)\]", common);

        AssertFarmInstancesAreaCommon();
    }

    private static void AssertFarmInstancesAreaCommon()
    {
        var farm = ReadRepoFile("game", "scenes", "Farm.tscn");
        var resource = Regex.Match(
            farm, @"\[ext_resource\s+type=""PackedScene""[^\]]*path=""res://scenes/AreaCommon\.tscn""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, "Farm.tscn has no PackedScene ext_resource for res://scenes/AreaCommon.tscn");
        var id = Regex.Escape(resource.Groups[1].Value);
        Assert.Matches(@"\[node\s+name=""[^""]+""\s+parent=""\.""\s+instance=ExtResource\(""" + id + @"""\)\]", farm);
    }

    [Fact]
    public void AC1_script_builds_one_button_per_slot_from_the_backpack_view()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Contains("BackpackView()", source, StringComparison.Ordinal);
        Assert.Contains("Button", source, StringComparison.Ordinal);
        Assert.Contains(".Slots", source, StringComparison.Ordinal);
    }

    // ---- BR-G2: the menu pauses the tree ----

    [Fact]
    public void BR_G2_script_pauses_the_tree_on_open_and_resumes_on_close()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*true", source);
        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*false", source);
    }

    [Fact]
    public void BR_G2_script_opens_on_open_menu_in_UnhandledInput_only_when_not_paused()
    {
        var source = ReadRepoFile(ScriptPath);

        var body = MethodBody(source, "_UnhandledInput");
        Assert.True(body is not null, "BackpackMenu.cs does not override _UnhandledInput");
        Assert.Contains(@"""open_menu""", body, StringComparison.Ordinal);
        Assert.Matches(@"!\s*GetTree\(\)\.Paused", body);
    }

    // ---- AC9: controller and keyboard-and-mouse ----

    [Fact]
    public void AC9_script_picks_up_and_puts_down_on_a_slot_button_press()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Matches(@"\.Pressed\s*\+=", source);
    }

    [Fact]
    public void AC9_script_cancels_a_hold_or_closes_on_ui_cancel()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Contains(@"""ui_cancel""", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_script_focuses_a_slot_and_recovers_focus_after_a_click()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Contains("GrabFocus()", source, StringComparison.Ordinal);
        Assert.Contains("MenuFocusRecovery.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_script_shows_the_hint_for_the_last_input_device()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Contains("LastInputDevice", source, StringComparison.Ordinal);
        Assert.Contains("BackpackText.Hint(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_close_button_is_labelled_with_the_close_glyph()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Contains("InputGlyphs.CloseLabel(", source, StringComparison.Ordinal);
    }

    // ---- AC10: closing, without the pause menu also opening ----

    [Fact]
    public void AC10_script_handles_open_menu_ui_cancel_and_pause_in_Input_and_consumes_them()
    {
        var source = ReadRepoFile(ScriptPath);

        var body = MethodBody(source, "_Input");
        Assert.True(body is not null, "BackpackMenu.cs does not override _Input");
        foreach (var action in new[] { "open_menu", "ui_cancel", "pause" })
        {
            Assert.True(body.Contains($@"""{action}""", StringComparison.Ordinal), $"BackpackMenu._Input never handles \"{action}\"");
        }

        Assert.Contains("SetInputAsHandled()", body, StringComparison.Ordinal);
    }

    // ---- AC2: the hotbar row mark ----

    [Fact]
    public void AC2_script_marks_the_hotbar_row_with_the_Hotbar_label()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Contains("BackpackText.HotbarLabel", source, StringComparison.Ordinal);
        Assert.Contains("HotbarRow", source, StringComparison.Ordinal);
    }

    // ---- AC6: no rules in the node script ----

    [Fact]
    public void AC6_script_leaves_move_merge_and_swap_to_the_session()
    {
        var source = ReadRepoFile(ScriptPath);

        Assert.Contains("MoveSlot(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxStack", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Move(", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// The body of the method <paramref name="name"/> (its first override), from its opening brace to the matching
    /// closing brace, or null when the script has no such override.
    /// </summary>
    private static string? MethodBody(string source, string name)
    {
        var signature = Regex.Match(source, @"override\s+void\s+" + Regex.Escape(name) + @"\s*\(");
        if (!signature.Success)
        {
            return null;
        }

        var open = source.IndexOf('{', signature.Index);
        if (open < 0)
        {
            return null;
        }

        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            depth += source[index] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
            {
                return source[open..(index + 1)];
            }
        }

        return source[open..];
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var path = RepoPath(parts);
        Assert.True(File.Exists(path), $"{string.Join('/', parts)} not found");
        return File.ReadAllText(path);
    }

    private static string RepoPath(params string[] parts) =>
        Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());

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
