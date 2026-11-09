using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// #164 AC10, the Godot glue: reads the real <c>game/ui/ChestMenu.tscn</c> and <c>game/scripts/Ui/ChestMenu.cs</c>
/// (instanced in <c>AreaCommon.tscn</c>, built like <c>BackpackMenu</c>), <c>game/art/farm/PlacedObjectsView.cs</c>
/// (instanced in <c>Farm.tscn</c>, drawn from session state with collision), the two placeable icons, and the HUD's
/// "Open chest" hint. Each check looks for the essential tokens, not exact formatting.
/// </summary>
public class ChestMenuSceneTests
{
    private static readonly string[] ScenePath = ["game", "ui", "ChestMenu.tscn"];

    private static readonly string[] ScriptPath = ["game", "scripts", "Ui", "ChestMenu.cs"];

    private const string ViewScript = "res://art/farm/PlacedObjectsView.cs";

    private static string Menu() => Code(ReadRepoFile(ScriptPath));

    private static string View() => Code(ReadRepoFile("game", "art", "farm", "PlacedObjectsView.cs"));

    // ---- the Chest screen ----

    [Fact]
    public void AC10_chest_menu_scene_has_a_CanvasLayer_root_with_the_ChestMenu_script()
    {
        var scene = ReadRepoFile(ScenePath);

        var id = ExtResourceId(scene, "Script", "res://scripts/Ui/ChestMenu.cs");
        Assert.True(id is not null, "ChestMenu.tscn has no ext_resource for res://scripts/Ui/ChestMenu.cs");
        var root = Nodes(scene).First();
        Assert.Equal("CanvasLayer", root.Type);
        Assert.Null(root.Parent);
        Assert.Matches(@"script\s*=\s*ExtResource\(""" + Regex.Escape(id!) + @"""\)", root.Body);
    }

    [Fact]
    public void AC10_chest_menu_scene_has_a_chest_grid_and_a_backpack_grid_of_6_columns()
    {
        var scene = ReadRepoFile(ScenePath);

        var grids = Nodes(scene).Where(node => node.Type == "GridContainer").ToList();
        Assert.Equal(2, grids.Count);
        Assert.All(grids, grid => Assert.Matches(new Regex(@"^columns\s*=\s*6\s*$", RegexOptions.Multiline), grid.Body));
        Assert.Contains("\"Chest\"", scene, StringComparison.Ordinal);
        Assert.Contains("\"Backpack\"", scene, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_area_common_instances_the_chest_menu()
    {
        var common = ReadRepoFile("game", "scenes", "AreaCommon.tscn");

        var instances = InstancesOf(common, "res://ui/ChestMenu.tscn");

        var menu = Assert.Single(instances);
        Assert.Equal(".", menu.Parent);
    }

    [Fact]
    public void AC10_the_script_reads_the_chest_and_backpack_and_moves_stacks_through_the_session()
    {
        var source = Menu();

        Assert.Contains("ChestAt(", source, StringComparison.Ordinal);
        Assert.Contains("BackpackView()", source, StringComparison.Ordinal);
        Assert.Contains("StoreInChest(", source, StringComparison.Ordinal);
        Assert.Contains("TakeFromChest(", source, StringComparison.Ordinal);
        Assert.Contains("Button", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_script_pauses_the_tree_and_closes_on_cancel_pause_and_open_menu()
    {
        var source = Menu();

        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*true", source);
        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*false", source);
        Assert.Contains("\"ui_cancel\"", source, StringComparison.Ordinal);
        Assert.Contains("\"pause\"", source, StringComparison.Ordinal);
        Assert.Contains("\"open_menu\"", source, StringComparison.Ordinal);
        Assert.Contains("InputGlyphs.CloseLabel", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_script_holds_no_rules()
    {
        var source = Menu();

        foreach (var forbidden in new[] { @"\bBackpack\.Add\s*\(", @"\.RemoveFrom\s*\(", @"\bMaxStack\b", @"\bnew\s+PlacedObject\b", @"\bRejection\.NotOffered\b" })
        {
            Assert.False(Regex.IsMatch(source, forbidden), $"ChestMenu.cs matches '{forbidden}': the screen moves stacks through GameSession only");
        }
    }

    [Fact]
    public void AC10_an_interact_press_facing_a_chest_opens_the_chest_screen()
    {
        var scripts = Path.Combine(RepoRoot(), "game", "scripts");
        var opener = Directory.EnumerateFiles(scripts, "*.cs", SearchOption.AllDirectories)
            .Select(path => (Path: path, Source: Code(File.ReadAllText(path))))
            .Where(file => file.Source.Contains("\"interact\"", StringComparison.Ordinal)
                           && file.Source.Contains("ChestAt(", StringComparison.Ordinal))
            .ToList();

        Assert.True(opener.Count > 0, "no script under game/scripts handles an interact press with GameSession.ChestAt");
    }

    [Fact]
    public void AC10_the_hud_shows_an_open_chest_hint_while_facing_a_chest()
    {
        var hud = Code(ReadRepoFile("game", "scripts", "Ui", "Hud.cs"));

        Assert.Contains("Open chest", hud, StringComparison.Ordinal);
        Assert.Contains("ChestAt(", hud, StringComparison.Ordinal);
        Assert.Contains("\"interact\"", hud, StringComparison.Ordinal);
    }

    // ---- the farm's placed objects ----

    [Fact]
    public void AC10_farm_scene_has_one_placed_objects_view()
    {
        var views = ScriptedNodes(ReadRepoFile("game", "scenes", "Farm.tscn"), ViewScript);

        Assert.Single(views);
    }

    [Fact]
    public void AC10_the_view_draws_the_sessions_placed_objects_from_placeable_art()
    {
        var source = View();

        Assert.Contains("GameRoot", source, StringComparison.Ordinal);
        Assert.Contains(".PlacedObjects", source, StringComparison.Ordinal);
        Assert.Contains("PlaceableArt.For(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_view_rebuilds_on_PlacedObjectsChanged()
    {
        var source = View();

        Assert.Matches(@"PlacedObjectsChanged\s*\+=", source);
        Assert.Matches(@"PlacedObjectsChanged\s*-=", source);
    }

    [Fact]
    public void AC10_each_placed_object_gets_a_box_collider_the_farmer_cant_walk_through()
    {
        var source = View();

        Assert.Contains("StaticBody3D", source, StringComparison.Ordinal);
        Assert.Contains("BoxShape3D", source, StringComparison.Ordinal);
        Assert.Contains("CollisionShape3D", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_view_holds_no_rules_and_changes_nothing()
    {
        var source = View();

        foreach (var forbidden in new[] { @"\.Place\s*\(", @"\.PickUpPlaced\s*\(", @"\.Use\s*\(", @"\bnew\s+PlacedObject\b", @"\bRejection\." })
        {
            Assert.False(Regex.IsMatch(source, forbidden), $"PlacedObjectsView.cs matches '{forbidden}': the view reads GameSession only");
        }
    }

    [Theory]
    [InlineData("placeable_fence.svg")]
    [InlineData("placeable_chest.svg")]
    public void AC10_the_placeable_icons_exist(string file)
    {
        var path = Path.Combine(RepoRoot(), "game", "art", "icons", file);

        Assert.True(File.Exists(path), $"game/art/icons/{file} is missing");
    }
}
