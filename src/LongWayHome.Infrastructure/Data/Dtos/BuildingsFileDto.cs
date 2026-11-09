namespace LongWayHome.Infrastructure.Data.Dtos;

/// <summary><c>buildings.json</c>: <c>{ "stages": [ ... ] }</c> (#163).</summary>
internal sealed record BuildingsFileDto
{
    public required IReadOnlyList<BuildingStageDto?> Stages { get; init; }
}
