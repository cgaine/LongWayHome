using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;

namespace LongWayHome.Tests.Games;

/// <summary>#151: the guards and helpers the red tests don't drive: a null node, FromSpawn and PlotBlock.Contains.</summary>
public class ResourceNodeGuardTests
{
    [Fact]
    public void A_null_resource_node_is_refused()
    {
        Assert.Throws<ArgumentException>(() => NodeGames.WithEnergyAndNodes(150, NodeGames.Node(2, 3), null!));
    }

    [Fact]
    public void A_game_constructed_without_nodes_has_none()
    {
        var game = TestGames.WithEnergy(150);

        Assert.Empty(game.ResourceNodes);
        Assert.Null(game.NodeAt(AreaId.Farm, new GridPosition(0, 0)));
    }

    [Fact]
    public void FromSpawn_copies_the_spawn_points_id_kind_area_and_tile_with_no_swings()
    {
        var spawn = new NodeSpawn("mine-3-4", AreaId.Mine, 2, new GridPosition(3, 4), NodeKind.RoskDeposit, true, true);

        var node = ResourceNode.FromSpawn(spawn);

        Assert.Equal(
            ("mine-3-4", NodeKind.RoskDeposit, AreaId.Mine, new GridPosition(3, 4), 0),
            (node.SpawnId, node.Kind, node.Area, node.Position, node.SwingsTaken));
    }

    [Fact]
    public void FromSpawn_refuses_null()
    {
        Assert.Throws<ArgumentNullException>(() => ResourceNode.FromSpawn(null!));
    }

    [Theory]
    [InlineData(30, 0, true)]
    [InlineData(41, 8, true)]
    [InlineData(35, 4, true)]
    [InlineData(29, 0, false)]
    [InlineData(42, 0, false)]
    [InlineData(30, 9, false)]
    [InlineData(41, 9, false)]
    public void PlotBlock_contains_exactly_its_own_tiles(int x, int y, bool inside)
    {
        Assert.Equal(inside, FarmPlot.Phase1Block.Contains(new GridPosition(x, y)));
    }
}
