using LongWayHome.Application.Games;
using LongWayHome.Domain.Residents;

namespace LongWayHome.Tests.Games;

/// <summary>#138: what Maggie and Clyde are called and say in the dialogue box (AC8, canonical-values.md).</summary>
public class ResidentLinesTests
{
    [Theory]
    [InlineData(ResidentId.Maggie, "Maggie")]
    [InlineData(ResidentId.Clyde, "Clyde")]
    [InlineData(ResidentId.Tom, "Tom")]
    [InlineData(ResidentId.June, "June")]
    [InlineData(ResidentId.Jesse, "Jesse")]
    [InlineData(ResidentId.Mira, "Mira")]
    public void AC8_Display_name_is_the_canonical_display_label(ResidentId id, string label)
    {
        Assert.Equal(label, ResidentLines.DisplayName(id));
    }

    [Fact]
    public void AC8_Maggies_greeting_is_her_default_line()
    {
        Assert.Equal(
            "Ember Bay's quieter than it used to be, but it's a good town. My store's open whenever you need it.",
            ResidentLines.Greeting(ResidentId.Maggie));
    }

    [Fact]
    public void AC8_Clydes_greeting_is_his_default_line()
    {
        Assert.Equal(
            "Fish bite best at first light. Mind the lake, though. It's deeper than it looks.",
            ResidentLines.Greeting(ResidentId.Clyde));
    }

    [Theory]
    [InlineData(ResidentId.Tom)]
    [InlineData(ResidentId.June)]
    [InlineData(ResidentId.Jesse)]
    [InlineData(ResidentId.Mira)]
    public void The_other_four_have_no_greeting_until_their_epics_add_one(ResidentId id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResidentLines.Greeting(id));
    }

    [Fact]
    public void A_value_outside_the_enum_has_no_display_name_or_greeting()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResidentLines.DisplayName((ResidentId)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => ResidentLines.Greeting((ResidentId)99));
    }
}
