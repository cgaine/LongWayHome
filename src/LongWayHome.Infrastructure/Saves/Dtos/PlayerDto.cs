namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record PlayerDto
{
    public required int Gold { get; init; }

    public required int Energy { get; init; }

    public required WateringCanDto WateringCan { get; init; }

    public required BackpackDto Backpack { get; init; }
}
