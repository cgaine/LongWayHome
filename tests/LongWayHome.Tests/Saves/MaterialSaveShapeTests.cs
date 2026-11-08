using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #152: a material Slot saves as <c>{ "kind": "material", "materialId": "&lt;code&gt;" }</c> with no <c>id</c>, other
/// items keep <c>{ "kind", "id" }</c> with no <c>materialId</c>, and a saved item that mixes the two, or a material with
/// no <c>materialId</c>, is refused.
/// </summary>
public sealed class MaterialSaveShapeTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path, []);

    private JsonObject SavedItem(ItemRef item, Action<JsonObject>? edit = null)
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack(new BackpackSlot(5, item, 3, null)));
        Store().Save(1, game);
        var document = _folder.ReadSlot(1);
        var saved = document["player"]!["backpack"]!["slots"]!.AsArray()
            .Select(entry => entry!.AsObject())
            .Single(entry => entry["index"]!.GetValue<int>() == 5)["item"]!.AsObject();
        if (edit is not null)
        {
            edit(saved);
            _folder.WriteSlot(1, document);
        }

        return saved;
    }

    [Fact]
    public void A_material_is_saved_with_materialId_and_no_id()
    {
        var item = SavedItem(ItemRef.Material(MaterialId.Wood));

        Assert.Equal("wood", item["materialId"]!.GetValue<string>());
        Assert.False(item.ContainsKey("id"));
    }

    [Fact]
    public void A_crop_is_still_saved_with_id_and_no_materialId()
    {
        var item = SavedItem(ItemRef.Crop(CropTypeId.Emberroot));

        Assert.Equal("emberroot", item["id"]!.GetValue<string>());
        Assert.False(item.ContainsKey("materialId"));
    }

    [Fact]
    public void A_saved_material_with_an_id_is_refused()
    {
        SavedItem(ItemRef.Material(MaterialId.Wood), item => item["id"] = "wood");

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }

    [Fact]
    public void A_saved_material_without_a_materialId_is_refused()
    {
        SavedItem(ItemRef.Material(MaterialId.Wood), item => item.Remove("materialId"));

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }

    [Fact]
    public void A_saved_crop_with_a_materialId_is_refused()
    {
        SavedItem(ItemRef.Crop(CropTypeId.Emberroot), item => item["materialId"] = "wood");

        Assert.Throws<SaveStoreException>(() => Store().Load(1));
    }
}
