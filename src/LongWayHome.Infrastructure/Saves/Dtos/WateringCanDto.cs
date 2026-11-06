namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record WateringCanDto
{
    public required int Capacity { get; init; }

    public required int Charges { get; init; }

    public required bool Upgraded { get; init; }
}
