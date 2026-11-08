namespace LongWayHome.Domain.Farming;

/// <summary>A rectangular block of the Farm plot's grid: its north-west origin Tile and its size in Tiles.</summary>
public readonly record struct PlotBlock(int OriginX, int OriginY, int Width, int Height)
{
    /// <summary>Whether <paramref name="position"/> lies inside the block.</summary>
    public bool Contains(GridPosition position) =>
        position.X >= OriginX && position.X < OriginX + Width &&
        position.Y >= OriginY && position.Y < OriginY + Height;
}
