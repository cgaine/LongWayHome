using System.Numerics;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Tests.Art;

/// <summary>
/// AC1–AC4 of #90, AC1–AC5 of #91 and AC1–AC4 of #92: the part table of the emberroot, dusktuber, sunbell, starcorn and
/// moonmelon models (<see cref="CropArt"/>), which draws every crop at every stage, and its
/// <see cref="CropArt.Bounds"/>. Rotations follow Godot's Euler YXZ (<c>Node3D.RotationDegrees</c>), which is
/// <see cref="Quaternion.CreateFromYawPitchRoll"/>, applied about the part's centre. The unrotated box of a part is
/// SizeX x SizeY x SizeZ for a Box or Prism, SizeX x SizeY x SizeX for a Sphere and max(SizeX, SizeZ) x SizeY x
/// max(SizeX, SizeZ) for a Cylinder.
/// </summary>
public class CropArtTests
{
    private const float Tolerance = 1e-4f;

    private static readonly CropTypeId[] DrawnCrops =
        [CropTypeId.Emberroot, CropTypeId.Dusktuber, CropTypeId.Sunbell, CropTypeId.Starcorn, CropTypeId.Moonmelon];

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

    private static IReadOnlyList<CropPart> PartsOf(CropTypeId crop, GrowthStage stage) => CropArt.Parts(crop, stage);

    private static string ColourRole(CropTypeId crop) => crop switch
    {
        CropTypeId.Emberroot => "ember",
        CropTypeId.Dusktuber => "dusktuber",
        CropTypeId.Sunbell => "sunbell-gold",
        CropTypeId.Starcorn => "starcorn",
        CropTypeId.Moonmelon => "moonmelon",
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

    // ---- AC1: parts for every stage of every crop, palette roles only ----

    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void AC1_drawn_crops_have_parts_at_every_stage(CropTypeId crop, GrowthStage stage) =>
        Assert.NotEmpty(CropArt.Parts(crop, stage));

    /// <summary>#92 draws the last crop, so every crop has a model at every stage and the prototype fallback is gone.</summary>
    [Fact]
    public void AC1_every_crop_and_stage_has_parts()
    {
        foreach (var crop in Enum.GetValues<CropTypeId>())
        {
            foreach (var stage in Enum.GetValues<GrowthStage>())
            {
                Assert.True(CropArt.Parts(crop, stage).Count > 0, $"{crop} {stage} has no parts");
            }
        }
    }

    /// <summary><see cref="CropArt.Parts"/> is non-nullable: a (crop, stage) with no model is a programming error.</summary>
    [Fact]
    public void AC1_parts_reject_an_undefined_crop() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CropArt.Parts((CropTypeId)999, GrowthStage.Ripe));

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

    /// <summary>Part names identify parts in the table and the tests, so each one is used once per model.</summary>
    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void Every_part_name_is_unique_within_its_model(CropTypeId crop, GrowthStage stage)
    {
        var names = PartsOf(crop, stage).Select(part => part.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>The issue's budget: one merged mesh per model, of under about 40 parts, holds 30 fps on a full plot (REQ-14).</summary>
    [Theory]
    [MemberData(nameof(DrawnModels))]
    public void Every_model_has_at_most_40_parts(CropTypeId crop, GrowthStage stage) =>
        Assert.InRange(PartsOf(crop, stage).Count, 1, 40);

    // ---- AC2: each stage is taller or wider than the one before ----

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Sunbell)]
    [InlineData(CropTypeId.Starcorn)]
    [InlineData(CropTypeId.Moonmelon)]
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
    [InlineData(CropTypeId.Sunbell, GrowthStage.Seed)]
    [InlineData(CropTypeId.Sunbell, GrowthStage.Sprout)]
    [InlineData(CropTypeId.Starcorn, GrowthStage.Seed)]
    [InlineData(CropTypeId.Starcorn, GrowthStage.Sprout)]
    [InlineData(CropTypeId.Moonmelon, GrowthStage.Seed)]
    [InlineData(CropTypeId.Moonmelon, GrowthStage.Sprout)]
    public void AC4_seed_and_sprout_include_a_part_in_the_crops_colour_role(CropTypeId crop, GrowthStage stage) =>
        Assert.Contains(PartsOf(crop, stage), part => part.Role == ColourRole(crop));

    /// <summary>The ripe bounds of every crop other than <paramref name="except"/> that <see cref="CropArt"/> draws.</summary>
    private static List<(CropTypeId Crop, (float Width, float Height) Bounds)> OtherDrawnRipeBounds(CropTypeId except) =>
        DrawnCrops
            .Where(crop => crop != except)
            .Select(crop => (crop, CropArt.Bounds(CropArt.Parts(crop, GrowthStage.Ripe))))
            .ToList();

