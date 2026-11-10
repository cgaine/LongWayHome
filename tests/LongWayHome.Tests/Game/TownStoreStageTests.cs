using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Game;

/// <summary>
/// #165 AC7: the town draws the general store at its stage's look (<see cref="TownBuildingLooks"/>) with that look's
/// night lights, and at stages 3 and 4 a cylinder <c>StoreStoreroom</c> collision for the storeroom drum, which no
/// town tree and neither the counter nor the sign point lies inside.
/// </summary>
public class TownStoreStageTests
{
    /// <summary>The Twinbough trunk collision radius <c>TownView</c> gives each tree.</summary>
    private const float TreeTrunkRadius = 0.42f;

    private static string TownView() =>
        SceneText.Code(SceneText.ReadRepoFile("game", "art", "areas", "TownView.cs"));

    private static (float X, float Z) DrumCentre()
    {
        var store = TownLayout.Buildings.Single(building => building.Kind == TownBuildingKind.Store);
        return (store.X + TownBuildingLooks.StoreroomOffsetX, store.Z + TownBuildingLooks.StoreroomOffsetZ);
    }

    [Fact]
    public void AC7_TownView_draws_the_store_at_the_look_of_its_stage_with_its_night_lights()
    {
        var source = TownView();

        Assert.Contains("TownBuildingLooks.Store(", source);
        Assert.Contains("TownModels.StoreNightLights(", source);
        Assert.DoesNotContain("TownModels.Store(StoreStage.MaggiesCorner)", source);
    }

    [Fact]
    public void AC7_TownView_adds_a_StoreStoreroom_cylinder_collision_for_the_storeroom_looks()
    {
        var source = TownView();

        Assert.Contains("TownBuildingLooks.HasStoreroom(", source);
        Assert.Contains("\"StoreStoreroom\"", source);
        Assert.Contains("CylinderShape3D", source);
        Assert.Contains("TownBuildingLooks.StoreroomRadius", source);
        Assert.Contains("TownBuildingLooks.StoreroomHeight", source);
    }

    [Fact]
    public void AC7_no_town_tree_lies_inside_the_storeroom_drum()
    {
        var (x, z) = DrumCentre();

        foreach (var tree in TownLayout.Trees())
        {
            Assert.True(
                AreaScatter.Distance(tree.X, tree.Z, x, z) >= TownBuildingLooks.StoreroomRadius + TreeTrunkRadius,
                $"the tree at ({tree.X}, {tree.Z}) stands in the storeroom drum at ({x}, {z})");
        }
    }

    [Theory]
    [InlineData("StoreCounter")]
    [InlineData("StoreSign")]
    public void AC7_the_store_interaction_points_lie_outside_the_storeroom_drum(string pointName)
    {
        var scene = SceneText.ReadRepoFile("game", "scenes", "Town.tscn");
        var point = Assert.Single(SceneText.Nodes(scene), node => node.Path == $"InteractionPoints/{pointName}");
        var (px, _, pz) = SceneText.Position(scene, point);
        var (x, z) = DrumCentre();

        Assert.True(
            AreaScatter.Distance((float)px, (float)pz, x, z) > TownBuildingLooks.StoreroomRadius,
            $"{pointName} at ({px}, {pz}) lies inside the storeroom drum at ({x}, {z})");
    }
}
