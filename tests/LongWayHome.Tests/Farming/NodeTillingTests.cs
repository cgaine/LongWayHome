using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Farming;

/// <summary>
/// #151: a Tile with a ResourceNode on it can't be tilled (BR-T2). The refusal is NodeInTheWay, checked after
/// AlreadyTilled and before TooTired, and spends no energy.
/// </summary>
public class NodeTillingTests
{
    private const int ToolEnergyCost = 2;

    private static readonly GridPosition NodeTile = new(10, 20);

    [Theory]
    [InlineData(NodeKind.Tree)]
    [InlineData(NodeKind.Stump)]
    [InlineData(NodeKind.Rock)]
    public void AC6_tilling_a_grass_tile_with_a_node_on_it_is_refused_with_NodeInTheWay(NodeKind kind)
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(NodeTile.X, NodeTile.Y, kind));

        var result = game.Till(NodeTile, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.NodeInTheWay), result);
    }

    [Fact]
    public void AC6_a_refused_till_on_a_nodes_tile_costs_no_energy_and_leaves_the_tile_grass()
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(NodeTile.X, NodeTile.Y));
        var before = TestGames.Snapshot(game);

        game.Till(NodeTile, ToolEnergyCost);

        Assert.Equal(150, game.Player.Energy);
        Assert.Equal(TileState.Grass, game.FarmPlot[NodeTile].State);
        Assert.Equal(before, TestGames.Snapshot(game));
        Assert.NotNull(game.NodeAt(AreaId.Farm, NodeTile));
    }

    [Fact]
    public void AC6_a_grass_tile_next_to_a_node_still_tills_as_before()
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(NodeTile.X, NodeTile.Y));
        var neighbour = new GridPosition(NodeTile.X + 1, NodeTile.Y);
        Assert.NotNull(game.NodeAt(AreaId.Farm, NodeTile)); // the node really stands next door

        var result = game.Till(neighbour, ToolEnergyCost);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(TileState.Tilled, game.FarmPlot[neighbour].State);
        Assert.Equal(148, game.Player.Energy);
    }

    [Fact]
    public void BR_T2_till_rejection_on_a_nodes_tile_is_NodeInTheWay()
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(NodeTile.X, NodeTile.Y, NodeKind.Rock));

        Assert.Equal(Rejection.NodeInTheWay, game.TillRejection(NodeTile, ToolEnergyCost));
    }

    [Fact]
    public void BR_T2_a_node_is_checked_before_TooTired()
    {
        var game = NodeGames.WithEnergyAndNodes(1, NodeGames.Node(NodeTile.X, NodeTile.Y));

        Assert.Equal(Rejection.NodeInTheWay, game.TillRejection(NodeTile, ToolEnergyCost));
    }

    [Fact]
    public void BR_T2_AlreadyTilled_is_checked_before_a_node()
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(NodeTile.X, NodeTile.Y));
        Assert.NotNull(game.NodeAt(AreaId.Farm, NodeTile)); // the node really stands on the Tile
        game.FarmPlot[NodeTile].Till(); // tilled behind the Game's back: the order of the checks is what's tested

        Assert.Equal(Rejection.AlreadyTilled, game.TillRejection(NodeTile, ToolEnergyCost));
    }

    [Fact]
    public void BR_T2_a_node_in_another_area_at_the_same_grid_position_does_not_block_the_farm_tile()
    {
        var elsewhere = new ResourceNode("forest-10-20", NodeKind.Tree, AreaId.ForestPath, NodeTile);
        var game = NodeGames.WithEnergyAndNodes(150, elsewhere);
        Assert.Single(game.ResourceNodes); // the forest node is held by the Game

        Assert.Null(game.TillRejection(NodeTile, ToolEnergyCost));
    }
}
