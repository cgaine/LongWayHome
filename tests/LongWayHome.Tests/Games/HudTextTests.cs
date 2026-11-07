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

    // ---- #95 AC4: the crops row's entries ----

    private static List<InventoryStack> DusktuberThenEmberrootWithSeeds() =>
    [
        new(ItemKind.Crop, CropTypeId.Dusktuber, 2),
        new(ItemKind.Seed, CropTypeId.Emberroot, 10),
        new(ItemKind.Crop, CropTypeId.Emberroot, 9),
    ];

    [Fact]
    public void AC4_Crops_entries_are_one_per_crop_stack_in_crop_type_order_without_seeds()
    {
        var entries = CropsText.Entries(DusktuberThenEmberrootWithSeeds(), TestGames.Shipped.CropTypes);

        Assert.Equal(
            [(CropTypeId.Emberroot, "Emberroot × 9"), (CropTypeId.Dusktuber, "Dusktuber × 2")],
            entries);
    }

    [Fact]
    public void AC4_Crops_entries_are_empty_with_no_stacks() =>
        Assert.Empty(CropsText.Entries([], TestGames.Shipped.CropTypes));

    [Fact]
    public void AC4_Crops_entries_are_empty_with_only_seeds()
    {
        var stacks = new List<InventoryStack> { new(ItemKind.Seed, CropTypeId.Emberroot, 10) };

        Assert.Empty(CropsText.Entries(stacks, TestGames.Shipped.CropTypes));
    }

    [Fact]
    public void AC4_Crops_entry_without_data_row_uses_canonical_label()
    {
        var stacks = new List<InventoryStack> { new(ItemKind.Crop, CropTypeId.Moonmelon, 3) };

        var entries = CropsText.Entries(stacks, TestGames.ShippedWithout(CropTypeId.Moonmelon).CropTypes);

        Assert.Equal([(CropTypeId.Moonmelon, "Moonmelon × 3")], entries);
    }

    [Fact]
    public void AC4_Crops_text_is_unchanged_for_the_same_stacks()
    {
        Assert.Equal(
            "Crops: Emberroot × 9, Dusktuber × 2",
            CropsText.For(DusktuberThenEmberrootWithSeeds(), TestGames.Shipped.CropTypes));
        Assert.Equal("Crops: none", CropsText.For([], TestGames.Shipped.CropTypes));
    }

    [Fact]
    public void AC4_Crops_text_lists_the_entries_texts()
    {
        var stacks = DusktuberThenEmberrootWithSeeds();
        var entries = CropsText.Entries(stacks, TestGames.Shipped.CropTypes);

        Assert.NotEmpty(entries);
        Assert.Equal(
            $"Crops: {string.Join(", ", entries.Select(e => e.Text))}",
            CropsText.For(stacks, TestGames.Shipped.CropTypes));
    }

    // ---- #95 AC7: the crops row's leading label ----

    [Fact]
    public void AC7_Crops_heading_is_Crops_colon_when_crops_are_carried()
    {
        var entries = CropsText.Entries(DusktuberThenEmberrootWithSeeds(), TestGames.Shipped.CropTypes);

        Assert.Equal("Crops:", CropsText.Heading(entries));
    }

    [Fact]
    public void AC7_Crops_heading_is_Crops_none_when_none_are_carried() =>
        Assert.Equal("Crops: none", CropsText.Heading(CropsText.Entries([], TestGames.Shipped.CropTypes)));

    [Fact]
    public void AC7_Crops_row_entries_are_separated_by_a_comma_and_space() =>
        Assert.Equal(", ", CropsText.EntrySeparator);

    [Fact]
    public void AC4_Crops_entries_null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => CropsText.Entries((IReadOnlyList<InventoryStack>)null!, TestGames.Shipped.CropTypes));
        Assert.Throws<ArgumentNullException>(() => CropsText.Entries([], null!));
        Assert.Throws<ArgumentNullException>(() => CropsText.Heading(null!));
    }

    [Fact]
    public void AC17_Crops_text_from_the_backpack_matches_the_text_for_the_same_crops()
    {
        var backpack = TestGames.BackpackWith(
            (ItemRef.Crop(CropTypeId.Dusktuber), 2),
            (ItemRef.Seed(CropTypeId.Emberroot), 10),
            (ItemRef.Crop(CropTypeId.Emberroot), 9));

        var text = CropsText.For(backpack, TestGames.Shipped.CropTypes);

        Assert.Equal("Crops: Emberroot × 9, Dusktuber × 2", text);
    }

    [Fact]
    public void AC17_Crops_text_from_a_backpack_with_no_crops_is_Crops_none()
    {
        Assert.Equal("Crops: none", CropsText.For(TestGames.StartingBackpack(), TestGames.Shipped.CropTypes));
    }

    [Fact]
    public void AC17_Crops_entries_from_the_backpack_are_one_per_crop_type_in_crop_type_order()
    {
        var backpack = TestGames.BackpackWith(
            (ItemRef.Crop(CropTypeId.Moonmelon), 1),
            (ItemRef.Crop(CropTypeId.Emberroot), 3));

        var entries = CropsText.Entries(backpack, TestGames.ShippedWithout(CropTypeId.Moonmelon).CropTypes);

        Assert.Equal(
            [(CropTypeId.Emberroot, "Emberroot × 3"), (CropTypeId.Moonmelon, "Moonmelon × 1")],
            entries);
    }

    [Fact]
    public void BR_P7_Crops_split_across_slots_are_one_entry_with_their_total()
    {
        var backpack = TestGames.BackpackWith(
            (ItemRef.Crop(CropTypeId.Emberroot), 150),
            (ItemRef.Crop(CropTypeId.Dusktuber), 2));

        Assert.Equal(
            "Crops: Emberroot × 150, Dusktuber × 2",
            CropsText.For(backpack, TestGames.Shipped.CropTypes));
    }

    [Fact]
    public void Shop_gold_text_is_the_hud_gold_text() =>
        Assert.Equal(GoldText.For(250), ShopText.Gold(250));

    [Fact]
    public void Crops_text_rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => CropsText.For((IReadOnlyList<InventoryStack>)null!, TestGames.Shipped.CropTypes));
        Assert.Throws<ArgumentNullException>(() => CropsText.For([], null!));
    }
}
