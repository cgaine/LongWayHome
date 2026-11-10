using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Infrastructure.Saves.Migrations;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #163: the save paths for Buildings the acceptance tests don't reach: a delivered material listed twice, a null
/// delivered entry, and the v1 → v2 step writing the new-Game Buildings explicitly.
/// </summary>
public sealed class BuildingSaveGuardTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private static JsonObject Entry(string id, int stage, params JsonNode?[] delivered) => new()
    {
        ["id"] = id,
        ["stage"] = stage,
        ["delivered"] = new JsonArray(delivered),
        ["deliveredGold"] = 0,
    };

    private static JsonObject Wood(int quantity) => new() { ["materialId"] = "wood", ["quantity"] = quantity };

    private void AssertUnreadable(JsonArray buildings)
    {
        var document = SaveDocuments.V2(SaveDocuments.GoodBackpack());
        document["buildings"] = buildings;
        _folder.WriteSlot(1, document);

        var thrown = Assert.Throws<SaveStoreException>(() => new JsonSaveStore(_folder.Path).Load(1));
        Assert.Equal(1, thrown.Slot);
    }

    [Fact]
    public void A_building_with_a_delivered_material_listed_twice_is_refused()
    {
        AssertUnreadable(new JsonArray(
            Entry("general_store", 1, Wood(3), Wood(4)),
            Entry("carpenter", 0),
            Entry("miras_house", 0),
            Entry("blacksmith", 0)));
    }

    [Fact]
    public void A_building_with_a_null_delivered_entry_is_refused()
    {
        AssertUnreadable(new JsonArray(
            Entry("general_store", 1, (JsonNode?)null),
            Entry("carpenter", 0),
            Entry("miras_house", 0),
            Entry("blacksmith", 0)));
    }

    [Fact]
    public void The_v1_to_v2_step_writes_the_new_game_buildings_in_enum_order()
    {
        var migrated = SaveV1ToV2.Migrate(SaveDocuments.V1([]));

        var expected = new JsonArray(
            Entry("general_store", 1), Entry("carpenter", 0), Entry("miras_house", 0), Entry("blacksmith", 0));
        Assert.True(
            JsonNode.DeepEquals(expected, migrated["buildings"]),
            $"buildings was {migrated["buildings"]?.ToJsonString()}");
    }
}
