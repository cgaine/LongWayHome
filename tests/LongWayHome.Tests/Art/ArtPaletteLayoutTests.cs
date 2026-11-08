using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// <see cref="ArtPalette"/> behaviour the #76 acceptance tests don't reach: every swatch's UV, not only the three
/// sampled ones, role matching, and the encoder giving the same bytes every call (the committed PNG relies on it).
/// </summary>
public class ArtPaletteLayoutTests
{
    [Fact]
    public void Every_swatch_uv_centre_is_the_centre_of_its_own_8x8_block()
    {
        for (var i = 0; i < ArtPalette.Swatches.Count; i++)
        {
            var (u, v) = ArtPalette.UvCentre(ArtPalette.Swatches[i].Role);

            Assert.Equal(((i % 8) * 8 + 4) / 64f, u, 1e-6f);
            Assert.Equal(((i / 8) * 8 + 4) / 128f, v, 1e-6f);
        }
    }

    [Fact]
    public void Last_swatch_roof_far_sits_at_column_2_row_5()
    {
        var (u, v) = ArtPalette.UvCentre("roof-far");

        Assert.Equal(20f / 64, u, 1e-6f);
        Assert.Equal(44f / 128, v, 1e-6f);
    }

    [Theory]
    [InlineData("Grass-Lit")]
    [InlineData("")]
    [InlineData(" grass-lit")]
    public void Roles_match_exactly(string role)
    {
        var error = Assert.Throws<ArgumentException>(() => ArtPalette.UvCentre(role));

        Assert.Equal("role", error.ParamName);
    }

    [Fact]
    public void Every_swatch_fits_in_the_texture()
    {
        Assert.InRange(ArtPalette.Swatches.Count, 1, ArtPalette.Capacity);
    }

    [Fact]
    public void Encode_png_gives_the_same_bytes_every_call()
    {
        Assert.Equal(ArtPalette.EncodePng(), ArtPalette.EncodePng());
    }

    [Fact]
    public void Swatch_hexes_are_uppercase_six_digit_colours()
    {
        Assert.All(ArtPalette.Swatches, swatch => Assert.Matches("^#[0-9A-F]{6}$", swatch.Hex));
    }
}
