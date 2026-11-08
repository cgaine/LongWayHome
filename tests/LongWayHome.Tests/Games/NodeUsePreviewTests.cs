using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #151: with the hoe aimed at a node's Tile, <see cref="GameSession.PreviewUse(GridPosition?, int)"/> names the
/// refusal and Use does nothing (AC10); the session exposes the Game's nodes and raises <c>NodesChanged</c> when a
/// Night completes, so the farm's node view rebuilds.
/// </summary>
public class NodeUsePreviewTests
{
    private static readonly GridPosition NodeTile = new(10, 20);

    /// <summary>The hotbar position of the hoe in the tools-only Backpack (Slot 0).</summary>
    private const int HoePosition = 0;

    private static GameSession SessionWithNodeAt(GridPosition tile, NodeKind kind = NodeKind.Tree) =>
        new(NodeGames.WithEnergyAndNodes(150, NodeGames.Node(tile.X, tile.Y, kind)), TestGames.Shipped);

    [Theory]
    [InlineData(NodeKind.Tree)]
    [InlineData(NodeKind.Stump)]
    [InlineData(NodeKind.Rock)]
    public void AC10_preview_with_the_hoe_aimed_at_a_nodes_tile_is_NodeInTheWay(NodeKind kind)
    {
        var session = SessionWithNodeAt(NodeTile, kind);
        Assert.Equal(ToolId.Hoe, session.HeldItemAt(HoePosition)?.Tool);

        var preview = session.PreviewUse(NodeTile, HoePosition);

        Assert.Equal(new UsePreview(null, Rejection.NodeInTheWay), preview);
    }

    [Fact]
    public void AC10_use_with_the_hoe_on_a_nodes_tile_is_refused_spends_no_energy_and_raises_no_change()
    {
        var session = SessionWithNodeAt(NodeTile);
        var before = TestGames.Snapshot(session.Game);
        var events = new List<string>();
        session.TileChanged += _ => events.Add("TileChanged");
        session.PlayerChanged += () => events.Add("PlayerChanged");

        var result = session.Use(NodeTile, HoePosition);

        Assert.Equal(ActionResult.Rejected(Rejection.NodeInTheWay), result);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
        Assert.Equal(TileState.Grass, session.Game.FarmPlot[NodeTile].State);
        Assert.Empty(events);
    }

    [Fact]
    public void AC10_the_use_prompt_for_the_hoe_aimed_at_a_nodes_tile_reads_Somethings_in_the_way()
    {
        var session = SessionWithNodeAt(NodeTile, NodeKind.Rock);
        var preview = session.PreviewUse(NodeTile, HoePosition);

        var prompt = UsePromptText.For(
            preview, session.HeldItemAt(HoePosition), session.Game.FarmPlot[NodeTile].Planting, session.Data.CropTypes);

        Assert.Equal(("Something's in the way", false), prompt);
    }

    [Fact]
    public void Session_resource_nodes_are_the_games_nodes()
    {
        var session = SessionWithNodeAt(NodeTile, NodeKind.Stump);

        Assert.Equal(["farm-10-20:Stump:Farm:10:20:0"], NodeGames.Describe(session.ResourceNodes));
    }

    [Fact]
    public void NodesChanged_is_raised_when_a_night_completes()
    {
        var session = SessionWithNodeAt(NodeTile);
        var events = new List<string>();
        session.NightCompleted += () => events.Add("NightCompleted");
        session.NodesChanged += () => events.Add("NodesChanged");

        Assert.Equal(ActionResult.Ok, session.Sleep());

        Assert.Contains("NodesChanged", events);
        Assert.Single(events, name => name == "NodesChanged");
    }
}
