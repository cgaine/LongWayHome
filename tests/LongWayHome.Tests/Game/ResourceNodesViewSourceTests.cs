using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// #151 AC9: reads the real <c>game/art/farm/ResourceNodesView.cs</c> and <c>Farm.tscn</c>. The farm draws its
/// ResourceNodes from <c>GameSession</c>'s node list as one <c>MultiMeshInstance3D</c> per kind, stage and Season
/// (REQ-14): the Twinbough <c>Full</c> for a tree in today's Season, the <c>Stump</c> and <c>RockKind.Rock</c>. Each
/// tree and stump gets a trunk cylinder and each rock a box, so the farmer can't walk through them; the view rebuilds
/// on <c>NodesChanged</c> and holds no rules.
/// </summary>
public class ResourceNodesViewSourceTests
{
    private const string ViewScript = "res://art/farm/ResourceNodesView.cs";

    private static string View() => Code(ReadRepoFile("game", "art", "farm", "ResourceNodesView.cs"));

    private static string Farm() => ReadRepoFile("game", "scenes", "Farm.tscn");

    [Fact]
    public void AC9_the_view_draws_the_nodes_in_multimesh_batches_not_one_mesh_per_node()
    {
        var source = View();

        Assert.True(
            source.Contains("MultiMeshInstance3D", StringComparison.Ordinal) || source.Contains("AreaArt.Many(", StringComparison.Ordinal),
            "ResourceNodesView.cs draws no MultiMeshInstance3D");
        Assert.DoesNotMatch(@"\bnew\s+MeshInstance3D\b", source);
    }

    [Fact]
    public void AC9_the_view_draws_full_twinbough_trees_in_the_season_stumps_and_rocks()
    {
        var source = View();

        Assert.Matches(@"NatureModels\.Twinbough\(\s*TreeStage\.Full\s*,", source);
        Assert.Contains("TreeStage.Stump", source, StringComparison.Ordinal);
        Assert.Matches(@"NatureModels\.Rock\(\s*RockKind\.Rock\s*\)", source);
        Assert.Contains("Season", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_trees_and_stumps_get_a_trunk_cylinder_collider()
    {
        var source = View();

        Assert.True(
            source.Contains("CylinderShape3D", StringComparison.Ordinal) || source.Contains("AreaArt.Trunks(", StringComparison.Ordinal),
            "ResourceNodesView.cs gives trees and stumps no CylinderShape3D trunk");
    }

    [Fact]
    public void AC9_rocks_get_a_box_collider()
    {
        var source = View();

        Assert.Contains("BoxShape3D", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_the_view_reads_the_sessions_nodes_and_rebuilds_on_NodesChanged()
    {
        var source = View();

        Assert.Contains("GameRoot", source, StringComparison.Ordinal);
        Assert.Contains(".ResourceNodes", source, StringComparison.Ordinal);
        Assert.Matches(@"NodesChanged\s*\+=", source);
        Assert.Matches(@"NodesChanged\s*-=", source);
    }

    [Fact]
    public void AC9_the_view_holds_no_rules_and_changes_nothing()
    {
        var source = View();

        foreach (var forbidden in new[]
                 {
                     @"\bTillRejection\s*\(", @"\.Till\s*\(", @"\.Use\s*\(", @"\bNewGameFactory\b", @"\bnew\s+ResourceNode\b",
                     @"\bNodeSpawns\b", @"\bJsonGameDataSource\b", @"\bRejection\.",
                 })
        {
            Assert.False(Regex.IsMatch(source, forbidden), $"ResourceNodesView.cs matches '{forbidden}': the view reads GameSession only");
        }
    }

    [Fact]
    public void AC9_farm_scene_has_one_resource_nodes_view()
    {
        var views = ScriptedNodes(Farm(), ViewScript);

        Assert.Single(views);
    }
}
