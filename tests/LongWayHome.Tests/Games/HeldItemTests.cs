using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The HUD label of each held item, from canonical-values.md.</summary>
public class HeldItemTests
{
    [Theory]
    [InlineData(ToolId.Hoe, "Hoe")]
    [InlineData(ToolId.WateringCan, "Watering can")]
    public void Tool_label_is_its_canonical_display_label(ToolId tool, string label) =>
        Assert.Equal(label, HeldItem.Of(tool).DisplayLabel);

    [Theory]
    [InlineData(CropTypeId.Emberroot, "Emberroot seeds")]
    [InlineData(CropTypeId.Dusktuber, "Dusktuber seeds")]
    [InlineData(CropTypeId.Moonmelon, "Moonmelon seeds")]
    [InlineData(CropTypeId.Sunbell, "Sunbell seeds")]
    [InlineData(CropTypeId.Starcorn, "Starcorn seeds")]
    public void Seed_label_is_the_crop_type_label_plus_seeds(CropTypeId seed, string label) =>
        Assert.Equal(label, HeldItem.SeedOf(seed).DisplayLabel);

    [Fact]
    public void Every_crop_type_has_a_seed_label() =>
        Assert.All(Enum.GetValues<CropTypeId>(), seed => Assert.EndsWith(" seeds", HeldItem.SeedOf(seed).DisplayLabel));

    [Theory]
    [InlineData(null, null)]
    [InlineData(ToolId.Hoe, CropTypeId.Emberroot)]
    public void An_item_that_is_not_exactly_one_tool_or_seed_has_no_label(ToolId? tool, CropTypeId? seed) =>
        Assert.Throws<InvalidOperationException>(() => new HeldItem(tool, seed).DisplayLabel);
}
