namespace LongWayHome.Domain.Farming;

/// <summary>
/// A position on an Area's tile grid. Y = 0 is the north row, X = 0 the west column. The public constructor and
/// <see cref="TryCreate"/> make Farm plot positions only: <c>0 ≤ X ≤ 71</c>, <c>0 ≤ Y ≤ 35</c> (BR-T1). A position on
/// another Area's grid (the forest path's 14 x 40, #154) comes from <c>AreaGrid</c>.
/// </summary>
public readonly record struct GridPosition
{
    /// <summary>A Farm plot position; outside 72 x 36 throws (BR-T1).</summary>
    public GridPosition(int X, int Y)
    {
        // BR-T1
        this.X = IsInRange(X, FarmPlot.Width)
            ? X
            : throw new ArgumentOutOfRangeException(nameof(X), X, $"X must be between 0 and {FarmPlot.Width - 1} (BR-T1).");

        // BR-T1
        this.Y = IsInRange(Y, FarmPlot.Height)
            ? Y
            : throw new ArgumentOutOfRangeException(nameof(Y), Y, $"Y must be between 0 and {FarmPlot.Height - 1} (BR-T1).");
    }

    // Unchecked: only AreaGrid uses it, after checking (x, y) against the Area's own grid.
    private GridPosition(int x, int y, bool _)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    /// <summary>Creates the position when it lies on the Farm plot; returns <c>false</c> otherwise (BR-T1).</summary>
    public static bool TryCreate(int x, int y, out GridPosition position)
    {
        // BR-T1
        if (IsInRange(x, FarmPlot.Width) && IsInRange(y, FarmPlot.Height))
        {
            position = new GridPosition(x, y);
            return true;
        }

        position = default;
        return false;
    }

    public void Deconstruct(out int X, out int Y)
    {
        X = this.X;
        Y = this.Y;
    }

    /// <summary>A position already checked against an Area's grid by <c>AreaGrid</c>.</summary>
    internal static GridPosition OnAreaGrid(int x, int y) => new(x, y, true);

    private static bool IsInRange(int value, int size) => value >= 0 && value < size;
}
