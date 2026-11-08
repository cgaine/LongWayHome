using LongWayHome.Application.Games;
using LongWayHome.Domain.Residents;

namespace LongWayHome.Tests.Games;

/// <summary>#138: the canonical <see cref="ResidentId"/> codes the save and the scenes' <c>ResidentView</c> use
/// (canonical-values.md).</summary>
public class ResidentCodesTests
{
    [Theory]
    [InlineData(ResidentId.Maggie, "maggie")]
    [InlineData(ResidentId.Clyde, "clyde")]
    [InlineData(ResidentId.Tom, "tom")]
    [InlineData(ResidentId.June, "june")]
    [InlineData(ResidentId.Jesse, "jesse")]
    [InlineData(ResidentId.Mira, "mira")]
    public void AC2_every_resident_round_trips_through_its_canonical_code(ResidentId id, string code)
    {
        Assert.Equal(code, ResidentCodes.Code(id));
        Assert.True(ResidentCodes.TryParse(code, out var parsed));
        Assert.Equal(id, parsed);
    }

    [Fact]
    public void AC2_the_codes_in_enum_order_are_maggie_clyde_tom_june_jesse_mira()
    {
        Assert.Equal(
            ["maggie", "clyde", "tom", "june", "jesse", "mira"],
            Enum.GetValues<ResidentId>().Select(ResidentCodes.Code).ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Maggie")]
    [InlineData("CLYDE")]
    [InlineData(" tom")]
    [InlineData("bob")]
    [InlineData("0")]
    public void AC5_anything_but_an_exact_code_is_not_a_resident(string? code)
    {
        Assert.False(ResidentCodes.TryParse(code, out var id));
        Assert.Equal(default, id);
    }

    [Fact]
    public void A_value_outside_the_enum_has_no_code()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResidentCodes.Code((ResidentId)99));
    }
}
