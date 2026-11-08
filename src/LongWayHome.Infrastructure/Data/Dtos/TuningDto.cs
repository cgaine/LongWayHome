namespace LongWayHome.Infrastructure.Data.Dtos;

internal sealed record TuningDto
{
    public required int StartingGold { get; init; }

    public required IReadOnlyList<StartingStackDto> StartingSeeds { get; init; }

    public required int MaxEnergy { get; init; }

    public required int PassOutEnergy { get; init; }

    public required int ToolEnergyCost { get; init; }

    public required int CanCapacity { get; init; }

    public required int CanUpgradedCapacity { get; init; }

    public required int DayStartMinutes { get; init; }

    public required int DayEndMinutes { get; init; }

    public required int DayLengthRealSeconds { get; init; }

    public required int RainChancePercent { get; init; }

    public required int SeasonLengthDays { get; init; }

    public required double WalkSpeedTilesPerSecond { get; init; }

    public required double RunSpeedTilesPerSecond { get; init; }

    public IReadOnlyDictionary<string, int>? SwingsToClear { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<MaterialStackDto>>? NodeYields { get; init; }
}
