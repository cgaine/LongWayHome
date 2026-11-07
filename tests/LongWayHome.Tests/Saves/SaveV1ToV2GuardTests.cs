using System.Text.Json.Nodes;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Infrastructure.Saves.Migrations;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// The save 1 → 2 step's other branches (#128): a version 1 file that was unreadable before stays unreadable, and the
/// step leaves a document it can't read for the loader to refuse.
/// </summary>
public sealed class SaveV1ToV2GuardTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private void AssertUnreadable(JsonObject document)
    {
        _folder.WriteSlot(1, document);
        var store = new JsonSaveStore(_folder.Path);

        Assert.Throws<SaveStoreException>(() => store.Load(1));
        Assert.Equal(SaveSlotState.Unreadable, store.List()[0].State);
    }

    private static JsonObject V1With(Action<JsonObject> edit)
    {
        var document = SaveDocuments.V1(new JsonArray(SaveDocuments.Stack("seed", "emberroot", 10)));
        edit(document["player"]!.AsObject());
        return document;
    }

    public static TheoryData<string, Action<JsonObject>> BrokenV1Players => new()
    {
        { "zero quantity", player => player["inventory"]![0]!["quantity"] = 0 },
        { "negative quantity", player => player["inventory"]![0]!["quantity"] = -4 },
        { "quantity not an integer", player => player["inventory"]![0]!["quantity"] = "ten" },
        { "unknown crop", player => player["inventory"]![0]!["cropTypeId"] = "nope" },
        { "unknown kind", player => player["inventory"]![0]!["kind"] = "gem" },
        { "tool kind stack", player => player["inventory"]![0]!["kind"] = "tool" },
        { "null stack", player => player["inventory"]![0] = null },
        { "null inventory", player => player["inventory"] = null },
        { "missing inventory", player => player.Remove("inventory") },
        { "null tools", player => player["tools"] = null },
        { "missing tools", player => player.Remove("tools") },
        { "null can", player => player["wateringCan"] = null },
    };

    [Theory]
    [MemberData(nameof(BrokenV1Players))]
    public void A_v1_save_that_was_unreadable_stays_unreadable(string because, Action<JsonObject> edit)
    {
        Assert.NotNull(because);

        AssertUnreadable(V1With(edit));
    }

    [Fact]
    public void A_v1_document_with_no_player_object_is_left_for_the_loader_to_refuse()
    {
        var document = SaveDocuments.V1([]);
        document["player"] = 5;

        var migrated = SaveV1ToV2.Migrate(document.DeepClone().AsObject());

        Assert.Equal(5, migrated["player"]!.GetValue<int>());
        AssertUnreadable(document);
    }

    [Fact]
    public void BR_P7_A_v1_save_listing_a_crop_twice_loads_with_each_stack_in_its_own_slot()
    {
        _folder.WriteSlot(1, SaveDocuments.V1(new JsonArray(
            SaveDocuments.Stack("seed", "emberroot", 4),
            SaveDocuments.Stack("seed", "emberroot", 6))));

        var backpack = new JsonSaveStore(_folder.Path).Load(1).Player.Backpack;

        Assert.Equal("2:Seed:Emberroot:4:-", TestGames.DescribeSlot(backpack.Slots[2]));
        Assert.Equal("3:Seed:Emberroot:6:-", TestGames.DescribeSlot(backpack.Slots[3]));
        Assert.Equal(10, backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
    }

    [Fact]
    public void A_v1_can_with_a_non_boolean_upgraded_flag_is_still_refused()
    {
        AssertUnreadable(V1With(player => player["wateringCan"]!["upgraded"] = "yes"));
    }

    [Fact]
    public void The_step_refuses_a_null_document() =>
        Assert.Throws<ArgumentNullException>(() => SaveV1ToV2.Migrate(null!));

    [Fact]
    public void A_v1_save_with_exactly_12_slots_of_items_and_tools_stays_at_capacity_12()
    {
        _folder.WriteSlot(1, SaveDocuments.V1(new JsonArray(SaveDocuments.Stack("crop", "emberroot", 10 * 99))));

        var backpack = new JsonSaveStore(_folder.Path).Load(1).Player.Backpack;

        Assert.Equal(12, backpack.Capacity);
        Assert.All(backpack.Slots.Skip(2), slot => Assert.Equal(99, slot.Quantity));
    }
}
