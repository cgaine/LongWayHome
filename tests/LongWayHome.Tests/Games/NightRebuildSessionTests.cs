using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Games;
using LongWayHome.Tests.Saves;
using static LongWayHome.Tests.Games.BuildingGames;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #165 AC6: a Night that rebuilds raises <see cref="GameSession.BuildingsRebuilt"/> with the rebuilt ids (and
/// BuildingsChanged) after the save and SaveFailed, before NightCompleted; a Night with no rebuild raises neither.
/// Afterwards the sign asks for the next stage from 0.
/// </summary>
public class NightRebuildSessionTests
{
    private static List<string> RecordNight(GameSession session, List<IReadOnlyList<BuildingId>> rebuilt)
    {
        var events = new List<string>();
        session.DayEnded += _ => events.Add("DayEnded");
        session.SaveFailed += _ => events.Add("SaveFailed");
        session.BuildingsRebuilt += ids =>
        {
            events.Add("BuildingsRebuilt");
            rebuilt.Add(ids);
        };
        session.BuildingsChanged += () => events.Add("BuildingsChanged");
        session.NightCompleted += () => events.Add("NightCompleted");
        return events;
    }

    private static GameSession PaidInTown()
    {
        var session = InTown(Store(
            gold: 300,
            held: [(Material(MaterialId.Wood), 50), (Material(MaterialId.Stone), 25)]));
        Assert.True(session.Deliver(BuildingId.GeneralStore, MaterialId.Wood).IsOk);
        Assert.True(session.Deliver(BuildingId.GeneralStore, MaterialId.Stone).IsOk);
        for (var press = 0; press < 3; press++)
        {
            Assert.True(session.DeliverGold(BuildingId.GeneralStore).IsOk);
        }

        Assert.Equal("GeneralStore:1:Wood=50;Stone=25:300", Describe(StoreOf(session.Game)));
        return session;
    }

    [Fact]
    public void AC6_sleeping_after_paying_the_store_in_full_raises_BuildingsRebuilt_once_with_the_store_before_NightCompleted()
    {
        var session = PaidInTown();
        var rebuilt = new List<IReadOnlyList<BuildingId>>();
        var events = RecordNight(session, rebuilt);

        Assert.Equal(ActionResult.Ok, session.Sleep());

        var ids = Assert.Single(rebuilt);
        Assert.Equal([BuildingId.GeneralStore], ids);
        Assert.Single(events, name => name == "BuildingsRebuilt");
        Assert.True(
            events.IndexOf("BuildingsRebuilt") < events.IndexOf("NightCompleted"),
            $"BuildingsRebuilt must come before NightCompleted: {string.Join(", ", events)}");
        Assert.Equal(2, StoreOf(session.Game).Stage);
    }

    [Fact]
    public void AC6_a_rebuilding_Night_raises_BuildingsChanged_too_before_NightCompleted()
    {
        var session = PaidInTown();
        var events = RecordNight(session, []);

        session.Sleep();

        Assert.Contains("BuildingsChanged", events);
        Assert.True(
            events.IndexOf("BuildingsChanged") < events.IndexOf("NightCompleted"),
            $"BuildingsChanged must come before NightCompleted: {string.Join(", ", events)}");
    }

    [Fact]
    public void AC6_BuildingsRebuilt_comes_after_SaveFailed_when_the_save_fails()
    {
        var game = Store(storeGold: 300, delivered: [(MaterialId.Wood, 50), (MaterialId.Stone, 25)]);
        var session = new GameSession(
            game,
            BuildingGames.Data,
            new FixedRandom(99),
            new ThrowingSaveStore(new IOException("disk full")),
            1,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.Zero)));
        var events = RecordNight(session, []);

        session.Sleep();

        Assert.Equal(["DayEnded", "SaveFailed", "BuildingsRebuilt", "BuildingsChanged", "NightCompleted"], events);
    }

    [Fact]
    public void AC6_passing_out_after_paying_in_full_raises_BuildingsRebuilt_too()
    {
        var session = PaidInTown();
        var rebuilt = new List<IReadOnlyList<BuildingId>>();
        RecordNight(session, rebuilt);

        session.AdvanceClock(double.MaxValue);

        Assert.Equal([BuildingId.GeneralStore], Assert.Single(rebuilt));
    }

    [Fact]
    public void AC6_a_Night_with_nothing_fully_delivered_never_raises_BuildingsRebuilt_or_BuildingsChanged()
    {
        var session = InTown(Store(storeGold: 120, delivered: [(MaterialId.Wood, 30)]));
        var events = RecordNight(session, []);

        session.Sleep();

        Assert.DoesNotContain("BuildingsRebuilt", events);
        Assert.DoesNotContain("BuildingsChanged", events);
        Assert.Contains("NightCompleted", events);
    }

    [Fact]
    public void AC7_after_the_rebuilding_Night_the_store_sign_asks_for_the_stage_3_cost_from_0()
    {
        var session = PaidInTown();

        session.Sleep();
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        var sign = session.RebuildSign(BuildingId.GeneralStore);

        Assert.NotNull(sign);
        Assert.Equal(3, sign.Stage);
        Assert.Equal(Stage3Unlocks, sign.Unlocks);
        Assert.Equal(
            [(MaterialId.Plank, 0, 40), (MaterialId.Stone, 0, 60), ((MaterialId?)null, 0, 1200)],
            sign.Lines.Select(line => (line.Material, line.Delivered, line.Needed)));
    }
}
