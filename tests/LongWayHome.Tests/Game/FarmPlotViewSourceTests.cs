using System.Text.RegularExpressions;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot scripts and checks #150's batched grass (REQ-14): <c>FarmPlotView</c> draws untouched grass
/// in <c>MultiMeshInstance3D</c> batches planned by <c>GrassBatches</c>, gives a Tile its own <c>TileView</c> only once
/// it isn't grass, makes that view as soon as the Tile changes, and rebuilds the batches on <c>NightCompleted</c>; and
/// <c>FarmGrid</c> maps through the pure <c>FarmGridLayout</c>.
/// </summary>
public class FarmPlotViewSourceTests
{
    private static string PlotView() => Code(ReadRepoFile("game", "scripts", "Farm", "FarmPlotView.cs"));

    private static string FarmGrid() => Code(ReadRepoFile("game", "scripts", "Farm", "FarmGrid.cs"));

    [Fact]
    public void AC9_plot_view_draws_grass_in_multimesh_batches_from_grass_batches()
    {
        var source = PlotView();

        Assert.Contains("MultiMeshInstance3D", source, StringComparison.Ordinal);
        Assert.Contains("GrassBatches.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_plot_view_makes_a_tile_view_only_for_a_non_grass_tile()
    {
        var source = PlotView();

        Assert.Contains("new TileView", source, StringComparison.Ordinal);
        Assert.Contains("TileState.Grass", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC9_tilling_a_tile_makes_its_tile_view_at_once()
    {
        var source = PlotView();
        var body = MethodBody(source, "OnTileChanged");
        Assert.True(body is not null, "FarmPlotView.cs has no OnTileChanged method");

        // The handler makes the view itself, or calls a method of the view that does.
        var makers = Regex.Matches(source, @"\b(?:private|public|internal)\s+[\w<>?]+\s+(\w+)\s*\(")
            .Select(match => match.Groups[1].Value)
            .Where(name => MethodBody(source, name) is { } method && method.Contains("new TileView", StringComparison.Ordinal))
            .ToList();
        Assert.True(
            body!.Contains("new TileView", StringComparison.Ordinal) || makers.Any(name => Regex.IsMatch(body, @"\b" + name + @"\s*\(")),
            "FarmPlotView.OnTileChanged neither makes a TileView nor calls a method that does, so a newly tilled Tile shows nothing");
    }

    [Fact]
    public void AC9_plot_view_rebuilds_the_grass_batches_on_night_completed()
    {
        var source = PlotView();
        Assert.Matches(@"NightCompleted\s*\+=", source);

        var body = MethodBody(source, "OnNightCompleted");
        Assert.True(body is not null, "FarmPlotView.cs has no OnNightCompleted method");
        Assert.True(
            Regex.IsMatch(body!, "Batch|Grass", RegexOptions.IgnoreCase),
            "FarmPlotView.OnNightCompleted doesn't rebuild the grass batches for the new Season");
    }

    [Fact]
    public void AC4_farm_grid_maps_through_farm_grid_layout()
    {
        var source = FarmGrid();

        Assert.Contains("FarmGridLayout.ToWorld", source, StringComparison.Ordinal);
        Assert.Contains("FarmGridLayout.CellAt", source, StringComparison.Ordinal);
    }

    /// <summary>The body of the first method named <paramref name="name"/>, braces matched; null when there is none.</summary>
    private static string? MethodBody(string source, string name)
    {
        var match = Regex.Match(source, @"\b" + Regex.Escape(name) + @"\s*\([^)]*\)\s*\{");
        if (!match.Success)
        {
            return null;
        }

        var depth = 0;
        for (var i = match.Index + match.Length - 1; i < source.Length; i++)
        {
            depth += source[i] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
            {
                return source[(match.Index + match.Length)..i];
            }
        }

        return null;
    }
}
