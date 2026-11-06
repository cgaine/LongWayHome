using System.Text.RegularExpressions;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot scripts (linked into the test output by the test project) and the shared shader (from the
/// repo) and checks the Godot glue of #78: tile slabs instead of outlined quads, the dark plot base, the wet sheen and
/// the highlight-and-brackets target marker.
/// </summary>
public class TileSlabSourceTests
{
    // ---- AC6 / BR-T1: slabs and grooves, no outline ----

    [Fact]
    public void AC6_tile_view_draws_no_outline()
    {
        var source = ReadScript("Farm", "TileView.cs");

        Assert.DoesNotContain("Outline", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildOutline", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LineStrip", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ImmediateMesh", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC6_tile_view_builds_a_0_96_by_0_06_slab_box()
    {
        var source = ReadScript("Farm", "TileView.cs");

        Assert.Contains("\"Slab\"", source, StringComparison.Ordinal);
        Assert.Contains("BoxMesh", source, StringComparison.Ordinal);
        Assert.Matches(@"new\s+Vector3\s*\(\s*0\.96f\s*,\s*0\.06f\s*,\s*0\.96f\s*\)", source);
    }

    [Fact]
    public void AC6_plot_view_adds_a_12_by_9_plot_base_in_soil_watered()
    {
        var source = ReadScript("Farm", "FarmPlotView.cs");

        Assert.Contains("\"PlotBase\"", source, StringComparison.Ordinal);
        Assert.Contains("\"soil-watered\"", source, StringComparison.Ordinal);
        Assert.Matches(@"\b12f?\s*,\s*9f?\b", source);
    }

    [Fact]
    public void BR_T1_plot_has_108_tiles()
    {
        var plot = new FarmPlot();

        Assert.Equal(12, FarmPlot.Width);
        Assert.Equal(9, FarmPlot.Height);
        Assert.Equal(108, plot.Tiles.Count);
        Assert.Equal(108, plot.Tiles.Select(t => t.Position).Distinct().Count());
    }

    // ---- AC2 / AC3 / AC10: the tile view reads its look from the Application layer ----

    [Fact]
    public void AC2_tile_view_uses_tile_look_and_art_materials()
    {
        var source = ReadScript("Farm", "TileView.cs");

        Assert.Contains("TileLook.For", source, StringComparison.Ordinal);
        Assert.Contains("ArtMaterials.Apply", source, StringComparison.Ordinal);
        Assert.Contains("\"Tufts\"", source, StringComparison.Ordinal);
        Assert.Contains("\"Furrows\"", source, StringComparison.Ordinal);
        Assert.Contains("\"NativePlant\"", source, StringComparison.Ordinal);
        Assert.Contains("TileDecoration.For", source, StringComparison.Ordinal);
        Assert.Matches(@"SetInstanceShaderParameter\s*\(\s*""wet""", source);
    }

    [Fact]
    public void AC10_planting_models_stand_on_the_slab_top()
    {
        var source = ReadScript("Farm", "TileView.cs");

        Assert.Matches(@"SurfaceHeight\s*=\s*0\.06f\s*;", source);
    }

    [Fact]
    public void AC3_shader_declares_wet_instance_uniform()
    {
        var shader = ReadRepoFile("game", "art", "shaders", "low_poly.gdshader");

        Assert.Matches(@"instance\s+uniform\s+float\s+wet\s*=\s*0\.0", shader);
        Assert.Contains("mix(1.0, 0.25, wet)", shader, StringComparison.Ordinal);
    }

    // ---- AC7: the target marker ----

    [Fact]
    public void AC7_target_marker_has_highlight_and_four_brackets_from_palette()
    {
        var source = ReadScript("Player", "ToolTargeting.cs");

        Assert.Contains("\"Cursor\"", source, StringComparison.Ordinal);
        Assert.Contains("\"Highlight\"", source, StringComparison.Ordinal);
        Assert.Contains("Bracket", source, StringComparison.Ordinal);
        Assert.Contains("ArtPalette", source, StringComparison.Ordinal);
        Assert.Contains("\"target-highlight\"", source, StringComparison.Ordinal);
        Assert.Contains("\"ember\"", source, StringComparison.Ordinal);
        Assert.Contains("0.45f", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LineStrip", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC7_target_marker_hides_when_target_is_null()
    {
        var source = ReadScript("Player", "ToolTargeting.cs");

        Assert.Matches(
            new Regex(@"if\s*\(\s*Target\s+is\s*\{\s*\}.*?\belse\b\s*\{[^}]*\.Visible\s*=\s*false", RegexOptions.Singleline),
            source);
    }

    // ---- helpers ----

    private static string ReadScript(string folder, string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game", "scripts", folder, fileName);
        Assert.True(File.Exists(path), $"game/scripts/{folder}/{fileName} not found");
        return File.ReadAllText(path);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        var path = Path.Combine(new[] { dir!.FullName }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"{path} is missing");
        return File.ReadAllText(path);
    }
}
