using LongWayHome.Domain.Residents;

namespace LongWayHome.Application.Games;

/// <summary>What each Resident is called and says (#138).</summary>
public static class ResidentLines
{
    /// <summary>The canonical display label of <paramref name="id"/>, e.g. <c>Maggie</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="ResidentId"/> member.</exception>
    public static string DisplayName(ResidentId id) => id switch
    {
        ResidentId.Maggie => "Maggie",
        ResidentId.Clyde => "Clyde",
        ResidentId.Tom => "Tom",
        ResidentId.June => "June",
        ResidentId.Jesse => "Jesse",
        ResidentId.Mira => "Mira",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Not a resident."),
    };

    /// <summary>The Resident's greeting line. Tom, June, Jesse and Mira have none until their epics add one.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The Resident has no line yet, or the value is not a member.</exception>
    public static string Greeting(ResidentId id) => id switch
    {
        ResidentId.Maggie =>
            "Ember Bay's quieter than it used to be, but it's a good town. My store's open whenever you need it.",
        ResidentId.Clyde => "Fish bite best at first light. Mind the lake, though. It's deeper than it looks.",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "This resident has no line yet."),
    };
}
