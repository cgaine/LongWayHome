using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>game/ui/Hud.tscn</c> (found by walking up to <c>LongWayHome.sln</c>) and checks the carried-crops
/// row (#95): <c>Panel/Rows/CropsRow</c>, an <c>HBoxContainer</c> whose first child is the <c>CropsLabel</c>.
/// </summary>
public partial class HudSceneTests
{
    [GeneratedRegex(@"^\[node\s+(?<attrs>[^\]]*)\]\s*$(?<body>.*?)(?=^\[|\z)", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex NodeSection();

    [GeneratedRegex(@"(?<key>\w+)=""(?<value>[^""]*)""")]
    private static partial Regex HeaderAttribute();

    [Fact]
    public void AC7_hud_has_a_CropsRow_HBoxContainer_under_Panel_Rows_that_ignores_the_mouse()
    {
        var row = Nodes().SingleOrDefault(n => n.Name == "CropsRow");

        Assert.True(row is not null, "Hud.tscn has no CropsRow node");
        Assert.Equal("HBoxContainer", row!.Type);
        Assert.Equal("Panel/Rows", row.Parent);
        Assert.Matches(new Regex(@"^mouse_filter\s*=\s*2\s*$", RegexOptions.Multiline), row.Body);
    }

    [Fact]
    public void AC7_CropsLabel_is_a_Label_in_CropsRow_with_font_size_22_and_outline_4()
    {
        var label = Nodes().SingleOrDefault(n => n.Name == "CropsLabel" && n.Parent == "Panel/Rows/CropsRow");

        Assert.True(label is not null, "Hud.tscn has no CropsLabel under Panel/Rows/CropsRow");
        Assert.Equal("Label", label!.Type);
        Assert.Matches(new Regex(@"^theme_override_font_sizes/font_size\s*=\s*22\s*$", RegexOptions.Multiline), label.Body);
        Assert.Matches(new Regex(@"^theme_override_constants/outline_size\s*=\s*4\s*$", RegexOptions.Multiline), label.Body);
    }

    [Fact]
    public void AC7_no_CropsLabel_directly_under_Panel_Rows()
    {
        var nodes = Nodes();

        Assert.NotEmpty(nodes);
        Assert.DoesNotContain(nodes, n => n.Name == "CropsLabel" && n.Parent == "Panel/Rows");
    }

    private sealed record SceneNode(string Name, string? Type, string? Parent, string Body);

    private static List<SceneNode> Nodes()
    {
        var path = Path.Combine(RepoRoot(), "game", "ui", "Hud.tscn");
        Assert.True(File.Exists(path), $"{path} is missing");
        var text = File.ReadAllText(path);

        return NodeSection().Matches(text)
            .Select(m =>
            {
                var attrs = HeaderAttribute().Matches(m.Groups["attrs"].Value)
                    .ToDictionary(a => a.Groups["key"].Value, a => a.Groups["value"].Value);
                return new SceneNode(
                    attrs.GetValueOrDefault("name", ""),
                    attrs.GetValueOrDefault("type"),
                    attrs.GetValueOrDefault("parent"),
                    m.Groups["body"].Value);
            })
            .ToList();
    }

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
