using System.Text.Json.Nodes;
using LongWayHome.Domain.Nodes;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #151: a save (still version 2) holds the Game's ResourceNodes as <c>resourceNodes</c> of
/// <c>{ spawnId, kind, swingsTaken }</c> (AC7). A node whose spawn point isn't in <c>nodes.json</c> is dropped on load;
/// a file with no <c>resourceNodes</c> loads with every atStart node except those inside the old plot's block,
/// X 30–41, Y 0–8 (AC8); a malformed <c>resourceNodes</c> is refused, like <c>residents</c>.
/// </summary>
public sealed class ResourceNodeSaveTests : IDisposable
{
    /// <summary>
    /// The store's spawn list: three atStart nodes outside the Phase 1 block (two just past its west and east edges),
    /// two inside it on its corners, one in its middle, and one that isn't atStart.
    /// </summary>
    private static readonly NodeSpawn[] Spawns =
    [
        NodeGames.Spawn(2, 2, NodeKind.Tree),
        NodeGames.Spawn(29, 8, NodeKind.Rock),
        NodeGames.Spawn(42, 0, NodeKind.Stump),
        NodeGames.Spawn(30, 0, NodeKind.Tree),
        NodeGames.Spawn(41, 8, NodeKind.Rock),
        NodeGames.Spawn(35, 4, NodeKind.Stump),
        NodeGames.Spawn(10, 30, NodeKind.Tree, atStart: false),
    ];

