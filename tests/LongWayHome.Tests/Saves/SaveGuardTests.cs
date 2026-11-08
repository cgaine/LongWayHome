using System.Text.Json.Nodes;
using LongWayHome.Application.Games;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Games;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>Guards and error paths of the save code that the acceptance-criteria tests do not reach.</summary>
public sealed class SaveGuardTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path);

    private void SaveThenEdit(Action<JsonObject> edit)
    {
        Store().Save(1, SaveGames.Rich());
        var document = _folder.ReadSlot(1);
        edit(document);
        _folder.WriteSlot(1, document);
    }

    private void AssertUnreadable()
    {
        var store = Store();
        var thrown = Assert.Throws<SaveStoreException>(() => store.Load(1));
        Assert.DoesNotContain('\n', thrown.Reason);
        Assert.Equal(SaveSlotState.Unreadable, store.List()[0].State);
    }

    [Fact]
    public void Migrator_refuses_a_target_below_1()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SaveMigrator(0, []));
    }

    [Fact]
    public void Migrator_refuses_a_step_count_that_does_not_match_the_target()
    {
        Assert.Throws<ArgumentException>(() => new SaveMigrator(3, [doc => doc]));
        Assert.Throws<ArgumentException>(() => new SaveMigrator([]));
    }

    [Fact]
    public void Migrator_refuses_a_null_step()
    {
        Assert.Throws<ArgumentException>(() => new SaveMigrator(2, [null!]));
    }

    [Fact]
    public void Migrator_refuses_a_null_document()
    {
        Assert.Throws<ArgumentNullException>(() => SaveMigrator.Default.Migrate(null!));
    }

    public static TheoryData<JsonNode?> RefusedVersions => new()
    {
        JsonValue.Create(3),
        JsonValue.Create(0),
        JsonValue.Create("1"),
        JsonValue.Create(1.5),
        new JsonArray(),
        null,
    };

    [Theory]
    [MemberData(nameof(RefusedVersions))]
    public void Migrator_refuses_a_bad_version(JsonNode? version)
    {
        var document = new JsonObject { ["saveVersion"] = version?.DeepClone() };

        Assert.Throws<InvalidDataException>(() => SaveMigrator.Default.Migrate(document));
    }

    [Fact]
    public void Migrator_sets_the_version_after_a_step_that_does_not()
    {
        var migrator = new SaveMigrator(2, [doc => doc]);

        var result = migrator.Migrate(new JsonObject { ["saveVersion"] = 1 });

        Assert.Equal(2, result["saveVersion"]!.GetValue<int>());
    }

    [Fact]
    public void Migrator_refuses_a_step_that_returns_null()
    {
        var migrator = new SaveMigrator(2, [_ => null!]);

        Assert.Throws<InvalidDataException>(() => migrator.Migrate(new JsonObject { ["saveVersion"] = 1 }));
    }

    [Fact]
    public void Store_refuses_a_null_folder_and_a_null_game()
    {
        Assert.Throws<ArgumentNullException>(() => new JsonSaveStore(null!));
        Assert.Throws<ArgumentNullException>(() => Store().Save(1, null!));
    }

    [Fact]
    public void A_missing_main_file_loads_the_bak()
    {
        var store = Store();
        store.Save(1, SaveGames.Plain(gold: 111));
        store.Save(1, SaveGames.Plain(gold: 222));
        File.Delete(_folder.SlotFile(1));

        Assert.Equal(111, store.Load(1).Player.Gold.Value);
        Assert.Equal(SaveSlotState.Saved, store.List()[0].State);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("42")]
    public void A_file_that_is_not_an_object_is_unreadable(string contents)
    {
        File.WriteAllText(_folder.SlotFile(1), contents);

        AssertUnreadable();
    }

    public static TheoryData<string, Action<JsonObject>> BrokenFiles => new()
    {
        { "null player", document => document["player"] = null },
        { "null farm plot", document => document["farmPlot"] = null },
        { "null shipping bin", document => document["shippingBin"] = null },
        { "null date", document => document["date"] = null },
        { "null backpack", document => document["player"]!["backpack"] = null },
        { "null can", document => document["player"]!["wateringCan"] = null },
        { "null backpack slots", document => document["player"]!["backpack"]!["slots"] = null },
        { "null tiles", document => document["farmPlot"]!["tiles"] = null },
        { "null tile", document => document["farmPlot"]!["tiles"]![0] = null },
        { "null tile position", document => document["farmPlot"]!["tiles"]![0]!["position"] = null },
        { "null bin items", document => document["shippingBin"]!["items"] = null },
        { "null bin item", document => document["shippingBin"]!["items"]![0] = null },
        { "null slot", document => document["player"]!["backpack"]!["slots"]![2] = null },
        { "null slot item id", document => document["player"]!["backpack"]!["slots"]![2]!["item"]!["id"] = null },
        { "unknown slot tier", document => document["player"]!["backpack"]!["slots"]![0]!["toolTier"] = "mithril" },
        { "negative clock", document => document["clock"] = -1 },
        { "missing clock", document => document.Remove("clock") },
        { "unknown field", document => document["extra"] = 1 },
        {
            "duplicate tool", document => document["player"]!["backpack"]!["slots"]![1]!["item"] =
                new JsonObject { ["kind"] = "tool", ["id"] = "hoe" }
        },
        // Sunbell (3) | Starcorn (4) is 7, not a CropTypeId: the enum converter parses it, the store refuses it.
        { "combined enum", document => document["shippingBin"]!["items"]![0]!["cropTypeId"] = "sunbell, starcorn" },
        { "unknown tool", document => document["player"]!["backpack"]!["slots"]![0]!["item"]!["id"] = "axe" },
        { "unknown crop in a slot", document => document["player"]!["backpack"]!["slots"]![2]!["item"]!["id"] = "nope" },
        { "unknown item kind", document => document["player"]!["backpack"]!["slots"]![2]!["item"]!["kind"] = "material" },
        {
            "duplicate slot", document => document["player"]!["backpack"]!["slots"]!.AsArray().Add(
                document["player"]!["backpack"]!["slots"]![2]!.DeepClone())
        },
        { "old tools field", document => document["player"]!["tools"] = new JsonArray("hoe", "watering_can") },
        {
            "duplicate shipped item", document => document["shippingBin"]!["items"]!.AsArray().Add(
                document["shippingBin"]!["items"]![0]!.DeepClone())
        },
        { "wrong width", document => document["farmPlot"]!["width"] = 13 },
        { "wrong height", document => document["farmPlot"]!["height"] = 8 },
        { "position off the plot", document => document["farmPlot"]!["tiles"]![0]!["position"]!["x"] = LongWayHome.Domain.Farming.FarmPlot.Width },
        { "watered grass", document => document["farmPlot"]!["tiles"]![0]!["watered"] = true },
        { "negative gold", document => document["player"]!["gold"] = -1 },
        { "energy above the maximum", document => document["player"]!["energy"] = 151 },
        { "year 0", document => document["date"]!["year"] = 0 },
        { "bad id", document => document["id"] = "not a guid" },
        { "bad saved at", document => document["savedAt"] = "yesterday" },
        { "gold overflow", document => document["player"]!["gold"] = 3_000_000_000L },
    };

    [Theory]
    [MemberData(nameof(BrokenFiles))]
    public void A_file_that_breaks_the_format_or_a_rule_is_unreadable(string because, Action<JsonObject> edit)
    {
        Assert.NotNull(because);
        SaveThenEdit(edit);

        AssertUnreadable();
    }

    [Fact]
    public void Session_constructor_refuses_bad_arguments()
    {
        var game = SaveGames.Plain();
        var data = TestGames.Shipped;
        var random = new FixedRandom(99);
        var store = Store();
        var time = TimeProvider.System;

        Assert.Throws<ArgumentNullException>(() => new GameSession(null!, data, random, store, 1, time));
        Assert.Throws<ArgumentNullException>(() => new GameSession(game, null!, random, store, 1, time));
        Assert.Throws<ArgumentNullException>(() => new GameSession(game, data, null!, store, 1, time));
        Assert.Throws<ArgumentNullException>(() => new GameSession(game, data, random, null!, 1, time));
        Assert.Throws<ArgumentNullException>(() => new GameSession(game, data, random, store, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(game, data, random, store, 0, time));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(game, data, random, store, 4, time));
    }

    [Fact]
    public void Night_refuses_a_null_save_step()
    {
        Assert.Throws<ArgumentNullException>(
            () => NightProcessor.Run(SaveGames.Plain(), TestGames.Shipped, DayEndReason.Slept, new FixedRandom(99), null!));
    }

    [Fact]
    public void A_session_without_a_store_raises_no_save_failure()
    {
        var session = new GameSession(SaveGames.Plain(), TestGames.Shipped, new FixedRandom(99));
        var failures = 0;
        session.SaveFailed += _ => failures++;

        session.Sleep();

        Assert.Equal(0, failures);
    }

    [Fact]
    public void A_session_without_a_store_leaves_saved_at_as_it_was()
    {
        var game = SaveGames.Plain();
        var savedAt = game.SavedAt;
        var session = new GameSession(game, TestGames.Shipped, new FixedRandom(99));

        session.Sleep();

        Assert.Equal(savedAt, game.SavedAt);
    }

    [Theory]
    [MemberData(nameof(NightSaveTests.StoreFailures), MemberType = typeof(NightSaveTests))]
    public void A_failed_save_leaves_saved_at_as_it_was(Exception failure)
    {
        var game = SaveGames.Rich(clock: 900);
        var session = new GameSession(
            game, TestGames.Shipped, new FixedRandom(99), new ThrowingSaveStore(failure), 1,
            new FixedTimeProvider(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        session.Sleep();

        Assert.Equal(SaveGames.SavedAt, game.SavedAt);
    }

    [Fact]
    public void A_store_failure_other_than_io_is_not_swallowed()
    {
        var session = new GameSession(
            SaveGames.Plain(), TestGames.Shipped, new FixedRandom(99), new ThrowingSaveStore(new InvalidOperationException("bug")), 1, TimeProvider.System);

        Assert.Throws<InvalidOperationException>(() => session.Sleep());
    }

    [Fact]
    public void Mark_saved_sets_saved_at()
    {
        var game = SaveGames.Plain();
        var at = new DateTimeOffset(2027, 1, 2, 3, 4, 5, TimeSpan.Zero);

        game.MarkSaved(at);

        Assert.Equal(at, game.SavedAt);
    }
}
