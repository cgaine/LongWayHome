using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot scripts, linked into the test output by the test project, and checks the target marker draws
/// on top of the world and the farmer turns to face the target when standing still (issue #71).
/// </summary>
public class ToolTargetingCursorTests
{
    [Fact]
    public void AC1_cursor_material_disables_depth_test_and_draws_on_top()
    {
        var body = ExtractBuildCursor(ReadScript("ToolTargeting.cs"));

        Assert.Matches(@"NoDepthTest\s*=\s*true", body);
        Assert.Matches(@"RenderPriority\s*=\s*1\b", body);
        Assert.Matches(@"AlbedoColor\s*=\s*CursorColor", body);
        Assert.Contains("ShadingModeEnum.Unshaded", body, StringComparison.Ordinal);
    }

    [Fact]
    public void AC3_player_faces_target_when_still()
    {
        var source = ReadScript("PlayerController.cs");

        Assert.Matches(@"FacingDirection\s*\.\s*Yaw\s*\(\s*Facing\s*\.\s*X\s*,\s*Facing\s*\.\s*Y\s*\)", source);
    }

    private static string ReadScript(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game", "scripts", "Player", fileName);
        Assert.True(File.Exists(path), $"game/scripts/Player/{fileName} not found");
        return File.ReadAllText(path);
    }

    /// <summary>The text of <c>BuildCursor</c>, from its declaration to the next <c>private</c> member or the end of
    /// the file, so a match elsewhere in the class does not count.</summary>
    private static string ExtractBuildCursor(string source)
    {
        // The declaration, not the call site (Mesh = BuildCursor()): the name followed by its body's opening brace.
        var declaration = Regex.Match(source, @"\bBuildCursor\s*\(\s*\)\s*\{");
        Assert.True(declaration.Success, "ToolTargeting.cs has no BuildCursor() method");
        var start = declaration.Index;

        var next = source.IndexOf("private", start + declaration.Length, StringComparison.Ordinal);
        return next < 0 ? source[start..] : source[start..next];
    }
}
