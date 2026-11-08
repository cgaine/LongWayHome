using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Art;

/// <summary>
/// The forest path's layout (#136, ART_DIRECTION §7): a 14 x 40 m strip running north–south (x −7..7, z −20..20, north
/// is −z, towards the farm), a dirt path winding down it between Twinbough trees, and the river along its east side
/// (x <see cref="RiverWestX"/>..7). The path is at x = 0 at both ends, where the exits to the farm (north) and the
/// town (south) are. Trees stand clear of the path, the river, each other and the exits. Pure C#, no Godot; placement is
/// art only and changes no rule (the scene gives each trunk its collision).
/// </summary>
public static class ForestPathLayout
{
    /// <summary>The seed the forest path's trees use.</summary>
    public const int Seed = 1361;

    /// <summary>Half the area's width: x −7..7.</summary>
    public const float HalfWidth = 7f;

    /// <summary>Half the area's length: z −20..20.</summary>
    public const float HalfLength = 20f;

    /// <summary>The river's west bank; the river runs from here to the east edge.</summary>
    public const float RiverWestX = 4.5f;

    /// <summary>Half the dirt path's width.</summary>
    public const float PathHalfWidth = 1f;

    /// <summary>How far the path swings east and west of x = 0.</summary>
    public const float PathSwing = 1.8f;

    /// <summary>How close a tree's trunk may come to the path's edge, the river's bank or the area's edge.</summary>
    public const float TreeClearance = 1.4f;

    /// <summary>The smallest distance between two trees.</summary>
    public const float TreeSpacing = 2.2f;

    /// <summary>How far in from the north and south edges the exits keep trees away from x = 0.</summary>
    public const float ExitDepth = 4f;

    /// <summary>Half the width kept clear of trees in front of each exit.</summary>
    public const float ExitHalfWidth = 3f;

    private const int TreeCount = 34;

    /// <summary>
    /// The path's centre line at <paramref name="z"/>: west of x = 0 in the north half, east in the south half, and at
    /// x = 0 at both ends and the middle.
    /// </summary>
    public static float PathCentreX(float z) => PathSwing * MathF.Sin(z * MathF.PI / HalfLength);

    /// <summary>The path's centre line from the north edge to the south edge, every <paramref name="step"/> metres.</summary>
    public static IReadOnlyList<(float X, float Z)> PathCentre(float step)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        var points = new List<(float X, float Z)>();
        var steps = (int)MathF.Ceiling(2f * HalfLength / step);
        for (var i = 0; i <= steps; i++)
        {
            var z = MathF.Min(-HalfLength + (i * step), HalfLength);
            points.Add((PathCentreX(z), z));
        }

        return points.AsReadOnly();
    }

    /// <summary>The Twinbough trees, the same for the same seed.</summary>
    public static IReadOnlyList<AreaProp> Trees(int seed = Seed) =>
        AreaScatter.Place(
            seed,
            TreeCount,
            (-HalfWidth + TreeClearance, RiverWestX - TreeClearance, -HalfLength + 1f, HalfLength - 1f),
            TreeSpacing,
            (0.85f, 1.15f),
            IsClearForTree);

    /// <summary>The forest path Tile containing each tree's trunk, in <see cref="Trees"/> order (#154).</summary>
    public static IReadOnlyList<GridPosition> TreeTiles() => throw new NotImplementedException();

    /// <summary>Whether a tree may stand at (x, z): off the path by its clearance and out of the exits.</summary>
    public static bool IsClearForTree(float x, float z) =>
        MathF.Abs(x - PathCentreX(z)) >= PathHalfWidth + TreeClearance
        && !(MathF.Abs(z) > HalfLength - ExitDepth && MathF.Abs(x) < ExitHalfWidth);
}
