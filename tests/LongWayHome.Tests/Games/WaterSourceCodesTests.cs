using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>#137: the canonical <see cref="WaterSource"/> codes the scenes' <c>WaterRefill</c> nodes name (BR-A2,
/// canonical-values.md).</summary>
public class WaterSourceCodesTests
{
    [Theory]
    [InlineData(WaterSource.Pond, "pond")]
    [InlineData(WaterSource.River, "river")]
    [InlineData(WaterSource.Lake, "lake")]
    public void BR_A2_every_water_source_round_trips_through_its_canonical_code(WaterSource source, string code)
    {
        Assert.Equal(code, WaterSourceCodes.Code(source));
        Assert.True(WaterSourceCodes.TryParse(code, out var parsed));
        Assert.Equal(source, parsed);
    }

    [Fact]
    public void BR_A2_the_three_sources_are_exactly_pond_river_and_lake()
    {
        Assert.Equal(
            ["pond", "river", "lake"],
            Enum.GetValues<WaterSource>().Select(WaterSourceCodes.Code).ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Pond")]
    [InlineData("RIVER")]
    [InlineData(" lake")]
    [InlineData("sea")]
    public void Anything_but_an_exact_code_is_not_a_water_source(string? code)
    {
        Assert.False(WaterSourceCodes.TryParse(code, out var source));
        Assert.Equal(default, source);
    }

    [Fact]
    public void A_value_outside_the_enum_has_no_code()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WaterSourceCodes.Code((WaterSource)99));
    }
}
