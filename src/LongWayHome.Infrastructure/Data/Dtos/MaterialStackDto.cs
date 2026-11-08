using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record MaterialStackDto
{
    public required MaterialId Material { get; init; }

    public required int Quantity { get; init; }
}
