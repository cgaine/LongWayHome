using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>Farm.tscn</c> and checks that the Opening (#139) follows the farm's moved south edge (#150):
/// Maggie starts beside the moved <c>SouthArrival</c>, her walk heads north from there up the grid and ends at the
/// farmhouse door, and her <c>ExitPoint</c> lies beyond the new south edge.
/// </summary>
public class FarmOpeningSceneTests
{
    private const string ArrivalScript = "res://scripts/World/AreaArrival.cs";

    /// <summary>The grid's south edge in world z (FarmGridLayout).</summary>
    private const double GridSouthZ = 31.5;

    private static (double X, double Y, double Z) At(string scene, string path) =>
        Position(scene, Nodes(scene).Single(node => node.Path == path));

    private static double Distance((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Z - b.Z) * (a.Z - b.Z)));

    [Fact]
    public void AC7_opening_starts_at_the_new_south_edge()
    {
        var scene = ReadRepoFile("game", "scenes", "Farm.tscn");
        var arrival = Position(scene, Assert.Single(
            ScriptedNodes(scene, ArrivalScript), node => Property(node, "ArrivalId") == "farm_south"));
        var maggie = At(scene, "OpeningSequence/Maggie");

        // Maggie stands beside the farmer where he arrives, at the grid's south edge.
        Assert.True(Distance(maggie, arrival) <= 1.5, $"Maggie starts at ({maggie.X}, {maggie.Z}), not beside the arrival ({arrival.X}, {arrival.Z})");
        Assert.True(arrival.Z >= GridSouthZ - 2.5, $"The farm's south arrival is at z = {arrival.Z}, not at the grid's south edge");
    }

    [Fact]
    public void AC7_opening_walk_heads_north_up_the_grid_and_ends_at_the_farmhouse_door()
    {
        var scene = ReadRepoFile("game", "scenes", "Farm.tscn");
        var maggie = At(scene, "OpeningSequence/Maggie");
        var door = At(scene, "InteractionPoints/DoorPoint");
        var waypoints = Nodes(scene)
            .Where(node => node.Parent == "OpeningSequence/Waypoints")
            .Select(node => Position(scene, node))
            .ToList();
        Assert.True(waypoints.Count >= 2, "Maggie's walk needs at least 2 waypoints");

        // Each waypoint is north of the last, starting north of Maggie, so she walks up the grid to the house.
        var z = maggie.Z;
        foreach (var waypoint in waypoints)
        {
            Assert.True(waypoint.Z < z, $"Waypoint at z = {waypoint.Z} is not north of z = {z}");
            z = waypoint.Z;
        }

        Assert.True(Distance(waypoints[^1], door) <= 2.0, $"The walk ends at ({waypoints[^1].X}, {waypoints[^1].Z}), not beside the door");
    }

    [Fact]
    public void AC7_maggie_leaves_beyond_the_new_south_edge()
    {
        var scene = ReadRepoFile("game", "scenes", "Farm.tscn");
        var exitPoint = At(scene, "OpeningSequence/ExitPoint");

        Assert.True(exitPoint.Z > GridSouthZ, $"Maggie's ExitPoint is at z = {exitPoint.Z}, on the grid rather than beyond its south edge");
        Assert.InRange(exitPoint.X, -1.5, 1.5);
    }
}
