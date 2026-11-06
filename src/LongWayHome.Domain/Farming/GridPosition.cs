namespace LongWayHome.Domain.Farming;

/// <summary>
/// A position on the Farm plot: <c>0 ≤ X ≤ 11</c>, <c>0 ≤ Y ≤ 8</c> (BR-T1). Y = 0 is the north row, X = 0 the west column.
/// </summary>
public readonly record struct GridPosition(int X, int Y)
{
    // BR-T1
    public int X { get; } = IsInRange(X, FarmPlot.Width)
        ? X
        : throw new ArgumentOutOfRangeException(nameof(X), X, $"X must be between 0 and {FarmPlot.Width - 1} (BR-T1).");

    // BR-T1
    public int Y { get; } = IsInRange(Y, FarmPlot.Height)
        ? Y
        : throw new ArgumentOutOfRangeException(nameof(Y), Y, $"Y must be between 0 and {FarmPlot.Height - 1} (BR-T1).");

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

    private static bool IsInRange(int value, int size) => value >= 0 && value < size;
}
