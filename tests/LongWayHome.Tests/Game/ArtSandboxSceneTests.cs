using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real game files to check the art sandbox (#75) is reached only through the title menu's
/// <c>--art-sandbox</c> flag, that gameplay scenes never reference <c>game/art/sandbox/</c>, and that gameplay scripts
/// use nothing from <c>game/art/</c> but the shared material helpers.
/// </summary>
public class ArtSandboxSceneTests
{
    private static string GamePath(params string[] parts) =>
        Path.Combine(new[] { AppContext.BaseDirectory, "game" }.Concat(parts).ToArray());

    [Fact]
    public void AC2_art_sandbox_scene_exists()
    {
        Assert.True(
            File.Exists(GamePath("art", "sandbox", "ArtSandbox.tscn")),
            "game/art/sandbox/ArtSandbox.tscn is missing");
    }

    [Fact]
    public void AC2_title_menu_opens_the_sandbox_deferred_on_the_flag()
    {
        var text = File.ReadAllText(GamePath("scripts", "Ui", "SaveSlotsMenu.cs"));

        Assert.Contains("OpensArtSandbox(OS.GetCmdlineUserArgs())", text);
        Assert.Contains("\"res://art/sandbox/ArtSandbox.tscn\"", text);
        Assert.Contains("CallDeferred", text);
    }

    [Fact]
    public void AC10_gameplay_does_not_reference_the_art_folder()
    {
        var scenes = new[]
            {
                "Farm.tscn", "Player.tscn", "Main.tscn", "AreaCommon.tscn", "ForestPath.tscn", "Town.tscn", "Lakeshore.tscn",
            }
            .Select(name => GamePath("scenes", name))
            .ToList();
        Assert.All(scenes, path => Assert.True(File.Exists(path), $"{path} is missing"));

        var scriptsRoot = GamePath("scripts");
        var titleMenu = Path.GetFullPath(GamePath("scripts", "Ui", "SaveSlotsMenu.cs"));
        var scripts = Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetFullPath(path) != titleMenu)
            .ToList();
        Assert.NotEmpty(scripts);

        // Gameplay scenes may instance art scenes and use the shared material (#77, ART_DIRECTION §8), but never the
        // sandbox.
        foreach (var path in scenes)
        {
            var text = File.ReadAllText(path);
            Assert.False(text.Contains("res://art/sandbox/"), $"{path} references res://art/sandbox/");
        }

        foreach (var path in scripts)
        {
            var text = File.ReadAllText(path);
            Assert.False(text.Contains("res://art/"), $"{path} references res://art/");
        }

        // Gameplay scripts may use the shared material helpers from game/art/ (ArtMaterials, ArtMeshes; #78 colours the
        // tile slabs with them), but never the sandbox's types, which share the LongWayHome.Game.Art namespace.
        string[] sandboxTypes = ["ArtSandbox", "SandboxLook", "SandboxCamera"];
        foreach (var path in scripts)
        {
            var text = File.ReadAllText(path);
            foreach (var type in sandboxTypes)
            {
                Assert.False(
                    Regex.IsMatch(text, $@"\b{type}\b"),
                    $"{path} uses the art sandbox's {type}");
            }
        }
    }
}
