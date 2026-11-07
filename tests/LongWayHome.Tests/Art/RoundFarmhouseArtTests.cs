using System.Numerics;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The round farmhouse's part tables (the concept art's round farmhouse, ART_DIRECTION §7.1), run-down and restored.
/// Parts are in house-local metres: origin at the drum's centre on the ground, the door facing +Z. Every check works on
/// the parts' rotated corners (<see cref="PartBoxes"/>), so a yawed or pitched part is judged by where it reaches.
/// </summary>
public class RoundFarmhouseArtTests
{
    public static TheoryData<string> States => new() { "RunDown", "Restored" };

    private static IReadOnlyList<CropPart> PartsOf(string state) =>
        state == "Restored" ? RoundFarmhouseArt.Restored : RoundFarmhouseArt.RunDown;

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static (Vector3 Min, Vector3 Max) Extent(CropPart part) => PartBoxes.Extents([PartBoxes.Corners(part)]);

    /// <summary>
    /// How far a part reaches from the house's centre, in plan. An upright cylinder or a sphere is round, so it reaches
    /// its centre's distance plus its radius, not its bounding box's corners; every other part reaches its farthest
    /// rotated corner.
    /// </summary>
    private static float Reach(CropPart part)
    {
        var round = part.Shape == CropPartShape.Sphere
            || (part.Shape == CropPartShape.Cylinder && part.PitchDegrees == 0f && part.RollDegrees == 0f);
        if (round)
        {
            var radius = MathF.Max(part.SizeX, part.Shape == CropPartShape.Cylinder ? part.SizeZ : part.SizeX) / 2f;
            return MathF.Sqrt(part.X * part.X + part.Z * part.Z) + radius;
        }

        return PartBoxes.Corners(part).Max(corner => MathF.Sqrt(corner.X * corner.X + corner.Z * corner.Z));
    }

    [Theory]
    [MemberData(nameof(States))]
    public void Every_part_role_is_a_palette_role(string state)
    {
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);
        var parts = PartsOf(state).Concat(RoundFarmhouseArt.RestoredNightLights).ToList();

