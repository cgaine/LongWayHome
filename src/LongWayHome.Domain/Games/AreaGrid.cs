using LongWayHome.Domain.Farming;

namespace LongWayHome.Domain.Games;

/// <summary>
/// Each Area's tile grid (#154): the farm 72 x 36 (BR-T1), the forest path 14 x 40, every other Area the farm's size for
/// now. Makes grid positions on an Area's grid; <see cref="GridPosition"/>'s own constructor stays the farm's.
/// </summary>
public static class AreaGrid
{
    /// <summary>The forest path's grid width: its 14 m strip, x −7..7.</summary>
    public const int ForestPathWidth = 14;

    /// <summary>The forest path's grid height: its 40 m strip, z −20..20.</summary>
    public const int ForestPathHeight = 40;

    /// <summary>The Area's grid size.</summary>
    public static (int Width, int Height) Size(AreaId area) => area == AreaId.ForestPath
        ? (ForestPathWidth, ForestPathHeight)
        : (FarmPlot.Width, FarmPlot.Height);

    /// <summary>Whether (x, y) lies on the Area's grid.</summary>
    public static bool Contains(AreaId area, int x, int y)
    {
        var (width, height) = Size(area);
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    /// <summary>Creates the position when it lies on the Area's grid; returns <c>false</c> otherwise.</summary>
    public static bool TryCreate(AreaId area, int x, int y, out GridPosition position)
    {
        if (Contains(area, x, y))
        {
            position = GridPosition.OnAreaGrid(x, y);
            return true;
        }

        position = default;
        return false;
    }

    /// <summary>The position (x, y) on the Area's grid; throws <see cref="ArgumentOutOfRangeException"/> outside it.</summary>
    public static GridPosition Position(AreaId area, int x, int y)
    {
        var (width, height) = Size(area);
        if (x < 0 || x >= width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"X must be between 0 and {width - 1} in {area}.");
        }

        if (y < 0 || y >= height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Y must be between 0 and {height - 1} in {area}.");
        }

        return GridPosition.OnAreaGrid(x, y);
    }
}