    [Fact]
    public void AC4_ripe_moonmelon_is_the_widest_ripe_crop_by_10cm_and_under_half_a_metre()
    {
        var moonmelon = CropArt.Bounds(PartsOf(CropTypeId.Moonmelon, GrowthStage.Ripe));
        var others = OtherDrawnRipeBounds(CropTypeId.Moonmelon);

        Assert.NotEmpty(others);
        foreach (var (crop, bounds) in others)
        {
            Assert.True(
                moonmelon.Width >= bounds.Width + 0.1f - Tolerance,
                $"ripe moonmelon width {moonmelon.Width} is not 0.1 m more than ripe {crop}'s {bounds.Width}");
        }

        Assert.True(moonmelon.Height < 0.5f, $"ripe moonmelon height {moonmelon.Height} is not under 0.5 m");
    }

    [Fact]
    public void AC4_ripe_moonmelon_has_at_least_five_moonmelon_ribs_around_its_centre()
    {
        var ribs = PartsOf(CropTypeId.Moonmelon, GrowthStage.Ripe)
            .Where(part => part.Name.StartsWith("Rib", StringComparison.Ordinal) && part.Role == "moonmelon")
            .ToList();

        Assert.True(ribs.Count >= 5, $"ripe moonmelon has {ribs.Count} moonmelon Rib parts, not at least 5");

        foreach (var rib in ribs)
        {
            var radius = MathF.Sqrt((rib.X * rib.X) + (rib.Z * rib.Z));
            Assert.True(radius >= 0.05f - Tolerance, $"{rib.Name} is {radius} m from the centre, not at least 0.05 m out");
        }

        // The ribs go all the way round: no gap of more than 90° between neighbours, the wrap-around included.
        var angles = ribs.Select(rib => MathF.Atan2(rib.X, rib.Z) * 180f / MathF.PI).Order().ToList();
        for (var i = 0; i < angles.Count; i++)
        {
            var next = i + 1 < angles.Count ? angles[i + 1] : angles[0] + 360f;
            Assert.True(
                next - angles[i] <= 90f + Tolerance,
                $"ripe moonmelon's ribs leave a {next - angles[i]}° gap after {angles[i]}°");
        }

        var meanX = ribs.Average(rib => rib.X);
        var meanZ = ribs.Average(rib => rib.Z);
        Assert.True(
            MathF.Sqrt((meanX * meanX) + (meanZ * meanZ)) <= 0.03f + Tolerance,
            $"ripe moonmelon's ribs centre on ({meanX}, {meanZ}), not within 0.03 m of the axis");
    }

    [Fact]
    public void AC4_ripe_starcorn_is_at_least_1_5_times_as_tall_as_every_other_ripe_crop()
    {
        var starcorn = CropArt.Bounds(PartsOf(CropTypeId.Starcorn, GrowthStage.Ripe));
        var others = OtherDrawnRipeBounds(CropTypeId.Starcorn);

        Assert.NotEmpty(others);
        foreach (var (crop, bounds) in others)
        {
            Assert.True(
                starcorn.Height >= (1.5f * bounds.Height) - Tolerance,
                $"ripe starcorn height {starcorn.Height} is not 1.5 x ripe {crop}'s {bounds.Height}");
        }
    }

    [Fact]
    public void AC4_ripe_sunbell_bounds_differ_from_every_other_ripe_crop_by_10cm()
    {
        var sunbell = CropArt.Bounds(PartsOf(CropTypeId.Sunbell, GrowthStage.Ripe));
        var others = OtherDrawnRipeBounds(CropTypeId.Sunbell);

        Assert.NotEmpty(others);
        foreach (var (crop, bounds) in others)
        {
            var dHeight = MathF.Abs(sunbell.Height - bounds.Height);
            var dWidth = MathF.Abs(sunbell.Width - bounds.Width);
            Assert.True(
                dHeight >= 0.1f - Tolerance || dWidth >= 0.1f - Tolerance,
                $"ripe sunbell {sunbell} is within 0.1 m of ripe {crop} {bounds} in both height and width");
        }
    }

    // ---- AC5: sunbell berries hang point down; starcorn's sparkles sit on the cob ----

