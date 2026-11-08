namespace LongWayHome.Application.Art;

/// <summary>Which town building stands on a <see cref="TownBuilding"/> spot.</summary>
public enum TownBuildingKind
{
    /// <summary>The general store (<see cref="GeneralStoreArt"/>).</summary>
    Store,

    /// <summary>The carpenter (<see cref="CarpenterArt"/>).</summary>
    Carpenter,

    /// <summary>The blacksmith (<see cref="BlacksmithArt"/>).</summary>
    Blacksmith,
}

/// <summary>A town building: what it is, the centre of its main drum (metres) and that drum's wall radius.</summary>
public readonly record struct TownBuilding(TownBuildingKind Kind, float X, float Z, float WallRadius);

/// <summary>
/// The town's layout (#136, ART_DIRECTION §7.3): a 40 x 28 m area (x −20..20, z −14..14, north is −z, towards the
/// forest path) with a road from the north exit to the south exit along x = 0, a round square at its middle, the
/// general store, the carpenter and the blacksmith around the square, the river along the east side (x
/// <see cref="RiverWestX"/>..20) with the broken bridge stub on it, and a few Twinbough trees in the free ground.
/// Buildings and trees stand clear of the road, the square, the river and each other. Pure C#, no Godot; the scene
/// gives each building a box collision around its drum. Placement is art only and changes no rule.
/// </summary>
public static class TownLayout
{
    /// <summary>The seed the town's trees use.</summary>
    public const int Seed = 1362;

    /// <summary>Half the area's width: x −20..20.</summary>
    public const float HalfWidth = 20f;

    /// <summary>Half the area's depth: z −14..14.</summary>
    public const float HalfDepth = 14f;

    /// <summary>The river's west bank; the river runs from here to the east edge.</summary>
    public const float RiverWestX = 16f;

    /// <summary>Half the road's width: x −1.5..1.5, as wide as the exits.</summary>
    public const float RoadHalfWidth = 1.5f;

    /// <summary>The square's radius around (0, 0).</summary>
    public const float SquareRadius = 5f;

    /// <summary>The z of the broken bridge stub's middle, on the river.</summary>
    public const float BridgeZ = 0f;

    /// <summary>How close a tree's trunk may come to a building's eaves, the road, the square or the river.</summary>
    public const float TreeClearance = 2f;

    private const int TreeCount = 10;

    /// <summary>
    /// The three buildings at their starting look: the store (Maggie's corner) north-west of the square, the
    /// carpenter north-east and the blacksmith south-west.
    /// </summary>
    public static IReadOnlyList<TownBuilding> Buildings { get; } =
    [
        new(TownBuildingKind.Store, -10f, -6f, GeneralStoreArt.WallRadius),
        new(TownBuildingKind.Carpenter, 9.5f, -6f, CarpenterArt.WallRadius),
        new(TownBuildingKind.Blacksmith, -10f, 7f, BlacksmithArt.WallRadius),
    ];

    /// <summary>How far a building's roof reaches past its wall, so nothing stands under its eaves.</summary>
    public const float EaveMargin = 1.2f;

    /// <summary>The Twinbough trees, the same for the same seed.</summary>
    public static IReadOnlyList<AreaProp> Trees(int seed = Seed) =>
        AreaScatter.Place(
            seed,
            TreeCount,
            (-HalfWidth + TreeClearance, RiverWestX - TreeClearance, -HalfDepth + TreeClearance, HalfDepth - TreeClearance),
            3f,
            (0.9f, 1.1f),
            IsClearForTree);

    /// <summary>Whether a tree may stand at (x, z): off the road, the square and every building's eaves by its clearance.</summary>
    public static bool IsClearForTree(float x, float z) =>
        MathF.Abs(x) >= RoadHalfWidth + TreeClearance
        && AreaScatter.Distance(x, z, 0f, 0f) >= SquareRadius + TreeClearance
        && Buildings.All(b => AreaScatter.Distance(x, z, b.X, b.Z) >= b.WallRadius + EaveMargin + TreeClearance);
}