        Assert.NotEmpty(parts);
        var unknown = parts.Where(part => !roles.Contains(part.Role)).Select(part => $"{part.Name} ({part.Role})").ToList();
        Assert.True(unknown.Count == 0, $"parts with no palette swatch: {string.Join(", ", unknown)}");
    }

    [Theory]
    [MemberData(nameof(States))]
    public void Part_names_are_unique(string state)
    {
        var duplicates = PartsOf(state).GroupBy(part => part.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        Assert.True(duplicates.Count == 0, $"duplicate part names: {string.Join(", ", duplicates)}");
    }

    [Theory]
    [MemberData(nameof(States))]
    public void Every_size_is_positive_except_a_cone_top(string state)
    {
        foreach (var part in PartsOf(state))
        {
            Assert.True(part.SizeX > 0f && part.SizeY > 0f, $"{part.Name} has size {part.SizeX} x {part.SizeY}");
            // A Cylinder's SizeZ is its top diameter, 0 for a cone; every other shape needs depth.
            Assert.True(part.SizeZ > 0f || part.Shape == CropPartShape.Cylinder, $"{part.Name} has no depth");
        }
    }

    [Theory]
    [MemberData(nameof(States))]
    public void No_part_is_below_the_ground(string state)
    {
        foreach (var part in PartsOf(state))
        {
            var (min, _) = Extent(part);
            Assert.True(min.Y >= -0.01f, $"{part.Name} reaches y {min.Y:0.###}, below the ground");
        }
    }

    [Theory]
    [MemberData(nameof(States))]
    public void The_drum_is_5_m_across_and_3_m_tall(string state)
    {
        var staves = PartsOf(state).Where(part => Named(part, "Stave")).ToList();

        Assert.NotEmpty(staves);
        var reach = staves.Max(Reach);
        Assert.InRange(reach, 2.45f, 2.55f);
        Assert.Equal(RoundFarmhouseArt.WallTop, staves.Max(part => Extent(part).Max.Y), 0.02f);
    }

    /// <summary>
    /// Below the eaves (y 2.7) everything but the ground clutter, the step, the door (ajar over the step when run-down)
    /// and the lantern stays within 3 m of the centre, so the house is a short cylinder a little wider than its drum.
    /// The eave tips hang from the eaves, so they don't count.
    /// </summary>
    [Theory]
    [MemberData(nameof(States))]
    public void Parts_below_the_eaves_stay_near_the_drum(string state)
    {
        var outside = PartsOf(state)
            .Where(part => !Named(part, "Step") && !Named(part, "Door") && !Named(part, "Weed") && !Named(part, "FallenLeaf") && !Named(part, "Lantern") && !Named(part, "Tip"))
            .Where(part => Extent(part).Min.Y < 2.7f && Reach(part) > 3f)
            .Select(part => $"{part.Name} ({Reach(part):0.##} m)")
            .ToList();

        Assert.True(outside.Count == 0, $"parts below the eaves more than 3 m out: {string.Join(", ", outside)}");
    }

    [Theory]
    [MemberData(nameof(States))]
    public void The_whole_house_fits_in_a_3_6_m_radius_and_under_8_m(string state)
    {
        var parts = PartsOf(state);

        Assert.True(parts.Max(Reach) <= 3.6f, $"the house reaches {parts.Max(Reach):0.##} m from its centre");
        Assert.True(parts.Max(part => Extent(part).Max.Y) < 8f);
    }

    [Theory]
    [MemberData(nameof(States))]
    public void The_roof_is_a_cone_of_leaf_rings_up_to_the_apex(string state)
    {
        var parts = PartsOf(state);
        var leaves = parts.Where(part => Named(part, "Leaf")).ToList();

        Assert.True(leaves.Count >= 40, $"only {leaves.Count} roof leaves");
        // The rings climb: each ring's leaves sit higher than the ring below.
        var ringHeights = leaves.GroupBy(part => part.Name.Split('_')[0]).OrderBy(group => group.Key)
            .Select(group => group.Average(part => part.Y)).ToList();
        Assert.True(ringHeights.Count >= 4);
        for (var i = 1; i < ringHeights.Count; i++)
        {
            Assert.True(ringHeights[i] > ringHeights[i - 1], $"ring {i} is not above ring {i - 1}");
        }

        var crown = parts.Where(part => Named(part, "Crown")).ToList();
        Assert.NotEmpty(crown);
        Assert.True(crown.Max(part => Extent(part).Max.Y) >= RoundFarmhouseArt.ApexY);
    }

    [Theory]
    [MemberData(nameof(States))]
    public void Both_states_have_the_hull_patch_the_chimney_the_door_and_two_windows(string state)
    {
        var parts = PartsOf(state);

        Assert.Contains(parts, part => part.Name == "PatchPanel" && part.Role == "ship-metal");
        Assert.Contains(parts, part => part.Name == "PatchStripe" && part.Role == "navy");
        Assert.Contains(parts, part => part.Name == "ChimneyStack" && part.Role == "stone");
        Assert.Contains(parts, part => part.Name == "Door");
        Assert.Contains(parts, part => part.Name == "WindowWestGlass");
        Assert.Contains(parts, part => part.Name == "WindowEastGlass");

        // The door faces the plot (+Z) and the chimney stands at the back.
        var door = parts.Single(part => part.Name == "Door");
        Assert.True(door.Z > RoundFarmhouseArt.WallRadius, $"the door is at z {door.Z}");
        Assert.True(parts.Single(part => part.Name == "ChimneyStack").Z < 0f);
    }

    [Fact]
    public void Run_down_has_gaps_wilted_leaves_and_nothing_glows()
    {
        var runDown = RoundFarmhouseArt.RunDown;
        var restored = RoundFarmhouseArt.Restored;

        Assert.All(runDown, part => Assert.Equal(0f, part.Glow));
        Assert.True(runDown.Count(part => Named(part, "Leaf")) < restored.Count(part => Named(part, "Leaf")), "the run-down roof has no gaps");
        Assert.True(runDown.Count(part => Named(part, "Stave")) < restored.Count(part => Named(part, "Stave")), "no stave pieces are missing");
        Assert.All(runDown.Where(part => Named(part, "Leaf")), part => Assert.Contains(part.Role, new[] { "roof-wilted", "tips-dry" }));
        Assert.Contains(runDown, part => Named(part, "WindowWestBoard"));
        Assert.Contains(runDown, part => Named(part, "Weed"));
        Assert.Equal("door-faded", runDown.Single(part => part.Name == "Door").Role);
        Assert.DoesNotContain(runDown, part => Named(part, "Smoke") || Named(part, "Lantern") || Named(part, "FlowerBox"));
    }

    [Fact]
    public void Restored_is_green_and_alive_with_glowing_tips_and_a_lantern()
    {
        var restored = RoundFarmhouseArt.Restored;

        Assert.All(restored.Where(part => Named(part, "Leaf")), part => Assert.Contains(part.Role, new[] { "foliage", "roof-rows" }));
        Assert.Equal("ember", restored.Single(part => part.Name == "Door").Role);
        Assert.All(restored.Where(part => Named(part, "Tip")), part => Assert.True(part.Glow > 0f, $"{part.Name} does not glow"));
        Assert.Contains(restored, part => part.Name == "LanternGlass" && part.Glow > 0f);
        Assert.Contains(restored, part => Named(part, "Smoke"));
        Assert.Contains(restored, part => Named(part, "FlowerBox"));
        Assert.DoesNotContain(restored, part => Named(part, "Weed") || Named(part, "WindowWestBoard"));
        // Only the tips, the crown and the lantern glow.
        Assert.All(restored.Where(part => part.Glow > 0f), part =>
            Assert.True(Named(part, "Tip") || Named(part, "Crown") || Named(part, "Lantern"), $"{part.Name} glows"));
    }

    [Fact]
    public void Night_lights_are_glowing_lantern_panes_in_front_of_each_window()
    {
        var lights = RoundFarmhouseArt.RestoredNightLights;

        Assert.NotEmpty(lights);
        Assert.All(lights, part =>
        {
            Assert.Equal("lantern", part.Role);
            Assert.Equal(1f, part.Glow);
        });
        foreach (var side in new[] { "West", "East" })
        {
            var glass = RoundFarmhouseArt.Restored.Single(part => part.Name == $"Window{side}Glass");
            var light = lights.Single(part => part.Name == $"NightWindow{side}");
            Assert.True(Reach(light) > Reach(glass), $"the {side} night pane is not in front of the glass");
        }
    }
}
