using System.Text.Json.Nodes;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #153 AC8: a save (still version 2) holds the Game's NodeRegrowths as <c>nodeRegrowths</c> of
/// <c>{ spawnId, clearedOn: { year, season, day } }</c>. A file without it loads with none; an entry whose spawn point
/// isn't in <c>nodes.json</c> is dropped; a malformed one is refused, like <c>residents</c>.
/// </summary>
public sealed class NodeRegrowthSaveTests : IDisposable
{
    private static readonly NodeSpawn[] Spawns =
    [
        NodeGames.Spawn(2, 2, NodeKind.Tree),
        NodeGames.Spawn(29, 8, NodeKind.Rock),
        NodeGames.Spawn(42, 0, NodeKind.Stump),
    ];

    private static readonly NodeRegrowth[] Regrowths =
    [
        new("farm-2-2", new GameDate(2, Season.Summer, 13)),
        new("farm-29-8", new GameDate(2, Season.Spring, 28)),
    ];

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path, Spawns);

    /// <summary>A rich Game whose tree and rock were cleared (so not standing), the stump still standing.</summary>
    private static GameState GameWithRegrowths(params NodeRegrowth[] regrowths)
    {
        var rich = SaveGames.Rich();
        return new GameState(
            rich.Id,
            rich.Date,
            rich.ClockMinutes,
            rich.Weather,
            rich.LastDayEnd,
            rich.CreatedAt,
            rich.SavedAt,
            rich.Player,
            rich.FarmPlot,
            rich.ShippingBin,
            rich.Residents,
            rich.OpeningDone,
            rich.PurchasedListings,
            [NodeGames.Node(42, 0, NodeKind.Stump)],
            regrowths);
    }

    private static JsonObject Entry(string spawnId, int year, string season, int day) => new()
    {
        ["spawnId"] = spawnId,
        ["clearedOn"] = new JsonObject { ["year"] = year, ["season"] = season, ["day"] = day },
    };

    /// <summary>A whole <c>nodeRegrowths</c> array as the game writes it, for <see cref="Regrowths"/>.</summary>
    private static JsonArray NodeRegrowthsArray() => new(
        Entry("farm-2-2", 2, "summer", 13),
        Entry("farm-29-8", 2, "spring", 28));

    /// <summary>Saves a plain rich Game to the slot, puts the given <c>nodeRegrowths</c> in its file, and loads it.</summary>
    private GameState LoadWith(int slot, JsonNode? nodeRegrowths)
    {
        Store().Save(slot, GameWithRegrowths());
        var document = _folder.ReadSlot(slot);
        document["nodeRegrowths"] = nodeRegrowths;
        _folder.WriteSlot(slot, document);
        return Store().Load(slot);
    }

    [Fact]
    public void AC8_a_save_round_trips_its_node_regrowths()
    {
        Store().Save(1, GameWithRegrowths(Regrowths));

        var loaded = Store().Load(1);

        Assert.Equal(Regrowths, loaded.NodeRegrowths);
        Assert.Equal(["farm-42-0:Stump:Farm:42:0:0"], NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC8_the_file_holds_node_regrowths_as_spawnId_and_clearedOn()
    {
        Store().Save(1, GameWithRegrowths(Regrowths));

        var document = _folder.ReadSlot(1);

        Assert.Equal(2, document["saveVersion"]!.GetValue<int>());
        Assert.True(document["nodeRegrowths"] is JsonArray, "the saved file has no nodeRegrowths array");
        Assert.True(
            JsonNode.DeepEquals(NodeRegrowthsArray(), document["nodeRegrowths"]),
            $"nodeRegrowths was {document["nodeRegrowths"]?.ToJsonString()}");
    }

    [Fact]
    public void AC8_a_game_with_no_regrowths_saves_an_empty_array()
    {
        Store().Save(1, GameWithRegrowths());

        Assert.Equal("[]", _folder.ReadSlot(1)["nodeRegrowths"]?.ToJsonString());
    }

    [Fact]
    public void AC8_a_v2_file_without_node_regrowths_loads_with_none()
    {
        // Control: the same file with its nodeRegrowths loads them, so the empty list below is the property's absence.
        Store().Save(1, GameWithRegrowths(Regrowths));
        Assert.Equal(Regrowths, Store().Load(1).NodeRegrowths);

        var document = _folder.ReadSlot(1);
        document.Remove("nodeRegrowths");
        _folder.WriteSlot(1, document);

        Assert.Empty(Store().Load(1).NodeRegrowths);
    }

    [Fact]
    public void AC8_a_regrowth_whose_spawn_id_is_not_in_nodes_json_is_dropped_on_load()
    {
        var regrowths = NodeRegrowthsArray();
        regrowths.Add(Entry("farm-retuned-away", 2, "summer", 1));

        var loaded = LoadWith(1, regrowths);

        Assert.Equal(Regrowths, loaded.NodeRegrowths);
    }

    public static TheoryData<string> BadNodeRegrowths => new()
    {
        "not an array",
        "null entry",
        "entry without a spawnId",
        "null spawnId",
        "entry without a clearedOn",
        "null clearedOn",
        "clearedOn day 29",
        "clearedOn unknown season",
        "two entries for one spawn point",
    };

    /// <summary>A whole <c>nodeRegrowths</c> array with one defect.</summary>
    private static JsonNode BrokenNodeRegrowths(string how)
    {
        var regrowths = NodeRegrowthsArray();
        switch (how)
        {
            case "not an array":
                return JsonValue.Create("farm-2-2");
            case "null entry":
                regrowths[1] = null;
                break;
            case "entry without a spawnId":
                regrowths[0]!.AsObject().Remove("spawnId");
                break;
            case "null spawnId":
                regrowths[0]!["spawnId"] = null;
                break;
            case "entry without a clearedOn":
                regrowths[1]!.AsObject().Remove("clearedOn");
                break;
            case "null clearedOn":
                regrowths[1]!["clearedOn"] = null;
                break;
            case "clearedOn day 29":
                regrowths[0]!["clearedOn"]!["day"] = 29;
                break;
            case "clearedOn unknown season":
                regrowths[0]!["clearedOn"]!["season"] = "monsoon";
                break;
            case "two entries for one spawn point":
                regrowths.Add(Entry("farm-2-2", 2, "summer", 14));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(how), how, null);
        }

        return regrowths;
    }

    [Theory]
    [MemberData(nameof(BadNodeRegrowths))]
    public void AC8_a_save_whose_node_regrowths_are_malformed_is_refused_on_load(string how)
    {
        // Control, in slot 2: the same file with a whole nodeRegrowths array loads it, so the refusal is the defect's.
        Assert.Equal(Regrowths, LoadWith(2, NodeRegrowthsArray()).NodeRegrowths);

        Assert.Throws<LongWayHome.Application.Saves.SaveStoreException>(() => LoadWith(1, BrokenNodeRegrowths(how)));
    }
}
