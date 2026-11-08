using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #150: every Tile of the 72 x 36 grid can be tilled, planted and watered through the session exactly as the old
/// 12 x 9 plot's Tiles could, with the same results and energy (BR-T2, BR-P2).
/// </summary>
public class GameSessionGridEdgeTests
{
    /// <summary>The position, failing the test (not throwing) when it isn't on the plot.</summary>
    private static GridPosition At(int x, int y)
    {
        Assert.True(GridPosition.TryCreate(x, y, out var position), $"({x}, {y}) is not on the Farm plot");
        return position;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(71, 35)]
    [InlineData(35, 20)]
    public void AC3_till_plant_water_at_new_grid_positions(int x, int y)
    {
        var position = At(x, y);
        // #151: a new Game with no nodes, so the grid's corners are grass with nothing on them.
        var session = TestGames.NewSession(TestGames.ShippedWithoutNodes);
        var game = session.Game;
        Assert.Equal(Player.MaxEnergy, game.Player.Energy);
        var charges = game.Player.WateringCan.Charges;

        Assert.Equal(ActionResult.Ok, session.Till(position));
        Assert.Equal(TileState.Tilled, game.FarmPlot[position].State);
        Assert.Equal(148, game.Player.Energy);

        Assert.Equal(ActionResult.Ok, session.Plant(position, CropTypeId.Emberroot));
        Assert.Equal(new Planting(CropTypeId.Emberroot, new GameDate(1, Season.Spring, 1), 0), game.FarmPlot[position].Planting);
        Assert.Equal(9, game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
        Assert.Equal(148, game.Player.Energy);

        Assert.Equal(ActionResult.Ok, session.Water(position));
        Assert.True(game.FarmPlot[position].Watered);
        Assert.Equal(charges - 1, game.Player.WateringCan.Charges);
        Assert.Equal(146, game.Player.Energy);
    }

    [Fact]
    public void BR_T2_tilled_tile_at_new_edge_cannot_be_tilled_again()
    {
        var position = At(71, 35);
        var session = TestGames.NewSession();
        Assert.True(session.Till(position).IsOk);
        var before = TestGames.Snapshot(session.Game);

        var result = session.Till(position);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), result);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Fact]
    public void BR_P2_tool_actions_on_a_new_edge_tile_cost_2_energy_as_before()
    {
        // A tool action costs 2 energy and can't happen with less; planting costs none (BR-P2).
        var position = At(0, 35);
        var session = TestGames.SessionWithEnergy(3);

        Assert.True(session.Till(position).IsOk);
        Assert.Equal(1, session.Game.Player.Energy);

        var water = session.Water(position);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), water);
        Assert.False(session.Game.FarmPlot[position].Watered);
        Assert.Equal(1, session.Game.Player.Energy);
    }

    [Fact]
    public void BR_P2_planting_on_a_far_tile_costs_no_energy()
    {
        var position = At(35, 20);
        var session = TestGames.NewSession();
        Assert.True(session.Till(position).IsOk);

        Assert.True(session.Plant(position, CropTypeId.Dusktuber).IsOk);

        Assert.Equal(148, session.Game.Player.Energy);
        Assert.Equal(4, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }
}
