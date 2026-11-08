using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Farming;

/// <summary>#137: refilling the Watering can at the pond, the river and the lake (REQ-51, BR-W3, BR-A2).</summary>
public class WaterSourceRefillTests
{
    public static TheoryData<WaterSource> Sources => new(WaterSource.Pond, WaterSource.River, WaterSource.Lake);

    [Theory]
    [MemberData(nameof(Sources))]
    public void AC1_refill_at_each_source_fills_the_can_for_free(WaterSource source)
    {
        var game = TestGames.WithCan(110, TestGames.CanWith(3), null);

        var result = game.RefillCan(source);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(20, game.Player.WateringCan.Charges);
        Assert.Equal(110, game.Player.Energy);
    }

    [Fact]
    public void AC2_river_refill_with_no_energy_fills_the_can()
    {
        var game = TestGames.WithCan(0, TestGames.CanWith(5), null);

        var result = game.RefillCan(WaterSource.River);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(game.Player.WateringCan.Capacity, game.Player.WateringCan.Charges);
        Assert.Equal(0, game.Player.Energy);
    }

    [Fact]
    public void AC3_lake_refill_on_a_full_can_stays_full()
    {
        var game = TestGames.WithCan(150, TestGames.CanWith(20), null);
        var before = TestGames.WaterSnapshot(game);

        var result = game.RefillCan(WaterSource.Lake);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(game.Player.WateringCan.Capacity, game.Player.WateringCan.Charges);
        Assert.Equal(before, TestGames.WaterSnapshot(game));
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void BR_W3_refill_at_any_source_sets_an_empty_upgraded_can_to_capacity(WaterSource source)
    {
        var game = TestGames.WithCan(150, new WateringCan(40, 0, true), null);

        var result = game.RefillCan(source);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(40, game.Player.WateringCan.Charges);
        Assert.Equal(150, game.Player.Energy);
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void BR_P2_refill_at_any_source_costs_no_energy_even_at_1_energy(WaterSource source)
    {
        var game = TestGames.WithCan(1, TestGames.CanWith(0), null);

        var result = game.RefillCan(source);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(1, game.Player.Energy);
        Assert.Equal(20, game.Player.WateringCan.Charges);
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void BR_W1_repeated_refills_at_any_source_never_exceed_capacity(WaterSource source)
    {
        var game = TestGames.WithCan(150, TestGames.CanWith(19), null);

        game.RefillCan(source);
        game.RefillCan(source);
        var result = game.RefillCan(source);

        Assert.Equal(ActionResult.Ok, result);
        Assert.InRange(game.Player.WateringCan.Charges, 0, game.Player.WateringCan.Capacity);
        Assert.Equal(game.Player.WateringCan.Capacity, game.Player.WateringCan.Charges);
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void BR_A1_every_source_refills_on_a_new_game_with_nothing_unlocked(WaterSource source)
    {
        var session = TestGames.NewSession();
        Assert.True(session.Game.Player.WateringCan.TryUseCharge());

        var result = session.Game.RefillCan(source);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(session.Game.Player.WateringCan.Capacity, session.Game.Player.WateringCan.Charges);
    }

    [Fact]
    public void Unknown_source_throws()
    {
        var game = TestGames.WithCan(150, TestGames.CanWith(3), null);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.RefillCan((WaterSource)99));
        Assert.Equal(3, game.Player.WateringCan.Charges);
    }

    [Theory]
    [MemberData(nameof(Sources))]
    public void Session_refill_at_each_source_fills_the_can_and_raises_PlayerChanged_once(WaterSource source)
    {
        var session = TestGames.SessionWithCan(150, TestGames.CanWith(0));
        var players = 0;
        var tiles = 0;
        session.PlayerChanged += () => players++;
        session.TileChanged += _ => tiles++;

        var result = session.RefillCan(source);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(20, session.Game.Player.WateringCan.Charges);
        Assert.Equal(150, session.Game.Player.Energy);
        Assert.Equal(1, players);
        Assert.Equal(0, tiles);
    }

    [Fact]
    public void Session_refill_with_an_unknown_source_throws_and_raises_nothing()
    {
        var session = TestGames.SessionWithCan(150, TestGames.CanWith(0));
        var players = 0;
        session.PlayerChanged += () => players++;

        Assert.Throws<ArgumentOutOfRangeException>(() => session.RefillCan((WaterSource)99));
        Assert.Equal(0, players);
        Assert.Equal(0, session.Game.Player.WateringCan.Charges);
    }
}
