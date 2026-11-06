using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// AC1 of #78: the three ground states and the target highlight are told apart by luminance contrast alone, which
/// colour-blind players can still see (ART_DIRECTION §2). Hexes are read from <see cref="ArtPalette"/> by role.
/// </summary>
public class TileReadabilityTests
{
    private static readonly string[] Grounds = ["grass-lit", "soil-tilled", "soil-watered"];

    [Fact]
    public void AC1_contrast_matches_known_values()
    {
        Assert.Equal(21.0, ArtContrast.Ratio("#FFFFFF", "#000000"), 3);
        Assert.Equal(21.0, ArtContrast.Ratio("#000000", "#FFFFFF"), 3);
        Assert.Equal(1.0, ArtContrast.Ratio("#7FA65A", "#7FA65A"), 6);
        Assert.Equal(1.0, ArtContrast.RelativeLuminance("#FFFFFF"), 6);
        Assert.Equal(0.0, ArtContrast.RelativeLuminance("#000000"), 6);

        // sRGB mid-grey #808080 has a WCAG relative luminance of about 0.2159.
        Assert.Equal(0.2159, ArtContrast.RelativeLuminance("#808080"), 3);
    }

    [Fact]
    public void AC1_soil_and_grass_pairs_contrast_at_least_1_8()
    {
        for (var i = 0; i < Grounds.Length; i++)
        {
            for (var j = i + 1; j < Grounds.Length; j++)
            {
                var ratio = ArtContrast.Ratio(Hex(Grounds[i]), Hex(Grounds[j]));
                Assert.True(ratio >= 1.8, $"{Grounds[i]} vs {Grounds[j]} contrast is {ratio:F2}, below 1.8");
            }
        }
    }

    [Fact]
    public void AC1_target_highlight_contrasts_at_least_2_5_with_each_ground()
    {
        foreach (var ground in Grounds)
        {
            var ratio = ArtContrast.Ratio(Hex("target-highlight"), Hex(ground));
            Assert.True(ratio >= 2.5, $"target-highlight vs {ground} contrast is {ratio:F2}, below 2.5");
        }
    }

    private static string Hex(string role) => ArtPalette.Swatches.Single(s => s.Role == role).Hex;
}
