namespace LongWayHome.Domain.Farming;

/// <summary>A position on the Farm plot: <c>0 ≤ X ≤ 11</c>, <c>0 ≤ Y ≤ 8</c> (BR-T1). Y = 0 is the north row.</summary>
public readonly record struct GridPosition(int X, int Y)
{
    public int X { get; } = Validate(X);

    public int Y { get; } = Validate(Y);

    public static bool TryCreate(int x, int y, out GridPosition position) => throw new NotImplementedException();

    private static int Validate(int value) => throw new NotImplementedException();
}
