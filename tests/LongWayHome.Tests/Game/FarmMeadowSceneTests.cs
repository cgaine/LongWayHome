using System.Globalization;
using System.Text.RegularExpressions;
using LongWayHome.Application.Art;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>Farm.tscn</c> and checks #150's grown farm: the <c>Boundary</c> walls enclose the 72 x 36 grid
/// (world x −36..36, z −4.5..31.5), the south exit and arrival sit at the new south edge on x = 0, no interaction point
/// or collision body stands on a grid Tile, the pond has moved north of the grid at (10, 0, −9) with its refill point,
/// and the river's points run along the x = 36.5 bank (BR-A2).
/// </summary>
public class FarmMeadowSceneTests
{
    private const string PointScript = "res://scripts/World/InteractionPoint.cs";

    private const string RefillScript = "res://scripts/World/WaterRefill.cs";

    private const string ExitScript = "res://scripts/World/AreaExit.cs";

    private const string ArrivalScript = "res://scripts/World/AreaArrival.cs";

    // The grid's world rectangle (FarmGridLayout): x -36..36, z -4.5..31.5.
    private const double GridMinX = -36.0;
    private const double GridMaxX = 36.0;
    private const double GridMinZ = -4.5;
    private const double GridMaxZ = 31.5;

    // The farm's outer edges: the west wall at x -36.5, the north wall at z -13.5, the south wall at z 32 and the
    // river bank's face at x 36.5.
    private const double WestWallX = -36.5;
    private const double NorthWallZ = -13.5;
    private const double SouthWallZ = 32.0;
    private const double BankFaceX = 36.5;

    /// <summary>The Player's radius: a point on the bank stands at most this far west of the bank face.</summary>
    private const double PlayerRadius = 0.35;

    private const double Tolerance = 1e-3;

    private static string Farm() => ReadRepoFile("game", "scenes", "Farm.tscn");

    // ---- AC7: the walls enclose the grid ----

    [Fact]
    public void AC7_west_wall_stands_at_x_minus_36_5_along_the_whole_west_edge()
    {
        var wall = Assert.Single(Walls(Farm()), w => w.Name == "West");

        Assert.InRange(wall.X, WestWallX - 0.25, WestWallX + 0.25);
        Assert.True(wall.MaxX <= GridMinX + Tolerance, $"Boundary/West reaches x = {wall.MaxX}, onto the grid");
        AssertSpansZ(wall, NorthWallZ + 0.5, SouthWallZ - 0.5);
    }

    [Fact]
    public void AC7_east_wall_is_the_river_bank_at_x_36_5_along_the_whole_east_edge()
    {
        var wall = Assert.Single(Walls(Farm()), w => w.Name == "East");

        Assert.InRange(wall.MinX, BankFaceX - 0.05, BankFaceX + 0.05);
        AssertSpansZ(wall, NorthWallZ + 0.5, SouthWallZ - 0.5);
    }

    [Fact]
    public void AC7_north_wall_stays_at_z_minus_13_5_across_the_whole_farm()
    {
        var wall = Assert.Single(Walls(Farm()), w => w.Name == "North");

        Assert.InRange(wall.Z, NorthWallZ - 0.25, NorthWallZ + 0.25);
        Assert.True(wall.MinX <= WestWallX + Tolerance && wall.MaxX >= BankFaceX - Tolerance,
            $"Boundary/North spans x = {wall.MinX} to {wall.MaxX}, short of x {WestWallX}..{BankFaceX}");
    }

    [Fact]
    public void AC7_south_walls_at_z_32_close_the_south_edge_but_a_3_tile_gap_on_x_0()
    {
        var south = Walls(Farm()).Where(w => Math.Abs(w.Z - SouthWallZ) < 0.25).ToList();
        Assert.True(south.Count > 0, "Farm.tscn's Boundary has no wall at the grid's south edge (z = 32)");

        foreach (var wall in south)
        {
            Assert.True(wall.MinZ >= GridMaxZ - Tolerance, $"Boundary/{wall.Name} reaches z = {wall.MinZ}, onto the grid");
            Assert.True(
                wall.MaxX <= -1.5 + Tolerance || wall.MinX >= 1.5 - Tolerance,
                $"Boundary/{wall.Name} spans x = {wall.MinX} to {wall.MaxX}, closing the south exit at x = 0");
        }

        // West of the gap and east of it, the walls leave no hole wider than a hair.
        AssertCovered(south, WestWallX + 0.5, -1.5, "west of the exit");
        AssertCovered(south, 1.5, BankFaceX, "east of the exit");
    }

    // ---- AC7: the exit and arrival move to the new south edge ----

