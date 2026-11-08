namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>
/// One NodeRegrowth in the save (version 2, #153): <c>{ "spawnId", "clearedOn": { year, season, day } }</c>, a cleared
/// respawning spawn point waiting to grow back (BR-N4).
/// </summary>
internal sealed record NodeRegrowthDto
{
    public required string SpawnId { get; init; }

    public required GameDateDto ClearedOn { get; init; }
}
