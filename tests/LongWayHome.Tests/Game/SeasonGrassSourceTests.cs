using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot scripts (linked into the test output by the test project) and checks the Godot glue of #79:
/// the meadow and the grass Tiles read the Season from the session on ready and on <c>NightCompleted</c>, apply
/// <c>SeasonGrass</c>, stay in summer with no session, never change per frame, and swap a Season without creating or
/// freeing nodes. Each check looks for the essential token, not exact formatting.
/// </summary>
public class SeasonGrassSourceTests
{
    // ---- AC4 / AC7: the meadow reads the Season on ready ----

    [Fact]
    public void AC4_meadow_view_reads_the_season_from_the_session_on_ready()
    {
        var source = ReadMeadowView();

        Assert.Matches(@"\bDate\s*\.\s*Season\b", source);
        Assert.Matches(@"\bSeasonGrass\s*\.\s*For\s*\(", source);
    }

    [Fact]
    public void AC7_meadow_view_with_no_session_stays_in_summer()
    {
        var source = ReadMeadowView();

        Assert.Matches(@"\bSeason\s*\.\s*Summer\b", source);
    }

    // ---- AC5: the swap on NightCompleted ----

    [Fact]
    public void AC5_meadow_view_reapplies_on_night_completed_and_unsubscribes_on_exit_tree()
    {
        var source = ReadMeadowView();

        Assert.Matches(@"\bNightCompleted\s*\+=", source);
        Assert.Matches(@"\bNightCompleted\s*-=", source);
        Assert.Matches(@"\b_ExitTree\s*\(", source);
    }

    [Fact]
    public void AC5_farm_plot_view_passes_the_season_to_refresh()
    {
        var source = ReadScript("Farm", "FarmPlotView.cs");

        Assert.Matches(@"\bDate\s*\.\s*Season\b", source);

        var calls = CallArguments(source, ".Refresh(");
        Assert.NotEmpty(calls);
        Assert.All(calls, args => Assert.True(
            args.Count == 3,
            $"FarmPlotView calls .Refresh({string.Join(", ", args)}) with {args.Count} arguments, expected 3 (tile, stage, season)"));
    }

    [Fact]
    public void AC5_tile_view_refresh_takes_a_season()
    {
        var source = ReadScript("Farm", "TileView.cs");

        var signature = Regex.Match(source, @"\bpublic\s+void\s+Refresh\s*\(([^)]*)\)");
        Assert.True(signature.Success, "TileView has no `public void Refresh(...)`");
        Assert.Matches(@"\bSeason\s+season\b", signature.Groups[1].Value);

        Assert.Matches(@"\bTileLook\s*\.\s*For\s*\(\s*[\w.]+\s*,\s*[\w.]*[Ss]eason\w*\s*\)", source);
        Assert.Matches(@"\bSeasonGrass\b", source);
    }

    [Fact]
    public void AC5_season_swap_creates_no_nodes()
    {
        var source = ReadMeadowView();

        // The method(s) that apply a Season's look: block-bodied methods that use the SeasonGrass fields. _Ready
        // builds the meadow and may not be the one, as NightCompleted must reuse it.
        var methods = BlockMethods(source).ToList();
        Assert.Contains(methods, m => m.Name == "_Ready"); // the method scanner itself still works
        var appliers = methods
            .Where(m => m.Name != "_Ready")
            .Where(m => Regex.IsMatch(m.Body, @"\b(TuftHeightScale|ShowShadePatches)\b"))
            .ToList();
        Assert.True(appliers.Count > 0, "MeadowView has no method besides _Ready that applies TuftHeightScale / ShowShadePatches");

        foreach (var (name, body) in appliers)
        {
            foreach (var forbidden in new[] { @"\bAddChild\s*\(", @"\bnew\s+MeshInstance3D\b", @"\bnew\s+MultiMesh(Instance3D)?\b", @"\bQueueFree\s*\(" })
            {
                Assert.False(Regex.IsMatch(body, forbidden), $"MeadowView.{name} applies the Season but matches {forbidden}");
            }
        }
    }

