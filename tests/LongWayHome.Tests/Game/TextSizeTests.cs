using System.Globalization;
using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>game/project.godot</c>, every <c>game/**/*.tscn</c> and <c>*.tres</c>, and every
/// <c>game/scripts/**/*.cs</c>, linked into the test output by the test project, and checks text is readable on a
/// Steam Deck (#55): a project theme with a 22 px default font size, and no font size below 20 anywhere.
/// </summary>
public partial class TextSizeTests
{
    private const int MinimumFontSize = 20;

    private static string GameDir => Path.Combine(AppContext.BaseDirectory, "game");

    [GeneratedRegex(@"^\s*([\w/]*font_size)\s*=\s*(\d+)", RegexOptions.Multiline)]
    private static partial Regex SceneFontSize();

    [GeneratedRegex(@"AddThemeFontSizeOverride\(\s*""[^""]*""\s*,\s*(\d+)\s*\)")]
    private static partial Regex ScriptFontSizeOverride();

    [GeneratedRegex(@"const\s+int\s+(\w*FontSize)\s*=\s*(\d+)")]
    private static partial Regex ScriptFontSizeConstant();

    [Fact]
    public void AC3_project_godot_gui_section_sets_the_custom_theme_to_res_ui_Theme_tres()
    {
        var path = Path.Combine(GameDir, "project.godot");
        var gui = ReadSection(File.ReadAllLines(path), "gui");

        Assert.True(gui is not null, "project.godot has no [gui] section");
        Assert.Contains("theme/custom=\"res://ui/Theme.tres\"", gui!);
    }

    [Fact]
    public void AC3_Theme_tres_sets_a_default_font_size_of_22()
    {
        var path = Path.Combine(GameDir, "ui", "Theme.tres");

        Assert.True(File.Exists(path), "game/ui/Theme.tres does not exist");
        var text = File.ReadAllText(path);
        Assert.Matches(new Regex(@"^default_font_size\s*=\s*22\s*$", RegexOptions.Multiline), text);
    }

    [Fact]
    public void AC4_the_scan_finds_scene_and_script_files()
    {
        Assert.NotEmpty(SceneFiles());
        Assert.NotEmpty(ScriptFiles());
    }

    [Fact]
    public void AC4_no_scene_or_resource_font_size_is_below_20()
    {
        var violations = SceneFiles()
            .SelectMany(file => ScanScene(Relative(file), File.ReadAllText(file)))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void AC4_no_script_font_size_override_is_below_20()
    {
        var violations = ScriptFiles()
            .SelectMany(file => ScanScriptOverrides(Relative(file), File.ReadAllText(file)))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void AC4_no_script_font_size_constant_is_below_20()
    {
        var violations = ScriptFiles()
            .SelectMany(file => ScanScriptConstants(Relative(file), File.ReadAllText(file)))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void AC4_the_scene_scan_reports_a_font_size_below_20_with_its_file_and_value()
    {
        var text = "[node name=\"Label\" type=\"Label\"]\ntheme_override_font_sizes/font_size = 16\n";

        var violation = Assert.Single(ScanScene("ui/Fake.tscn", text));

        Assert.Contains("ui/Fake.tscn", violation);
        Assert.Contains("16", violation);
    }

    [Theory]
    [InlineData("default_font_size = 18")]
    [InlineData("font_size=12")]
    public void AC4_the_scene_scan_reports_every_font_size_form(string line)
    {
        Assert.Single(ScanScene("ui/Fake.tres", line));
    }

    [Fact]
    public void AC4_the_scene_scan_accepts_a_font_size_of_20()
    {
        Assert.Empty(ScanScene("ui/Fake.tscn", "theme_override_font_sizes/font_size = 20"));
    }

    [Fact]
    public void AC4_the_script_scans_report_a_font_size_below_20_with_its_file_and_value()
    {
        var overrideViolation = Assert.Single(
            ScanScriptOverrides("scripts/Fake.cs", "label.AddThemeFontSizeOverride(\"font_size\", 14);"));
        var constantViolation = Assert.Single(
            ScanScriptConstants("scripts/Fake.cs", "private const int SlotFontSize = 18;"));

        Assert.Contains("scripts/Fake.cs", overrideViolation);
        Assert.Contains("14", overrideViolation);
        Assert.Contains("scripts/Fake.cs", constantViolation);
        Assert.Contains("18", constantViolation);
    }

    /// <summary>Every <c>font_size = N</c> (any key ending in <c>font_size</c>) below 20, as "file: key = N".</summary>
    private static IEnumerable<string> ScanScene(string file, string text) =>
        SceneFontSize().Matches(text)
            .Where(m => Value(m.Groups[2]) < MinimumFontSize)
            .Select(m => $"{file}: {m.Groups[1].Value} = {m.Groups[2].Value} (minimum {MinimumFontSize})");

    /// <summary>Every <c>AddThemeFontSizeOverride("…", N)</c> literal below 20.</summary>
    private static IEnumerable<string> ScanScriptOverrides(string file, string text) =>
        ScriptFontSizeOverride().Matches(text)
            .Where(m => Value(m.Groups[1]) < MinimumFontSize)
            .Select(m => $"{file}: {m.Value} (minimum {MinimumFontSize})");

    /// <summary>Every <c>const int &lt;Name&gt;FontSize = N</c> below 20.</summary>
    private static IEnumerable<string> ScanScriptConstants(string file, string text) =>
        ScriptFontSizeConstant().Matches(text)
            .Where(m => Value(m.Groups[2]) < MinimumFontSize)
            .Select(m => $"{file}: {m.Groups[1].Value} = {m.Groups[2].Value} (minimum {MinimumFontSize})");

    private static int Value(Group group) => int.Parse(group.Value, CultureInfo.InvariantCulture);

    private static string[] SceneFiles() =>
        Directory.EnumerateFiles(GameDir, "*.tscn", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(GameDir, "*.tres", SearchOption.AllDirectories))
            .ToArray();

    private static string[] ScriptFiles()
    {
        var scripts = Path.Combine(GameDir, "scripts");
        return Directory.Exists(scripts)
            ? Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
            : [];
    }

    private static string Relative(string file) =>
        "game/" + Path.GetRelativePath(GameDir, file).Replace('\\', '/');

    /// <summary>The lines of a <c>[name]</c> section of an INI-style file, or null when it has none.</summary>
    private static IReadOnlyList<string>? ReadSection(string[] lines, string name)
    {
        var start = Array.FindIndex(lines, line => line.Trim() == $"[{name}]");
        if (start < 0)
        {
            return null;
        }

        return lines.Skip(start + 1)
            .TakeWhile(line => !line.TrimStart().StartsWith('['))
            .Select(line => line.Trim())
            .ToList();
    }
}
