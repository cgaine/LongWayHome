using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The meadow layout (#77): the seeded prop scatter, its exclusion zones and overgrown band, the stone edging around
/// the plot, the path's stepping stones and the palette role of every mesh under <c>Meadow</c>. The zones, band,
/// plot and edging geometry are re-encoded here from the issue's Specification, not taken from production code.
/// </summary>
public class MeadowLayoutTests
{
    private const float Eps = 1e-4f;

    // The plot (FarmGrid): x -6..6, z -4.5..4.5. North is negative z (the house is at z -9).
    private const float PlotHalfX = 6f;
    private const float PlotHalfZ = 4.5f;

    // Edging stones are centred 0.15 m outside the plot edge.
    private const float EdgeX = 6.15f;
    private const float EdgeZ = 4.65f;
    private const float StoneSpacing = 0.3f;
    private const float StoneRadius = 0.125f;

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
    }

    private sealed record Circle(string Name, float X, float Z, float Radius)
    {
        public bool StrictlyContains(float x, float z) =>
            MathF.Sqrt((x - X) * (x - X) + (z - Z) * (z - Z)) < Radius - Eps;
    }

    private static readonly Rect PlotPlusMargin = new("plot plus margin", -7f, 7f, -5.5f, 5.5f);

    private static readonly Rect[] ExclusionRects =
    [
        PlotPlusMargin,
        new("path corridor", -1f, 1f, -7f, -5.5f),
        new("house footprint plus 1 m", -4f, 4f, -12f, -6f),
        new("shipping bin plus 1 m", 1.4f, 4.6f, -7.4f, -4.6f),
        new("shop stall area", -11f, -7f, -2f, 2f),
    ];

    private static readonly Circle[] ExclusionCircles =
    [
        new("door point range + 0.5", 0f, -7f, 1.25f + 0.5f),
        new("bin point range + 0.5", 3f, -6f, 1.7f + 0.5f),
        new("shop point range + 0.5", -9f, 0f, 2.2f + 0.5f),
        new("pond point range + 0.5", 10f, 0f, 3.8f + 0.5f),
        new("player start + 1 m", 0f, -5.8f, 1f),
    ];

    private static bool InOvergrownBand(MeadowProp p) =>
        MathF.Abs(p.X) >= 12f - Eps || MathF.Abs(p.Z) >= 9f - Eps;

    private static bool OnGround(MeadowProp p) =>
        p.X >= -15.5f - Eps && p.X <= 15.5f + Eps && p.Z >= -12.5f - Eps && p.Z <= 12.5f + Eps;

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
                Assert.False(
                    PlotPlusMargin.StrictlyContains(p.X, p.Z),
                    $"{context}: shade patch {p} is centred inside the plot plus margin");
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

        Assert.All(props, p => Assert.True(OnGround(p), $"{p} lies off the ground x -15.5..15.5, z -12.5..12.5"));
    }

    [Fact]
    public void AC3_shade_patches_lie_outside_the_plot_plus_margin()
    {
        var patches = MeadowLayout.Generate(MeadowLayout.FarmSeed).Where(p => p.Kind == "ShadePatch").ToList();
        Assert.NotEmpty(patches);

        Assert.All(patches, p => Assert.False(
            PlotPlusMargin.StrictlyContains(p.X, p.Z),
            $"shade patch {p} is centred inside x -7..7, z -5.5..5.5"));
    }

    // ---- AC4 ----

    [Fact]
    public void AC4_rocks_stumps_and_saplings_lie_only_in_the_overgrown_band()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed)
            .Where(p => OvergrownKinds.Contains(p.Kind))
            .ToList();
        Assert.NotEmpty(props);

        Assert.All(props, p => Assert.True(InOvergrownBand(p), $"{p} is not in |x| >= 12 or |z| >= 9"));
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

    // ---- AC5 / BR-T1: edging and path stones ----

    public enum Side { North, South, West, East }

    private static IEnumerable<Side> SidesOf(MeadowProp s)
    {
        if (MathF.Abs(s.Z + EdgeZ) <= Eps && MathF.Abs(s.X) <= EdgeX + Eps) yield return Side.North;
        if (MathF.Abs(s.Z - EdgeZ) <= Eps && MathF.Abs(s.X) <= EdgeX + Eps) yield return Side.South;
        if (MathF.Abs(s.X + EdgeX) <= Eps && MathF.Abs(s.Z) <= EdgeZ + Eps) yield return Side.West;
        if (MathF.Abs(s.X - EdgeX) <= Eps && MathF.Abs(s.Z) <= EdgeZ + Eps) yield return Side.East;
    }

    /// <summary>Positions along a side, sorted: x for north and south, z for west and east.</summary>
    private static List<float> Along(IReadOnlyList<MeadowProp> stones, Side side) =>
        stones.Where(s => SidesOf(s).Contains(side))
            .Select(s => side is Side.North or Side.South ? s.X : s.Z)
            .OrderBy(v => v)
            .ToList();

    private static List<(float From, float To)> GapsOver(List<float> along, float limit)
    {
        var gaps = new List<(float, float)>();
        for (var i = 1; i < along.Count; i++)
        {
            if (along[i] - along[i - 1] > limit + Eps)
            {
                gaps.Add((along[i - 1], along[i]));
            }
        }

        return gaps;
    }

    [Fact]
    public void AC5_edging_stones_are_centred_0_15_m_outside_the_plot_edge()
    {
        var stones = MeadowLayout.EdgingStones();
        Assert.NotEmpty(stones);

        Assert.All(stones, s =>
        {
            Assert.Equal("EdgingStone", s.Kind);
            Assert.True(SidesOf(s).Any(), $"{s} is not on x = ±6.15 or z = ±4.65 within the edging rectangle");
        });
    }

    [Fact]
    public void AC5_edging_reaches_every_corner()
    {
        var stones = MeadowLayout.EdgingStones();
        Assert.NotEmpty(stones);

        foreach (var (cx, cz) in new[] { (-EdgeX, -EdgeZ), (EdgeX, -EdgeZ), (-EdgeX, EdgeZ), (EdgeX, EdgeZ) })
        {
            Assert.True(
                stones.Any(s => MathF.Sqrt((s.X - cx) * (s.X - cx) + (s.Z - cz) * (s.Z - cz)) <= 0.16f + Eps),
                $"no edging stone within 0.16 m of the corner ({cx}, {cz})");
        }

        foreach (var side in Enum.GetValues<Side>())
        {
            var along = Along(stones, side);
            var half = side is Side.North or Side.South ? EdgeX : EdgeZ;
            Assert.True(along.Count > 0, $"the {side} side has no stones");
            Assert.True(along[0] <= -half + 0.16f + Eps, $"the {side} side starts at {along[0]}, short of its corner");
            Assert.True(along[^1] >= half - 0.16f - Eps, $"the {side} side ends at {along[^1]}, short of its corner");
        }
    }

    [Theory]
    [InlineData(Side.South)]
    [InlineData(Side.West)]
    [InlineData(Side.East)]
    public void AC5_edging_runs_unbroken_on_the_east_south_and_west_sides(Side side)
    {
        var along = Along(MeadowLayout.EdgingStones(), side);
        Assert.True(along.Count > 1, $"the {side} side has {along.Count} stones");

        var gaps = GapsOver(along, StoneSpacing);
        Assert.True(gaps.Count == 0, $"the {side} side has gaps over 0.3 m: {string.Join(", ", gaps)}");
    }

    [Fact]
    public void AC5_north_edging_leaves_a_1_2_m_gap_for_the_path_and_is_unbroken_elsewhere()
    {
        var along = Along(MeadowLayout.EdgingStones(), Side.North);
        Assert.True(along.Count > 1, $"the north side has {along.Count} stones");

        Assert.DoesNotContain(along, x => MathF.Abs(x) < 0.6f - Eps);
        Assert.Contains(along, x => x < 0f && x >= -0.9f - Eps);
        Assert.Contains(along, x => x > 0f && x <= 0.9f + Eps);

        var gaps = GapsOver(along, StoneSpacing);
        Assert.True(
            gaps.Count == 1 && gaps[0].From < 0f && gaps[0].To > 0f,
            $"the north side should break only across x = 0, but has gaps over 0.3 m at {string.Join(", ", gaps)}");
    }

    [Fact]
    public void BR_T1_no_edging_stone_sits_on_a_tile()
    {
        var stones = MeadowLayout.EdgingStones();
        Assert.NotEmpty(stones);

        Assert.All(stones, s =>
        {
            // Distance from the stone's centre to the plot rectangle x -6..6, z -4.5..4.5 (0 when inside).
            var dx = MathF.Max(0f, MathF.Abs(s.X) - PlotHalfX);
            var dz = MathF.Max(0f, MathF.Abs(s.Z) - PlotHalfZ);
            var distance = MathF.Sqrt(dx * dx + dz * dz);
            Assert.True(
                distance >= StoneRadius - Eps,
                $"{s} (a {StoneRadius} m radius stone) overlaps the plot by {StoneRadius - distance} m");
        });
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
        ["MownMargin"] = "grass-mown",
        ["Path"] = "dirt-path",
        ["PathStones"] = "stone",
        ["Edging"] = "rock",
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