    [Fact]
    public void AC7_south_exit_is_centred_on_x_0_at_the_grid_south_edge_and_leads_to_the_forest_path()
    {
        var scene = Farm();
        var exit = Assert.Single(ScriptedNodes(scene, ExitScript), node => node.Name == "SouthExit");
        var (x, _, z) = Position(scene, exit);

        Assert.InRange(x, -0.01, 0.01);
        Assert.InRange(z, SouthWallZ - 0.6, SouthWallZ + 0.6);
        Assert.Equal("forest_path", Property(exit, "TargetArea"));
        Assert.Equal("forest_path_north", Property(exit, "ArrivalId"));

        var path = Nodes(scene).SingleOrDefault(node => node.Name == "SouthExitPath");
        Assert.True(path is not null, "Farm.tscn has no SouthExitPath");
        var (pathX, _, pathZ) = Position(scene, path!);
        Assert.InRange(pathX, -0.01, 0.01);
        Assert.InRange(pathZ, GridMaxZ, SouthWallZ + 2.5);
    }

    [Fact]
    public void AC7_south_arrival_is_1_5_tiles_inside_the_south_exit()
    {
        var scene = Farm();
        var arrival = Assert.Single(ScriptedNodes(scene, ArrivalScript), node => Property(node, "ArrivalId") == "farm_south");
        var exit = Assert.Single(ScriptedNodes(scene, ExitScript), node => node.Name == "SouthExit");
        var (x, _, z) = Position(scene, arrival);
        var exitZ = Position(scene, exit).Z;

        Assert.InRange(x, -0.01, 0.01);
        Assert.InRange(z, 30.25, 30.75);
        Assert.Equal(1.5, exitZ - z, 2);
    }

    // ---- AC7: nothing solid or interactive stands on a grid Tile ----

    [Fact]
    public void AC7_no_interaction_point_stands_on_the_grid()
    {
        var scene = Farm();
        var points = ScriptedNodes(scene, PointScript);
        Assert.NotEmpty(points);

        var onGrid = points
            .Select(point => (point.Path, Position: Position(scene, point)))
            .Where(point => OnGrid(point.Position.X, point.Position.Z))
            .Select(point => $"{point.Path} at ({point.Position.X}, {point.Position.Z})")
            .ToList();
        Assert.True(onGrid.Count == 0, $"Interaction points on the grid: {string.Join(", ", onGrid)}");
    }

    [Fact]
    public void AC7_no_collision_body_overlaps_a_grid_tile()
    {
        var footprints = CollisionFootprints(Farm());
        Assert.Contains(footprints, f => f.Path.StartsWith("House/", StringComparison.Ordinal));
        Assert.Contains(footprints, f => f.Path.StartsWith("Pond/", StringComparison.Ordinal));
        Assert.Contains(footprints, f => f.Path.StartsWith("Boundary/", StringComparison.Ordinal));

        var overlapping = footprints
            .Where(f => f.MinX < GridMaxX - Tolerance && f.MaxX > GridMinX + Tolerance
                && f.MinZ < GridMaxZ - Tolerance && f.MaxZ > GridMinZ + Tolerance)
            .Select(f => $"{f.Path} (x {f.MinX}..{f.MaxX}, z {f.MinZ}..{f.MaxZ})")
            .ToList();
        Assert.True(overlapping.Count == 0, $"Collision bodies on the grid: {string.Join(", ", overlapping)}");
    }

    [Fact]
    public void AC7_shop_stall_if_still_present_stands_north_of_the_grid()
    {
        var scene = Farm();
        var stall = Nodes(scene).SingleOrDefault(node => node.Name == "ShopStall" && node.Parent == ".");
        if (stall is null)
        {
            return; // #147 removed it
        }

        var (x, _, z) = Position(scene, stall);
        Assert.True(z < GridMinZ, $"ShopStall at ({x}, {z}) is not north of the grid (z < {GridMinZ})");
    }

    [Fact]
    public void AC7_farmhouse_door_and_bin_stay_where_they_are()
    {
        // Characterization: the grid is laid out round the old plot, so these don't move.
        var scene = Farm();
        var nodes = Nodes(scene);

        Assert.Equal((0.0, 1.5, -9.0), Position(scene, nodes.Single(node => node.Path == "House")));
        Assert.Equal((0.0, 0.0, -7.0), Position(scene, nodes.Single(node => node.Path == "InteractionPoints/DoorPoint")));
        if (nodes.SingleOrDefault(node => node.Path == "InteractionPoints/BinPoint") is { } bin)
        {
            Assert.Equal((3.0, 0.0, -6.0), Position(scene, bin));
        }
    }