    // ---- AC6: winter tufts and shade patches ----

    [Fact]
    public void AC6_meadow_view_toggles_shade_patches_and_scales_tuft_height()
    {
        var source = ReadMeadowView();

        Assert.Matches(@"\bShowShadePatches\b", source);
        Assert.Matches(@"\bTuftHeightScale\b", source);
    }

    [Fact]
    public void AC6_tile_view_applies_the_seasons_tuft_role_and_height()
    {
        // The Season's tuft role and height are baked into the grass detail mesh built from its SeasonGrass
        // (GrassTileArt, tested in GrassTileArtTests); the view swaps that mesh in for the Season.
        var source = ReadScript("Farm", "TileView.cs");

        Assert.Matches(@"\bGrassTileModels\s*\.\s*MeshFor\s*\(\s*_variant\s*,\s*grass\s*\)", source);
        Assert.Matches(@"\bApplyTufts\s*\(\s*SeasonGrass\.For\s*\(\s*season\s*\)\s*\)", source);
        Assert.Matches(@"\.\s*Tufts\b", source);
        Assert.Matches(@"\bTuftHeightScale\b", source);
    }

    // ---- AC8: never per frame ----

    [Fact]
    public void AC8_meadow_view_has_no_per_frame_update()
    {
        var source = ReadMeadowView();

        Assert.DoesNotMatch(@"\b_Process\s*\(", source);
        Assert.DoesNotMatch(@"\b_PhysicsProcess\s*\(", source);
    }

    // ---- helpers ----

    private static string ReadMeadowView()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game", "art", "farm", "MeadowView.cs");
        Assert.True(File.Exists(path), "game/art/farm/MeadowView.cs not found");
        return File.ReadAllText(path);
    }

    private static string ReadScript(string folder, string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "game", "scripts", folder, fileName);
        Assert.True(File.Exists(path), $"game/scripts/{folder}/{fileName} not found");
        return File.ReadAllText(path);
    }

    /// <summary>The top-level arguments of every call that starts with <paramref name="callStart"/> (ending in "(").</summary>
    private static List<List<string>> CallArguments(string source, string callStart)
    {
        var calls = new List<List<string>>();
        var from = 0;
        while ((from = source.IndexOf(callStart, from, StringComparison.Ordinal)) >= 0)
        {
            var open = from + callStart.Length - 1;
            var close = MatchingClose(source, open, '(', ')');
            var args = new List<string>();
            var depth = 0;
            var start = open + 1;
            for (var i = open + 1; i < close; i++)
            {
                var c = source[i];
                if (c is '(' or '[' or '{')
                {
                    depth++;
                }
                else if (c is ')' or ']' or '}')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    args.Add(source[start..i].Trim());
                    start = i + 1;
                }
            }

            var last = source[start..close].Trim();
            if (last.Length > 0 || args.Count > 0)
            {
                args.Add(last);
            }

            calls.Add(args);
            from = close;
        }

        return calls;
    }

    /// <summary>Every block-bodied method: its name and the text between its braces.</summary>
    private static IEnumerable<(string Name, string Body)> BlockMethods(string source)
    {
        var header = new Regex(
            @"(?:public|private|protected|internal)\s+(?:(?:static|override|virtual|async|partial|new)\s+)*[\w<>\[\]?,.\s]+?\s+(\w+)\s*\([^;{}]*?\)\s*\{");
        foreach (Match m in header.Matches(source))
        {
            var open = m.Index + m.Length - 1;
            var close = MatchingClose(source, open, '{', '}');
            yield return (m.Groups[1].Value, source[(open + 1)..close]);
        }
    }

    private static int MatchingClose(string source, int open, char openChar, char closeChar)
    {
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == openChar)
            {
                depth++;
            }
            else if (source[i] == closeChar && --depth == 0)
            {
                return i;
            }
        }

        Assert.Fail($"unbalanced '{openChar}' at offset {open}");
        return -1;
    }
}
