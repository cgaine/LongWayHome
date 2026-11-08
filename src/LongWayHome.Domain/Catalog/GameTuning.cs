using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Catalog;

/// <summary>The game's tuning constants, loaded from <c>tuning.json</c>.</summary>
public sealed record GameTuning
{
    /// <summary>The shipped Night wait before a cleared node grows back (BR-N4), used when the tuning doesn't set one.</summary>
    public const int DefaultNodeRegrowthNights = 2;

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

    /// <summary>The swings that clear a node, per tool tier (BR-N2): basic 4, rosk 3, groth 2.</summary>
    public IReadOnlyDictionary<ToolTier, int> SwingsToClear { get; init; } = new Dictionary<ToolTier, int>();

    /// <summary>What clearing a node gives, per node kind (BR-N3).</summary>
    public IReadOnlyDictionary<NodeKind, IReadOnlyList<MaterialStack>> NodeYields { get; init; } =
        new Dictionary<NodeKind, IReadOnlyList<MaterialStack>>();

    /// <summary>The Nights a cleared respawning node waits before it grows back (BR-N4), at least 1.</summary>
    public int NodeRegrowthNights { get; init; } = DefaultNodeRegrowthNights;
}
