namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files for #165 AC6: the wake-up fade shows "The general store has been rebuilt." on a
/// <c>Panel/Rebuilt</c> line under the Night's message, from <c>BuildingsRebuilt</c> or, when the Night ended off the
/// farm, from <c>PendingNightMessage.Rebuilt</c> left by <c>NightReturn</c>.
/// </summary>
public class NightRebuildGlueTests
{
    private static string Script(params string[] parts) =>
        SceneText.Code(SceneText.ReadRepoFile(new[] { "game" }.Concat(parts).ToArray()));

    [Fact]
    public void AC6_NightTransition_tscn_has_a_Rebuilt_label_under_Panel()
    {
        var scene = SceneText.ReadRepoFile("game", "ui", "NightTransition.tscn");

        var node = Assert.Single(SceneText.Nodes(scene), n => n.Path == "Panel/Rebuilt");
        Assert.Equal("Label", node.Type);
    }

    [Fact]
    public void AC6_NightTransition_shows_the_rebuilt_lines_from_BuildingsRebuilt_and_from_the_pending_message()
    {
        var source = Script("scripts", "Ui", "NightTransition.cs");

        Assert.Contains("BuildingsRebuilt +=", source);
        Assert.Contains("BuildingsRebuilt -=", source);
        Assert.Contains("BuildingLabels.RebuiltText(", source);
        Assert.Contains("\"Panel/Rebuilt\"", source);
        Assert.Contains("pending.Rebuilt", source);
    }

    [Fact]
    public void AC6_NightReturn_keeps_the_rebuilt_ids_on_the_pending_message()
    {
        var source = Script("scripts", "World", "NightReturn.cs");

        Assert.Contains("BuildingsRebuilt +=", source);
        Assert.Contains("BuildingsRebuilt -=", source);
        var call = source.IndexOf("new PendingNightMessage(", StringComparison.Ordinal);
        Assert.True(call >= 0, "NightReturn never builds a PendingNightMessage");
        var args = source[call..source.IndexOf(';', call)];
        Assert.Equal(3, args.Split(',').Length);
    }

    [Fact]
    public void AC6_PendingNightMessage_carries_the_rebuilt_ids()
    {
        var source = Script("scripts", "Core", "GameRoot.cs");

        Assert.Contains(
            "record PendingNightMessage(DayEndReason Reason, bool SaveFailed, IReadOnlyList<BuildingId> Rebuilt)",
            source);
    }
}
