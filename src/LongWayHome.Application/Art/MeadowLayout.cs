namespace LongWayHome.Application.Art;

/// <summary>One placed meadow piece: its kind, ground position (metres), yaw (degrees) and uniform scale.</summary>
public readonly record struct MeadowProp(string Kind, float X, float Z, float YawDegrees, float Scale);

/// <summary>
/// The farm meadow's layout (#77): the seeded scatter of native plants, rocks, stumps, saplings and shade patches,
/// the stone edging around the plot, the path's stepping stones, and the palette role of every mesh node under
/// <c>Meadow</c>. Pure C#, no Godot.
/// <para>
/// The plot is x −6..6, z −4.5..4.5 (north is −z, towards the house). Props are drawn uniformly over the ground and
/// rejected inside the exclusion zones (the plot plus its 1 m mown margin, the path, the house, bin and shop, every
/// interaction point's range plus 0.5 m and the Player's start); rocks, stumps and saplings go only in the overgrown
/// band (|x| ≥ 12 or |z| ≥ 9). A prop that finds no place in <see cref="MaxAttempts"/> tries is skipped, so
/// generation always ends. Placement is art only: it changes no rule and nothing here has collision.
/// </para>
/// </summary>
public static class MeadowLayout
{
    /// <summary>The seed the farm's meadow uses.</summary>
    public const int FarmSeed = 6512;

    /// <summary>How many positions are tried for one prop before it is skipped.</summary>
    public const int MaxAttempts = 200;

    /// <summary>A shade patch's radius at scale 1; its outline points lie within this times its scale.</summary>
    public const float ShadePatchRadius = 1.5f;

    /// <summary>The largest a shade patch can reach from its centre (radius at the largest scale, 1.2).</summary>
    public const float MaxShadePatchRadius = ShadePatchRadius * MaxScale;

    private const float MinScale = 0.8f;
    private const float MaxScale = 1.2f;

    // The meadow ground the props are drawn over (the 32 x 26 m ground, less 0.5 m at the edge).
    private const float GroundHalfX = 15.5f;
    private const float GroundHalfZ = 12.5f;

    // The overgrown band: |x| >= 12 or |z| >= 9.
    private const float BandX = 12f;
    private const float BandZ = 9f;

    /// <summary>Half the plot's width: the plot is x −6..6.</summary>
    public const float PlotHalfX = 6f;

    /// <summary>Half the plot's depth: the plot is z −4.5..4.5.</summary>
    public const float PlotHalfZ = 4.5f;

    /// <summary>The mown margin's width around the plot.</summary>
    public const float MarginWidth = 1f;

    /// <summary>Half the dirt path's width: the path is x −0.6..0.6.</summary>
    public const float PathHalfWidth = 0.6f;

    // The edging line 0.15 m outside the plot.
    private const float EdgingOffset = 0.15f;
    private const float EdgingSpacing = 0.3f;

    /// <summary>Half the north edging gap where the path meets the plot: no stone centre within this of x = 0.</summary>
    private const float PathGapHalf = PathHalfWidth;

