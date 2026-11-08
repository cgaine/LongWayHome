using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #138: the dialogue box's line-stepping, <see cref="DialogueFlow"/> (AC8, AC10): advancing line by line, closing after
/// the last, skipping at once, and <see cref="DialogueFlow.Finished"/> raised exactly once per conversation.
/// </summary>
public class DialogueFlowTests
{
    private static (DialogueFlow Flow, Func<int> Finished) Started(params string[] lines)
    {
        var flow = new DialogueFlow();
        var finished = 0;
        flow.Finished += () => finished++;
        flow.Start("Maggie", lines);
        return (flow, () => finished);
    }

    [Fact]
    public void AC8_Start_opens_on_the_first_line_with_the_speaker()
    {
        var (flow, finished) = Started("One.", "Two.");

        Assert.True(flow.IsOpen);
        Assert.Equal("Maggie", flow.Speaker);
        Assert.Equal("One.", flow.CurrentLine);
        Assert.False(flow.IsLastLine);
        Assert.Equal(0, finished());
    }

    [Fact]
    public void AC8_A_one_line_dialogue_opens_on_its_last_line_and_advance_closes_it()
    {
        var (flow, finished) = Started("Fish bite best at first light.");

        Assert.True(flow.IsLastLine);
        flow.Advance();

        Assert.False(flow.IsOpen);
        Assert.Equal(1, finished());
    }

    [Fact]
    public void AC10_Advance_steps_through_each_line_then_closes_after_the_last_raising_finished_once()
    {
        var (flow, finished) = Started("One.", "Two.", "Three.");

        flow.Advance();
        Assert.Equal("Two.", flow.CurrentLine);
        Assert.False(flow.IsLastLine);
        Assert.Equal(0, finished());

        flow.Advance();
        Assert.Equal("Three.", flow.CurrentLine);
        Assert.True(flow.IsLastLine);
        Assert.True(flow.IsOpen);
        Assert.Equal(0, finished());

        flow.Advance();
        Assert.False(flow.IsOpen);
        Assert.Equal(1, finished());
    }

    [Fact]
    public void AC10_Skip_closes_a_multi_line_dialogue_at_once_and_raises_finished_once()
    {
        var (flow, finished) = Started("One.", "Two.", "Three.");

        flow.Skip();

        Assert.False(flow.IsOpen);
        Assert.Equal(1, finished());
    }

    [Fact]
    public void AC10_Skip_on_a_later_line_also_closes_at_once()
    {
        var (flow, finished) = Started("One.", "Two.", "Three.");
        flow.Advance();

        flow.Skip();

        Assert.False(flow.IsOpen);
        Assert.Equal(1, finished());
    }

    [Fact]
    public void AC10_Advance_and_skip_after_closing_are_ignored_and_never_raise_finished_again()
    {
        var (flow, finished) = Started("One.", "Two.");
        flow.Skip();

        flow.Advance();
        flow.Skip();
        flow.Advance();

        Assert.False(flow.IsOpen);
        Assert.Equal(1, finished());
    }

    [Fact]
    public void AC10_A_new_flow_is_not_open_and_ignores_advance_and_skip()
    {
        var flow = new DialogueFlow();
        var finished = 0;
        flow.Finished += () => finished++;

        flow.Advance();
        flow.Skip();

        Assert.False(flow.IsOpen);
        Assert.Equal(0, finished);
    }

    [Fact]
    public void AC10_A_second_conversation_raises_finished_once_more()
    {
        var (flow, finished) = Started("One.");
        flow.Advance();

        flow.Start("Clyde", ["Mind the lake."]);
        Assert.True(flow.IsOpen);
        Assert.Equal("Clyde", flow.Speaker);
        Assert.Equal("Mind the lake.", flow.CurrentLine);
        flow.Advance();

        Assert.False(flow.IsOpen);
        Assert.Equal(2, finished());
    }

    [Fact]
    public void Start_refuses_no_lines_a_null_list_and_a_null_speaker()
    {
        var flow = new DialogueFlow();

        Assert.ThrowsAny<ArgumentException>(() => flow.Start("Maggie", []));
        Assert.ThrowsAny<ArgumentException>(() => flow.Start("Maggie", null!));
        Assert.ThrowsAny<ArgumentException>(() => flow.Start(null!, ["One."]));
        Assert.False(flow.IsOpen);
    }
}
