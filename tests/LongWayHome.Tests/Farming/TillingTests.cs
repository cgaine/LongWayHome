using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Farming;

/// <summary>Tilling a Tile with the hoe: BR-T2, BR-T8 and the 2-energy cost (BR-P1, BR-P2).</summary>
public class TillingTests
{
    private const int ToolEnergyCost = 2;

    private static readonly GridPosition Target = new(3, 4);

    [Fact]
    public void AC1_tilling_grass_with_150_energy_tills_it_and_leaves_148()
    {
        var game = TestGames.WithEnergy(150);

        var result = game.Till(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Ok, result);
        Assert.True(result.IsOk);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.Equal(148, game.Player.Energy);
    }

    [Fact]
    public void AC2_tilling_a_tilled_tile_returns_AlreadyTilled_and_changes_nothing()
    {
        var game = TestGames.WithEnergy(150);
        game.Till(Target, ToolEnergyCost);
        var before = TestGames.Snapshot(game);

        var result = game.Till(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), result);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.Equal(148, game.Player.Energy);
        Assert.Equal(before, TestGames.Snapshot(game));
    }

    [Fact]
    public void AC3_with_exactly_2_energy_tilling_succeeds_and_leaves_0()
    {
        var game = TestGames.WithEnergy(2);

        var result = game.Till(Target, ToolEnergyCost);

        Assert.True(result.IsOk);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Target].State);
        Assert.Equal(0, game.Player.Energy);
    }

    [Fact]
    public void AC3_with_1_energy_tilling_returns_TooTired_and_changes_nothing()
    {
        var game = TestGames.WithEnergy(1);

        var result = game.Till(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), result);
        Assert.Equal(TileState.Grass, game.FarmPlot[Target].State);
        Assert.Equal(1, game.Player.Energy);
    }

    [Fact]
    public void AC4_75_tills_from_a_new_game_succeed_and_the_76th_is_TooTired()
    {
        // #151: a new Game with no nodes, so the first 76 Tiles of the plot are all grass with nothing on them.
        var session = TestGames.NewSession(TestGames.ShippedWithoutNodes);
        var positions = session.Game.FarmPlot.Tiles.Select(tile => tile.Position).Take(76).ToList();

        var results = positions.Take(75).Select(session.Till).ToList();

        Assert.All(results, result => Assert.True(result.IsOk));
        Assert.Equal(0, session.Game.Player.Energy);
        Assert.Equal(75, session.Game.FarmPlot.Tiles.Count(tile => tile.State == TileState.Tilled));

        var last = session.Till(positions[75]);

        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), last);
        Assert.Equal(TileState.Grass, session.Game.FarmPlot[positions[75]].State);
        Assert.Equal(0, session.Game.Player.Energy);
    }

    [Fact]
    public void AC8_no_sequence_of_actions_turns_tilled_soil_back_to_grass()
    {
        // #151: a new Game with no nodes, so every other Tile tills and spends energy.
        var session = TestGames.NewSession(TestGames.ShippedWithoutNodes);
        session.Till(Target);
        HeldItem[] held = [HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan), HeldItem.SeedOf(CropTypeId.Emberroot)];
        var others = session.Game.FarmPlot.Tiles.Select(tile => tile.Position).Where(p => p != Target).ToList();

        // Each round tries every held item on the tilled tile, then spends energy elsewhere, until the Player is spent.
        foreach (var other in others.Take(80))
        {
            foreach (var item in held)
            {
                session.PreviewUse(Target, item);
                session.Use(Target, item);
                session.Use(null, item);
            }

            session.Till(Target);
            session.Use(other, HeldItem.Of(ToolId.Hoe));
            Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Target].State);
        }

        Assert.Equal(0, session.Game.Player.Energy);
        Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Target].State);
    }

    [Fact]
    public void BR_T2_only_grass_can_be_tilled()
    {
        var tile = new Tile(Target);

        var first = tile.Till();
        var second = tile.Till();

        Assert.Equal(ActionResult.Ok, first);
        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), second);
        Assert.Equal(TileState.Tilled, tile.State);
    }

    [Fact]
    public void BR_T8_a_tilled_tile_stays_tilled()
    {
        var tile = new Tile(Target);
        tile.Till();

        for (var i = 0; i < 5; i++)
        {
            tile.Till();
        }

        Assert.Equal(TileState.Tilled, tile.State);
        Assert.False(tile.Watered);
    }

    [Fact]
    public void Already_tilled_is_checked_before_too_tired()
    {
        var game = TestGames.WithEnergy(1);
        game.FarmPlot[Target].Till();

        var result = game.Till(Target, ToolEnergyCost);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), result);
        Assert.Equal(1, game.Player.Energy);
    }

    [Theory]
    [InlineData(150, true)]
    [InlineData(1, false)]
    public void A_rejected_till_spends_no_energy_and_changes_no_tile(int energy, bool tillFirst)
    {
        var game = TestGames.WithEnergy(energy);
        if (tillFirst)
        {
            game.Till(Target, ToolEnergyCost);
        }

        var before = TestGames.Snapshot(game);

        var result = game.Till(Target, ToolEnergyCost);

        Assert.False(result.IsOk);
        Assert.Equal(before, TestGames.Snapshot(game));
    }

    [Fact]
    public void Tilling_one_tile_leaves_every_other_tile_grass()
    {
        var game = TestGames.WithEnergy(150);

        game.Till(Target, ToolEnergyCost);

        Assert.All(
            game.FarmPlot.Tiles.Where(tile => tile.Position != Target),
            tile => Assert.Equal(TileState.Grass, tile.State));
    }
}
