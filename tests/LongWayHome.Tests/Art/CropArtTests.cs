using System.Numerics;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Tests.Art;

/// <summary>
/// AC1–AC4 of #90: the part table of the emberroot and dusktuber models (<see cref="CropArt"/>) and its
/// <see cref="CropArt.Bounds"/>. Rotations follow Godot's Euler YXZ (<c>Node3D.RotationDegrees</c>), which is
/// <see cref="Quaternion.CreateFromYawPitchRoll"/>, applied about the part's centre. The unrotated box of a part is
/// SizeX x SizeY x SizeZ for a Box or Prism, SizeX x SizeY x SizeX for a Sphere and max(SizeX, SizeZ) x SizeY x
/// max(SizeX, SizeZ) for a Cylinder.
/// </summary>
public class CropArtTests
{
    private const float Tolerance = 1e-4f;

    private static readonly CropTypeId[] DrawnCrops = [CropTypeId.Emberroot, CropTypeId.Dusktuber];

    private static readonly CropTypeId[] PrototypeCrops = [CropTypeId.Moonmelon, CropTypeId.Sunbell, CropTypeId.Starcorn];

    private static readonly GrowthStage[] StagesInOrder =
        [GrowthStage.Seed, GrowthStage.Sprout, GrowthStage.Young, GrowthStage.Ripe];

    public static TheoryData<CropTypeId, GrowthStage> DrawnModels()
    {
        var data = new TheoryData<CropTypeId, GrowthStage>();
        foreach (var crop in DrawnCrops)
        {
            foreach (var stage in StagesInOrder)
            {
                data.Add(crop, stage);
            }
        }

        return data;
    }

    public static TheoryData<CropTypeId, GrowthStage> PrototypeModels()
    {
        var data = new TheoryData<CropTypeId, GrowthStage>();
        foreach (var crop in PrototypeCrops)
        {
            foreach (var stage in StagesInOrder)
            {
                data.Add(crop, stage);
            }
        }

        return data;
    }

    private static IReadOnlyList<CropPart> PartsOf(CropTypeId crop, GrowthStage stage)
    {
        var parts = CropArt.Parts(crop, stage);
        Assert.NotNull(parts);
        return parts!;
    }

    private static string ColourRole(CropTypeId crop) => crop switch
    {
        CropTypeId.Emberroot => "ember",
        CropTypeId.Dusktuber => "dusktuber",
        _ => throw new ArgumentOutOfRangeException(nameof(crop)),
    };

    /// <summary>The eight corners of a part's rotated box, computed here independently of <see cref="CropArt"/>.</summary>
    private static IEnumerable<Vector3> Corners(CropPart part)
    {
        var (sizeX, sizeZ) = part.Shape switch
        {
            CropPartShape.Sphere => (part.SizeX, part.SizeX),
            CropPartShape.Cylinder => (Math.Max(part.SizeX, part.SizeZ), Math.Max(part.SizeX, part.SizeZ)),
            _ => (part.SizeX, part.SizeZ),
        };
        var half = new Vector3(sizeX / 2f, part.SizeY / 2f, sizeZ / 2f);
        var rotation = Quaternion.CreateFromYawPitchRoll(
            part.YawDegrees * MathF.PI / 180f,
            part.PitchDegrees * MathF.PI / 180f,
            part.RollDegrees * MathF.PI / 180f);
        var centre = new Vector3(part.X, part.Y, part.Z);

        foreach (var sx in new[] { -1f, 1f })
        {
            foreach (var sy in new[] { -1f, 1f })
            {
                foreach (var sz in new[] { -1f, 1f })
                {
                    yield return centre + Vector3.Transform(half * new Vector3(sx, sy, sz), rotation);
                }
            }
        }
    }

