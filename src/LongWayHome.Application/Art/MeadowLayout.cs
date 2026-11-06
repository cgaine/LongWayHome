namespace LongWayHome.Application.Art;

/// <summary>One placed meadow piece: its kind, ground position (metres), yaw (degrees) and uniform scale.</summary>
public readonly record struct MeadowProp(string Kind, float X, float Z, float YawDegrees, float Scale);

/// <summary>
/// The farm meadow's layout (#77): the seeded scatter of native plants, rocks, stumps, saplings and shade patches,
/// the stone edging around the plot, the path's stepping stones, and the palette role of every mesh node under
/// <c>Meadow</c>. Pure C#, no Godot.
/// </summary>
public static class MeadowLayout
{
    /// <summary>The seed the farm's meadow uses.</summary>
    public const int FarmSeed = 6512;

    /// <summary>How many positions are tried for one prop before it is skipped.</summary>
    public const int MaxAttempts = 200;

    /// <summary>Node name under <c>Meadow</c> to <see cref="ArtPalette"/> role.</summary>
    public static IReadOnlyDictionary<string, string> PartRoles => throw new NotImplementedException();

    /// <summary>The scattered props and shade patches for <paramref name="seed"/>; the same list for the same seed.</summary>
    public static IReadOnlyList<MeadowProp> Generate(int seed) => throw new NotImplementedException();

    /// <summary>The edging stones around the plot, kind <c>EdgingStone</c>, X/Z at each stone's centre.</summary>
    public static IReadOnlyList<MeadowProp> EdgingStones() => throw new NotImplementedException();

    /// <summary>The three stepping stones on the dirt path, kind <c>PathStone</c>.</summary>
    public static IReadOnlyList<MeadowProp> PathStones() => throw new NotImplementedException();
}
