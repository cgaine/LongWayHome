namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record CropsFileDto
{
    public required IReadOnlyList<CropTypeDto> CropTypes { get; init; }
}
