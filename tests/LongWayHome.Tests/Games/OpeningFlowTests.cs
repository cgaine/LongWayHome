using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #139: <see cref="OpeningFlow"/>, the Opening's step order over a <see cref="GameSession"/> (BR-I1, AC7–AC9,
/// AC11, AC12, BR-G2). Steps advance only through their own input; the tool steps only through the session's
/// TileChanged after the matching action.
/// </summary>
public class OpeningFlowTests
{
    private static readonly GridPosition Soil = new(3, 2);

    private static readonly GridPosition Other = new(6, 4);

    private static (GameSession Session, OpeningFlow Flow) NewFlow(GameState? game = null)
    {
        var session = new GameSession(game ?? OpeningGames.New(), TestGames.Shipped);
        return (session, new OpeningFlow(session));
    }

    /// <summary>Plays the step the flow is on with its own input: lines finish, the walk arrives, the tool is used.</summary>
    private static void Play(GameSession session, OpeningFlow flow)
    {
        switch (flow.Current)
        {
            case OpeningStep.Crash:
                flow.CrashFinished();
                break;
            case OpeningStep.Walk:
                flow.WalkArrived();
                break;
            case OpeningStep.Till:
                Assert.Equal(ActionResult.Ok, session.Till(Soil));
                break;
            case OpeningStep.Plant:
                Assert.Equal(ActionResult.Ok, session.Plant(Soil, CropTypeId.Emberroot));
                break;
            case OpeningStep.Water:
                Assert.Equal(ActionResult.Ok, session.Water(Soil));
                break;
            default:
                flow.LinesFinished();
                break;
        }
    }

    /// <summary>Plays steps until the flow is on <paramref name="step"/>.</summary>
    private static void PlayTo(GameSession session, OpeningFlow flow, OpeningStep step)
    {
        for (var guard = 0; flow.Current != step; guard++)
        {
            Assert.True(guard < 20, $"the Opening never reached {step}; it is on {flow.Current}");
            Play(session, flow);
        }
    }

    /// <summary>Every input that is not a tool action: what a button press or the sequence's timers can send.</summary>
    private static void SendEveryNonToolInput(OpeningFlow flow, OpeningStep except)
    {
        if (except != OpeningStep.Crash)
        {
            flow.CrashFinished();
        }

        if (except != OpeningStep.Walk)
        {
            flow.WalkArrived();
        }

        if (except is not (OpeningStep.Arrival or OpeningStep.Farmhouse or OpeningStep.Seeds or OpeningStep.PlantLine
            or OpeningStep.WaterLine or OpeningStep.Farewell))
        {
            flow.LinesFinished();
        }
    }

    public static TheoryData<OpeningStep, string[]> DialogueSteps => new()
    {
        {
            OpeningStep.Arrival,
            [
                "Easy now. That was some landing. I saw your ship come down over the trees.",
                "Don't you worry, your secret's safe with me. I'm Maggie. I keep the store in town.",
                "There's an old farmhouse up here nobody's used in years. Come on, it's yours if you want it.",
            ]
        },
        {
            OpeningStep.Farmhouse,
            [
                "Here we are. A bit tired, like the rest of us, but it'll keep the rain off.",
                "You've a hoe there. Pick a patch of grass and turn the soil over.",
            ]
        },
        {
            OpeningStep.Seeds,
            ["That's it. Now, these are for you: emberroot and dusktuber, they grow well in spring."]
        },
        { OpeningStep.PlantLine, ["Pick the seeds and plant one in that soil."] },
        { OpeningStep.WaterLine, ["Now give it a drink. The pond's there when the can runs dry."] },
        {
            OpeningStep.Farewell,
            [
                "Water them every day and they'll grow. Sell what you harvest at my store.",
                "When you've some gold put by, I've an axe and a pickaxe that'll help you clear this meadow. Follow the path down through the forest to town.",
            ]
        },
    };

    // ---- BR-I1: start and order ----

    [Fact]
    public void BR_I1_A_new_game_on_spring_1_starts_the_opening_at_the_crash()
    {
        var (_, flow) = NewFlow();

        Assert.Equal(OpeningStep.Crash, flow.Current);
        Assert.True(flow.IsActive);
    }

