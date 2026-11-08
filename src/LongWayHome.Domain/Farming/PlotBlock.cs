namespace LongWayHome.Domain.Farming;

/// <summary>A rectangular block of the Farm plot's grid: its north-west origin Tile and its size in Tiles.</summary>
public readonly record struct PlotBlock(int OriginX, int OriginY, int Width, int Height);
