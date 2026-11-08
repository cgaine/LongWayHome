using LongWayHome.Domain.Farming;

namespace LongWayHome.Domain.Games;

/// <summary>
/// Each Area's tile grid (#154): the farm 72 x 36 (BR-T1), the forest path 14 x 40, every other Area the farm's size for
/// now. Makes grid positions on an Area's grid; <see cref="GridPosition"/>'s own constructor stays the farm's.
/// </summary>
public static class AreaGrid
{
    /// <summary>The Area's grid size.</summary>
    public static (int Width, int Height) Size(AreaId area) => throw new NotImplementedException();

    /// <summary>Whether (x, y) lies on the Area's grid.</summary>
    public static bool Contains(AreaId area, int x, int y) => throw new NotImplementedException();

    /// <summary>Creates the position when it lies on the Area's grid; returns <c>false</c> otherwise.</summary>
    public static bool TryCreate(AreaId area, int x, int y, out GridPosition position) =>
        throw new NotImplementedException();

    /// <summary>The position (x, y) on the Area's grid; throws <see cref="ArgumentOutOfRangeException"/> outside it.</summary>
    public static GridPosition Position(AreaId area, int x, int y) => throw new NotImplementedException();
}
