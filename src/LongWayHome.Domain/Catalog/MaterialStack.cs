using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Catalog;

/// <summary>A quantity of one material, as a node's yield lists it (<c>tuning.json</c> <c>nodeYields</c>).</summary>
public sealed record MaterialStack(MaterialId Material, int Quantity);
