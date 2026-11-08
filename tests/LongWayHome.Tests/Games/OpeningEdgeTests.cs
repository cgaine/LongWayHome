using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #139: the Opening's paths the acceptance tests don't drive: a Day ending mid-Opening ends the flow (BR-I1, BR-G3),
/// the till step reads only a real till, the clock after the Opening, detaching, and the argument checks.
/// </summary>
public class OpeningEdgeTests
{
    private static readonly GridPosition Soil = new(3, 2);

    private static readonly GridPosition Other = new(6, 4);

    private static (GameSession Session, OpeningFlow Flow) NewFlow(GameState? game = null)
    {
        var session = new GameSession(game ?? OpeningGames.New(), TestGames.Shipped);
        return (session, new OpeningFlow(session));
    }

    private static void FinishLinesUntil(OpeningFlow flow, OpeningStep step)
    {
        for (var guard = 0; flow.Current != step; guard++)
        {
            Assert.True(guard < 10, $"never reached {step}; on {flow.Current}");
            switch (flow.Current)
            {
                case OpeningStep.Crash:
                    flow.CrashFinished();
                    break;
                case OpeningStep.Walk:
                    flow.WalkArrived();
                    break;
                default:
                    flow.LinesFinished();
                    break;
            }
        }
    }

    [Theory]
    [InlineData(OpeningStep.Crash)]
    [InlineData(OpeningStep.Walk)]
    [InlineData(OpeningStep.Till)]
    public void BR_I1_Sleeping_mid_opening_ends_the_flow_once(OpeningStep step)
    {
        var (session, flow) = NewFlow();
        FinishLinesUntil(flow, step);
        var raised = 0;
        flow.StepChanged += () => raised++;

        Assert.Equal(ActionResult.Ok, session.Sleep());

        Assert.Equal(OpeningStep.Done, flow.Current);
        Assert.False(flow.IsActive);
        Assert.Equal(1, raised);
        Assert.Equal(10, OpeningGames.SeedCount(session.Game, CropTypeId.Emberroot));
    }

    [Fact]
    public void BR_G3_Passing_out_mid_opening_ends_the_flow_and_a_later_day_changes_nothing()
    {
        var (session, flow) = NewFlow();
        FinishLinesUntil(flow, OpeningStep.Walk);

        session.AdvanceClock(TestGames.Shipped.Tuning.DayLengthRealSeconds);
        var raised = 0;
        flow.StepChanged += () => raised++;
        session.Sleep();

        Assert.Equal(OpeningStep.Done, flow.Current);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void BR_I1_Watering_a_tile_tilled_before_the_till_step_does_not_pass_it()
    {
        var (session, flow) = NewFlow();
        FinishLinesUntil(flow, OpeningStep.Walk);
        Assert.Equal(ActionResult.Ok, session.Till(Other));
        FinishLinesUntil(flow, OpeningStep.Till);

        Assert.Equal(ActionResult.Ok, session.Water(Other));

        Assert.Equal(OpeningStep.Till, flow.Current);
        Assert.Equal(ActionResult.Ok, session.Till(Soil));
        Assert.Equal(OpeningStep.Seeds, flow.Current);
    }

    [Fact]
    public void BR_I1_Planting_on_a_tile_watered_before_the_water_step_passes_it()
    {
        // A planted Tile is now watered; watering it again would be refused (BR-T9), so the step is done.
        var (session, flow) = NewFlow();
        FinishLinesUntil(flow, OpeningStep.Till);
        Assert.Equal(ActionResult.Ok, session.Till(Soil));
        FinishLinesUntil(flow, OpeningStep.Plant);
        Assert.Equal(ActionResult.Ok, session.Till(Other));
        Assert.Equal(ActionResult.Ok, session.Water(Other));
        Assert.Equal(ActionResult.Ok, session.Plant(Soil, CropTypeId.Emberroot));
        FinishLinesUntil(flow, OpeningStep.Water);

        Assert.Equal(ActionResult.Ok, session.Plant(Other, CropTypeId.Emberroot));

        Assert.Equal(OpeningStep.Farewell, flow.Current);
    }

    [Fact]
    public void BR_I1_A_seed_planted_in_watered_soil_in_the_plant_step_has_the_water_step_already_done()
    {
        var (session, flow) = NewFlow();
        FinishLinesUntil(flow, OpeningStep.Till);
        Assert.Equal(ActionResult.Ok, session.Till(Soil));
        FinishLinesUntil(flow, OpeningStep.Plant);
        Assert.Equal(ActionResult.Ok, session.Water(Soil));
        Assert.Equal(ActionResult.Ok, session.Plant(Soil, CropTypeId.Emberroot));
        Assert.Equal(OpeningStep.WaterLine, flow.Current);
        var seen = new List<OpeningStep>();
        flow.StepChanged += () => seen.Add(flow.Current);

        flow.LinesFinished();

        Assert.Equal([OpeningStep.Water, OpeningStep.Farewell], seen);
        Assert.False(session.Water(Soil).IsOk);
    }

    [Fact]
    public void BR_G2_The_clock_runs_once_the_opening_is_done()
    {
        var (_, flow) = NewFlow(TestGames.WithEnergy(150));

        Assert.True(flow.ClockRuns);
    }

    [Fact]
    public void Disposing_twice_is_harmless_and_the_flow_keeps_its_step()
    {
        var (session, flow) = NewFlow();

        flow.Dispose();
        flow.Dispose();
        session.Sleep();

        Assert.Equal(OpeningStep.Crash, flow.Current);
    }

    [Fact]
    public void The_flow_needs_a_session()
    {
        Assert.Throws<ArgumentNullException>(() => new OpeningFlow(null!));
    }

    [Fact]
    public void GiveStarterSeeds_needs_a_seed_list()
    {
        var game = OpeningGames.NotDone();

        Assert.Throws<ArgumentNullException>(() => game.GiveStarterSeeds(null!));
    }

    [Fact]
    public void BR_I1_A_day_ending_with_no_room_for_the_seeds_still_completes_the_opening()
    {
        var session = new GameSession(OpeningGames.NotDone(TestGames.FullBackpack()), TestGames.Shipped);

        Assert.Equal(ActionResult.Ok, session.Sleep());

        Assert.True(session.Game.OpeningDone);
        Assert.Equal(0, OpeningGames.SeedCount(session.Game, CropTypeId.Emberroot));
    }

    [Fact]
    public void BR_I1_A_refused_GiveStarterSeeds_can_be_tried_again_in_the_same_session()
    {
        // Only a successful hand-over counts as "given this session".
        var game = OpeningGames.NotDone(TestGames.BackpackWith((TestGames.Filler, 9 * Backpack.MaxStack)));
        var session = new GameSession(game, TestGames.Shipped);
        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), session.GiveStarterSeeds());

        game.Player.Backpack.Remove(TestGames.Filler, Backpack.MaxStack);

        Assert.Equal(ActionResult.Ok, session.GiveStarterSeeds());
    }
}
