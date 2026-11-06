namespace LongWayHome.Application.Games;

/// <summary>
/// Snaps a world-space move to the farmer's four-way facing (issue #70, REQ-52). Directions are (X, Y) with X east and
/// Y south: north (0,-1), south (0,1), west (-1,0), east (1,0).
/// </summary>
public static class FacingDirection
{
    /// <summary>
    /// The cardinal direction nearest the move (<paramref name="x"/> east, <paramref name="y"/> = world Z, south
    /// positive). On an exact diagonal it keeps <paramref name="current"/> when that is the horizontal direction
    /// pressed, otherwise it returns the vertical one.
    /// </summary>
    public static (int X, int Y) Snap(float x, float y, (int X, int Y) current) => throw new NotImplementedException();
}
