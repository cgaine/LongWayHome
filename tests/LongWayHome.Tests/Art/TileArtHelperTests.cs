using LongWayHome.Application.Art;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The branches of #78's art helpers that the acceptance-criteria tests don't drive: the palette's hex-by-role
/// lookup the target marker reads its colours from, malformed hexes, and the native-plant rule of the decoration.
/// </summary>
public class TileArtHelperTests
{
    [Fact]
    public void Palette_hex_by_role_matches_the_swatch_list()
    {
        Assert.Equal("#FFF6E0", ArtPalette.Hex("target-highlight"));
        Assert.Equal("#E9773A", ArtPalette.Hex("ember"));
        Assert.All(ArtPalette.Swatches, s => Assert.Equal(s.Hex, ArtPalette.Hex(s.Role)));
    }

    [Fact]
    public void Palette_hex_of_an_unknown_role_throws()
    {
        Assert.Throws<ArgumentException>(() => ArtPalette.Hex("not-a-role"));
    }

    [Theory]
    [InlineData("FFFFFF")]
    [InlineData("#FFF")]
    [InlineData("#GG0000")]
    [InlineData("")]
    public void Contrast_rejects_a_malformed_hex(string hex)
    {
        Assert.Throws<ArgumentException>(() => ArtContrast.RelativeLuminance(hex));
        Assert.Throws<ArgumentException>(() => ArtContrast.Ratio(hex, "#000000"));
    }

    [Fact]
    public void Native_plant_only_where_the_hash_is_a_multiple_of_six()
    {
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                var position = new GridPosition(x, y);
                var expected = unchecked((uint)TileDecoration.Hash(position)) % 6 == 0;

                Assert.Equal(expected, TileDecoration.For(position).NativePlant != NativePlantKind.None);
            }
        }
    }
}
