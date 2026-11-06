namespace LongWayHome.Application.Games;

/// <summary>
/// Snaps a world-space move to the farmer's four-way facing (issue #70, REQ-52). Directions are (X, Y) with X east and
/// Y south: north (0,-1), south (0,1), west (-1,0), east (1,0).
/// </summary>
public static class FacingDirection
{
    /// <summary>How close |x| and |y| must be for a move to count as an exact diagonal.</summary>
    private const float DiagonalTolerance = 1e-5f;

    /// <summary>
    /// The cardinal direction nearest the move (<paramref name="x"/> east, <paramref name="y"/> = world Z, south
    /// positive). On an exact diagonal it keeps <paramref name="current"/> when that is the horizontal direction
    /// pressed, otherwise it returns the vertical one.
    /// </summary>
    public static (int X, int Y) Snap(float x, float y, (int X, int Y) current)
    {
        var horizontal = (Math.Sign(x), 0);
        var vertical = (0, Math.Sign(y));
        var absX = MathF.Abs(x);
        var absY = MathF.Abs(y);

        // An exact diagonal keeps the current facing when it is one of the two directions pressed, so walking
        // diagonally doesn't flip it. A diagonal needs both axes: a tiny move along one axis alone is within the
        // tolerance of zero, but snapping it as a diagonal could give the zero direction.
        if (absX > 0f && absY > 0f && MathF.Abs(absX - absY) <= DiagonalTolerance)
        {
            return current == horizontal ? horizontal : vertical;
        }

        return absX > absY ? horizontal : vertical;
    }

    /// <summary>
    /// The model's Y rotation, in radians, for a cardinal facing (<paramref name="x"/> east, <paramref name="y"/>
    /// south): <c>Atan2(x, y)</c>, so south is 0, east π/2, north π and west −π/2 (issue #71).
    /// </summary>
    public static float Yaw(int x, int y) => throw new NotImplementedException();
}
