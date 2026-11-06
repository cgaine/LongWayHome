using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>Reads the real <c>game/project.godot</c> and checks the game opens on the title scene (#53).</summary>
public class MainSceneTests
{
    [Fact]
    public void AC1_main_scene_is_the_title_scene()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game", "project.godot");
        var text = File.ReadAllText(path);

        var mainScene = Regex.Match(text, @"^run/main_scene=""(?<scene>[^""]*)""", RegexOptions.Multiline);

        Assert.True(mainScene.Success, "project.godot has no run/main_scene");
        Assert.Equal("res://scenes/Main.tscn", mainScene.Groups["scene"].Value);
    }
}
