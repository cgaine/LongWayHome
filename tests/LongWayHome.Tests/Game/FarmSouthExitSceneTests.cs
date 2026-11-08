using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>Farm.tscn</c> and checks that #150's south exit, now at the grid's south edge, can't be touched
/// from a grid Tile: farming the south row never sends the Player to the forest path.
/// </summary>
public class FarmSouthExitSceneTests
{
    private const string ExitScript = "res://scripts/World/AreaExit.cs";

    /// <summary>The grid's south edge in world z (FarmGridLayout).</summary>
    private const double GridSouthZ = 31.5;

    /// <summary>The exit trigger is 1 tile deep; the Player's capsule radius is 0.35.</summary>
    private const double TriggerHalfDepth = 0.5;

    private const double PlayerRadius = 0.35;

    [Fact]
    public void AC7_south_exit_trigger_lies_beyond_the_grid_so_farming_the_south_row_never_leaves()
    {
        var scene = ReadRepoFile("game", "scenes", "Farm.tscn");
        var exit = Assert.Single(ScriptedNodes(scene, ExitScript), node => node.Name == "SouthExit");
        var (_, _, z) = Position(scene, exit);

        // A Player standing anywhere on the grid (centre at most z 31.5) reaches z 31.85 at most.
        Assert.True(
            z - TriggerHalfDepth >= GridSouthZ + PlayerRadius - 1e-3,
            $"SouthExit's trigger starts at z = {z - TriggerHalfDepth}, within reach of a Player on the grid's south row");
    }
}
