using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>#138: <see cref="DialogueFlow"/> state the AC tests don't reach: what it reads while closed, a conversation
/// replaced while open, and its own copy of the lines.</summary>
public class DialogueFlowStateTests
{
    [Fact]
    public void A_closed_flow_has_no_line_and_is_not_on_its_last_line()
    {
        var flow = new DialogueFlow();
        Assert.Equal("", flow.Speaker);
        Assert.Equal("", flow.CurrentLine);
        Assert.False(flow.IsLastLine);

        flow.Start("Maggie", ["One."]);
        flow.Advance();

        Assert.Equal("", flow.CurrentLine);
        Assert.False(flow.IsLastLine);
        Assert.Equal("Maggie", flow.Speaker);
    }

    [Fact]
    public void Starting_while_open_replaces_the_conversation_without_raising_finished()
    {
        var flow = new DialogueFlow();
        var finished = 0;
        flow.Finished += () => finished++;
        flow.Start("Maggie", ["One.", "Two."]);

        flow.Start("Clyde", ["Mind the lake."]);

        Assert.Equal(0, finished);
        Assert.Equal("Clyde", flow.Speaker);
        Assert.Equal("Mind the lake.", flow.CurrentLine);
        Assert.True(flow.IsLastLine);
        flow.Advance();
        Assert.Equal(1, finished);
    }

    [Fact]
    public void The_flow_keeps_its_own_copy_of_the_lines()
    {
        var lines = new List<string> { "One.", "Two." };
        var flow = new DialogueFlow();
        flow.Start("Maggie", lines);

        lines.Clear();
        flow.Advance();

        Assert.Equal("Two.", flow.CurrentLine);
    }

    [Fact]
    public void A_refused_start_leaves_an_open_conversation_as_it_was()
    {
        var flow = new DialogueFlow();
        flow.Start("Maggie", ["One.", "Two."]);

        Assert.ThrowsAny<ArgumentException>(() => flow.Start("Clyde", []));

        Assert.True(flow.IsOpen);
        Assert.Equal("Maggie", flow.Speaker);
        Assert.Equal("One.", flow.CurrentLine);
    }
}
