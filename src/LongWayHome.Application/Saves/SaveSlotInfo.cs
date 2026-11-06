using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Saves;

/// <summary>A save slot as listed by a save store. Date, Gold and SavedAt are set only when State is Saved.</summary>
public sealed record SaveSlotInfo(int Slot, SaveSlotState State, GameDate? Date, int? Gold, DateTimeOffset? SavedAt);
