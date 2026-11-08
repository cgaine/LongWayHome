namespace LongWayHome.Application.Art;

/// <summary>One placed meadow piece: its kind, ground position (metres), yaw (degrees) and uniform scale.</summary>
public readonly record struct MeadowProp(string Kind, float X, float Z, float YawDegrees, float Scale);

/// <summary>
/// The farm meadow's layout (#77, grown in #150): the seeded scatter of native plants, rocks, stumps, saplings and shade
/// patches, the path's stepping stones, and the palette role of every mesh node under <c>Meadow</c>. Pure C#, no Godot.
/// <para>
/// Since #150 the 72 x 36 grid (world x −36..36, z −4.5..31.5, <c>FarmGridLayout</c>) is farm ground, so there is no
/// mown margin or stone edging round a plot, and every prop lies off the grid, in the strip north of it (z −13..−4.5,
/// north is −z, towards the house). Props are drawn uniformly over that strip and rejected inside the exclusion zones
/// (the grid, the path, the house, bin and shop, every interaction point's range plus 0.5 m and the Player's start), so
/// rocks, stumps and saplings, which belong to the overgrown band, also stay off the grid. A prop that finds no place in
/// <see cref="MaxAttempts"/> tries is skipped, so generation always ends. Placement is art only: it changes no rule and
/// nothing here has collision.
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

    /// <summary>The river's west bank down the farm's east edge (#137, moved in #150): the meadow ground's east edge and
    /// the face of the scene's <c>Boundary/East</c> wall, just east of the grid.</summary>
    public const float RiverWestX = 36.5f;

    /// <summary>The meadow ground's west edge: the face of the scene's <c>Boundary/West</c> wall, the grid's west edge.</summary>
    public const float GroundWestX = -36f;

    /// <summary>The meadow ground's north edge: the face of the scene's <c>Boundary/North</c> wall.</summary>
    public const float GroundNorthZ = -13f;

    /// <summary>The meadow ground's south edge: the face of the scene's south walls, the grid's south edge.</summary>
    public const float GroundSouthZ = 31.5f;

    /// <summary>The grid's west edge in world x (<c>FarmGridLayout</c>).</summary>
    public const float GridWestX = -36f;

    /// <summary>The grid's east edge in world x.</summary>
    public const float GridEastX = 36f;

    /// <summary>The grid's north edge in world z: row 0's north side.</summary>
    public const float GridNorthZ = -4.5f;

    /// <summary>The grid's south edge in world z.</summary>
    public const float GridSouthZ = 31.5f;

    /// <summary>Half the dirt path's width: the path is x −0.6..0.6.</summary>
    public const float PathHalfWidth = 0.6f;

    /// <summary>The farmhouse door's z, where the path starts; it runs south to the grid's north edge.</summary>
    public const float DoorZ = -7f;

    // Props are drawn over the north strip, 0.5 m inside the ground's edges.
    private const float DrawMinX = GroundWestX + 0.5f;
    private const float DrawMaxX = RiverWestX - 0.5f;
    private const float DrawMinZ = GroundNorthZ + 0.5f;
    private const float DrawMaxZ = GridNorthZ;

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

    /// <summary>The 72 x 36 grid, all farm ground.</summary>
    private static readonly Rect Grid = new(GridWestX, GridEastX, GridNorthZ, GridSouthZ);

    /// <summary>The dirt path from the door to the grid's north edge.</summary>
    private static readonly Rect PathStrip = new(-PathHalfWidth, PathHalfWidth, DoorZ, GridNorthZ);

    /// <summary>The pond's water (#150: north of the grid, east of the farmhouse).</summary>
    private static readonly Circle PondWater = new(10f, -9f, 2.5f);

    private static readonly Rect[] ExclusionRects =
    [
        Grid,
        new(-1f, 1f, DoorZ, GridNorthZ), // the path corridor
        new(-4f, 4f, -12f, -6f), // the house footprint plus 1 m
        new(1.4f, 4.6f, -7.4f, -4.6f), // the shipping bin plus 1 m
        new(-11f, -7f, -11f, -7f), // the shop stall area (moved north of the grid, #150)
    ];

    private static readonly Circle[] ExclusionCircles =
    [
        new(0f, -7f, 1.25f + 0.5f), // DoorPoint, range 1.25
        new(3f, -6f, 1.7f + 0.5f), // BinPoint, range 1.7
        new(-9f, -9f, 2.2f + 0.5f), // ShopPoint, range 2.2
        new(PondWater.X, PondWater.Z, 3.8f + 0.5f), // PondPoint, range 3.8 (also covers the 2.5 m water)
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

    /// <summary>How far a grass cover patch may reach from its centre: half a metre at its largest scale.</summary>
    public const float MaxGrassCoverReach = 0.5f * MaxCoverScale * 1.4143f;

    private const float MaxCoverScale = 1.25f;

    /// <summary>The house's walls (x −3..3, z −11..−7), the bin and the shop stall: grass cover keeps off them.</summary>
    private static readonly Rect[] CoverBlockers =
    [
        new(-3f, 3f, -11f, -7f),
        new(2.4f, 3.6f, -6.4f, -5.6f),
        new(-11f, -7f, -11f, -7f),
    ];

    /// <summary>
    /// The wild grass cover over the meadow north of the grid (ART_DIRECTION §7.2): one 1 m patch per square metre of
    /// the strip, kind <c>Grass&lt;variant&gt;</c> (a <see cref="GrassTileArt"/> layout, 0 to 7), each nudged up to
    /// 0.35 m off its cell's centre, turned to any yaw and scaled 0.9 to 1.25, so no grid shows. A patch keeps its whole
    /// reach (<see cref="MaxGrassCoverReach"/>) off the grid, the path, the pond's water and the ground's edge, and its
    /// centre off the house, the bin and the shop stall. The same list every call.
    /// </summary>
    public static IReadOnlyList<MeadowProp> GrassCover()
    {
        var random = new Random(FarmSeed + 1);
        var cover = new List<MeadowProp>();
        for (var cx = (int)GroundWestX; cx <= (int)RiverWestX; cx++)
        {
            for (var cz = (int)GroundNorthZ; cz <= (int)GridNorthZ; cz++)
            {
                // Draw every value for every cell, kept or not, so one cell's rule never shifts another's patch.
                var x = cx + Between(random, -0.35f, 0.35f);
                var z = cz + Between(random, -0.35f, 0.35f);
                var yaw = Yaw(random);
                var scale = Between(random, 0.9f, MaxCoverScale);
                var variant = random.Next(GrassTileArt.Variants);
                if (!CoverAllowed(x, z)) continue;
                cover.Add(new MeadowProp($"Grass{variant}", x, z, yaw, scale));
            }
        }

        return cover;
    }

    private static bool CoverAllowed(float x, float z)
    {
        var reach = MaxGrassCoverReach;
        if (x - reach < GroundWestX || x + reach > RiverWestX || z - reach < GroundNorthZ || z + reach > GroundSouthZ) return false;
        if (Grid.DistanceTo(x, z) < reach) return false;
        if (PathStrip.DistanceTo(x, z) < reach) return false;
        if ((PondWater with { Radius = PondWater.Radius + reach }).Contains(x, z)) return false;
        return !CoverBlockers.Any(r => r.Contains(x, z));
    }

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
                    var x = Between(random, DrawMinX, DrawMaxX);
                    var z = Between(random, DrawMinZ, DrawMaxZ);
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

    /// <summary>The three stepping stones on the dirt path, kind <c>PathStone</c>.</summary>
    /// <remarks>Staggered down the path from the door (z −7) towards the grid, about 0.5 m apart.</remarks>
    public static IReadOnlyList<MeadowProp> PathStones() =>
    [
        new("PathStone", 0.1f, -6.7f, 20f, 1f),
        new("PathStone", -0.12f, -6.2f, 75f, 0.95f),
        new("PathStone", 0.08f, -5.75f, 140f, 1.05f),
    ];

    /// <summary>
    /// The farm's placement rules: outside every exclusion zone. The grid is one of them, so every prop, the rocks,
    /// stumps and saplings of the overgrown band included, stands off the farmable Tiles.
    /// </summary>
    private static bool IsAllowed(string kind, float x, float z)
    {
        if (kind == "ShadePatch")
        {
            // The whole patch, not just its centre, stays off the grid.
            return Grid.DistanceTo(x, z) >= MaxShadePatchRadius;
        }

        return !ExclusionRects.Any(r => r.Contains(x, z)) && !ExclusionCircles.Any(c => c.Contains(x, z));
    }

    /// <summary>A yaw in [0, 360): a double just under 360 can round up to 360 as a float, so that wraps to 0.</summary>
    private static float Yaw(Random random) => (float)(random.NextDouble() * 360.0) % 360f;

    private static float Between(Random random, float min, float max) =>
        min + (float)random.NextDouble() * (max - min);
}
