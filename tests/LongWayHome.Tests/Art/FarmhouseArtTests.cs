using System.Numerics;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The run-down farmhouse's part table (#104), checked against ART_DIRECTION §7.1 and the issue's acceptance criteria.
/// Parts are in house-local metres: origin at the footprint centre on the ground, the door side +Z, the footprint
/// x −3..3, z −2..2. Every check works on the parts' rotated corners (<see cref="PartBoxes"/>), so a yawed or pitched
/// part is judged by where it actually reaches. Part groups are read from the name prefixes the plan fixed:
/// <c>Roof*</c>, <c>Patch*</c>, <c>Chimney*</c>, <c>Door</c>, <c>Step</c>, <c>Weed*</c>, <c>FallenLeaf*</c>.
/// </summary>
public class FarmhouseArtTests
{
    private const float Tolerance = 1e-3f;

    /// <summary>The palette on main before #104, in order: <c>door-faded</c> and <c>glass-dull</c> go after these.</summary>
    private static readonly (string Role, string Hex)[] SwatchesBefore104 =
    [
        ("grass-lit", "#7FA65A"),
        ("grass-shade", "#3F7A6C"),
        ("grass-mown", "#8CB862"),
        ("grass-fall", "#A8A458"),
        ("snow", "#E4ECF0"),
        ("foliage", "#3E8F82"),
        ("leaf-underside", "#7A68A8"),
        ("soil-tilled", "#8A5A3C"),
        ("soil-watered", "#4A3226"),
        ("timber", "#9C7E8C"),
        ("trunk", "#6B5A78"),
        ("stone", "#8F8A96"),
        ("rock", "#9A95A2"),
        ("dirt-path", "#B8946C"),
        ("lake-deep", "#2D6A86"),
        ("lake-shallow", "#5FB3AA"),
        ("ember", "#E9773A"),
        ("lantern", "#FFB04A"),
        ("glow-cool", "#6FF2E0"),
        ("glow-rare", "#B48CFF"),
        ("ship-metal", "#C9D6E8"),
        ("target-highlight", "#FFF6E0"),
        ("dusktuber", "#4A2A66"),
        ("moonmelon", "#C9DCF5"),
        ("sunbell-gold", "#F7C531"),
        ("sunbell-orange", "#E9821A"),
        ("starcorn", "#E8B830"),
        ("coverall-shade", "#A3B3CB"),
        ("navy", "#34405E"),
        ("boots", "#3B3F4A"),
        ("earth-blue", "#3D7FC4"),
        ("earth-green", "#5BAF6A"),
        ("wrist-screen", "#9FD3FF"),
        ("timber-side", "#7E6474"),
        ("timber-grain", "#6E5664"),
        ("timber-trim", "#5A4458"),
        ("roof-wilted", "#8A7A4A"),
        ("tips-dry", "#9A7E44"),
        ("trunk-grey", "#6E6670"),
        ("wall-grey", "#8A7C80"),
        ("roof-rows", "#2F7266"),
        ("roof-tips", "#4FA294"),
        ("roof-far", "#2F6B60"),
        ("grass-spring-mown", "#A6D07C"),
        ("grass-fall-mown", "#C0BC72"),
        ("starcorn-sparkle", "#FFFFFF"),
        ("skin", "#D9A07A"),
        ("skin-shade", "#B57E5F"),
        ("hair", "#5A3E2E"),
        ("seed-pouch", "#D9C39A"),
    ];

    private static IReadOnlyList<CropPart> Parts => FarmhouseArt.RunDown;

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static List<CropPart> Group(string prefix) => Parts.Where(part => Named(part, prefix)).ToList();

    private static (Vector3 Min, Vector3 Max) Extent(CropPart part) => PartBoxes.Extents([PartBoxes.Corners(part)]);

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    /// <summary>The shipping bin's footprint (x 2.4..3.6, z −6.4..−5.6 in the world) in house-local metres.</summary>
    private static bool OverlapsBin(Vector3 min, Vector3 max) =>
        max.X > 2.4f && min.X < 3.6f && max.Z > 2.6f && min.Z < 3.4f;

    // ---- AC1: palette roles ----

