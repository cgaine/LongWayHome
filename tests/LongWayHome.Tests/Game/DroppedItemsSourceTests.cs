using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files from the repository and checks the Godot glue of #130's dropped items: the
/// <c>DroppedItemsView</c> in <c>Farm.tscn</c> draws one billboard sprite per DroppedItem from state, with no collision,
/// lights or shadows; <c>ToolUse</c> and <c>ShopMenu</c> pass the farmer's feet tile; a Player script picks up at the
/// feet tile. Each check looks for the essential tokens, not exact formatting.
/// </summary>
public class DroppedItemsSourceTests
{
    private static readonly string[] ViewPath = ["game", "scripts", "World", "DroppedItemsView.cs"];

    // ---- AC12: the view exists, is in the farm, and redraws from state ----

    [Fact]
    public void AC12_dropped_items_view_script_exists()
    {
        Assert.True(File.Exists(RepoPath(ViewPath)), "game/scripts/World/DroppedItemsView.cs not found");
    }

    [Fact]
    public void AC12_farm_scene_instances_the_dropped_items_view()
    {
        var scene = ReadRepoFile("game", "scenes", "Farm.tscn");

        var resource = Regex.Match(
            scene, @"\[ext_resource\s+type=""Script""[^\]]*path=""res://scripts/World/DroppedItemsView\.cs""[^\]]*id=""([^""]+)""");
        Assert.True(resource.Success, "Farm.tscn has no ext_resource for DroppedItemsView.cs");
        var id = Regex.Escape(resource.Groups[1].Value);
        Assert.Matches(@"\[node\s+name=""[^""]+""\s+type=""Node3D""[^\]]*\]\s*script\s*=\s*ExtResource\(""" + id + @"""\)", scene);
    }

    [Fact]
    public void AC12_view_rebuilds_on_dropped_items_changed_and_after_the_night()
    {
        var source = ReadRepoFile(ViewPath);

        Assert.Matches(@"DroppedItemsChanged\s*\+=", source);
        Assert.Matches(@"DroppedItemsChanged\s*-=", source);
        Assert.Matches(@"NightCompleted\s*\+=", source);
        Assert.Contains(".DroppedItems", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC12_view_draws_a_billboard_sprite3d_at_the_tile()
    {
        var source = ReadRepoFile(ViewPath);

        Assert.Contains("Sprite3D", source, StringComparison.Ordinal);
        Assert.Matches(@"Billboard\s*=\s*BaseMaterial3D\.BillboardModeEnum\.(Enabled|FixedY)", source);
        Assert.Contains("FarmGrid.ToWorld(", source, StringComparison.Ordinal);
        Assert.Contains("DroppedItemLayout.Offset(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC12_view_shows_the_seed_or_crop_icon()
    {
        var source = ReadRepoFile(ViewPath);

        Assert.Contains("ItemIcons.", source, StringComparison.Ordinal);
        Assert.Contains("IconTextures.Get(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC12_view_has_no_collision_lights_or_shadows()
    {
        var source = ReadRepoFile(ViewPath);

        Assert.DoesNotContain("CollisionShape3D", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StaticBody3D", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Area3D", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\b(Omni|Spot|Directional)?Light3D\b", source);

        // REQ-14: a Sprite3D casts a shadow by default, so the view turns it off.
        Assert.Matches(@"CastShadow\s*=\s*GeometryInstance3D\.ShadowCastingSetting\.Off", source);
    }

    // ---- AC1, AC4, AC11: the Godot layer passes the feet tile ----

    [Fact]
    public void AC11_tool_use_passes_the_feet_tile_to_use()
    {
        var source = ReadRepoFile("game", "scripts", "Player", "ToolUse.cs");

        var call = Regex.Match(source, @"_session\s*!?\s*\.\s*Use\s*\((?<args>[^;]*)\)\s*;");
        Assert.True(call.Success, "ToolUse.cs never calls _session.Use(...)");
        Assert.True(
            ArgumentCount(call.Groups["args"].Value) >= 3,
            $"ToolUse passes no feet position to Use: Use({call.Groups["args"].Value})");
    }

    [Fact]
    public void AC4_shop_menu_passes_the_feet_tile_to_buy_and_shows_the_after_buy_message()
    {
        var source = ReadRepoFile("game", "scripts", "Ui", "ShopMenu.cs");

        var call = Regex.Match(source, @"_session\s*!?\s*\.\s*Buy\s*\((?<args>[^;]*)\)\s*;");
        Assert.True(call.Success, "ShopMenu.cs never calls _session.Buy(...)");
        Assert.True(
            ArgumentCount(call.Groups["args"].Value) >= 3,
            $"ShopMenu passes no feet position to Buy: Buy({call.Groups["args"].Value})");
        Assert.Contains("ShopText.AfterBuy(", source, StringComparison.Ordinal);
        Assert.Contains("LastBuyDropped", source, StringComparison.Ordinal);
    }

    // ---- AC5, AC11: a Player script picks up at the feet tile ----

    [Fact]
    public void AC5_a_player_script_picks_up_at_the_feet_tile()
    {
        var source = Assert.Single(PlayerScripts(), text => text.Contains("PickUp(", StringComparison.Ordinal));
        Assert.Contains("FeetTile.Nearest(", source, StringComparison.Ordinal);
        Assert.Contains("FarmGrid.CellAt(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC5_the_pick_up_script_retries_on_inventory_changed_and_never_while_paused()
    {
        var source = Assert.Single(PlayerScripts(), text => text.Contains("PickUp(", StringComparison.Ordinal));

        Assert.Matches(@"InventoryChanged\s*\+=", source);
        Assert.Matches(@"InventoryChanged\s*-=", source);
        Assert.Contains("HasDroppedItemsAt(", source, StringComparison.Ordinal);
        Assert.Matches(@"\.Paused\b|IsPaused\(", source);
    }

    /// <summary>Top-level comma-separated arguments in a call's argument text.</summary>
    private static int ArgumentCount(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return 0;
        }

        var depth = 0;
        var count = 1;
        foreach (var c in args)
        {
            depth += c is '(' or '[' or '{' ? 1 : c is ')' or ']' or '}' ? -1 : 0;
            if (c == ',' && depth == 0)
            {
                count++;
            }
        }

        return count;
    }

    private static IEnumerable<string> PlayerScripts() =>
        Directory.GetFiles(RepoPath("game", "scripts", "Player"), "*.cs").Select(File.ReadAllText).ToList();

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
