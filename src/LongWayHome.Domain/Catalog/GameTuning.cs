namespace LongWayHome.Domain.Catalog;

/// <summary>The game's tuning constants, loaded from <c>tuning.json</c>.</summary>
public sealed record GameTuning
{
    public required int StartingGold { get; init; }

    public required IReadOnlyList<StartingStack> StartingSeeds { get; init; }

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
}