    [Fact]
    public void AC5_each_ripe_sunbell_berry_hangs_point_down_below_its_sepals()
    {
        var parts = PartsOf(CropTypeId.Sunbell, GrowthStage.Ripe);
        var berryParts = parts.Where(part => part.Name.StartsWith("Berry", StringComparison.Ordinal)).ToList();
        var indices = berryParts
            .Where(part => part.Name.EndsWith("Top", StringComparison.Ordinal))
            .Select(part => part.Name["Berry".Length..^"Top".Length])
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(Enumerable.Range(0, 6).Select(i => i.ToString()).Order(StringComparer.Ordinal), indices);
        for (var i = 0; i < 6; i++)
        {
            var top = Assert.Single(parts, part => part.Name == $"Berry{i}Top");
            var tip = Assert.Single(parts, part => part.Name == $"Berry{i}Tip");
            var sepals = parts.Where(part => part.Name.StartsWith($"Berry{i}Sepals", StringComparison.Ordinal)).ToList();

            Assert.Equal("sunbell-gold", top.Role);
            Assert.Equal("sunbell-orange", tip.Role);
            Assert.NotEmpty(sepals);
            Assert.All(sepals, sepal => Assert.Equal("foliage", sepal.Role));

            Assert.True(tip.Y < top.Y, $"Berry{i}Tip at y {tip.Y} is not below Berry{i}Top at y {top.Y}");
            Assert.True(MathF.Abs(tip.X - top.X) <= 0.01f + Tolerance, $"Berry{i}Tip X {tip.X} is not under Top X {top.X}");
            Assert.True(MathF.Abs(tip.Z - top.Z) <= 0.01f + Tolerance, $"Berry{i}Tip Z {tip.Z} is not under Top Z {top.Z}");
            foreach (var sepal in sepals)
            {
                Assert.True(sepal.Y > top.Y, $"{sepal.Name} at y {sepal.Y} is not above Berry{i}Top at y {top.Y}");
            }

            // The tip's narrow end points down: a cone, narrow at the bottom, not tipped over.
            Assert.Equal(CropPartShape.Cylinder, tip.Shape);
            Assert.True(tip.SizeX < tip.SizeZ, $"Berry{i}Tip bottom {tip.SizeX} is not narrower than its top {tip.SizeZ}");
            Assert.True(MathF.Abs(tip.PitchDegrees) <= 5f, $"Berry{i}Tip is pitched {tip.PitchDegrees}°");
            Assert.True(MathF.Abs(tip.RollDegrees) <= 5f, $"Berry{i}Tip is rolled {tip.RollDegrees}°");
        }
    }

    [Fact]
    public void AC5_nothing_on_the_ripe_sunbell_suggests_a_bell() =>
        Assert.DoesNotContain(
            PartsOf(CropTypeId.Sunbell, GrowthStage.Ripe),
            part => part.Name.Contains("Bell", StringComparison.Ordinal));

    [Fact]
    public void AC5_ripe_starcorn_has_at_least_six_sparkles_on_the_cob_surface()
    {
        const float onSurface = 0.03f;
        var parts = PartsOf(CropTypeId.Starcorn, GrowthStage.Ripe);
        var cob = Assert.Single(parts, part => part.Name == "Cob");
        var sparkles = parts.Where(part => part.Role == "starcorn-sparkle").ToList();

        Assert.Equal(CropPartShape.Cylinder, cob.Shape);
        Assert.True(sparkles.Count >= 6, $"ripe starcorn has {sparkles.Count} starcorn-sparkle parts, not at least 6");

        var inverse = Quaternion.Conjugate(Quaternion.CreateFromYawPitchRoll(
            cob.YawDegrees * MathF.PI / 180f,
            cob.PitchDegrees * MathF.PI / 180f,
            cob.RollDegrees * MathF.PI / 180f));
        var cobCentre = new Vector3(cob.X, cob.Y, cob.Z);
        var halfLength = cob.SizeY / 2f;

        foreach (var sparkle in sparkles)
        {
            var local = Vector3.Transform(new Vector3(sparkle.X, sparkle.Y, sparkle.Z) - cobCentre, inverse);
            var r = MathF.Sqrt((local.X * local.X) + (local.Z * local.Z));
            var t = Math.Clamp((local.Y + halfLength) / cob.SizeY, 0f, 1f);
            var surface = (cob.SizeX / 2f) + (((cob.SizeZ / 2f) - (cob.SizeX / 2f)) * t);

            Assert.True(
                MathF.Abs(local.Y) <= halfLength + onSurface,
                $"{sparkle.Name} is {local.Y} m along the cob, beyond its half-length {halfLength}");
            Assert.True(
                MathF.Abs(r - surface) <= onSurface,
                $"{sparkle.Name} is {r} m from the cob's axis, not within {onSurface} m of its surface radius {surface}");
        }
    }

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

