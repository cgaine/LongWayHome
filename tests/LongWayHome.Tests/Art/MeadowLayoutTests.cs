using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The meadow layout (#77, grown in #150): the seeded prop scatter, its exclusion zones, the path's stepping stones and
/// the palette role of every mesh under <c>Meadow</c>. Since #150 the 72 x 36 grid (world x −36..36, z −4.5..31.5)
/// is farm ground, so every prop lies outside it, in the strip north of it, and the mown margin and stone edging round
/// the old plot are gone. The zones and grid geometry are re-encoded here from the issues' Specifications, not taken
/// from production code.
/// </summary>
public class MeadowLayoutTests
{
    private const float Eps = 1e-4f;

    // The grid (FarmGridLayout): x -36..36, z -4.5..31.5. North is negative z (the house is at z -9).
    private const float GridMinX = -36f;
    private const float GridMaxX = 36f;
    private const float GridMinZ = -4.5f;
    private const float GridMaxZ = 31.5f;

    // The farm inside its walls: the west wall's face at x -36, the river bank at x 36.5, the north wall's face at
    // z -13 and the south wall's face at z 31.5.
    private const float FarmMinX = -36f;
    private const float FarmMaxX = 36.5f;
    private const float FarmMinZ = -13f;
    private const float FarmMaxZ = 31.5f;

    private static readonly Dictionary<string, int> ExpectedCounts = new()
    {
        ["Tuft"] = 120,
        ["Frond"] = 25,
        ["Puffball"] = 20,
        ["BeadStalk"] = 20,
        ["Rock"] = 14,
        ["Stump"] = 6,
        ["Sapling"] = 8,
        ["ShadePatch"] = 10,
    };

    private static readonly string[] OvergrownKinds = ["Rock", "Stump", "Sapling"];

    private sealed record Rect(string Name, float MinX, float MaxX, float MinZ, float MaxZ)
    {
        public bool StrictlyContains(float x, float z) =>
            x > MinX + Eps && x < MaxX - Eps && z > MinZ + Eps && z < MaxZ - Eps;

        public float DistanceTo(float x, float z)
        {
            var dx = MathF.Max(0f, MathF.Max(MinX - x, x - MaxX));
            var dz = MathF.Max(0f, MathF.Max(MinZ - z, z - MaxZ));
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }

    private sealed record Circle(string Name, float X, float Z, float Radius)
    {
        public bool StrictlyContains(float x, float z) =>
            MathF.Sqrt((x - X) * (x - X) + (z - Z) * (z - Z)) < Radius - Eps;
    }

    private static readonly Rect Grid = new("the grid", GridMinX, GridMaxX, GridMinZ, GridMaxZ);

    private static readonly Rect[] ExclusionRects =
    [
        Grid,
        new("path corridor", -1f, 1f, -7f, -4.5f),
        new("house footprint plus 1 m", -4f, 4f, -12f, -6f),
        new("shipping bin plus 1 m", 1.4f, 4.6f, -7.4f, -4.6f),
        new("shop stall area (moved north, #150)", -11f, -7f, -11f, -7f),
    ];

    private static readonly Circle[] ExclusionCircles =
    [
        new("door point range + 0.5", 0f, -7f, 1.25f + 0.5f),
        new("bin point range + 0.5", 3f, -6f, 1.7f + 0.5f),
        new("shop point range + 0.5", -9f, -9f, 2.2f + 0.5f),
        new("pond point range + 0.5", 10f, -9f, 3.8f + 0.5f),
        new("player start + 1 m", 0f, -5.8f, 1f),
    ];

    /// <summary>The overgrown band is everything of the farm outside the grid (#150).</summary>
    private static bool InOvergrownBand(MeadowProp p) => !Grid.StrictlyContains(p.X, p.Z);

    private static bool OnGround(MeadowProp p) =>
        p.X >= FarmMinX - Eps && p.X <= FarmMaxX + Eps && p.Z >= FarmMinZ - Eps && p.Z <= FarmMaxZ + Eps;

    /// <summary>Every zone the prop (not a shade patch) sits inside; empty when it is clear of them all.</summary>
    private static IEnumerable<string> ZonesHit(MeadowProp p) =>
        ExclusionRects.Where(r => r.StrictlyContains(p.X, p.Z)).Select(r => r.Name)
            .Concat(ExclusionCircles.Where(c => c.StrictlyContains(p.X, p.Z)).Select(c => c.Name));

    private static void AssertPlacementRules(IReadOnlyList<MeadowProp> props, string context)
    {
        foreach (var p in props)
        {
            Assert.True(OnGround(p), $"{context}: {p} lies off the meadow ground");

            if (p.Kind == "ShadePatch")
            {
                Assert.True(
                    Grid.DistanceTo(p.X, p.Z) >= MeadowLayout.MaxShadePatchRadius - Eps,
                    $"{context}: shade patch {p} reaches onto the grid");
                continue;
            }

            var hit = ZonesHit(p).ToList();
            Assert.True(hit.Count == 0, $"{context}: {p} lies inside {string.Join(", ", hit)}");

            if (OvergrownKinds.Contains(p.Kind))
            {
                Assert.True(InOvergrownBand(p), $"{context}: {p} lies outside the overgrown band");
            }
        }
    }

    private static Dictionary<string, int> CountByKind(IEnumerable<MeadowProp> props) =>
        props.GroupBy(p => p.Kind).ToDictionary(g => g.Key, g => g.Count());

    // ---- AC2 ----

    [Fact]
    public void AC2_farm_seed_is_6512()
    {
        var props = MeadowLayout.Generate(6512);

        Assert.Equal(6512, MeadowLayout.FarmSeed);
        Assert.NotEmpty(props);
    }

    [Fact]
    public void AC2_generate_returns_the_same_list_on_every_call()
    {
        var first = MeadowLayout.Generate(MeadowLayout.FarmSeed);
        var second = MeadowLayout.Generate(MeadowLayout.FarmSeed);

        Assert.Equal(first, second);
    }

    [Fact]
    public void AC2_farm_seed_places_every_prop_with_the_exact_counts()
    {
        var counts = CountByKind(MeadowLayout.Generate(MeadowLayout.FarmSeed));

        Assert.Equal(ExpectedCounts.OrderBy(kv => kv.Key), counts.OrderBy(kv => kv.Key));
    }

    // ---- AC3 ----

    [Fact]
    public void AC3_no_prop_lies_inside_an_exclusion_zone()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed);
        Assert.NotEmpty(props);

        foreach (var p in props.Where(p => p.Kind != "ShadePatch"))
        {
            var hit = ZonesHit(p).ToList();
            Assert.True(hit.Count == 0, $"{p} lies inside {string.Join(", ", hit)}");
        }
    }

    [Fact]
    public void AC3_every_prop_lies_on_the_meadow_ground()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed);
        Assert.NotEmpty(props);

        Assert.All(props, p => Assert.True(OnGround(p), $"{p} lies off the ground x -36..36.5, z -13..31.5"));
    }

    [Fact]
    public void AC3_shade_patches_lie_wholly_off_the_grid()
    {
        var patches = MeadowLayout.Generate(MeadowLayout.FarmSeed).Where(p => p.Kind == "ShadePatch").ToList();
        Assert.NotEmpty(patches);

        Assert.All(patches, p => Assert.True(
            Grid.DistanceTo(p.X, p.Z) >= MeadowLayout.MaxShadePatchRadius - Eps,
            $"shade patch {p} reaches onto the grid x -36..36, z -4.5..31.5"));
    }

    [Fact]
    public void AC3_props_spread_along_the_whole_north_strip()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed);

        Assert.Contains(props, p => p.X < -24f);
        Assert.Contains(props, p => p.X > 24f);
    }

    // ---- AC4 ----

    [Fact]
    public void AC4_rocks_stumps_and_saplings_lie_only_in_the_overgrown_band()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed)
            .Where(p => OvergrownKinds.Contains(p.Kind))
            .ToList();
        Assert.NotEmpty(props);

        Assert.All(props, p => Assert.True(InOvergrownBand(p), $"{p} is on the grid x -36..36, z -4.5..31.5"));
    }

    [Fact]
    public void AC4_every_yaw_is_in_0_to_360_and_every_scale_in_0_8_to_1_2()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed);
        Assert.NotEmpty(props);

        Assert.All(props, p =>
        {
            Assert.True(p.YawDegrees >= 0f && p.YawDegrees < 360f, $"{p} yaw is outside [0, 360)");
            Assert.True(p.Scale >= 0.8f - Eps && p.Scale <= 1.2f + Eps, $"{p} scale is outside [0.8, 1.2]");
        });
    }

    // ---- AC5 / BR-T1: no margin or edging on the grid; the path's stones ----

    [Fact]
    public void BR_T1_meadow_has_no_mown_margin_or_edging_on_farmable_tiles()
    {
        Assert.DoesNotContain("MownMargin", MeadowLayout.PartRoles.Keys);
        Assert.DoesNotContain("Edging", MeadowLayout.PartRoles.Keys);
    }

    [Fact]
    public void AC5_three_stepping_stones_lie_on_the_dirt_path()
    {
        var stones = MeadowLayout.PathStones();

        Assert.Equal(3, stones.Count);
        Assert.All(stones, s =>
        {
            Assert.Equal("PathStone", s.Kind);
            Assert.True(s.X >= -0.6f - Eps && s.X <= 0.6f + Eps, $"{s} is off the path's width x -0.6..0.6");
            Assert.True(s.Z >= -7f - Eps && s.Z <= -5.5f + Eps, $"{s} is off the path's length z -7..-5.5");
        });
        Assert.Equal(3, stones.Select(s => (MathF.Round(s.X, 3), MathF.Round(s.Z, 3))).Distinct().Count());
    }

    // ---- AC9 ----

    private static readonly Dictionary<string, string> ExpectedPartRoles = new()
    {
        ["Ground"] = "grass-lit",
        ["ShadePatches"] = "grass-shade",
        ["Path"] = "dirt-path",
        ["PathStones"] = "stone",
        ["TuftBlades"] = "grass-lit",
        ["FrondFrond"] = "foliage",
        ["FrondCurl"] = "leaf-underside",
        ["PuffballStem"] = "grass-shade",
        ["PuffballHead"] = "leaf-underside",
        ["BeadStalkStalk"] = "grass-shade",
        ["BeadStalkBeads"] = "lantern",
        ["RockRock"] = "rock",
        ["StumpBark"] = "trunk",
        ["StumpTop"] = "timber",
        ["SaplingStem"] = "trunk",
        ["SaplingCrown"] = "foliage",
    };

    [Fact]
    public void AC9_part_roles_match_the_specification()
    {
        var roles = MeadowLayout.PartRoles;

        Assert.Equal(
            ExpectedPartRoles.OrderBy(kv => kv.Key, StringComparer.Ordinal),
            roles.OrderBy(kv => kv.Key, StringComparer.Ordinal));
    }

    [Fact]
    public void AC9_every_part_role_is_a_palette_swatch()
    {
        var roles = MeadowLayout.PartRoles;
        Assert.NotEmpty(roles);

        foreach (var (node, role) in roles)
        {
            var error = Record.Exception(() => ArtPalette.UvCentre(role));
            Assert.True(error is null, $"{node}'s role '{role}' is not an ArtPalette swatch");
        }
    }

    // ---- Edge: skipping after 200 attempts ----

    [Fact]
    public void Edge_generate_terminates_within_counts_and_zones_for_seeds_0_to_49()
    {
        Assert.Equal(200, MeadowLayout.MaxAttempts);

        for (var seed = 0; seed < 50; seed++)
        {
            var props = MeadowLayout.Generate(seed);
            var counts = CountByKind(props);

            Assert.True(
                counts.Keys.All(ExpectedCounts.ContainsKey),
                $"seed {seed}: unexpected kinds {string.Join(", ", counts.Keys.Except(ExpectedCounts.Keys))}");
            foreach (var (kind, count) in counts)
            {
                Assert.True(count <= ExpectedCounts[kind], $"seed {seed}: {count} {kind}, more than {ExpectedCounts[kind]}");
            }

            AssertPlacementRules(props, $"seed {seed}");
        }
    }
}
