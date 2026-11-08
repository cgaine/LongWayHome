namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record MaterialsFileDto
{
    public required IReadOnlyList<MaterialTypeDto> MaterialTypes { get; init; }
}
