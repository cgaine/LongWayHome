using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// #152: the Godot glue xUnit can't run, read from the repository. <c>FarmerAnimations</c> builds the axe and pickaxe
/// swings, <c>ToolUse</c> plays the previewed action's clip only on a successful Use (so a refused swing plays none, as
/// for the hoe), the nodes view shakes a node on a swing that doesn't clear it, and <c>GameRoot</c> loads
/// <c>materials.json</c>.
/// </summary>
public class SwingGlueSourceTests
{
    [Fact]
    public void AC11_farmer_animations_names_the_axe_and_pickaxe_swings_in_snake_case()
    {
        var source = Code(ReadRepoFile("game", "art", "farmer", "FarmerAnimations.cs"));

        Assert.Matches(@"FarmerClip\.AxeSwing\s*=>\s*""axe_swing""", source);
        Assert.Matches(@"FarmerClip\.PickaxeSwing\s*=>\s*""pickaxe_swing""", source);
    }

    [Fact]
    public void AC11_tool_use_plays_the_chop_or_break_clip_only_after_a_successful_use_as_for_the_hoe()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Player", "ToolUse.cs"));

        // The hoe's pattern: read the preview's action first, Use, then play ForUse(action) only when IsOk.
        var preview = Regex.Match(source, @"PreviewUse\([^;]*\)\.Action");
        var use = Regex.Match(source, @"\.Use\s*\(");
        var play = Regex.Match(source, @"if\s*\(\s*result\.IsOk\s*&&\s*FarmerAnimationState\.ForUse\(\s*action\s*\)");
        Assert.True(preview.Success && use.Success && play.Success, "ToolUse.cs no longer follows the hoe's preview → Use → IsOk → ForUse pattern");
        Assert.True(preview.Index < use.Index && use.Index < play.Index);

        // No separate path for the new tools that could play a clip on a refusal.
        Assert.DoesNotMatch(@"FarmerClip\.(AxeSwing|PickaxeSwing)", source);
    }

    [Fact]
    public void Nodes_view_shakes_a_node_on_a_swing_that_does_not_clear_it()
    {
        var source = Code(ReadRepoFile("game", "art", "farm", "ResourceNodesView.cs"));

        Assert.Matches(@"(?i)shake", source);
        Assert.True(
            source.Contains("CreateTween(", StringComparison.Ordinal) || source.Contains("Tween", StringComparison.Ordinal),
            "ResourceNodesView.cs shakes nothing: no Tween");
    }

    [Fact]
    public void Game_root_reads_materials_json_and_passes_it_to_the_data_source()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Core", "GameRoot.cs"));

        Assert.Contains("JsonGameDataSource.MaterialsFileName", source, StringComparison.Ordinal);
        Assert.Matches(@"new\s+JsonGameDataSource\(\s*crops!?\s*,\s*shop!?\s*,\s*tuning!?\s*,\s*nodes!?\s*,\s*\w+!?\s*\)", source);
    }
}
