namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real Godot files and checks #145's counter at Maggie's corner of the general store: a <c>shop</c>
/// <c>InteractionPoint</c> in <c>Town.tscn</c>, far enough from Maggie that the counter and her talk range never
/// overlap, on walkable ground outside the store's collision box; and the farm's Phase 1 stall still in place.
/// </summary>
public class TownCounterSceneTests
{
    private const string PointScript = "res://scripts/World/InteractionPoint.cs";

    /// <summary>Maggie stands at (-11.6, 0, -2) in <c>Town.tscn</c> (#138).</summary>
    private const double MaggieX = -11.6;

    private const double MaggieZ = -2.0;

    /// <summary>Her talk range (1.5) plus the counter's (1.5), plus a margin: one <c>interact</c> can't reach both.</summary>
    private const double MinDistanceFromMaggie = 3.2;

    /// <summary>The Store collision box: centre (-10, -6), 6.4 x 6.4, so x -13.2..-6.8 and z -9.2..-2.8.</summary>
    private const double StoreMinX = -13.2;

    private const double StoreMaxX = -6.8;

    private const double StoreMinZ = -9.2;

    private const double StoreMaxZ = -2.8;

    private static string Scene(string name) => SceneText.ReadRepoFile("game", "scenes", $"{name}.tscn");

    private static List<SceneText.Node> ShopPoints(string sceneName)
    {
        var scene = Scene(sceneName);
        return SceneText.ScriptedNodes(scene, PointScript)
            .Where(node => SceneText.Property(node, "PointId") == "shop")
            .ToList();
    }

    private static SceneText.Node Counter()
    {
        var points = ShopPoints("Town");
        return Assert.Single(points, node => node.Path == "InteractionPoints/StoreCounter");
    }

    [Fact]
    public void AC10_town_has_a_shop_interaction_point_named_StoreCounter()
    {
        var points = ShopPoints("Town");

        var point = Assert.Single(points);
        Assert.Equal("InteractionPoints/StoreCounter", point.Path);
    }

    [Fact]
    public void AC10_the_store_counter_has_range_1_5()
    {
        Assert.Equal(1.5, SceneText.Number(Counter(), "Range"));
    }

    [Fact]
    public void AC10_the_store_counter_is_at_least_3_2_tiles_from_maggie()
    {
        var (x, _, z) = SceneText.Position(Scene("Town"), Counter());

        var distance = Math.Sqrt(((x - MaggieX) * (x - MaggieX)) + ((z - MaggieZ) * (z - MaggieZ)));

        Assert.True(distance >= MinDistanceFromMaggie, $"StoreCounter at ({x}, {z}) is {distance:F2} tiles from Maggie");
    }

    [Fact]
    public void AC10_maggie_still_stands_at_her_place_by_the_store()
    {
        // The distance check above is only meaningful while Maggie is where #138 put her.
        var scene = Scene("Town");
        var maggie = Assert.Single(SceneText.Nodes(scene), node => node.Name == "Maggie" && node.Parent == ".");

        var (x, _, z) = SceneText.Position(scene, maggie);

        Assert.Equal((MaggieX, MaggieZ), (x, z));
    }

    [Fact]
    public void AC10_the_store_counter_stands_outside_the_store_collision_box()
    {
        var (x, _, z) = SceneText.Position(Scene("Town"), Counter());

        var inside = x > StoreMinX && x < StoreMaxX && z > StoreMinZ && z < StoreMaxZ;

        Assert.False(inside, $"StoreCounter at ({x}, {z}) is inside the Store's collision box");
    }

    [Fact]
    public void AC10_the_store_counter_is_on_the_ground_near_the_store()
    {
        var (x, y, z) = SceneText.Position(Scene("Town"), Counter());

        // Within a few tiles of the store's footprint, so it reads as the store's counter.
        Assert.Equal(0.0, y);
        Assert.InRange(x, StoreMinX - 3.0, StoreMaxX + 3.0);
        Assert.InRange(z, StoreMinZ - 3.0, StoreMaxZ + 3.0);
    }

    [Fact]
    public void AC12_the_farm_still_has_its_shop_point_beside_the_stall()
    {
        // Characterization: the Phase 1 stall stays until #147 (passes before #145 too).
        var point = Assert.Single(ShopPoints("Farm"));

        Assert.Equal("InteractionPoints/ShopPoint", point.Path);
        Assert.Equal(2.2, SceneText.Number(point, "Range"));
    }
}
