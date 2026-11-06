using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>What the Player holds: a tool or a seed type.</summary>
public sealed record HeldItem(ToolId? Tool, CropTypeId? Seed)
{
    public static HeldItem Of(ToolId tool) => new(tool, null);

    public static HeldItem SeedOf(CropTypeId seed) => new(null, seed);
}
