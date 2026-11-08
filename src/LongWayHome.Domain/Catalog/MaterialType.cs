using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Catalog;

/// <summary>Reference data for one material (<c>materials.json</c>): its id, name and stack size (BR-K2: 99).</summary>
public sealed record MaterialType(MaterialId Id, string Name, int MaxStack);
