using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Residents;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Games;

/// <summary>
/// Builds a new Game from the real <c>game/data/*.json</c> files, so the BR-P6 starting values are
/// asserted against the shipped tuning rather than literals in code.
/// </summary>
public class NewGameFactoryTests
{
    private static readonly GameData Shipped = LoadShippedData();

    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void AC1_BR_P6_new_game_starts_with_100_gold_150_energy_tools_and_full_can()
    {
        var player = NewGameFactory.Create(Shipped, Now).Player;

        Assert.Equal(new Gold(100), player.Gold);
        Assert.Equal(150, player.Energy);
        Assert.Equal(1, player.Backpack.Quantity(ItemRef.Tool(ToolId.Hoe)));
        Assert.Equal(1, player.Backpack.Quantity(ItemRef.Tool(ToolId.WateringCan)));
        Assert.Equal(2, player.Backpack.Slots.Count(slot => slot.Item?.Kind == ItemKind.Tool));
        Assert.Equal(20, player.WateringCan.Capacity);
        Assert.Equal(20, player.WateringCan.Charges);
        Assert.False(player.WateringCan.Upgraded);
    }

    [Fact]
    public void AC2_BR_P6_new_game_holds_10_emberroot_and_5_dusktuber_seeds_only_once_maggie_gives_them()
    {
        var inventory = WithStarterSeeds(Shipped).Player.Backpack;

        Assert.Equal(
            [(ItemKind.Seed, CropTypeId.Emberroot, 10), (ItemKind.Seed, CropTypeId.Dusktuber, 5)],
            TestGames.Stacks(inventory));
        Assert.DoesNotContain(TestGames.Stacks(inventory), stack => stack.Kind == ItemKind.Crop);
    }

    [Fact]
    public void AC3_BR_T1_new_game_plot_is_12_by_9_grass_unwatered_tiles()
    {
        var plot = NewGameFactory.Create(Shipped, Now).FarmPlot;

        Assert.Equal(12, FarmPlot.Width);
        Assert.Equal(9, FarmPlot.Height);
        Assert.Equal(108, plot.Tiles.Count);

        var expectedPositions =
            from y in Enumerable.Range(0, 9)
            from x in Enumerable.Range(0, 12)
            select (x, y);
        Assert.Equal(
            expectedPositions.OrderBy(p => p),
            plot.Tiles.Select(tile => (tile.Position.X, tile.Position.Y)).OrderBy(p => p));
        Assert.All(plot.Tiles, tile =>
        {
            Assert.Equal(TileState.Grass, tile.State);
            Assert.False(tile.Watered);
        });
    }

    [Fact]
    public void AC4_BR_G1_new_game_starts_year_1_spring_day_1_at_clock_360_clear()
    {
        var game = NewGameFactory.Create(Shipped, Now);

        Assert.Equal(1, game.Date.Year);
        Assert.Equal(Season.Spring, game.Date.Season);
        Assert.Equal(1, game.Date.Day);
        Assert.Equal(360, game.ClockMinutes);
        Assert.Equal(Weather.Clear, game.Weather);
        Assert.Null(game.LastDayEnd);
        Assert.Equal(Now, game.CreatedAt);
        Assert.Equal(Now, game.SavedAt);
        Assert.NotEqual(Guid.Empty, game.Id);
    }

    [Fact]
    public void BR_P7_duplicate_starting_seed_entries_merge_into_one_stack()
    {
        var tuning = Shipped.Tuning with
        {
            StartingSeeds = [new StartingStack(CropTypeId.Emberroot, 3), new StartingStack(CropTypeId.Emberroot, 4)],
        };
        var data = new GameData(Shipped.CropTypes, Shipped.ShopListings, tuning);

        var inventory = WithStarterSeeds(data).Player.Backpack;

        Assert.Equal(
            [(ItemKind.Seed, CropTypeId.Emberroot, 7)],
            TestGames.Stacks(inventory));
    }

    [Fact]
    public void AC1_New_game_backpack_has_12_slots_hoe_in_slot_0_basic_can_in_slot_1_then_the_starting_seeds_from_maggie()
    {
        var backpack = WithStarterSeeds(Shipped).Player.Backpack;

        Assert.Equal(12, backpack.Capacity);
        Assert.Equal(0, backpack.HotbarRow);
        Assert.Equal(
            [
                "0:Tool:Hoe:1:Basic",
                "1:Tool:WateringCan:1:Basic",
                "2:Seed:Emberroot:10:-",
                "3:Seed:Dusktuber:5:-",
                .. Enumerable.Range(4, 8).Select(index => $"{index}:-:-:0:-"),
            ],
            backpack.Slots.Select(TestGames.DescribeSlot));
    }

