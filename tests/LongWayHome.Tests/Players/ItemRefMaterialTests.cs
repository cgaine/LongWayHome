using System.Text.Json;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Players;

/// <summary>
/// #152: <see cref="ItemRef.Material"/> and <see cref="ItemRef.IsValid"/> for the <c>material</c> kind (exactly one id
/// set, matching the kind), materials stacking to 99 (BR-K2), and the <see cref="MaterialId"/> and
/// <see cref="ItemKind"/> codes of canonical-values.md.
/// </summary>
public class ItemRefMaterialTests
{
    [Theory]
    [InlineData(MaterialId.Wood)]
    [InlineData(MaterialId.Sap)]
    [InlineData(MaterialId.Stone)]
    [InlineData(MaterialId.RoskPump)]
    public void Material_builds_a_valid_material_item_carrying_only_its_material_id(MaterialId material)
    {
        var item = ItemRef.Material(material);

        Assert.Equal(ItemKind.Material, item.Kind);
        Assert.Equal(material, item.MaterialId);
        Assert.Null(item.CropTypeId);
        Assert.Null(item.ToolId);
        Assert.True(item.IsValid);
    }

    [Fact]
    public void Material_items_of_the_same_id_are_equal_and_of_different_ids_are_not()
    {
        Assert.Equal(ItemRef.Material(MaterialId.Wood), ItemRef.Material(MaterialId.Wood));
        Assert.NotEqual(ItemRef.Material(MaterialId.Wood), ItemRef.Material(MaterialId.Sap));
    }

    public static TheoryData<string, ItemRef> Invalid => new()
    {
        { "material with no id", new ItemRef(ItemKind.Material, null, null, null) },
        { "material with a crop id too", new ItemRef(ItemKind.Material, CropTypeId.Emberroot, null, MaterialId.Wood) },
        { "material with a tool id too", new ItemRef(ItemKind.Material, null, ToolId.Axe, MaterialId.Wood) },
        { "material with only a crop id", new ItemRef(ItemKind.Material, CropTypeId.Emberroot, null, null) },
        { "seed with a material id too", new ItemRef(ItemKind.Seed, CropTypeId.Emberroot, null, MaterialId.Wood) },
        { "crop with a material id too", new ItemRef(ItemKind.Crop, CropTypeId.Emberroot, null, MaterialId.Wood) },
        { "tool with a material id too", new ItemRef(ItemKind.Tool, null, ToolId.Axe, MaterialId.Wood) },
    };

    [Theory]
    [MemberData(nameof(Invalid))]
    public void An_item_whose_ids_do_not_match_its_kind_is_invalid(string name, ItemRef item)
    {
        Assert.False(item.IsValid, name);

        // Only meaningful once the material case exists: a valid material item is told apart from these.
        Assert.True(ItemRef.Material(MaterialId.Wood).IsValid);
    }

    [Fact]
    public void A_material_stacks_to_99_in_one_slot_then_spills_to_the_next()
    {
        var backpack = new Backpack();

        Assert.Equal(0, backpack.Add(ItemRef.Material(MaterialId.Stone), 120));

        Assert.Equal((99, 21), (backpack.Slots[0].Quantity, backpack.Slots[1].Quantity));
        Assert.Equal(ItemRef.Material(MaterialId.Stone), backpack.Slots[1].Item);
    }

    [Theory]
    [InlineData(MaterialId.Wood, "wood")]
    [InlineData(MaterialId.Sap, "sap")]
    [InlineData(MaterialId.Stone, "stone")]
    [InlineData(MaterialId.RoskOre, "rosk_ore")]
    [InlineData(MaterialId.GrothOre, "groth_ore")]
    [InlineData(MaterialId.Glimmercap, "glimmercap")]
    [InlineData(MaterialId.Plank, "plank")]
    [InlineData(MaterialId.SealedPlank, "sealed_plank")]
    [InlineData(MaterialId.RoskPump, "rosk_pump")]
    public void Every_material_id_serializes_as_its_canonical_code(MaterialId material, string code) =>
        Assert.Equal($"\"{code}\"", JsonSerializer.Serialize(material, JsonGameDataSource.SerializerOptions));

    [Fact]
    public void The_material_ids_are_exactly_the_nine_canonical_values() =>
        Assert.Equal(
            ["Wood", "Sap", "Stone", "RoskOre", "GrothOre", "Glimmercap", "Plank", "SealedPlank", "RoskPump"],
            Enum.GetNames<MaterialId>());

    [Fact]
    public void The_material_kind_serializes_as_material() =>
        Assert.Equal("\"material\"", JsonSerializer.Serialize(ItemKind.Material, JsonGameDataSource.SerializerOptions));
}
