using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>
/// One cost line of a Rebuild sign: an item (<see cref="Material"/> set) or the gold (<see cref="Material"/> null),
/// its display name, how much has been delivered, how much the stage needs and how much the Player holds.
/// </summary>
public sealed record RebuildLine(MaterialId? Material, string Label, int Delivered, int Needed, int Held)
{
    /// <summary>Whether the line is gold rather than an item.</summary>
    public bool IsGold => Material is null;

    /// <summary>Whether <see cref="Delivered"/> covers <see cref="Needed"/>.</summary>
    public bool Complete => throw new NotImplementedException();
}

/// <summary>
/// What a Building's Rebuild sign shows (#163): the stage it builds to, the <see cref="Unlocks"/> text and one line per
/// cost item then the gold line; plus the sign's three pips (<c>RebuildSignArt.Parts(wood, stone, gold)</c>).
/// </summary>
public sealed record RebuildSignView(BuildingId Building, int Stage, string Unlocks, IReadOnlyList<RebuildLine> Lines)
{
    /// <summary>Filled when every wood, plank and sealed_plank line is complete; filled when there is none.</summary>
    public bool WoodPip => throw new NotImplementedException();

    /// <summary>Filled when every stone and ore line is complete; filled when there is none.</summary>
    public bool StonePip => throw new NotImplementedException();

    /// <summary>Filled when the gold line is complete; filled when there is none.</summary>
    public bool GoldPip => throw new NotImplementedException();

    /// <summary>Whether every line is complete.</summary>
    public bool AllComplete => throw new NotImplementedException();
}