    // ---- AC8: the pond and river still refill the can ----

    [Fact]
    public void AC8_pond_moves_north_of_the_grid_with_its_refill_point_at_the_same_offset()
    {
        var scene = Farm();
        var nodes = Nodes(scene);
        var pond = nodes.Single(node => node.Path == "Pond");
        var point = Assert.Single(ScriptedNodes(scene, PointScript), node => Property(node, "PointId") == "pond");

        var pondAt = Position(scene, pond);
        var pointAt = Position(scene, point);
        Assert.Equal((10.0, 0.0, -9.0), pondAt);

        // Today's PondPoint stands at the pond's centre (offset 0, 0, 0) with range 3.8.
        Assert.Equal((0.0, 0.0, 0.0), (pointAt.X - pondAt.X, pointAt.Y - pondAt.Y, pointAt.Z - pondAt.Z));
        Assert.Equal(3.8, Number(point, "Range") ?? 0, 3);
        var refill = Assert.Single(ScriptedNodes(scene, RefillScript), node => node.Parent == point.Path);
        Assert.True(Property(refill, "Source") is null or "pond", $"the pond's WaterRefill has Source = \"{Property(refill, "Source")}\"");
    }

    [Fact]
    public void AC8_a_river_point_on_the_new_bank_refills_from_the_river()
    {
        var scene = Farm();
        var onBank = RiverPoints(scene)
            .Where(point => point.X >= BankFaceX - 1.5 && point.X <= BankFaceX - PlayerRadius + Tolerance)
            .Where(point => Property(Assert.Single(ScriptedNodes(scene, RefillScript), node => node.Parent == point.Node.Path), "Source") == "river")
            .ToList();

        Assert.NotEmpty(onBank);
    }

    // ---- BR-A2: the river runs along the meadow ----

    [Fact]
    public void BR_A2_river_points_run_along_the_x_36_5_bank_no_more_than_2_apart_from_z_minus_12_to_31()
    {
        var scene = Farm();
        var points = RiverPoints(scene);
        Assert.True(points.Count >= 2, $"Farm.tscn has {points.Count} river points");

        foreach (var point in points)
        {
            Assert.InRange(point.X, BankFaceX - 1.5, BankFaceX - PlayerRadius + Tolerance);
            Assert.InRange(point.Z, NorthWallZ + 0.5, GridMaxZ);
            Assert.Equal(1.5, Number(point.Node, "Range") ?? 0, 3);
            var refill = Assert.Single(ScriptedNodes(scene, RefillScript), node => node.Parent == point.Node.Path);
            Assert.Equal("river", Property(refill, "Source"));
        }

        var zs = points.Select(point => point.Z).Order().ToList();
        Assert.True(zs[0] <= -11.0, $"The farm's northernmost river point is at z = {zs[0]}, short of z -12");
        Assert.True(zs[^1] >= 30.0, $"The farm's southernmost river point is at z = {zs[^1]}, short of z 31");
        foreach (var (a, b) in zs.Zip(zs.Skip(1)))
        {
            Assert.True(b - a <= 2.0 + Tolerance, $"The farm's river points at z = {a} and z = {b} are more than 2 apart");
        }
    }

    [Fact]
    public void BR_A2_meadow_river_bank_is_at_the_grids_east_edge()
    {
        Assert.Equal(36.5f, MeadowLayout.RiverWestX);
    }

    [Fact]
    public void BR_A2_farm_river_view_runs_the_whole_length_of_the_farm()
    {
        var source = Code(ReadRepoFile("game", "art", "areas", "FarmRiverView.cs"));

        var min = Regex.Match(source, @"\bMinZ\s*=\s*(-?[\d.]+)f?\s*;");
        var max = Regex.Match(source, @"\bMaxZ\s*=\s*(-?[\d.]+)f?\s*;");
        Assert.True(min.Success && max.Success, "FarmRiverView.cs declares no MinZ / MaxZ");
        Assert.True(double.Parse(min.Groups[1].Value, CultureInfo.InvariantCulture) <= NorthWallZ,
            $"FarmRiverView's river starts at z = {min.Groups[1].Value}, south of the north wall");
        Assert.True(double.Parse(max.Groups[1].Value, CultureInfo.InvariantCulture) >= SouthWallZ,
            $"FarmRiverView's river ends at z = {max.Groups[1].Value}, north of the south wall");
    }

    // ---- Helpers ----

    private static bool OnGrid(double x, double z) =>
        x > GridMinX + Tolerance && x < GridMaxX - Tolerance && z > GridMinZ + Tolerance && z < GridMaxZ - Tolerance;

