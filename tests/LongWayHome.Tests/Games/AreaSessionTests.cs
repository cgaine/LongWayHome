using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Saves;

namespace LongWayHome.Tests.Games;

/// <summary>
/// Walking between areas through <see cref="GameSession"/> (#136): <c>CurrentArea</c>, <c>EnterArea</c>, the
/// <c>AreaChanged</c> event, the Night's return to the farm, and what the core sees off the farm.
/// </summary>
public class AreaSessionTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 11, 8, 9, 0, 0, TimeSpan.Zero);

    private static readonly GridPosition Feet = new(1, 7);

    private static readonly GridPosition Target = new(4, 3);

    private static readonly AreaId[] StartAreas = [AreaId.Farm, AreaId.ForestPath, AreaId.Town, AreaId.Lakeshore];

    private static List<AreaId> RecordAreaChanges(GameSession session)
    {
        var changes = new List<AreaId>();
        session.AreaChanged += area => changes.Add(area);
        return changes;
    }

    // ---- AC1: the start areas stay open ----

    [Fact]
    public void AC1_Start_areas_stay_open_after_nights()
    {
        var session = TestGames.NewSession();

        for (var night = 0; night < 5; night++)
        {
            Assert.Equal(ActionResult.Ok, session.Sleep());
        }

        Assert.Equal(new GameDate(1, Season.Spring, 6), session.Game.Date);
        Assert.All(StartAreas, area => Assert.True(session.Game.IsAreaOpen(area), $"{area} is closed after five Nights"));
    }

    // ---- AC3: every session starts on the farm ----

    [Fact]
    public void AC3_New_and_loaded_sessions_start_on_the_farm()
    {
        using var folder = new TempSaveFolder();
        var store = new JsonSaveStore(folder.Path);
        var time = new FixedTimeProvider(StartTime);

        var plain = TestGames.NewSession();
        var started = GameStarter.StartNew(TestGames.Shipped, store, 1, new FixedRandom(99), time);
        var loaded = GameStarter.Load(TestGames.Shipped, store, 1, new FixedRandom(99), time);

        Assert.Equal(AreaId.Farm, plain.CurrentArea);
        Assert.Equal(AreaId.Farm, started.CurrentArea);
        Assert.Equal(AreaId.Farm, loaded.CurrentArea);
    }

    // ---- AC4: entering an open area ----

    [Fact]
    public void AC4_Entering_town_moves_the_player_and_costs_nothing()
    {
        var session = TestGames.NewSession();
        session.AdvanceClock(60);
        var before = (
            session.Game.Player.Energy,
            session.Game.ClockMinutes,
            session.Game.Player.Gold,
            TestGames.Describe(session.Game.Player.Backpack));
        var changes = RecordAreaChanges(session);

        var result = session.EnterArea(AreaId.Town);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(AreaId.Town, session.CurrentArea);
        Assert.Equal(new[] { AreaId.Town }, changes);
        var after = (
            session.Game.Player.Energy,
            session.Game.ClockMinutes,
            session.Game.Player.Gold,
            TestGames.Describe(session.Game.Player.Backpack));
        Assert.Equal(before, after);
    }

    [Fact]
    public void AC4_Entering_the_current_area_raises_nothing()
    {
        var session = TestGames.NewSession();
        var changes = RecordAreaChanges(session);

        var result = session.EnterArea(AreaId.Farm);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(AreaId.Farm, session.CurrentArea);
        Assert.Empty(changes);
    }

    // ---- AC5: a closed area is refused ----

    [Theory]
    [InlineData(AreaId.AcrossRiver)]
    [InlineData(AreaId.Mine)]
    public void AC5_Entering_a_closed_area_is_rejected(AreaId area)
    {
        var session = TestGames.NewSession();
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        var changes = RecordAreaChanges(session);

        var result = session.EnterArea(area);

        Assert.Equal(ActionResult.Rejected(Rejection.AreaClosed), result);
        Assert.Equal(AreaId.Town, session.CurrentArea);
        Assert.Empty(changes);
    }

    // ---- AC6: every Day starts on the farm ----

    [Fact]
    public void AC6_After_sleeping_away_from_the_farm_the_player_is_on_the_farm_at_night_completed()
    {
        var session = TestGames.NewSession();
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Lakeshore));
        var seen = new List<AreaId>();
        session.NightCompleted += () => seen.Add(session.CurrentArea);

        Assert.Equal(ActionResult.Ok, session.Sleep());

        Assert.Equal(new[] { AreaId.Farm }, seen);
    }

    [Fact]
    public void AC6_After_passing_out_away_from_the_farm_the_player_is_on_the_farm_at_night_completed()
    {
        var session = TestGames.NewSession();
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Lakeshore));
        var seen = new List<AreaId>();
        session.NightCompleted += () => seen.Add(session.CurrentArea);

        // The whole Day: from 06:00 the clock reaches 02:00 and the Player passes out (BR-G3).
        session.AdvanceClock(TestGames.Shipped.Tuning.DayLengthRealSeconds);

        Assert.Equal(DayEndReason.PassedOut, session.Game.LastDayEnd);
        Assert.Equal(new[] { AreaId.Farm }, seen);
    }

    // ---- Edge: the core only sees the current area ----

    [Fact]
    public void Pickup_only_sees_the_current_area()
    {
        var session = TestGames.SessionWithBackpack(TestGames.FullBackpack());
        session.Game.Gain(ItemRef.Crop(CropTypeId.Emberroot), 1, Feet);
        Assert.True(session.Game.Player.Backpack.Remove(TestGames.Filler, 99));
        Assert.True(session.HasDroppedItemsAt(Feet));
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));

        Assert.False(session.HasDroppedItemsAt(Feet));
        Assert.False(session.PickUp(Feet));
        Assert.Single(session.Game.DroppedItems);
        Assert.Equal(0, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));

        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Farm));
        Assert.True(session.HasDroppedItemsAt(Feet));
        Assert.True(session.PickUp(Feet));
        Assert.Empty(session.Game.DroppedItems);
    }

    [Fact]
    public void Tools_have_no_target_off_the_farm()
    {
        // Slot 0 (hotbar position 0) holds the hoe; the target is grass, so on the farm the hoe would till it.
        var session = TestGames.SessionWithEnergy(150);
        Assert.Equal(new UsePreview(UseActionKind.Till, null), session.PreviewUse(Target, 0));
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        var before = TestGames.FullSnapshot(session.Game);

        var preview = session.PreviewUse(Target, 0);
        var result = session.Use(Target, 0, Feet);

        Assert.Equal(session.PreviewUse(null, 0), preview);
        Assert.False(result.IsOk);
        Assert.Equal(TileState.Grass, session.Game.FarmPlot[Target].State);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }
}