    private readonly record struct Rect(float MinX, float MaxX, float MinZ, float MaxZ)
    {
        public bool Contains(float x, float z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;

        /// <summary>Distance from (x, z) to the rectangle; 0 inside it.</summary>
        public float DistanceTo(float x, float z)
        {
            var dx = MathF.Max(0f, MathF.Max(MinX - x, x - MaxX));
            var dz = MathF.Max(0f, MathF.Max(MinZ - z, z - MaxZ));
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }

    private readonly record struct Circle(float X, float Z, float Radius)
    {
        public bool Contains(float x, float z) => (x - X) * (x - X) + (z - Z) * (z - Z) <= Radius * Radius;
    }

    /// <summary>The plot plus its mown margin, x −7..7, z −5.5..5.5.</summary>
    private static readonly Rect PlotPlusMargin = new(
        -(PlotHalfX + MarginWidth), PlotHalfX + MarginWidth, -(PlotHalfZ + MarginWidth), PlotHalfZ + MarginWidth);

    private static readonly Rect[] ExclusionRects =
    [
        PlotPlusMargin,
        new(-1f, 1f, -7f, -5.5f), // the path corridor
        new(-4f, 4f, -12f, -6f), // the house footprint plus 1 m
        new(1.4f, 4.6f, -7.4f, -4.6f), // the shipping bin plus 1 m
        new(-11f, -7f, -2f, 2f), // the shop stall area
    ];

    private static readonly Circle[] ExclusionCircles =
    [
        new(0f, -7f, 1.25f + 0.5f), // DoorPoint, range 1.25
        new(3f, -6f, 1.7f + 0.5f), // BinPoint, range 1.7
        new(-9f, 0f, 2.2f + 0.5f), // ShopPoint, range 2.2
        new(10f, 0f, 3.8f + 0.5f), // PondPoint, range 3.8 (also covers the 2.5 m water)
        new(0f, -5.8f, 1f), // the Player's start
    ];

    /// <summary>Each kind and how many of it, in the order they are drawn from the generator.</summary>
    private static readonly (string Kind, int Count)[] Counts =
    [
        ("ShadePatch", 10),
        ("Rock", 14),
        ("Stump", 6),
        ("Sapling", 8),
        ("Tuft", 120),
        ("Frond", 25),
        ("Puffball", 20),
        ("BeadStalk", 20),
    ];

    /// <summary>Node name under <c>Meadow</c> to <see cref="ArtPalette"/> role.</summary>
    public static IReadOnlyDictionary<string, string> PartRoles { get; } = new Dictionary<string, string>
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

    /// <summary>The scattered props and shade patches for <paramref name="seed"/>; the same list for the same seed.</summary>
    public static IReadOnlyList<MeadowProp> Generate(int seed) => Generate(seed, IsAllowed);

    /// <summary>
    /// <see cref="Generate(int)"/> with the placement test supplied: <paramref name="allowed"/> says whether a kind
    /// may stand at (x, z). Each prop tries <see cref="MaxAttempts"/> positions and is skipped if none is allowed.
    /// </summary>
    internal static IReadOnlyList<MeadowProp> Generate(int seed, Func<string, float, float, bool> allowed)
    {
        var random = new Random(seed);
        var props = new List<MeadowProp>();
        foreach (var (kind, count) in Counts)
        {
            for (var i = 0; i < count; i++)
            {
                for (var attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    var x = Between(random, -GroundHalfX, GroundHalfX);
                    var z = Between(random, -GroundHalfZ, GroundHalfZ);
                    if (!allowed(kind, x, z))
                    {
                        continue;
                    }

                    props.Add(new MeadowProp(kind, x, z, Yaw(random), Between(random, MinScale, MaxScale)));
                    break;
                }
            }
        }

        return props;
    }

    /// <summary>
    /// The outline of a shade patch from <see cref="Generate(int)"/>: 6 to 9 points around its centre, in order,
    /// each within <see cref="ShadePatchRadius"/> times its scale. The same outline for the same patch.
    /// </summary>
    public static IReadOnlyList<(float X, float Z)> ShadePatchOutline(MeadowProp patch)
    {
        // Seeded from the patch's position so each patch has its own shape, independent of call order.
        var random = new Random(unchecked((int)MathF.Round(patch.X * 1000f) * 73856093 ^ (int)MathF.Round(patch.Z * 1000f) * 19349663));
        var sides = random.Next(6, 10);
        var radius = ShadePatchRadius * patch.Scale;
        var start = patch.YawDegrees * MathF.PI / 180f;
        var points = new List<(float X, float Z)>(sides);
        for (var i = 0; i < sides; i++)
        {
            var angle = start + (i + Between(random, -0.3f, 0.3f)) * 2f * MathF.PI / sides;
            var r = radius * Between(random, 0.85f, 1f); // at least 1.02 m, so every patch is 2 m or more across
            points.Add((patch.X + r * MathF.Cos(angle), patch.Z + r * MathF.Sin(angle)));
        }

        return points;
    }

    /// <summary>The edging stones around the plot, kind <c>EdgingStone</c>, X/Z at each stone's centre.</summary>
    /// <remarks>
    /// Every 0.3 m on the lines 0.15 m outside each plot edge, with a stone on each corner; the north side
    /// (z = −4.65) leaves out every stone within 0.6 m of x = 0, where the path meets the plot.
    /// </remarks>
    public static IReadOnlyList<MeadowProp> EdgingStones()
    {
        const float edgeX = PlotHalfX + EdgingOffset;
        const float edgeZ = PlotHalfZ + EdgingOffset;
        var alongX = (int)MathF.Round(2f * edgeX / EdgingSpacing);
        var alongZ = (int)MathF.Round(2f * edgeZ / EdgingSpacing);

        var random = new Random(FarmSeed);
        var stones = new List<MeadowProp>();
        void Add(float x, float z) => stones.Add(new MeadowProp(
            "EdgingStone", x, z, Yaw(random), Between(random, 0.9f, 1.1f)));

        // North and south, corner to corner.
        for (var i = 0; i <= alongX; i++)
        {
            var x = -edgeX + i * EdgingSpacing;
            if (MathF.Abs(x) >= PathGapHalf)
            {
                Add(x, -edgeZ);
            }

            Add(x, edgeZ);
        }

        // West and east, between the corners the north and south rows already hold.
        for (var i = 1; i < alongZ; i++)
        {
            var z = -edgeZ + i * EdgingSpacing;
            Add(-edgeX, z);
            Add(edgeX, z);
        }

        return stones;
    }

    /// <summary>The three stepping stones on the dirt path, kind <c>PathStone</c>.</summary>
    /// <remarks>Staggered down the path from the door (z −7) to the margin (z −5.5), about 0.5 m apart.</remarks>
    public static IReadOnlyList<MeadowProp> PathStones() =>
    [
        new("PathStone", 0.1f, -6.7f, 20f, 1f),
        new("PathStone", -0.12f, -6.2f, 75f, 0.95f),
        new("PathStone", 0.08f, -5.75f, 140f, 1.05f),
    ];

    /// <summary>The farm's placement rules: outside every exclusion zone, and the overgrown band for the big props.</summary>
    private static bool IsAllowed(string kind, float x, float z)
    {
        if (kind == "ShadePatch")
        {
            // The whole patch, not just its centre, stays outside the plot and margin.
            return PlotPlusMargin.DistanceTo(x, z) >= MaxShadePatchRadius;
        }

        if (kind is "Rock" or "Stump" or "Sapling" && !(MathF.Abs(x) >= BandX || MathF.Abs(z) >= BandZ))
        {
            return false;
        }

        return !ExclusionRects.Any(r => r.Contains(x, z)) && !ExclusionCircles.Any(c => c.Contains(x, z));
    }

    /// <summary>A yaw in [0, 360): a double just under 360 can round up to 360 as a float, so that wraps to 0.</summary>
    private static float Yaw(Random random) => (float)(random.NextDouble() * 360.0) % 360f;

    private static float Between(Random random, float min, float max) =>
        min + (float)random.NextDouble() * (max - min);
}
