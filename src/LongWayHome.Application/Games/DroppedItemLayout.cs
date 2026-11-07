namespace LongWayHome.Application.Games;

/// <summary>Where each DroppedItem on one tile is drawn: a small (x, z) offset from the tile centre, so each shows.</summary>
public static class DroppedItemLayout
{
    /// <summary>(0, 0) for the first item on a tile, a distinct small offset for each of the next.</summary>
    public static (float X, float Z) Offset(int indexOnTile) => throw new NotImplementedException();
}
