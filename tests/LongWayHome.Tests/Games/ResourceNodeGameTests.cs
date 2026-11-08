using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #151: the Game holds its ResourceNodes read-only, finds one by area and Tile with <c>NodeAt</c>, and refuses two
/// nodes for one spawn point or on one Tile.
/// </summary>
public class ResourceNodeGameTests
{
    [Fact]
    public void A_game_holds_the_nodes_it_was_constructed_with()
    {
        var game = NodeGames.WithEnergyAndNodes(
            150, NodeGames.Node(2, 3), NodeGames.Node(60, 30, NodeKind.Stump), NodeGames.Node(5, 5, NodeKind.Rock, swingsTaken: 1));

        Assert.Equal(
            ["farm-2-3:Tree:Farm:2:3:0", "farm-5-5:Rock:Farm:5:5:1", "farm-60-30:Stump:Farm:60:30:0"],
            NodeGames.Describe(game.ResourceNodes));
    }

    [Fact]
    public void The_resource_node_list_is_read_only()
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(2, 3));

        Assert.Single(game.ResourceNodes);
        Assert.False(game.ResourceNodes is ICollection<ResourceNode> { IsReadOnly: false }, "Game.ResourceNodes can be changed from outside");
    }

    [Fact]
    public void NodeAt_finds_the_node_on_its_area_and_tile()
    {
        var rock = NodeGames.Node(5, 5, NodeKind.Rock);
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(2, 3), rock);

        Assert.Same(rock, game.NodeAt(AreaId.Farm, new GridPosition(5, 5)));
    }

    [Fact]
    public void NodeAt_is_null_on_an_empty_tile_and_in_another_area()
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(5, 5));
        Assert.Single(game.ResourceNodes);

        Assert.Null(game.NodeAt(AreaId.Farm, new GridPosition(5, 6)));
        Assert.Null(game.NodeAt(AreaId.ForestPath, new GridPosition(5, 5)));
    }

    [Fact]
    public void Two_nodes_with_the_same_spawn_id_are_refused()
    {
        var first = NodeGames.Node(5, 5);
        var second = new ResourceNode(first.SpawnId, NodeKind.Rock, AreaId.Farm, new GridPosition(6, 6));

        var thrown = Assert.Throws<ArgumentException>(() => NodeGames.WithEnergyAndNodes(150, first, second));

        Assert.Contains(first.SpawnId, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_nodes_on_the_same_tile_of_one_area_are_refused()
    {
        var first = NodeGames.Node(5, 5);
        var second = new ResourceNode("farm-other", NodeKind.Rock, AreaId.Farm, new GridPosition(5, 5));

        Assert.Throws<ArgumentException>(() => NodeGames.WithEnergyAndNodes(150, first, second));
    }

    [Fact]
    public void Nodes_on_the_same_grid_position_in_different_areas_are_allowed()
    {
        var farm = NodeGames.Node(5, 5);
        var forest = new ResourceNode("forest-5-5", NodeKind.Tree, AreaId.ForestPath, new GridPosition(5, 5));

        var game = NodeGames.WithEnergyAndNodes(150, farm, forest);

        Assert.Same(farm, game.NodeAt(AreaId.Farm, new GridPosition(5, 5)));
        Assert.Same(forest, game.NodeAt(AreaId.ForestPath, new GridPosition(5, 5)));
    }
}
