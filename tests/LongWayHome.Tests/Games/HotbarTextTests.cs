using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The hotbar's compact labels (#95 AC8): used when the full labels with seed icons don't fit 1280 px.</summary>
public class HotbarTextTests
{
    [Theory]
    [InlineData(CropTypeId.Emberroot, 99, "Emberroot × 99")]
    [InlineData(CropTypeId.Dusktuber, 2, "Dusktuber × 2")]
    [InlineData(CropTypeId.Moonmelon, 99, "Moonmelon × 99")]
    [InlineData(CropTypeId.Sunbell, 10, "Sunbell × 10")]
    [InlineData(CropTypeId.Starcorn, 1, "Starcorn × 1")]
    public void AC8_compact_seed_label_drops_the_word_seeds(CropTypeId crop, int quantity, string expected) =>
        Assert.Equal(expected, HotbarText.Compact(HeldItem.SeedOf(crop), quantity));

    [Theory]
    [InlineData(ToolId.Hoe, "Hoe")]
    [InlineData(ToolId.WateringCan, "Watering can")]
    public void AC8_compact_tool_label_is_its_display_label(ToolId tool, string expected) =>
        Assert.Equal(expected, HotbarText.Compact(HeldItem.Of(tool), 0));

    [Fact]
    public void AC8_compact_seed_label_is_the_full_hotbar_label_without_seeds()
    {
        var session = TestGames.NewSession();
        session.Game.Player.Backpack.Add(ItemRef.Seed(CropTypeId.Moonmelon), 99);
        var seed = HeldItem.SeedOf(CropTypeId.Moonmelon);

        var full = session.Hotbar(seed).Single(slot => slot.Item == seed).Label;
        var compact = HotbarText.Compact(seed, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Moonmelon)));

        Assert.Equal(full.Replace(" seeds", ""), compact);
        Assert.True(compact.Length < full.Length);
    }

    [Fact]
    public void AC8_compact_label_of_a_null_item_throws() =>
        Assert.Throws<ArgumentNullException>(() => HotbarText.Compact(null!, 1));

    [Theory]
    [InlineData(0, 12, "Row 1/2")]
    [InlineData(1, 12, "Row 2/2")]
    [InlineData(0, 24, "Row 1/4")]
    [InlineData(5, 36, "Row 6/6")]
    public void AC10_row_label_is_the_1_based_row_of_capacity_over_6(int hotbarRow, int capacity, string expected) =>
        Assert.Equal(expected, HotbarText.Row(hotbarRow, capacity));
}
