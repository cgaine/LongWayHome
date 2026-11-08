namespace LongWayHome.Application.Art;

/// <summary>One plank of the lakeshore's dock: its centre (metres) on the deck, running east–west.</summary>
public readonly record struct DockPlank(float X, float Z);

/// <summary>
/// The lakeshore's layout (#136, ART_DIRECTION §7): a 40 x 16 m area (x −20..20, z −8..8, north is −z, towards the
/// town) with a sandy shore down to the lake's <see cref="WaterlineZ"/>, the lake south of it, a plank dock running
/// out over the water at x <see cref="DockWestX"/>..<see cref="DockEastX"/>, and the river coming down the east side
/// (x <see cref="RiverWestX"/>..20) into the lake. A few rocks lie on the shore, clear of the exit, the dock and each
/// other. Pure C#, no Godot; the scene puts the collision at the waterline, the bank and the dock's sides. Placement is
/// art only and changes no rule.
/// </summary>
public static class LakeshoreLayout
{
    /// <summary>The seed the shore's rocks use.</summary>
    public const int Seed = 1363;

    /// <summary>Half the area's width: x −20..20.</summary>
    public const float HalfWidth = 20f;

    /// <summary>Half the area's depth: z −8..8.</summary>
    public const float HalfDepth = 8f;

    /// <summary>Where the shore meets the lake; the lake runs south from here.</summary>
    public const float WaterlineZ = 2f;

    /// <summary>Where the grass gives way to the sandy shore.</summary>
    public const float ShoreNorthZ = -1f;

    /// <summary>The river's west bank, north of the lake.</summary>
    public const float RiverWestX = 16f;

    /// <summary>The dock's west side.</summary>
    public const float DockWestX = -7f;

    /// <summary>The dock's east side.</summary>
    public const float DockEastX = -5f;

    /// <summary>Where the dock's deck starts, on the shore just north of the waterline.</summary>
    public const float DockNorthZ = 1f;

    /// <summary>Where the dock ends, out over the water.</summary>
    public const float DockSouthZ = 7f;

    /// <summary>The distance between plank centres along the dock.</summary>
    public const float PlankPitch = 0.4f;

    /// <summary>How close a rock may come to the dock or the exit's road.</summary>
    public const float RockClearance = 1.5f;

    private const int RockCount = 8;

    /// <summary>The dock's planks from the shore end to the far end, centred on the dock.</summary>
    public static IReadOnlyList<DockPlank> DockPlanks()
    {
        var planks = new List<DockPlank>();
        var x = (DockWestX + DockEastX) / 2f;
        for (var z = DockNorthZ + (PlankPitch / 2f); z < DockSouthZ; z += PlankPitch)
        {
            planks.Add(new DockPlank(x, z));
        }

        return planks.AsReadOnly();
    }

    /// <summary>The shore's rocks, the same for the same seed.</summary>
    public static IReadOnlyList<AreaProp> Rocks(int seed = Seed) =>
        AreaScatter.Place(
            seed,
            RockCount,
            (-HalfWidth + 1f, RiverWestX - 1f, ShoreNorthZ, WaterlineZ - 0.5f),
            2.5f,
            (0.5f, 0.9f),
            IsClearForRock);

    /// <summary>Whether a rock may lie at (x, z): clear of the dock and of the road down from the north exit.</summary>
    public static bool IsClearForRock(float x, float z) =>
        (x <= DockWestX - RockClearance || x >= DockEastX + RockClearance)
        && MathF.Abs(x) >= 1.5f + RockClearance;
}
