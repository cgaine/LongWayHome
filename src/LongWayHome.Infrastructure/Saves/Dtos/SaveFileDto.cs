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
}