    [Fact]
    public void AC1_every_part_role_is_a_palette_role()
    {
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(Parts);
        var unknown = Parts.Where(part => !roles.Contains(part.Role)).Select(part => $"{part.Name} ({part.Role})").ToList();
        Assert.True(unknown.Count == 0, $"parts with no palette swatch: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void AC1_door_faded_and_glass_dull_are_appended_after_every_existing_swatch()
    {
        var actual = ArtPalette.Swatches.Select(swatch => (swatch.Role, swatch.Hex)).ToArray();

        // The palette is append-only, so later swatches may follow these two.
        Assert.True(
            actual.Length >= SwatchesBefore104.Length + 2,
            $"the palette has {actual.Length} swatches, expected at least the {SwatchesBefore104.Length} from before #104 plus 2");
        Assert.Equal(SwatchesBefore104, actual[..SwatchesBefore104.Length]);
        Assert.Equal(("door-faded", "#B88464"), actual[SwatchesBefore104.Length]);
        Assert.Equal(("glass-dull", "#8C97A3"), actual[SwatchesBefore104.Length + 1]);
    }

    // ---- AC2: footprint, step, ground ----

    /// <summary>
    /// Every part reaching below the walls' top stays on the footprint (plus 0.1 m for the posts), apart from the
    /// ground clutter in front. The roof's drooping fringe hangs from the eaves (y 3.0) by up to 0.25 m and the dead
    /// twigs start at the post tops, so a part only counts when it reaches below y 2.7, 0.3 m under the eaves: the
    /// fringe and twigs are above the Player's head and the collision box, which is what this check protects.
    /// </summary>
    [Fact]
    public void AC2_parts_below_the_eaves_stay_on_the_footprint()
    {
        var outside = new List<string>();
        foreach (var part in Parts)
        {
            if (Named(part, "Step") || Named(part, "FallenLeaf") || Named(part, "Weed")) continue;
            var (min, max) = Extent(part);
            if (min.Y >= 2.7f) continue;
            if (min.X < -3.1f - Tolerance || max.X > 3.1f + Tolerance || min.Z < -2.1f - Tolerance || max.Z > 2.1f + Tolerance)
            {
                outside.Add($"{part.Name} x {min.X:0.###}..{max.X:0.###}, z {min.Z:0.###}..{max.Z:0.###}");
            }
        }

        Assert.NotEmpty(Parts);
        Assert.True(outside.Count == 0, $"parts below the eaves outside x ±3.1, z ±2.1: {string.Join("; ", outside)}");
    }

    [Fact]
    public void AC2_step_fallen_leaves_and_weeds_stay_within_z_2_3_and_clear_of_the_bin()
    {
        var clutter = Parts
            .Where(part => Named(part, "Step") || Named(part, "FallenLeaf") || Named(part, "Weed"))
            .ToList();

        Assert.NotEmpty(clutter);
        foreach (var part in clutter)
        {
            var (min, max) = Extent(part);
            Assert.True(max.Z <= 2.3f + Tolerance, $"{part.Name} reaches z {max.Z:0.###}, past 2.3");
            Assert.False(OverlapsBin(min, max), $"{part.Name} (x {min.X:0.###}..{max.X:0.###}, z {min.Z:0.###}..{max.Z:0.###}) overlaps the bin");
        }
    }

    [Fact]
    public void AC2_front_step_is_1_2_m_wide_and_reaches_no_more_than_0_3_m_from_the_wall()
    {
        var steps = Group("Step");

        Assert.True(steps.Count > 0, "there is no Step part");
        var (min, max) = Extent(steps);
        var width = max.X - min.X;
        Assert.InRange(width, 1.2f * 0.75f, 1.2f * 1.25f);
        Assert.True(min.Z >= 2.0f - Tolerance && max.Z <= 2.3f + Tolerance, $"the step spans z {min.Z:0.###}..{max.Z:0.###}, not within 2.0..2.3");
    }

    [Fact]
    public void AC2_whole_house_fits_inside_the_meadow_exclusion()
    {
        var bounds = FarmhouseArt.Bounds(Parts);

        // MeadowLayout's exclusion is x −4..4, z −12..−6 in the world: x −4..4, z −3..3 about the house at z −9.
        Assert.True(bounds.MinX >= -4f && bounds.MaxX <= 4f, $"the house spans x {bounds.MinX}..{bounds.MaxX}");
        Assert.True(bounds.MinZ >= -3f && bounds.MaxZ <= 3f, $"the house spans z {bounds.MinZ}..{bounds.MaxZ}");
    }

    [Fact]
    public void AC2_no_part_is_below_the_ground()
    {
        Assert.NotEmpty(Parts);
        foreach (var part in Parts)
        {
            var (min, _) = Extent(part);
            Assert.True(min.Y >= -0.01f, $"{part.Name} reaches y {min.Y:0.###}, below the ground");
        }
    }

    [Fact]
    public void AC2_Bounds_covers_rotated_parts()
    {
        CropPart[] parts =
        [
            new("Flat", CropPartShape.Box, 2f, 1f, 2f, 0f, 0.5f, 0f, 0f, 0f, 0f, "stone"),
            // 4 m along X, 0.2 m along Z, yawed 90°: it now runs along Z, z −2..2, x 4.9..5.1.
            new("Turned", CropPartShape.Box, 4f, 1f, 0.2f, 5f, 2f, 0f, 0f, 90f, 0f, "stone"),
        ];

        var bounds = FarmhouseArt.Bounds(parts);

        Assert.Equal(-1f, bounds.MinX, Tolerance);
        Assert.Equal(5.1f, bounds.MaxX, Tolerance);
        Assert.Equal(0f, bounds.MinY, Tolerance);
        Assert.Equal(2.5f, bounds.MaxY, Tolerance);
        Assert.Equal(-2f, bounds.MinZ, Tolerance);
        Assert.Equal(2f, bounds.MaxZ, Tolerance);
    }

    // ---- AC3: roof, chimney, hull-panel patch ----

    [Fact]
    public void AC3_roof_top_is_between_4_4_and_5_0()
    {
        var roof = Group("Roof");

        Assert.True(roof.Count > 0, "there are no Roof parts");
        var (_, max) = Extent(roof);
        Assert.InRange(max.Y, 4.4f, 5.0f);
    }

    [Fact]
    public void AC3_chimney_lies_on_the_back_half()
    {
        var chimney = Group("Chimney");

        Assert.True(chimney.Count > 0, "there are no Chimney parts");
        foreach (var part in chimney)
        {
            var (_, max) = Extent(part);
            Assert.True(max.Z < 0f, $"{part.Name} reaches z {max.Z:0.###}, not behind the middle");
        }
    }

    [Fact]
    public void AC3_chimney_rises_above_the_roof_ridge()
    {
        var chimney = Group("Chimney");
        var roof = Group("Roof");

        Assert.True(chimney.Count > 0, "there are no Chimney parts");
        Assert.True(roof.Count > 0, "there are no Roof parts");
        Assert.True(
            Extent(chimney).Max.Y > Extent(roof).Max.Y,
            $"the chimney top {Extent(chimney).Max.Y:0.###} is not above the roof's {Extent(roof).Max.Y:0.###}");
    }

    [Fact]
    public void AC3_hull_patch_lies_on_the_front_slope()
    {
        var patch = Group("Patch");

        Assert.True(patch.Count > 0, "there are no Patch parts");
        foreach (var part in patch)
        {
            var (min, _) = Extent(part);
            Assert.True(min.Z > 0f, $"{part.Name} reaches z {min.Z:0.###}, not in front of the middle");
        }
    }

    [Fact]
    public void AC3_hull_patch_has_a_ship_metal_panel_and_a_navy_stripe()
    {
        var roles = Group("Patch").Select(part => part.Role).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ship-metal", roles);
        Assert.Contains("navy", roles);
    }

    // ---- AC4: the ajar door, the wilted roof, no glow ----

    [Fact]
    public void AC4_front_door_is_door_faded_centred_and_ajar()
    {
        var door = Parts.SingleOrDefault(part => part.Name == "Door");

        Assert.True(door is not null, "there is no single part named Door");
        Assert.Equal("door-faded", door!.Role);
        Assert.True(MathF.Abs(door.X) <= 0.2f + Tolerance, $"the door's centre is at x {door.X}");
        Assert.True(door.Z > 1.5f, $"the door's centre is at z {door.Z}, not on the front face");
        Assert.InRange(MathF.Abs(door.YawDegrees), 10f, 30f);
    }

    [Fact]
    public void AC4_roof_uses_no_restored_roles()
    {
        string[] restored = ["foliage", "roof-rows", "roof-tips", "roof-far", "ember"];
        var roof = Group("Roof");

        Assert.True(roof.Count > 0, "there are no Roof parts");
        var offending = roof.Where(part => restored.Contains(part.Role)).Select(part => $"{part.Name} ({part.Role})").ToList();
        Assert.True(offending.Count == 0, $"roof parts in restored roles: {string.Join(", ", offending)}");
    }

    [Fact]
    public void AC4_roof_is_wilted_leaves_dry_tips_and_trim_battens_only()
    {
        string[] allowed = ["roof-wilted", "tips-dry", "timber-trim"];
        var roof = Group("Roof");

        Assert.True(roof.Count > 0, "there are no Roof parts");
        Assert.Contains(roof, part => part.Role == "roof-wilted");
        Assert.Contains(roof, part => part.Role == "tips-dry");
        var other = roof.Where(part => !allowed.Contains(part.Role)).Select(part => $"{part.Name} ({part.Role})").ToList();
        Assert.True(other.Count == 0, $"roof parts outside roof-wilted, tips-dry, timber-trim: {string.Join(", ", other)}");
    }

    [Fact]
    public void AC4_nothing_glows_or_twinkles()
    {
        Assert.NotEmpty(Parts);
        foreach (var part in Parts)
        {
            Assert.True(part.Glow == 0f, $"{part.Name} glows ({part.Glow})");
            Assert.False(part.Twinkle, $"{part.Name} twinkles");
        }
    }

    // ---- AC5: one draw ----

    [Fact]
    public void AC5_RunDown_is_small_enough_for_one_draw()
    {
        Assert.InRange(Parts.Count, 1, 600);
    }
}