    [Fact]
    public void Ripe_starcorn_has_a_foliage_stalk_a_starcorn_cob_two_husks_and_gold_tassels()
    {
        var parts = PartsOf(CropTypeId.Starcorn, GrowthStage.Ripe);
        var tassels = parts.Where(part => part.Name.StartsWith("Tassel", StringComparison.Ordinal)).ToList();

        Assert.Contains(parts, part => part.Name == "Stalk" && part.Shape == CropPartShape.Cylinder && part.Role == "foliage");
        Assert.Contains(parts, part => part.Name == "Cob" && part.Role == "starcorn");
        Assert.Contains(parts, part => part.Name == "HuskLeft" && part.Role == "grass-mown");
        Assert.Contains(parts, part => part.Name == "HuskRight" && part.Role == "grass-mown");
        Assert.NotEmpty(tassels);
        Assert.All(tassels, tassel => Assert.Equal("sunbell-gold", tassel.Role));
    }

    [Fact]
    public void Ripe_sunbell_is_a_bush_of_foliage_spheres()
    {
        var foliage = PartsOf(CropTypeId.Sunbell, GrowthStage.Ripe)
            .Where(part => part.Name.StartsWith("Foliage", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(foliage);
        Assert.All(foliage, part =>
        {
            Assert.Equal(CropPartShape.Sphere, part.Shape);
            Assert.Equal("foliage", part.Role);
        });
    }

    [Fact]
    public void Young_sunbell_has_one_small_green_berry()
    {
        var berry = Assert.Single(PartsOf(CropTypeId.Sunbell, GrowthStage.Young), part => part.Name == "Berry");

        Assert.Equal("grass-mown", berry.Role);
    }

    [Fact]
    public void Young_moonmelon_is_a_low_vine_with_a_pale_bud()
    {
        var parts = PartsOf(CropTypeId.Moonmelon, GrowthStage.Young);
        var young = CropArt.Bounds(parts);
        var sprout = CropArt.Bounds(PartsOf(CropTypeId.Moonmelon, GrowthStage.Sprout));

        Assert.True(young.Height < 0.25f, $"young moonmelon height {young.Height} is not under 0.25 m");
        Assert.True(young.Width > sprout.Width, $"young moonmelon width {young.Width} is not more than the sprout's {sprout.Width}");
        Assert.Contains(parts, part => part.Name == "Vine" && part.Role == "foliage");
        Assert.Contains(parts, part => part.Name == "Bud" && part.Role == "moonmelon");
        Assert.Contains(parts, part => part.Role == "leaf-underside");
    }

    [Fact]
    public void Ripe_moonmelon_has_a_foliage_vine_stalk_and_tendril()
    {
        var parts = PartsOf(CropTypeId.Moonmelon, GrowthStage.Ripe);

        Assert.Contains(parts, part => part.Name == "Vine" && part.Role == "foliage");
        Assert.Contains(parts, part => part.Name == "Stalk" && part.Role == "foliage");
        Assert.Contains(parts, part => part.Name == "Tendril" && part.Role == "foliage");
        Assert.Contains(parts, part => part.Name == "Crown" && part.Role == "moonmelon");
    }

    /// <summary>The issue's budget for the widest model: keep the ripe moonmelon under about 30 parts (REQ-14).</summary>
    [Fact]
    public void Ripe_moonmelon_has_at_most_30_parts() =>
        Assert.InRange(PartsOf(CropTypeId.Moonmelon, GrowthStage.Ripe).Count, 1, 30);

    [Fact]
    public void Young_starcorn_has_a_foliage_stalk() =>
        Assert.Contains(
            PartsOf(CropTypeId.Starcorn, GrowthStage.Young),
            part => part.Name == "Stalk" && part.Role == "foliage");

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Sunbell)]
    [InlineData(CropTypeId.Starcorn)]
    [InlineData(CropTypeId.Moonmelon)]
    public void Seed_is_a_dirt_path_mound_and_a_seed_in_the_crops_colour(CropTypeId crop)
    {
        var parts = PartsOf(crop, GrowthStage.Seed);

        Assert.Contains(parts, part => part.Name == "Mound" && part.Role == "dirt-path");
        Assert.Contains(parts, part => part.Name == "Seed" && part.Role == ColourRole(crop));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Sunbell)]
    [InlineData(CropTypeId.Starcorn)]
    [InlineData(CropTypeId.Moonmelon)]
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