    [Fact]
    public void BR_P6_New_game_has_100_gold_150_energy_and_a_full_basic_20_charge_can_in_the_backpack()
    {
        var player = NewGameFactory.Create(Shipped, Now).Player;

        Assert.Equal(new Gold(100), player.Gold);
        Assert.Equal(150, player.Energy);
        Assert.Equal(ToolTier.Basic, player.Backpack.TierOf(ToolId.Hoe));
        Assert.Equal(ToolTier.Basic, player.Backpack.TierOf(ToolId.WateringCan));
        Assert.Equal(20, player.WateringCan.Capacity);
        Assert.Equal(20, player.WateringCan.Charges);
        Assert.False(player.WateringCan.Upgraded);
        Assert.Equal(0, player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }

    [Fact]
    public void BR_P7_Starting_seed_entries_go_through_Add_so_a_crop_listed_twice_shares_a_slot()
    {
        var tuning = Shipped.Tuning with
        {
            StartingSeeds = [new StartingStack(CropTypeId.Emberroot, 3), new StartingStack(CropTypeId.Emberroot, 4)],
        };
        var data = new GameData(Shipped.CropTypes, Shipped.ShopListings, tuning);

        var backpack = WithStarterSeeds(data).Player.Backpack;

        Assert.Equal("2:Seed:Emberroot:7:-", TestGames.DescribeSlot(backpack.Slots[2]));
        Assert.Null(backpack.Slots[3].Item);
    }

    [Fact]
    public void Starting_seeds_that_do_not_fit_in_the_backpack_are_refused_BackpackFull_and_add_nothing()
    {
        var tuning = Shipped.Tuning with
        {
            StartingSeeds = [new StartingStack(CropTypeId.Emberroot, (10 * Backpack.MaxStack) + 1)],
        };
        var data = new GameData(Shipped.CropTypes, Shipped.ShopListings, tuning);
        var session = new GameSession(NewGameFactory.Create(data, Now), data);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), session.GiveStarterSeeds());
        Assert.Empty(TestGames.Stacks(session.Game.Player.Backpack));
    }

    [Fact]
    public void Game_session_exposes_the_game_and_data_it_was_created_with()
    {
        var game = NewGameFactory.Create(Shipped, Now);

        var session = new GameSession(game, Shipped);

        Assert.Same(game, session.Game);
        Assert.Same(Shipped, session.Data);
    }

    // ---- #138: Maggie and Clyde are present from the start ----

    [Fact]
    public void AC1_New_game_has_six_residents_maggie_and_clyde_present()
    {
        var game = NewGameFactory.Create(Shipped, Now);

        Assert.Equal(Enum.GetValues<ResidentId>(), game.Residents.Select(resident => resident.Id));
        Assert.Equal(
            [true, true, false, false, false, false],
            game.Residents.Select(resident => resident.Present));
        Assert.True(game.IsPresent(ResidentId.Maggie));
        Assert.True(game.IsPresent(ResidentId.Clyde));
        Assert.All(
            new[] { ResidentId.Tom, ResidentId.June, ResidentId.Jesse, ResidentId.Mira },
            id => Assert.False(game.IsPresent(id), $"{id} is present in a new Game"));
    }

    [Fact]
    public void AC1_The_six_residents_are_maggie_clyde_tom_june_jesse_and_mira_in_that_order()
    {
        Assert.Equal(
            ["maggie", "clyde", "tom", "june", "jesse", "mira"],
            NewGameFactory.Create(Shipped, Now).Residents.Select(resident => ResidentCodes.Code(resident.Id)));
    }

    // ---- #139: the Opening hands over the seeds ----

    [Fact]
    public void AC1_A_new_game_has_not_finished_the_opening()
    {
        var game = NewGameFactory.Create(Shipped, Now);

        Assert.False(game.OpeningDone);
    }

    [Fact]
    public void AC1_A_new_games_backpack_holds_only_the_hoe_in_slot_0_and_the_full_basic_can_in_slot_1()
    {
        var player = NewGameFactory.Create(Shipped, Now).Player;

        Assert.Equal(
            [
                "0:Tool:Hoe:1:Basic",
                "1:Tool:WateringCan:1:Basic",
                .. Enumerable.Range(2, 10).Select(index => $"{index}:-:-:0:-"),
            ],
            player.Backpack.Slots.Select(TestGames.DescribeSlot));
        Assert.Equal(20, player.WateringCan.Charges);
        Assert.Equal(new Gold(100), player.Gold);
        Assert.Equal(150, player.Energy);
    }

    [Fact]
    public void BR_P6_A_new_game_holds_no_seeds_until_maggie_gives_them()
    {
        var backpack = NewGameFactory.Create(Shipped, Now).Player.Backpack;

        Assert.Empty(TestGames.Stacks(backpack));
        Assert.NotEmpty(Shipped.Tuning.StartingSeeds);
    }

    /// <summary>A new Game from <paramref name="data"/> once Maggie has given the tuning's starter Seeds (BR-P6, #139).</summary>
    private static LongWayHome.Domain.Games.Game WithStarterSeeds(GameData data)
    {
        var session = new GameSession(NewGameFactory.Create(data, Now), data);
        Assert.Equal(ActionResult.Ok, session.GiveStarterSeeds());
        return session.Game;
    }

    private static GameData LoadShippedData()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "data");
        string Read(string fileName) => File.ReadAllText(Path.Combine(folder, fileName));

        return new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            Read(JsonGameDataSource.TuningFileName)).Load();
    }
}
