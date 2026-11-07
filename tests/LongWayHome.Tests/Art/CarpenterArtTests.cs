using System.Numerics;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The carpenter at each rebuild stage (design doc, Carpenter; ART_DIRECTION §7.3): two round drums grown together, the
/// workshop with braced double doors in front and the family's taller drum behind. Building-local metres: origin at the
/// workshop's centre on the ground, the doors facing +Z. Every check works on the parts' rotated corners.
/// </summary>
public class CarpenterArtTests
{
    public static TheoryData<CarpenterStage> Stages() => new(Enum.GetValues<CarpenterStage>());

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_uses_palette_roles_with_unique_names_and_positive_sizes(CarpenterStage stage)
    {
        var parts = CarpenterArt.For(stage).Concat(CarpenterArt.NightLights(stage)).ToList();
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        var duplicates = parts.GroupBy(part => part.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Assert.True(duplicates.Count == 0, $"duplicate names: {string.Join(", ", duplicates)}");
        Assert.All(parts, part => Assert.True(part.SizeX > 0f && part.SizeY > 0f, $"{part.Name} has a zero size"));
    }

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_stands_on_the_ground_and_fits_its_plot(CarpenterStage stage)
    {
        var (min, max) = Extent(CarpenterArt.For(stage));

        Assert.True(min.Y >= -0.05f, $"{stage} reaches y {min.Y:0.###}");
        Assert.InRange(max.Y, CarpenterArt.HouseApexY, CarpenterArt.HouseApexY + 0.8f);
        Assert.True(min.X >= -6.2f && max.X <= 6.2f && min.Z >= -5.4f && max.Z <= 5.6f,
            $"{stage} spans x {min.X:0.##}..{max.X:0.##}, z {min.Z:0.##}..{max.Z:0.##}");
    }

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_is_two_drums_with_the_taller_family_drum_behind_and_braced_double_doors_in_front(CarpenterStage stage)
    {
        var parts = CarpenterArt.For(stage);
        var workshop = parts.Where(part => Named(part, "Stave")).ToList();
        var house = parts.Where(part => Named(part, "HouseStave")).ToList();

        Assert.NotEmpty(workshop);
        Assert.NotEmpty(house);
        Assert.True(house.Average(part => part.Z) < workshop.Average(part => part.Z), "the family drum isn't behind the workshop");
        Assert.True(Extent(house).Max.Y > Extent(workshop).Max.Y + 1f, "the family drum isn't taller");
        // Two storeys: a window over a window on the family drum.
        Assert.True(parts.Single(part => part.Name == "HouseWindowHighGlass").Y > parts.Single(part => part.Name == "HouseWindowLowGlass").Y + 1.2f);

        var west = parts.Single(part => part.Name == "DoorWest");
        var east = parts.Single(part => part.Name == "DoorEast");
        Assert.True(west.X < 0f && east.X > 0f && west.Z > CarpenterArt.WallRadius, "the doors aren't a pair at the front");
        Assert.True(west.SizeX + east.SizeX > 1.7f && west.SizeY > 1.6f, "the doors aren't big");
        Assert.Contains(parts, part => part.Name == "DoorWestBrace");
        Assert.Contains(parts, part => part.Name == "DoorEastBrace");
    }

    [Fact]
    public void Empty_is_wilted_and_barred_with_no_sign_or_planks()
    {
        var parts = CarpenterArt.For(CarpenterStage.Empty);

        Assert.DoesNotContain(parts, part => Named(part, "Leaf") && part.Role is "foliage" or "roof-rows");
        Assert.Contains(parts, part => part.Role == "wall-grey");
        Assert.Contains(parts, part => part.Name == "DoorBar");
        Assert.Contains(parts, part => Named(part, "Weed"));
        Assert.DoesNotContain(parts, part => Named(part, "Sign") || Named(part, "Planks"));
        Assert.Empty(CarpenterArt.NightLights(CarpenterStage.Empty));
        Assert.All(parts, part => Assert.Equal(0f, part.Glow));
    }

    [Fact]
    public void Reopened_is_alive_with_ember_doors_the_saw_sign_and_a_plank_stack()
    {
        var parts = CarpenterArt.For(CarpenterStage.Reopened);

        Assert.DoesNotContain(parts, part => part.Role is "wall-grey" or "roof-wilted" or "trunk-grey");
        Assert.Equal("ember", parts.Single(part => part.Name == "DoorWest").Role);
        Assert.Contains(parts, part => Named(part, "SignSaw") && part.Role == "ship-metal");
        Assert.Equal(5, parts.Count(part => Named(part, "Planks")));
        Assert.DoesNotContain(parts, part => Named(part, "Shed") || Named(part, "Barrel") || Named(part, "Frame") || Named(part, "Sealed"));
        Assert.Equal(4, CarpenterArt.NightLights(CarpenterStage.Reopened).Count(part => !part.Name.EndsWith("Arch", StringComparison.Ordinal)));
    }

    [Fact]
    public void The_workshop_adds_a_shed_with_a_workbench_and_sap_barrels_that_glow_amber()
    {
        var parts = CarpenterArt.For(CarpenterStage.Workshop);
        var shed = parts.Where(part => Named(part, "Shed") || Named(part, "Bench") || Named(part, "Barrel")).ToList();

        // East of the workshop; the shed's roof leaves tuck against the drum's wall.
        Assert.True(Extent(shed).Min.X > 2.0f, "the shed isn't on the east side");
        Assert.Contains(parts, part => part.Name == "BenchTop");
        Assert.Contains(parts, part => Named(part, "BenchSaw"));
        var lids = parts.Where(part => Named(part, "Barrel") && part.Name.EndsWith("Lid", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, lids.Count);
        Assert.All(lids, lid => Assert.True(lid.Role == "lantern" && lid.Glow > 0f));
        Assert.DoesNotContain(parts, part => Named(part, "Frame") || Named(part, "Sealed"));
    }

    [Fact]
    public void The_builder_adds_a_timber_frame_for_a_new_home_and_sealed_plank_stacks()
    {
        var parts = CarpenterArt.For(CarpenterStage.Builder);
        var frame = parts.Where(part => Named(part, "Frame")).ToList();

        Assert.True(frame.Count(part => Named(part, "FramePost")) >= 6, "the frame has too few posts");
        Assert.Contains(frame, part => Named(part, "FrameRafter"));
        Assert.All(frame.Where(part => Named(part, "FramePost") || Named(part, "FrameRafter")), part => Assert.Equal("timber", part.Role));
        Assert.True(Extent(frame).Max.X < -2.5f, "the frame isn't on the west side");
        Assert.True(parts.Count(part => Named(part, "Sealed") && part.Role == "lantern") >= 10, "the sealed planks have no amber seal");
        Assert.Contains(parts, part => Named(part, "Barrel"));
    }
}
