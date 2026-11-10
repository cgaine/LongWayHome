using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>The display names of the town's Buildings, and the wake-up line for a rebuilt one (#165).</summary>
public static class BuildingLabels
{
    /// <summary>"general store", "carpenter", "Mira's house" or "blacksmith".</summary>
    public static string Label(BuildingId id) => throw new NotImplementedException();

    /// <summary>"The general store has been rebuilt.", or "Mira's house has been rebuilt." (no "The" before a possessive).</summary>
    public static string RebuiltLine(BuildingId id) => throw new NotImplementedException();

    /// <summary>One <see cref="RebuiltLine"/> per id, joined with "\n"; "" for none.</summary>
    public static string RebuiltText(IReadOnlyList<BuildingId> ids) => throw new NotImplementedException();
}
