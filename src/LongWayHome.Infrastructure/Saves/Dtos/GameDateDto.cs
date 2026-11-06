using LongWayHome.Domain.Catalog;

namespace LongWayHome.Infrastructure.Saves.Dtos;

internal sealed record GameDateDto
{
    public required int Year { get; init; }

    public required Season Season { get; init; }

    public required int Day { get; init; }
}