    [Fact]
    public void BR_I1_The_steps_play_in_order_from_the_crash_to_done()
    {
        var (session, flow) = NewFlow();
        var seen = new List<OpeningStep> { flow.Current };
        flow.StepChanged += () => seen.Add(flow.Current);

        PlayTo(session, flow, OpeningStep.Done);

        Assert.Equal(
            [
                OpeningStep.Crash, OpeningStep.Arrival, OpeningStep.Walk, OpeningStep.Farmhouse, OpeningStep.Till,
                OpeningStep.Seeds, OpeningStep.PlantLine, OpeningStep.Plant, OpeningStep.WaterLine, OpeningStep.Water,
                OpeningStep.Farewell, OpeningStep.Done,
            ],
            seen);
    }

    [Fact]
    public void BR_I1_The_opening_is_not_done_until_the_farewell_lines_finish()
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, OpeningStep.Farewell);

        Assert.False(session.Game.OpeningDone);
        Assert.True(flow.IsActive);
    }

    [Fact]
    public void BR_I1_An_unfinished_game_not_on_spring_1_completes_the_opening_and_plays_nothing()
    {
        var (session, flow) = NewFlow(OpeningGames.NotDone(TestGames.ToolsOnly(), new GameDate(1, Season.Spring, 2)));

        Assert.Equal(OpeningStep.Done, flow.Current);
        Assert.False(flow.IsActive);
        Assert.True(session.Game.OpeningDone);
    }

    [Fact]
    public void BR_I1_Spring_1_of_a_later_year_is_not_the_opening_day()
    {
        var (session, flow) = NewFlow(OpeningGames.NotDone(TestGames.ToolsOnly(), new GameDate(2, Season.Spring, 1)));

        Assert.Equal(OpeningStep.Done, flow.Current);
        Assert.True(session.Game.OpeningDone);
    }

    // ---- AC7: crash then Maggie's lines ----

    [Fact]
    public void AC7_When_the_crash_finishes_maggie_speaks_her_arrival_lines()
    {
        var (_, flow) = NewFlow();

        flow.CrashFinished();

        Assert.Equal(OpeningStep.Arrival, flow.Current);
        Assert.Equal("Maggie", flow.Speaker);
        Assert.Equal(
            [
                "Easy now. That was some landing. I saw your ship come down over the trees.",
                "Don't you worry, your secret's safe with me. I'm Maggie. I keep the store in town.",
                "There's an old farmhouse up here nobody's used in years. Come on, it's yours if you want it.",
            ],
            flow.Lines);
    }

    [Theory]
    [MemberData(nameof(DialogueSteps))]
    public void AC7_Each_dialogue_step_has_maggies_lines_from_the_issue(OpeningStep step, string[] lines)
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, step);

        Assert.Equal(lines, flow.Lines);
        Assert.Equal("Maggie", flow.Speaker);
    }

    [Theory]
    [InlineData(OpeningStep.Crash)]
    [InlineData(OpeningStep.Walk)]
    [InlineData(OpeningStep.Till)]
    [InlineData(OpeningStep.Plant)]
    [InlineData(OpeningStep.Water)]
    [InlineData(OpeningStep.Done)]
    public void AC7_Steps_where_maggie_does_not_talk_have_no_lines(OpeningStep step)
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, step);

        Assert.Empty(flow.Lines);
    }

    [Fact]
    public void AC7_Finishing_a_block_of_lines_early_is_the_same_as_reading_them_all()
    {
        // open_menu skips the rest of a block: the DialogueBox finishes, and the flow hears LinesFinished either way.
        var (_, flow) = NewFlow();
        flow.CrashFinished();

        flow.LinesFinished();

        Assert.Equal(OpeningStep.Walk, flow.Current);
    }

    // ---- AC8, BR-G2: the clock ----

    [Theory]
    [InlineData(OpeningStep.Crash)]
    [InlineData(OpeningStep.Arrival)]
    [InlineData(OpeningStep.Farmhouse)]
    [InlineData(OpeningStep.Seeds)]
    [InlineData(OpeningStep.PlantLine)]
    [InlineData(OpeningStep.WaterLine)]
    [InlineData(OpeningStep.Farewell)]
    public void BR_G2_The_clock_does_not_run_during_the_crash_or_while_maggie_talks(OpeningStep step)
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, step);

        Assert.False(flow.ClockRuns);
    }

    [Theory]
    [InlineData(OpeningStep.Walk)]
    [InlineData(OpeningStep.Till)]
    [InlineData(OpeningStep.Plant)]
    [InlineData(OpeningStep.Water)]
    public void AC8_The_clock_runs_during_the_walk_and_the_till_plant_and_water_steps(OpeningStep step)
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, step);

        Assert.True(flow.ClockRuns);
    }

    // ---- AC9: nothing skips a step ----

    [Theory]
    [InlineData(OpeningStep.Walk)]
    [InlineData(OpeningStep.Till)]
    [InlineData(OpeningStep.Plant)]
    [InlineData(OpeningStep.Water)]
    public void AC9_No_other_input_moves_past_the_walk_or_a_tool_step(OpeningStep step)
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, step);
        var raised = 0;
        flow.StepChanged += () => raised++;

        flow.CrashFinished();
        flow.LinesFinished();
        if (step != OpeningStep.Walk)
        {
            flow.WalkArrived();
        }

        Assert.Equal(step, flow.Current);
        Assert.Equal(0, raised);
    }

    [Theory]
    [InlineData(OpeningStep.Crash)]
    [InlineData(OpeningStep.Arrival)]
    [InlineData(OpeningStep.Farmhouse)]
    public void AC9_Inputs_for_other_steps_are_ignored(OpeningStep step)
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, step);

        SendEveryNonToolInput(flow, except: step);

        Assert.Equal(step, flow.Current);
    }

    [Fact]
    public void AC9_The_till_step_moves_on_when_a_tile_is_tilled()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Till);

        Assert.Equal(ActionResult.Ok, session.Till(Soil));

        Assert.Equal(OpeningStep.Seeds, flow.Current);
    }

    [Fact]
    public void AC9_Tilling_before_the_till_step_does_not_skip_the_walk()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Walk);

        Assert.Equal(ActionResult.Ok, session.Till(Other));

        Assert.Equal(OpeningStep.Walk, flow.Current);
    }

    [Fact]
    public void AC9_A_refused_till_does_not_move_past_the_till_step()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Walk);
        Assert.Equal(ActionResult.Ok, session.Till(Other));
        PlayTo(session, flow, OpeningStep.Till);

        Assert.False(session.Till(Other).IsOk);

        Assert.Equal(OpeningStep.Till, flow.Current);
    }

    [Fact]
    public void AC9_The_plant_step_ignores_tilling_another_tile()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Plant);

        Assert.Equal(ActionResult.Ok, session.Till(Other));

        Assert.Equal(OpeningStep.Plant, flow.Current);
    }

    [Fact]
    public void AC9_The_plant_step_moves_on_when_a_seed_is_planted()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Plant);

        Assert.Equal(ActionResult.Ok, session.Plant(Soil, CropTypeId.Dusktuber));

        Assert.Equal(OpeningStep.WaterLine, flow.Current);
    }

    [Fact]
    public void AC9_The_water_step_ignores_watering_a_tile_with_nothing_planted()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Water);
        Assert.Equal(ActionResult.Ok, session.Till(Other));

        Assert.Equal(ActionResult.Ok, session.Water(Other));

        Assert.Equal(OpeningStep.Water, flow.Current);
    }

    [Fact]
    public void AC9_The_water_step_ignores_planting_another_seed()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Water);
        Assert.Equal(ActionResult.Ok, session.Till(Other));

        Assert.Equal(ActionResult.Ok, session.Plant(Other, CropTypeId.Emberroot));

        Assert.Equal(OpeningStep.Water, flow.Current);
    }

    [Fact]
    public void AC9_The_water_step_moves_on_when_a_planted_tile_is_watered()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Water);

        Assert.Equal(ActionResult.Ok, session.Water(Soil));

        Assert.Equal(OpeningStep.Farewell, flow.Current);
    }

    [Fact]
    public void AC9_Each_step_change_raises_StepChanged_once()
    {
        var (session, flow) = NewFlow();
        var raised = 0;
        flow.StepChanged += () => raised++;

        PlayTo(session, flow, OpeningStep.Done);

        Assert.Equal(11, raised);
    }

    // ---- BR-P6: the seeds come from Maggie ----

    [Fact]
    public void BR_P6_Maggie_gives_the_starter_seeds_when_her_seeds_lines_finish()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Seeds);
        Assert.Equal(0, OpeningGames.SeedCount(session.Game, CropTypeId.Emberroot));

        flow.LinesFinished();

        Assert.Equal(OpeningStep.PlantLine, flow.Current);
        Assert.Equal(10, OpeningGames.SeedCount(session.Game, CropTypeId.Emberroot));
        Assert.Equal(5, OpeningGames.SeedCount(session.Game, CropTypeId.Dusktuber));
    }

    [Fact]
    public void BR_P6_Playing_the_whole_opening_gives_the_seeds_once()
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, OpeningStep.Done);

        // One emberroot was planted.
        Assert.Equal(9, OpeningGames.SeedCount(session.Game, CropTypeId.Emberroot));
        Assert.Equal(5, OpeningGames.SeedCount(session.Game, CropTypeId.Dusktuber));
    }

    // ---- AC11: the end ----

    [Fact]
    public void AC11_Finishing_the_farewell_lines_completes_the_opening()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Farewell);

        flow.LinesFinished();

        Assert.Equal(OpeningStep.Done, flow.Current);
        Assert.False(flow.IsActive);
        Assert.True(session.Game.OpeningDone);
    }

    [Fact]
    public void AC11_After_the_opening_a_new_flow_over_the_same_game_plays_nothing()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Done);

        var again = new OpeningFlow(session);

        Assert.Equal(OpeningStep.Done, again.Current);
        Assert.False(again.IsActive);
    }

    [Fact]
    public void AC11_Inputs_after_the_opening_ends_change_nothing()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Done);
        var raised = 0;
        flow.StepChanged += () => raised++;

        flow.CrashFinished();
        flow.LinesFinished();
        flow.WalkArrived();
        session.Till(Other);

        Assert.Equal(OpeningStep.Done, flow.Current);
        Assert.Equal(0, raised);
    }

    // ---- AC12: a loaded Game past the Opening ----

    [Fact]
    public void AC12_A_game_whose_opening_is_done_starts_the_flow_done_and_inactive()
    {
        var (_, flow) = NewFlow(TestGames.WithEnergy(150));

        Assert.Equal(OpeningStep.Done, flow.Current);
        Assert.False(flow.IsActive);
        Assert.Empty(flow.Lines);
    }

    [Fact]
    public void AC12_A_game_whose_opening_is_done_ignores_the_crash_input()
    {
        var (_, flow) = NewFlow(TestGames.WithEnergy(150));

        flow.CrashFinished();

        Assert.Equal(OpeningStep.Done, flow.Current);
    }

    // ---- the step hint ----

    [Theory]
    [InlineData(OpeningStep.Till, InputDeviceKind.KeyboardMouse, "[Space] Till a tile")]
    [InlineData(OpeningStep.Till, InputDeviceKind.Controller, "(A) Till a tile")]
    [InlineData(OpeningStep.Plant, InputDeviceKind.KeyboardMouse, "[Space] Plant a seed")]
    [InlineData(OpeningStep.Plant, InputDeviceKind.Controller, "(A) Plant a seed")]
    [InlineData(OpeningStep.Water, InputDeviceKind.KeyboardMouse, "[Space] Water it")]
    [InlineData(OpeningStep.Water, InputDeviceKind.Controller, "(A) Water it")]
    public void Each_tool_step_shows_its_hint_with_the_use_tool_glyph(
        OpeningStep step, InputDeviceKind device, string hint)
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, step);

        Assert.Equal(hint, flow.Hint(device));
        Assert.StartsWith(InputGlyphs.For("use_tool", device) + " ", flow.Hint(device));
    }

    [Theory]
    [InlineData(OpeningStep.Crash)]
    [InlineData(OpeningStep.Arrival)]
    [InlineData(OpeningStep.Walk)]
    [InlineData(OpeningStep.Seeds)]
    [InlineData(OpeningStep.WaterLine)]
    [InlineData(OpeningStep.Farewell)]
    [InlineData(OpeningStep.Done)]
    public void Hint_is_null_on_steps_without_a_tool_action(OpeningStep step)
    {
        var (session, flow) = NewFlow();

        PlayTo(session, flow, step);

        Assert.Null(flow.Hint(InputDeviceKind.KeyboardMouse));
        Assert.Null(flow.Hint(InputDeviceKind.Controller));
    }

    // ---- detaching ----

    [Fact]
    public void A_disposed_flow_no_longer_follows_the_sessions_tile_changes()
    {
        var (session, flow) = NewFlow();
        PlayTo(session, flow, OpeningStep.Till);

        flow.Dispose();
        session.Till(Soil);

        Assert.Equal(OpeningStep.Till, flow.Current);
    }
}
