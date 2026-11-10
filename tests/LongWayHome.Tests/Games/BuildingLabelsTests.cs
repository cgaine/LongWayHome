using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Tests.Games;

/// <summary>#165 AC6: the Building names and the wake-up line for each rebuilt Building.</summary>
public class BuildingLabelsTests
{
    [Theory]
    [InlineData(BuildingId.GeneralStore, "general store")]
    [InlineData(BuildingId.Carpenter, "carpenter")]
    [InlineData(BuildingId.MirasHouse, "Mira's house")]
    [InlineData(BuildingId.Blacksmith, "blacksmith")]
    public void AC6_every_Building_has_its_label(BuildingId id, string expected) =>
        Assert.Equal(expected, BuildingLabels.Label(id));

    [Theory]
    [InlineData(BuildingId.GeneralStore, "The general store has been rebuilt.")]
    [InlineData(BuildingId.Carpenter, "The carpenter has been rebuilt.")]
    [InlineData(BuildingId.MirasHouse, "Mira's house has been rebuilt.")]
    [InlineData(BuildingId.Blacksmith, "The blacksmith has been rebuilt.")]
    public void AC6_every_Building_has_its_rebuilt_line(BuildingId id, string expected) =>
        Assert.Equal(expected, BuildingLabels.RebuiltLine(id));

    [Fact]
    public void AC6_the_rebuilt_text_for_the_store_is_its_one_line()
    {
        Assert.Equal("The general store has been rebuilt.", BuildingLabels.RebuiltText([BuildingId.GeneralStore]));
    }

    [Fact]
    public void AC6_the_rebuilt_text_joins_one_line_per_Building_with_newlines_in_the_given_order()
    {
        var text = BuildingLabels.RebuiltText([BuildingId.GeneralStore, BuildingId.Carpenter, BuildingId.MirasHouse]);

        Assert.Equal(
            "The general store has been rebuilt.\nThe carpenter has been rebuilt.\nMira's house has been rebuilt.",
            text);
    }

    [Fact]
    public void AC6_a_Night_with_no_rebuild_has_empty_rebuilt_text()
    {
        Assert.Equal("", BuildingLabels.RebuiltText([]));
    }
}
