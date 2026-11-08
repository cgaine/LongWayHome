using LongWayHome.Application.Saves;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #152 AC10: materials save in Backpack Slots as <c>{ "kind": "material", … "&lt;code&gt;" }</c> and load back.
/// </summary>
public sealed class MaterialSaveTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path, []);

    /// <summary>A Game whose Backpack holds every material, one Slot each, in Slots 3 to 11.</summary>
    private static LongWayHome.Domain.Games.Game GameWithEveryMaterial() =>
        TestGames.WithBackpack(TestGames.RestoredBackpack(
            Enum.GetValues<MaterialId>()
                .Select((material, index) => new BackpackSlot(index + 3, ItemRef.Material(material), 10 + index, null))
                .ToArray()));

    [Fact]
    public void AC10_materials_round_trip_through_a_save_in_their_backpack_slots()
    {
        var game = GameWithEveryMaterial();

        Store().Save(1, game);
        var loaded = Store().Load(1);

        Assert.Equal(TestGames.Describe(game.Player.Backpack), TestGames.Describe(loaded.Player.Backpack));
        foreach (var material in Enum.GetValues<MaterialId>())
        {
            Assert.Equal(
                game.Player.Backpack.Quantity(ItemRef.Material(material)),
                loaded.Player.Backpack.Quantity(ItemRef.Material(material)));
        }
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
    public void AC10_a_material_slot_is_saved_with_kind_material_and_its_canonical_code(MaterialId material, string code)
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(5, ItemRef.Material(material), 42, null)));

        Store().Save(1, game);

        var slot = _folder.ReadSlot(1)["player"]!["backpack"]!["slots"]!.AsArray()
            .Select(entry => entry!.AsObject())
            .Single(entry => entry["index"]!.GetValue<int>() == 5);
        var item = slot["item"]!.AsObject();
        Assert.Equal("material", item["kind"]!.GetValue<string>());

        // The issue names the field materialId; the existing seed/crop/tool format names it id. Either carries the code.
        var id = (item["materialId"] ?? item["id"])!.GetValue<string>();
        Assert.Equal(code, id);
        Assert.Equal(42, slot["quantity"]!.GetValue<int>());
    }

    [Fact]
    public void AC10_a_saved_material_slot_with_an_unknown_code_is_refused()
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack(
            new BackpackSlot(5, ItemRef.Material(MaterialId.Wood), 1, null)));
        Store().Save(1, game);
        var document = _folder.ReadSlot(1);
        var item = document["player"]!["backpack"]!["slots"]!.AsArray()
            .Select(entry => entry!.AsObject())
            .Single(entry => entry["index"]!.GetValue<int>() == 5)["item"]!.AsObject();
        foreach (var key in new[] { "materialId", "id" })
        {
            if (item.ContainsKey(key))
            {
                item[key] = "oak";
            }
        }

        _folder.WriteSlot(1, document);

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }
}
