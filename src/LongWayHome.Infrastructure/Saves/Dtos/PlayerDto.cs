using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record PlayerDto
{
    public required int Gold { get; init; }

    public required int Energy { get; init; }

    public required IReadOnlyList<ToolId> Tools { get; init; }

    public required WateringCanDto WateringCan { get; init; }

    public required IReadOnlyList<InventoryStackDto> Inventory { get; init; }
}
