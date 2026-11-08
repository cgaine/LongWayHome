using LongWayHome.Domain.Games;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>A whole save file: the Game, following the domain model's example payload, plus its save version.</summary>
internal sealed record SaveFileDto
{
    public required int SaveVersion { get; init; }

    public required Guid Id { get; init; }

    public required GameDateDto Date { get; init; }

    public required int Clock { get; init; }

    public required Weather Weather { get; init; }

    /// <summary>Null on a Game whose first Day has not ended yet; written, but may be null.</summary>
    public required DayEndReason? LastDayEnd { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset SavedAt { get; init; }

    public required PlayerDto Player { get; init; }

    public required FarmPlotDto FarmPlot { get; init; }

    public required ShippingBinDto ShippingBin { get; init; }

    /// <summary>
    /// One entry per Resident in enum order (#138). Optional: version 2 files written before #138 lack it, and load
    /// with the new-Game default (BR-R1).
    /// </summary>
    public IReadOnlyList<ResidentDto?>? Residents { get; init; }

    /// <summary>
    /// Whether the Opening is done (BR-I1, #139). Always written; optional on load: a version 2 file written before
    /// #139 lacks it and loads as true, since that Game is already underway.
    /// </summary>
    public bool? OpeningDone { get; init; }

    /// <summary>
    /// The ids of the once-per-game Shop listings bought (BR-L5, #145), sorted ordinal. Always written; optional on
    /// load: a version 2 file written before #145 lacks it and loads with none.
    /// </summary>
    public IReadOnlyList<string?>? PurchasedListings { get; init; }

    /// <summary>
    /// The ResourceNodes standing in the world (#151). Always written; optional on load: a version 1 file migrated
    /// by SaveV1ToV2, or a version 2 file written before #151, lacks it and loads with a node at every atStart spawn
    /// point outside the old plot's block (<c>FarmPlot.Phase1Block</c>).
    /// </summary>
    public IReadOnlyList<ResourceNodeDto?>? ResourceNodes { get; init; }

    /// <summary>
    /// The cleared respawning spawn points waiting to grow back (BR-N4, #153). Always written; optional on load: a
    /// version 2 file written before #153 lacks it and loads with none.
    /// </summary>
    public IReadOnlyList<NodeRegrowthDto?>? NodeRegrowths { get; init; }

    /// <summary>
    /// The AreaId codes of the Areas whose starting (atStart) nodes this Game has placed (#154), sorted ordinal.
    /// Always written; optional on load: a version 2 file written before #154 lacks it and counts as <c>["farm"]</c>.
    /// </summary>
    public IReadOnlyList<string?>? NodeAreasPlaced { get; init; }
}
