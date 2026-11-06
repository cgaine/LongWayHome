using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The HUD gold and crops text: <see cref="GoldText.For"/> and <see cref="CropsText.For"/>.</summary>
public class HudTextTests
{
    [Theory]
    [InlineData(100, "Gold 100")]
    [InlineData(0, "Gold 0")]
    public void AC1_Gold_text_is_Gold_and_the_amount(int gold, string expected) =>
        Assert.Equal(expected, GoldText.For(gold));

    [Fact]
    public void BR_P5_Gold_text_shows_the_held_value_as_is() =>
        Assert.Equal("Gold 12345", GoldText.For(12345));

    [Fact]
    public void AC2_New_game_inventory_has_no_crops()
    {
        var session = TestGames.NewSession();

        var text = CropsText.For(session.Game.Player.Inventory.Stacks, TestGames.Shipped.CropTypes);

        Assert.Equal("Crops: none", text);
    }

    [Fact]
    public void AC3_Crops_are_listed_in_crop_type_order_without_seeds()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Crop, CropTypeId.Dusktuber, 2);
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 9);
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 10);

        var text = CropsText.For(inventory.Stacks, TestGames.Shipped.CropTypes);

        Assert.Equal("Crops: Emberroot × 9, Dusktuber × 2", text);
    }

    [Fact]
    public void AC3_Crops_are_listed_in_crop_type_order_whatever_order_the_stacks_come_in()
    {
        var stacks = new List<InventoryStack>
        {
            new(ItemKind.Crop, CropTypeId.Dusktuber, 2),
            new(ItemKind.Seed, CropTypeId.Emberroot, 10),
            new(ItemKind.Crop, CropTypeId.Emberroot, 9),
        };

        var text = CropsText.For(stacks, TestGames.Shipped.CropTypes);

        Assert.Equal("Crops: Emberroot × 9, Dusktuber × 2", text);
    }

    [Fact]
    public void Crop_without_data_row_uses_canonical_label()
    {
        var stacks = new List<InventoryStack> { new(ItemKind.Crop, CropTypeId.Moonmelon, 3) };

        var text = CropsText.For(stacks, TestGames.ShippedWithout(CropTypeId.Moonmelon).CropTypes);

        Assert.Equal("Crops: Moonmelon × 3", text);
    }

    [Fact]
    public void Crop_without_data_row_uses_canonical_label_with_no_rows_at_all()
    {
        var stacks = new List<InventoryStack> { new(ItemKind.Crop, CropTypeId.Starcorn, 1) };

        var text = CropsText.For(stacks, new Dictionary<CropTypeId, CropType>());

        Assert.Equal("Crops: Starcorn × 1", text);
    }

    [Fact]
    public void Shop_gold_text_is_the_hud_gold_text() =>
        Assert.Equal(GoldText.For(250), ShopText.Gold(250));

    [Fact]
    public void Crops_text_rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => CropsText.For(null!, TestGames.Shipped.CropTypes));
        Assert.Throws<ArgumentNullException>(() => CropsText.For([], null!));
    }
}
