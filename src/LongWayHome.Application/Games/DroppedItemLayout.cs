namespace LongWayHome.Application.Games;

/// <summary>Where each DroppedItem on one tile is drawn: a small (x, z) offset from the tile centre, so each shows.</summary>
public static class DroppedItemLayout
{
    /// <summary>How far from the tile centre the second and later items sit, in tiles; well inside the tile.</summary>
    private const float Spread = 0.25f;

    /// <summary>The offsets in turn: the centre, then the four corners; a sixth item starts over.</summary>
    private static readonly (float X, float Z)[] Offsets =
    [
        (0f, 0f),
        (-Spread, -Spread),
        (Spread, Spread),
        (Spread, -Spread),
        (-Spread, Spread),
    ];

    /// <summary>(0, 0) for the first item on a tile, a distinct small offset for each of the next. Negative throws.</summary>
    public static (float X, float Z) Offset(int indexOnTile)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(indexOnTile);
        return Offsets[indexOnTile % Offsets.Length];
    }
}
