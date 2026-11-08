using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #152 AC11: materials in the hotbar and the Backpack menu read "&lt;label&gt; × &lt;count&gt;" with their
/// <c>material_&lt;code&gt;</c> icon. The labels are canonical-values.md's MaterialId display labels.
/// </summary>
public class MaterialTextTests
{
    private static readonly string[] Icons =
        ["res://art/icons/material_wood.svg", "res://art/icons/material_sap.svg", "res://art/icons/material_stone.svg"];

    [Theory]
    [InlineData(MaterialId.Wood, "Wood")]
    [InlineData(MaterialId.Sap, "Sap")]
    [InlineData(MaterialId.Stone, "Stone")]
    [InlineData(MaterialId.RoskOre, "Rosk ore")]
    [InlineData(MaterialId.GrothOre, "Groth ore")]
    [InlineData(MaterialId.Glimmercap, "Glimmercap")]
    [InlineData(MaterialId.Plank, "Plank")]
    [InlineData(MaterialId.SealedPlank, "Sealed plank")]
    [InlineData(MaterialId.RoskPump, "Rosk pump")]
    public void AC11_a_held_materials_label_is_its_canonical_display_label(MaterialId material, string label) =>
        Assert.Equal(label, HeldItem.MaterialOf(material).DisplayLabel);

    [Theory]
    [InlineData(MaterialId.Wood, 5, "Wood × 5")]
    [InlineData(MaterialId.Sap, 2, "Sap × 2")]
    [InlineData(MaterialId.Stone, 99, "Stone × 99")]
    public void AC11_a_material_slot_in_the_backpack_menu_reads_label_times_count(MaterialId material, int quantity, string expected)
    {
        var slot = new BackpackSlot(4, ItemRef.Material(material), quantity, null);

        Assert.Equal(expected, BackpackText.Label(slot));
    }

    [Theory]
    [InlineData(MaterialId.Wood, 5, "Wood × 5")]
    [InlineData(MaterialId.Stone, 3, "Stone × 3")]
    public void AC11_the_compact_hotbar_label_of_a_material_reads_label_times_count(MaterialId material, int quantity, string expected) =>
        Assert.Equal(expected, HotbarText.Compact(HeldItem.MaterialOf(material), quantity));

    [Fact]
    public void AC11_the_hotbar_shows_wood_sap_and_stone_with_their_counts_and_icons()
    {
        var session = TestGames.SessionWithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(2, ItemRef.Material(MaterialId.Wood), 5, null),
            new BackpackSlot(3, ItemRef.Material(MaterialId.Sap), 2, null),
            new BackpackSlot(4, ItemRef.Material(MaterialId.Stone), 3, null)));

        var row = session.Hotbar(0).Skip(2).Take(3).ToList();

        Assert.Equal(["Wood × 5", "Sap × 2", "Stone × 3"], row.Select(slot => slot.Label));
        Assert.Equal([5, 2, 3], row.Select(slot => slot.Quantity));
        Assert.Equal(Icons, row.Select(slot => slot.Item is { } item ? ItemIcons.For(item) : null));
        Assert.Equal(Icons, row.Select(slot => ItemIcons.For(slot.SlotItem!.Value)));
    }

    [Theory]
    [InlineData(MaterialId.Wood, "res://art/icons/material_wood.svg")]
    [InlineData(MaterialId.Sap, "res://art/icons/material_sap.svg")]
    [InlineData(MaterialId.Stone, "res://art/icons/material_stone.svg")]
    [InlineData(MaterialId.RoskOre, "res://art/icons/material_rosk_ore.svg")]
    [InlineData(MaterialId.SealedPlank, "res://art/icons/material_sealed_plank.svg")]
    public void AC11_a_material_items_icon_is_its_material_icon(MaterialId material, string icon) =>
        Assert.Equal(icon, ItemIcons.For(ItemRef.Material(material)));

    [Fact]
    public void AC11_the_backpack_menu_shows_wood_with_its_label_count_and_icon()
    {
        var session = TestGames.SessionWithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(7, ItemRef.Material(MaterialId.Wood), 12, null)));

        var slot = session.BackpackView().Slots[7];

        Assert.Equal(("Wood × 12", 12, "res://art/icons/material_wood.svg"), (slot.Label, slot.Quantity, slot.IconPath));
    }
}