    private static (float Width, float Height) ExpectedBounds(IEnumerable<CropPart> parts)
    {
        var corners = parts.SelectMany(Corners).ToList();
        var xExtent = corners.Max(c => c.X) - corners.Min(c => c.X);
        var zExtent = corners.Max(c => c.Z) - corners.Min(c => c.Z);
        return (Math.Max(xExtent, zExtent), corners.Max(c => c.Y));
    }

    private static CropPart Part(
        CropPartShape shape,
        float sizeX,
        float sizeY,
        float sizeZ,
        float x = 0f,
        float y = 0f,
        float z = 0f,
        float pitch = 0f,
        float yaw = 0f,
        float roll = 0f,
        string name = "Part") =>
        new(name, shape, sizeX, sizeY, sizeZ, x, y, z, pitch, yaw, roll, "foliage");

    // ---- AC1: parts for every stage of emberroot and dusktuber, null for the rest, palette roles only ----

    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void AC1_emberroot_and_dusktuber_have_parts_at_every_stage(CropTypeId crop, GrowthStage stage)
    {
        var parts = CropArt.Parts(crop, stage);

        Assert.NotNull(parts);
        Assert.NotEmpty(parts!);
    }

    [Theory]
    [MemberData(nameof(PrototypeModels))]
    public void AC1_moonmelon_sunbell_and_starcorn_have_no_parts_yet(CropTypeId crop, GrowthStage stage) =>
        Assert.Null(CropArt.Parts(crop, stage));

    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void AC1_every_part_role_is_an_art_palette_role(CropTypeId crop, GrowthStage stage)
    {
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        foreach (var part in PartsOf(crop, stage))
        {
            Assert.True(roles.Contains(part.Role), $"{crop} {stage} part {part.Name} has role '{part.Role}', not in ArtPalette");
        }
    }

    // ---- AC2: each stage is taller or wider than the one before ----

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    public void AC2_each_stage_grows_by_at_least_5cm_in_height_or_width(CropTypeId crop)
    {
        var bounds = StagesInOrder.Select(stage => CropArt.Bounds(PartsOf(crop, stage))).ToArray();

        for (var i = 1; i < bounds.Length; i++)
        {
            var taller = bounds[i].Height - bounds[i - 1].Height;
            var wider = bounds[i].Width - bounds[i - 1].Width;
            Assert.True(
                taller >= 0.05f - Tolerance || wider >= 0.05f - Tolerance,
                $"{crop} {StagesInOrder[i]} {bounds[i]} is not 0.05 m taller or wider than {StagesInOrder[i - 1]} {bounds[i - 1]}");
        }
    }

    // ---- AC3: every corner stays on the slab footprint ----

    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void AC3_every_rotated_corner_lies_inside_the_slab_footprint(CropTypeId crop, GrowthStage stage)
    {
        foreach (var part in PartsOf(crop, stage))
        {
            foreach (var corner in Corners(part))
            {
                Assert.True(
                    MathF.Abs(corner.X) <= 0.46f + Tolerance && MathF.Abs(corner.Z) <= 0.46f + Tolerance,
                    $"{crop} {stage} part {part.Name} has a corner at {corner}, outside |X|, |Z| <= 0.46");
            }
        }
    }

    // ---- AC4: ripe silhouettes differ; seed and sprout carry the crop's colour ----

