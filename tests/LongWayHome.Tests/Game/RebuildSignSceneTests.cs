using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files for #163 AC10: the general store's Rebuild sign in <c>Town.tscn</c> (a <c>sign</c>
/// InteractionPoint with a <c>RebuildSignPoint</c> for <c>general_store</c>, in front of the store and clear of its
/// eaves, Maggie, the road and the square), the Rebuild menu in <c>AreaCommon.tscn</c>, the HUD's "Rebuild" hint and
/// the glue that draws the sign and delivers.
/// </summary>
public class RebuildSignSceneTests
{
    private const string PointScript = "res://scripts/World/InteractionPoint.cs";

    private const string SignScript = "res://scripts/World/RebuildSignPoint.cs";

    private const string MenuScene = "res://ui/RebuildMenu.tscn";

    private const string MenuScript = "res://scripts/Ui/RebuildMenu.cs";

    /// <summary>Where the issue puts the sign: (-7, 0, -1.5), moved at most 1.5 m to clear the store, Maggie, the road and the square.</summary>
    private const double SignX = -7.0;

    private const double SignZ = -1.5;

    private const double MaggieX = -11.6;

    private const double MaggieZ = -2.0;

    private static string Scene(string name) => SceneText.ReadRepoFile("game", "scenes", $"{name}.tscn");

    private static string Script(params string[] parts) =>
        SceneText.Code(SceneText.ReadRepoFile(new[] { "game" }.Concat(parts).ToArray()));

    private static List<SceneText.Node> SignPoints() =>
        SceneText.ScriptedNodes(Scene("Town"), PointScript)
            .Where(node => SceneText.Property(node, "PointId") == "sign")
            .ToList();

    private static (double X, double Z) SignPosition()
    {
        var point = Assert.Single(SignPoints());
        var (x, _, z) = SceneText.Position(Scene("Town"), point);
        return (x, z);
    }

