using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>Guard paths of the Game root, GameSession and NewGameFactory.</summary>
public class GameTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Game_rejects_a_null_player_or_farm_plot()
    {
        Assert.Throws<ArgumentNullException>(() => CreateGame(player: null!, farmPlot: new FarmPlot()));
        Assert.Throws<ArgumentNullException>(() => CreateGame(player: CreatePlayer(), farmPlot: null!));
    }

    [Fact]
    public void Game_exposes_the_values_it_was_created_with()
    {
        var player = CreatePlayer();
        var plot = new FarmPlot();
        var id = Guid.NewGuid();
        var saved = Now.AddHours(1);

        var game = new GameState(
            id, new GameDate(2, Season.Fall, 9), 720, Weather.Rain, DayEndReason.PassedOut, Now, saved, player, plot);

        Assert.Equal(id, game.Id);
        Assert.Equal(new GameDate(2, Season.Fall, 9), game.Date);
        Assert.Equal(720, game.ClockMinutes);
        Assert.Equal(Weather.Rain, game.Weather);
        Assert.Equal(DayEndReason.PassedOut, game.LastDayEnd);
        Assert.Equal(Now, game.CreatedAt);
        Assert.Equal(saved, game.SavedAt);
        Assert.Same(player, game.Player);
        Assert.Same(plot, game.FarmPlot);
    }

    // ---- #145: once-per-game purchases ----

    [Fact]
    public void BR_L5_A_game_created_without_purchases_has_none()
    {
        // Characterization: the constructor default is empty (passes before #145 too).
        var game = CreateGame(CreatePlayer(), new FarmPlot());

        Assert.Empty(game.PurchasedListings);
    }

    [Fact]
    public void BR_L5_RecordPurchase_adds_the_listing_id()
    {
        var game = CreateGame(CreatePlayer(), new FarmPlot());

        game.RecordPurchase("tool_axe");

        Assert.Equal(["tool_axe"], game.PurchasedListings);
    }

    [Fact]
    public void BR_L5_RecordPurchase_of_an_id_already_recorded_changes_nothing()
    {
        var game = CreateGame(CreatePlayer(), new FarmPlot());
        game.RecordPurchase("tool_axe");

        game.RecordPurchase("tool_axe");

        Assert.Equal(["tool_axe"], game.PurchasedListings);
    }

    [Fact]
    public void BR_L5_RecordPurchase_keeps_every_id_recorded()
    {
        var game = CreateGame(CreatePlayer(), new FarmPlot());

        game.RecordPurchase("tool_pickaxe");
        game.RecordPurchase("can_upgrade");

        Assert.Equal(["can_upgrade", "tool_pickaxe"], game.PurchasedListings.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void BR_L5_The_constructor_keeps_the_given_purchased_listings()
    {
        var game = new GameState(
            Guid.NewGuid(),
            new GameDate(1, Season.Spring, 1),
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            CreatePlayer(),
            new FarmPlot(),
            purchasedListings: new HashSet<string> { "tool_axe", "can_upgrade" });

        Assert.Equal(["can_upgrade", "tool_axe"], game.PurchasedListings.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BR_L5_RecordPurchase_of_a_null_or_blank_id_throws_and_records_nothing(string? listingId)
    {
        var game = CreateGame(CreatePlayer(), new FarmPlot());

        Assert.ThrowsAny<ArgumentException>(() => game.RecordPurchase(listingId!));
        Assert.Empty(game.PurchasedListings);
    }

    [Fact]
    public void BR_L5_The_constructor_refuses_a_blank_purchased_listing_id()
    {
        Assert.Throws<ArgumentException>(() => new GameState(
            Guid.NewGuid(),
            new GameDate(1, Season.Spring, 1),
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            CreatePlayer(),
            new FarmPlot(),
            purchasedListings: new HashSet<string> { "tool_axe", "" }));
    }

    [Fact]
    public void BR_L5_The_constructor_copies_the_purchased_listings_it_is_given()
    {
        var given = new HashSet<string> { "tool_axe" };
        var game = new GameState(
            Guid.NewGuid(),
            new GameDate(1, Season.Spring, 1),
            360,
            Weather.Clear,
            null,
            Now,
            Now,
            CreatePlayer(),
            new FarmPlot(),
            purchasedListings: given);

        given.Add("tool_pickaxe");

        Assert.Equal(["tool_axe"], game.PurchasedListings);
    }

    [Fact]
    public void Game_session_rejects_a_null_game_or_data()
    {
        var data = new GameData(new Dictionary<CropTypeId, CropType>(), [], CreateTuning());
        var game = CreateGame(CreatePlayer(), new FarmPlot());

        Assert.Throws<ArgumentNullException>(() => new GameSession(null!, data));
        Assert.Throws<ArgumentNullException>(() => new GameSession(game, null!));
    }

    [Fact]
    public void New_game_factory_rejects_null_data()
    {
        Assert.Throws<ArgumentNullException>(() => NewGameFactory.Create(null!, Now));
    }

    [Fact]
    public void BR_P6_new_game_takes_its_starting_values_from_the_tuning()
    {
        var tuning = CreateTuning() with
        {
            StartingGold = 7,
            MaxEnergy = 90,
            CanCapacity = 3,
            DayStartMinutes = 400,
            StartingSeeds = [new StartingStack(CropTypeId.Starcorn, 2)],
        };

        var data = new GameData(new Dictionary<CropTypeId, CropType>(), [], tuning);
        var session = new GameSession(NewGameFactory.Create(data, Now), data);
        // BR-P6 (#139): the starting Seeds come from Maggie in the Opening.
        session.GiveStarterSeeds();
        var game = session.Game;

        Assert.Equal(new Gold(7), game.Player.Gold);
        Assert.Equal(90, game.Player.Energy);
        Assert.Equal(3, game.Player.WateringCan.Charges);
        Assert.Equal(400, game.ClockMinutes);
        Assert.Equal(
            [(ItemKind.Seed, CropTypeId.Starcorn, 2)],
            TestGames.Stacks(game.Player.Backpack));
    }

    [Fact]
    public void New_games_get_distinct_ids()
    {
        var data = new GameData(new Dictionary<CropTypeId, CropType>(), [], CreateTuning());

        Assert.NotEqual(NewGameFactory.Create(data, Now).Id, NewGameFactory.Create(data, Now).Id);
    }

    private static GameState CreateGame(Player player, FarmPlot farmPlot) =>
        new(Guid.NewGuid(), new GameDate(1, Season.Spring, 1), 360, Weather.Clear, null, Now, Now, player, farmPlot);

    private static Player CreatePlayer() =>
        new(new Gold(100), 150, new WateringCan(20), TestGames.ToolsOnly());

    private static GameTuning CreateTuning() => new()
    {
        StartingGold = 100,
        StartingSeeds = [new StartingStack(CropTypeId.Emberroot, 10)],
        MaxEnergy = 150,
        PassOutEnergy = 75,
        ToolEnergyCost = 2,
        CanCapacity = 20,
        CanUpgradedCapacity = 40,
        DayStartMinutes = 360,
        DayEndMinutes = 1560,
        DayLengthRealSeconds = 720,
        RainChancePercent = 22,
        SeasonLengthDays = 28,
        WalkSpeedTilesPerSecond = 3.6,
        RunSpeedTilesPerSecond = 5.2,
    };
}