    /// <summary>What a save with no <c>resourceNodes</c> loads with: the atStart nodes outside X 30–41, Y 0–8.</summary>
    private static readonly string[] OutsideBlock =
        ["farm-2-2:Tree:Farm:2:2:0", "farm-29-8:Rock:Farm:29:8:0", "farm-42-0:Stump:Farm:42:0:0"];

    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path, Spawns);

    /// <summary>A rich Game holding two of the spawn points' nodes, one with swings taken.</summary>
    private static LongWayHome.Domain.Games.Game GameWithNodes() =>
        NodeGames.WithNodes(SaveGames.Rich(), NodeGames.Node(2, 2), NodeGames.Node(42, 0, NodeKind.Stump, swingsTaken: 2));

    /// <summary>A whole <c>resourceNodes</c> array as the game writes it, for the two nodes of <see cref="GameWithNodes"/>.</summary>
    private static JsonArray ResourceNodesArray() => new(
        new JsonObject { ["spawnId"] = "farm-2-2", ["kind"] = "tree", ["swingsTaken"] = 0 },
        new JsonObject { ["spawnId"] = "farm-42-0", ["kind"] = "stump", ["swingsTaken"] = 2 });

    // ---- AC7: round trip ----

    [Fact]
    public void AC7_a_save_round_trips_its_resource_nodes()
    {
        Store().Save(1, GameWithNodes());

        var loaded = Store().Load(1);

        Assert.Equal(
            ["farm-2-2:Tree:Farm:2:2:0", "farm-42-0:Stump:Farm:42:0:2"],
            NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC7_the_file_holds_resource_nodes_as_spawnId_kind_and_swingsTaken()
    {
        Store().Save(1, GameWithNodes());

        var document = _folder.ReadSlot(1);

        Assert.Equal(2, document["saveVersion"]!.GetValue<int>());
        Assert.True(document["resourceNodes"] is JsonArray, "the saved file has no resourceNodes array");
        var entries = document["resourceNodes"]!.AsArray().Select(entry => entry!.AsObject()).ToList();
        Assert.All(entries, entry => Assert.Equal("spawnId,kind,swingsTaken", string.Join(",", entry.Select(pair => pair.Key))));
        Assert.Equal(
            ["farm-2-2:tree:0", "farm-42-0:stump:2"],
            entries
                .Select(entry => $"{entry["spawnId"]}:{entry["kind"]}:{entry["swingsTaken"]}")
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AC7_a_game_with_every_node_cleared_saves_an_empty_array_and_loads_with_none()
    {
        Store().Save(1, NodeGames.WithNodes(SaveGames.Rich()));

        Assert.Equal("[]", _folder.ReadSlot(1)["resourceNodes"]?.ToJsonString());
        Assert.Empty(Store().Load(1).ResourceNodes);
    }

    [Fact]
    public void AC7_a_node_whose_spawn_id_is_not_in_nodes_json_is_dropped_on_load()
    {
        Store().Save(1, SaveGames.Rich());
        var document = _folder.ReadSlot(1);
        var nodes = ResourceNodesArray();
        nodes.Add(new JsonObject { ["spawnId"] = "farm-retuned-away", ["kind"] = "rock", ["swingsTaken"] = 0 });
        document["resourceNodes"] = nodes;
        _folder.WriteSlot(1, document);

        var loaded = Store().Load(1);

        Assert.Equal(
            ["farm-2-2:Tree:Farm:2:2:0", "farm-42-0:Stump:Farm:42:0:2"],
            NodeGames.Describe(loaded.ResourceNodes));
    }

    public static TheoryData<string> BadResourceNodes => new()
    {
        "not an array",
        "null entry",
        "entry without a spawnId",
        "null spawnId",
        "unknown kind",
        "entry without a kind",
        "non-int swingsTaken",
        "negative swingsTaken",
    };

    /// <summary>Writes a whole <c>resourceNodes</c> array into the document with one defect.</summary>
    private static void BreakResourceNodes(JsonObject document, string how)
    {
        var nodes = ResourceNodesArray();
        switch (how)
        {
            case "not an array":
                document["resourceNodes"] = "farm-2-2";
                return;
            case "null entry":
                nodes[1] = null;
                break;
            case "entry without a spawnId":
                nodes[0]!.AsObject().Remove("spawnId");
                break;
            case "null spawnId":
                nodes[0]!["spawnId"] = null;
                break;
            case "unknown kind":
                nodes[0]!["kind"] = "boulder";
                break;
            case "entry without a kind":
                nodes[1]!.AsObject().Remove("kind");
                break;
            case "non-int swingsTaken":
                nodes[1]!["swingsTaken"] = "two";
                break;
            case "negative swingsTaken":
                nodes[1]!["swingsTaken"] = -1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(how), how, null);
        }

        document["resourceNodes"] = nodes;
    }

    [Theory]
    [MemberData(nameof(BadResourceNodes))]
    public void AC7_a_save_whose_resource_nodes_are_malformed_is_refused_on_load(string how)
    {
        // Control, in slot 2: the same file with a whole resourceNodes array loads, so the refusal below is the defect's.
        Store().Save(2, SaveGames.Rich());
        var control = _folder.ReadSlot(2);
        control["resourceNodes"] = ResourceNodesArray();
        _folder.WriteSlot(2, control);
        Assert.Equal(
            ["farm-2-2:Tree:Farm:2:2:0", "farm-42-0:Stump:Farm:42:0:2"],
            NodeGames.Describe(Store().Load(2).ResourceNodes));

        Store().Save(1, SaveGames.Rich());
        var document = _folder.ReadSlot(1);
        BreakResourceNodes(document, how);
        _folder.WriteSlot(1, document);

        Assert.Throws<LongWayHome.Application.Saves.SaveStoreException>(() => Store().Load(1));
    }

    // ---- AC8: saves from before #151 ----

    [Fact]
    public void AC8_a_v2_save_with_no_resource_nodes_loads_every_atStart_node_outside_the_old_plots_block()
    {
        _folder.WriteSlot(1, SaveDocuments.V2(SaveDocuments.GoodBackpack()));

        var loaded = Store().Load(1);

        Assert.Equal(OutsideBlock, NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC8_a_saved_file_with_its_resource_nodes_property_removed_loads_the_atStart_nodes_outside_the_block()
    {
        Store().Save(1, GameWithNodes());
        var document = _folder.ReadSlot(1);
        document.Remove("resourceNodes");
        _folder.WriteSlot(1, document);

        var loaded = Store().Load(1);

        Assert.Equal(OutsideBlock, NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC8_a_v1_save_loads_every_atStart_node_outside_the_old_plots_block()
    {
        _folder.WriteSlot(1, SaveDocuments.V1(new JsonArray(SaveDocuments.Stack("seed", "emberroot", 10))));

        var loaded = Store().Load(1);

        Assert.Equal(OutsideBlock, NodeGames.Describe(loaded.ResourceNodes));
    }

    [Fact]
    public void AC8_an_old_save_never_gets_a_node_inside_x_30_to_41_y_0_to_8()
    {
        _folder.WriteSlot(1, SaveDocuments.V1(new JsonArray(SaveDocuments.Stack("seed", "emberroot", 10))));

        var loaded = Store().Load(1);

        Assert.NotEmpty(loaded.ResourceNodes);
        Assert.DoesNotContain(
            loaded.ResourceNodes,
            node => node.Position.X is >= 30 and <= 41 && node.Position.Y is >= 0 and <= 8);
    }
}