    [Fact]
    public void AC4_ripe_emberroot_is_at_least_10cm_taller_than_ripe_dusktuber()
    {
        var emberroot = CropArt.Bounds(PartsOf(CropTypeId.Emberroot, GrowthStage.Ripe));
        var dusktuber = CropArt.Bounds(PartsOf(CropTypeId.Dusktuber, GrowthStage.Ripe));

        Assert.True(
            emberroot.Height - dusktuber.Height >= 0.1f - Tolerance,
            $"ripe emberroot height {emberroot.Height} is not 0.1 m above ripe dusktuber's {dusktuber.Height}");
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, GrowthStage.Seed)]
    [InlineData(CropTypeId.Emberroot, GrowthStage.Sprout)]
    [InlineData(CropTypeId.Dusktuber, GrowthStage.Seed)]
    [InlineData(CropTypeId.Dusktuber, GrowthStage.Sprout)]
    public void AC4_seed_and_sprout_include_a_part_in_the_crops_colour_role(CropTypeId crop, GrowthStage stage) =>
        Assert.Contains(PartsOf(crop, stage), part => part.Role == ColourRole(crop));

    // ---- The named parts the spec calls out ----

    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void Part_names_are_unique_within_a_model(CropTypeId crop, GrowthStage stage)
    {
        var names = PartsOf(crop, stage).Select(part => part.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Ripe_emberroot_has_seven_fronds_two_of_them_leaf_underside()
    {
        var parts = PartsOf(CropTypeId.Emberroot, GrowthStage.Ripe);
        var fronds = parts.Where(part => part.Name.StartsWith("Frond", StringComparison.Ordinal)).ToList();

        Assert.Equal(
            Enumerable.Range(0, 7).Select(i => $"Frond{i}").Order(StringComparer.Ordinal),
            fronds.Select(part => part.Name).Order(StringComparer.Ordinal));
        Assert.All(fronds, frond => Assert.Equal(CropPartShape.Prism, frond.Shape));
        foreach (var frond in fronds)
        {
            var expected = frond.Name is "Frond1" or "Frond4" ? "leaf-underside" : "foliage";
            Assert.Equal(expected, frond.Role);
        }
    }

    [Fact]
    public void Ripe_emberroot_has_an_ember_shoulder_and_a_lantern_core()
    {
        var parts = PartsOf(CropTypeId.Emberroot, GrowthStage.Ripe);

        Assert.Contains(parts, part => part.Name == "Shoulder" && part.Role == "ember");
        Assert.Contains(parts, part => part.Name == "Core" && part.Role == "lantern");
    }

    [Fact]
    public void Ripe_dusktuber_has_a_dusktuber_bulb_and_eight_moonmelon_speckles()
    {
        var parts = PartsOf(CropTypeId.Dusktuber, GrowthStage.Ripe);
        var speckles = parts.Where(part => part.Name.StartsWith("Speckle", StringComparison.Ordinal)).ToList();

        Assert.Contains(parts, part => part.Name == "Bulb" && part.Role == "dusktuber");
        Assert.Equal(
            Enumerable.Range(0, 8).Select(i => $"Speckle{i}").Order(StringComparer.Ordinal),
            speckles.Select(part => part.Name).Order(StringComparer.Ordinal));
        Assert.All(speckles, speckle => Assert.Equal("moonmelon", speckle.Role));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    public void Seed_is_a_dirt_path_mound_and_a_seed_in_the_crops_colour(CropTypeId crop)
    {
        var parts = PartsOf(crop, GrowthStage.Seed);

        Assert.Contains(parts, part => part.Name == "Mound" && part.Role == "dirt-path");
        Assert.Contains(parts, part => part.Name == "Seed" && part.Role == ColourRole(crop));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    public void Sprout_is_a_stem_two_seed_leaves_and_a_bud_in_the_crops_colour(CropTypeId crop)
    {
        var parts = PartsOf(crop, GrowthStage.Sprout);

        Assert.Contains(parts, part => part.Name == "Stem" && part.Role == "foliage");
        Assert.Contains(parts, part => part.Name == "SeedLeafLeft" && part.Role == "grass-mown");
        Assert.Contains(parts, part => part.Name == "SeedLeafRight" && part.Role == "grass-mown");
        Assert.Contains(parts, part => part.Name == "Bud" && part.Role == ColourRole(crop));
    }

    // ---- Bounds ----

    [Fact]
    public void Bounds_of_one_unrotated_box_are_its_larger_footprint_side_and_its_top()
    {
        var (width, height) = CropArt.Bounds([Part(CropPartShape.Box, 0.2f, 0.1f, 0.3f, y: 0.05f)]);

        Assert.Equal(0.3f, width, Tolerance);
        Assert.Equal(0.1f, height, Tolerance);
    }

    [Fact]
    public void Bounds_height_is_the_top_above_y_0_not_the_vertical_extent()
    {
        var (_, height) = CropArt.Bounds([Part(CropPartShape.Box, 0.1f, 0.1f, 0.1f, y: 0.3f)]);

        Assert.Equal(0.35f, height, Tolerance);
    }

    [Fact]
    public void Bounds_of_a_box_yawed_90_swap_its_x_and_z_extents()
    {
        // A 0.1 m cube at the origin and a 0.5 m long box at x 0.5. Unrotated the X span is -0.05..0.75 (0.8 m);
        // yawed 90° the long box lies along Z, so the X span is -0.05..0.55 (0.6 m) and the Z span 0.5 m.
        var (width, _) = CropArt.Bounds(
        [
            Part(CropPartShape.Box, 0.1f, 0.1f, 0.1f, y: 0.05f, name: "A"),
            Part(CropPartShape.Box, 0.5f, 0.1f, 0.1f, x: 0.5f, y: 0.05f, yaw: 90f, name: "B"),
        ]);

        Assert.Equal(0.6f, width, Tolerance);
    }

    [Fact]
    public void Bounds_of_a_box_pitched_90_lie_its_height_along_z()
    {
        // A 0.1 x 0.4 x 0.1 box at y 0.3, pitched 90°: its 0.4 m side lies along Z and its top is 0.3 + 0.05.
        var (width, height) = CropArt.Bounds([Part(CropPartShape.Box, 0.1f, 0.4f, 0.1f, y: 0.3f, pitch: 90f)]);

        Assert.Equal(0.4f, width, Tolerance);
        Assert.Equal(0.35f, height, Tolerance);
    }

    [Fact]
    public void Bounds_apply_roll_before_yaw_as_godot_euler_yxz()
    {
        // A 0.4 x 0.02 x 0.02 lath at y 0.1, rolled 30° then yawed 90°: its outer end lifts by 0.2 sin 30° and it
        // then swings round to lie along Z. Read as yaw-then-roll about world axes it would stay flat.
        var (width, height) = CropArt.Bounds(
            [Part(CropPartShape.Box, 0.4f, 0.02f, 0.02f, y: 0.1f, yaw: 90f, roll: 30f)]);

        var cos = MathF.Cos(MathF.PI / 6f);
        var sin = MathF.Sin(MathF.PI / 6f);
        Assert.Equal(0.1f + 0.2f * sin + 0.01f * cos, height, 1e-3f);
        Assert.Equal(0.4f * cos + 0.02f * sin, width, 1e-3f);
    }

    [Fact]
    public void Bounds_of_a_cylinder_use_the_larger_of_its_bottom_and_top_diameters()
    {
        var (width, height) = CropArt.Bounds([Part(CropPartShape.Cylinder, 0.1f, 0.2f, 0.3f, y: 0.1f)]);

        Assert.Equal(0.3f, width, Tolerance);
        Assert.Equal(0.2f, height, Tolerance);
    }

    [Fact]
    public void Bounds_of_a_sphere_use_its_diameter_size_x_on_both_horizontal_axes()
    {
        // SizeZ is not part of a sphere's box: the diameter SizeX is its X and Z extent.
        var (width, height) = CropArt.Bounds([Part(CropPartShape.Sphere, 0.2f, 0.1f, 0.6f, y: 0.05f)]);

        Assert.Equal(0.2f, width, Tolerance);
        Assert.Equal(0.1f, height, Tolerance);
    }

    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void Bounds_of_every_model_match_its_rotated_corners(CropTypeId crop, GrowthStage stage)
    {
        var parts = PartsOf(crop, stage);
        var expected = ExpectedBounds(parts);

        var actual = CropArt.Bounds(parts);

        Assert.Equal(expected.Width, actual.Width, 1e-3f);
        Assert.Equal(expected.Height, actual.Height, 1e-3f);
    }
}
