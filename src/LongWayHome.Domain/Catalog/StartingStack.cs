namespace LongWayHome.Domain.Catalog;

/// <summary>A stack of Seeds a new Game starts with.</summary>
public sealed record StartingStack(CropTypeId CropTypeId, int Quantity);