    private sealed record Footprint(string Path, string Name, double X, double Z, double MinX, double MaxX, double MinZ, double MaxZ);

    private sealed record RiverPoint(Node Node, double X, double Z);

    private static List<RiverPoint> RiverPoints(string scene) =>
        ScriptedNodes(scene, PointScript)
            .Where(node => Property(node, "PointId") == "river")
            .Select(node =>
            {
                var (x, _, z) = Position(scene, node);
                return new RiverPoint(node, x, z);
            })
            .ToList();

    private static List<Footprint> Walls(string scene) =>
        CollisionFootprints(scene).Where(f => f.Path.StartsWith("Boundary/", StringComparison.Ordinal)).ToList();

    private static void AssertSpansZ(Footprint wall, double from, double to) =>
        Assert.True(wall.MinZ <= from + Tolerance && wall.MaxZ >= to - Tolerance,
            $"Boundary/{wall.Name} spans z = {wall.MinZ} to {wall.MaxZ}, short of z {from}..{to}");

    /// <summary>Asserts the walls' x ranges together cover <paramref name="from"/>..<paramref name="to"/>.</summary>
    private static void AssertCovered(List<Footprint> walls, double from, double to, string where)
    {
        var reached = from;
        foreach (var wall in walls.OrderBy(w => w.MinX))
        {
            if (wall.MinX <= reached + 0.05)
            {
                reached = Math.Max(reached, wall.MaxX);
            }
        }

        Assert.True(reached >= to - 0.05, $"The south walls {where} stop at x = {reached}, short of x = {to}");
    }

    /// <summary>
    /// The XZ footprint of every collision shape under a <c>StaticBody3D</c> in the scene: a <c>BoxShape3D</c>'s
    /// size or a <c>CylinderShape3D</c>'s radius about the shape's position (its parents' translations added).
    /// </summary>
    private static List<Footprint> CollisionFootprints(string scene)
    {
        var shapes = SubResources(scene);
        var nodes = Nodes(scene);
        var byPath = nodes.ToDictionary(node => node.Path);
        var footprints = new List<Footprint>();
        foreach (var node in nodes.Where(node => node.Type == "CollisionShape3D"))
        {
            if (node.Parent is null || !byPath.TryGetValue(node.Parent, out var body) || body.Type != "StaticBody3D")
            {
                continue;
            }

            var shapeId = Regex.Match(node.Body, @"^shape\s*=\s*SubResource\(""([^""]+)""\)", RegexOptions.Multiline);
            Assert.True(shapeId.Success && shapes.ContainsKey(shapeId.Groups[1].Value), $"{node.Path} has no shape sub_resource");
            var (type, body2) = shapes[shapeId.Groups[1].Value];
            var (x, _, z) = Position(scene, node);
            double halfX, halfZ;
            if (type == "BoxShape3D")
            {
                var size = Regex.Match(body2, @"^size\s*=\s*Vector3\(([^)]*)\)", RegexOptions.Multiline);
                Assert.True(size.Success, $"{node.Path}'s BoxShape3D has no size");
                var parts = size.Groups[1].Value.Split(',').Select(p => double.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();
                (halfX, halfZ) = (parts[0] / 2, parts[2] / 2);
            }
            else if (type == "CylinderShape3D")
            {
                var radius = Regex.Match(body2, @"^radius\s*=\s*([-0-9.eE]+)", RegexOptions.Multiline);
                var r = radius.Success ? double.Parse(radius.Groups[1].Value, CultureInfo.InvariantCulture) : 0.5;
                (halfX, halfZ) = (r, r);
            }
            else
            {
                Assert.Fail($"{node.Path} uses a {type}, which this test can't measure");
                continue;
            }

            footprints.Add(new Footprint(node.Path, node.Name, x, z, x - halfX, x + halfX, z - halfZ, z + halfZ));
        }

        return footprints;
    }

    /// <summary>Every <c>[sub_resource]</c>'s id to its type and property lines.</summary>
    private static Dictionary<string, (string Type, string Body)> SubResources(string scene) =>
        Regex.Matches(scene, @"^\[sub_resource\s+(?<attrs>[^\]]*)\]\s*$(?<body>.*?)(?=^\[|\z)", RegexOptions.Multiline | RegexOptions.Singleline)
            .Select(match => (
                Id: Regex.Match(match.Groups["attrs"].Value, @"\bid=""([^""]+)""").Groups[1].Value,
                Type: Regex.Match(match.Groups["attrs"].Value, @"\btype=""([^""]+)""").Groups[1].Value,
                Body: match.Groups["body"].Value))
            .ToDictionary(item => item.Id, item => (item.Type, item.Body));
}