    private static double Distance(double x1, double z1, double x2, double z2) =>
        Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((z1 - z2) * (z1 - z2)));

    // ---- Town.tscn ----

    [Fact]
    public void AC10_town_has_one_sign_interaction_point_named_StoreSign_with_range_1_5()
    {
        var point = Assert.Single(SignPoints());

        Assert.Equal("InteractionPoints/StoreSign", point.Path);
        Assert.Equal(1.5, SceneText.Number(point, "Range"));
    }

    [Fact]
    public void AC10_the_store_sign_has_a_RebuildSignPoint_child_for_the_general_store()
    {
        var scene = Scene("Town");

        var sign = Assert.Single(SceneText.ScriptedNodes(scene, SignScript));

        Assert.Equal("InteractionPoints/StoreSign", sign.Parent);
        Assert.Equal("general_store", SceneText.Property(sign, "Building"));
    }

    [Fact]
    public void AC10_the_rebuild_sign_point_script_exports_its_building()
    {
        var source = Script("scripts", "World", "RebuildSignPoint.cs");

        Assert.Contains("[Export]", source, StringComparison.Ordinal);
        Assert.Contains("Building", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_sign_stands_in_front_of_the_store_within_1_5_of_its_place()
    {
        var (x, z) = SignPosition();

        Assert.True(Distance(x, z, SignX, SignZ) <= 1.5, $"StoreSign at ({x}, {z}) is over 1.5 from (-7, -1.5)");
    }

    [Fact]
    public void AC10_the_sign_is_clear_of_the_stores_eaves()
    {
        var (x, z) = SignPosition();
        var store = TownLayout.Buildings.Single(building => building.Kind == TownBuildingKind.Store);

        Assert.True(
            Distance(x, z, store.X, store.Z) >= store.WallRadius + TownLayout.EaveMargin,
            $"StoreSign at ({x}, {z}) stands under the store's eaves");
    }

    [Fact]
    public void AC10_the_sign_is_off_the_road_and_the_square()
    {
        var (x, z) = SignPosition();

        Assert.True(Math.Abs(x) >= TownLayout.RoadHalfWidth, $"StoreSign at ({x}, {z}) is on the road");
        Assert.True(Distance(x, z, 0, 0) >= TownLayout.SquareRadius, $"StoreSign at ({x}, {z}) is on the square");
    }

    [Fact]
    public void AC10_the_signs_range_never_overlaps_maggies_talk_range()
    {
        var (x, z) = SignPosition();

        // Her talk range (1.5) plus the sign's (1.5): one interact can't reach both.
        Assert.True(Distance(x, z, MaggieX, MaggieZ) >= 3.0, $"StoreSign at ({x}, {z}) is within reach of Maggie");
    }

    // ---- The Rebuild menu ----

    [Fact]
    public void AC10_area_common_instances_the_rebuild_menu()
    {
        var menu = Assert.Single(SceneText.InstancesOf(Scene("AreaCommon"), MenuScene));

        Assert.Equal(".", menu.Parent);
    }

    [Fact]
    public void AC10_the_rebuild_menu_scene_uses_the_rebuild_menu_script()
    {
        var scene = SceneText.ReadRepoFile("game", "ui", "RebuildMenu.tscn");

        Assert.Single(SceneText.ScriptedNodes(scene, MenuScript));
    }

    [Fact]
    public void AC10_the_rebuild_menu_opens_at_a_sign_point_and_pauses_the_tree()
    {
        var source = Script("scripts", "Ui", "RebuildMenu.cs");

        Assert.Contains("\"sign\"", source, StringComparison.Ordinal);
        Assert.Contains("Paused = true", source, StringComparison.Ordinal);
        Assert.Contains("Paused = false", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_rebuild_menu_shows_each_line_from_the_session_with_RebuildText()
    {
        var source = Script("scripts", "Ui", "RebuildMenu.cs");

        Assert.Contains(".RebuildSign(", source, StringComparison.Ordinal);
        Assert.Contains("RebuildText.Title(", source, StringComparison.Ordinal);
        Assert.Contains("RebuildText.Row(", source, StringComparison.Ordinal);
        Assert.Contains(".Unlocks", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_rebuild_menu_delivers_items_and_gold_through_the_session()
    {
        var source = Script("scripts", "Ui", "RebuildMenu.cs");

        Assert.Contains(".Deliver(", source, StringComparison.Ordinal);
        Assert.Contains(".DeliverGold(", source, StringComparison.Ordinal);
        Assert.Contains("RebuildText.DeliverGoldButton", source, StringComparison.Ordinal);
        Assert.Contains("RebuildText.Rejection(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_the_rebuild_menu_closes_on_cancel_and_pause_and_recovers_focus()
    {
        var source = Script("scripts", "Ui", "RebuildMenu.cs");

        Assert.Contains("\"ui_cancel\"", source, StringComparison.Ordinal);
        Assert.Contains("\"pause\"", source, StringComparison.Ordinal);
        Assert.Contains("MenuFocusRecovery", source, StringComparison.Ordinal);
    }

    // ---- The HUD hint ----

    [Fact]
    public void AC10_hud_has_a_RebuildHint_label()
    {
        var scene = SceneText.ReadRepoFile("game", "ui", "Hud.tscn");

        var hint = SceneText.Nodes(scene).SingleOrDefault(node => node.Name == "RebuildHint");

        Assert.True(hint is not null, "Hud.tscn has no RebuildHint node");
        Assert.Equal("Label", hint!.Type);
    }

    [Fact]
    public void AC10_hud_shows_the_rebuild_hint_in_range_of_a_sign_point()
    {
        var source = Script("scripts", "Ui", "Hud.cs");

        Assert.Contains("\"sign\"", source, StringComparison.Ordinal);
        Assert.Contains("} Rebuild\"", source, StringComparison.Ordinal);
    }

    // ---- The sign's look in town ----

    [Fact]
    public void AC10_town_view_draws_the_sign_while_the_session_has_one_and_redraws_on_BuildingsChanged()
    {
        var source = Script("art", "areas", "TownView.cs");

        Assert.Contains(".RebuildSign(", source, StringComparison.Ordinal);
        Assert.Contains("TownModels.RebuildSign(", source, StringComparison.Ordinal);
        Assert.Contains("BuildingsChanged", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_town_view_fills_the_signs_pips_from_the_view()
    {
        var source = Script("art", "areas", "TownView.cs");

        Assert.Contains(".WoodPip", source, StringComparison.Ordinal);
        Assert.Contains(".StonePip", source, StringComparison.Ordinal);
        Assert.Contains(".GoldPip", source, StringComparison.Ordinal);
    }
}
