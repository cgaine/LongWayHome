using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// The canonical codes of <see cref="WaterSource"/> (canonical-values.md), as the scenes' <c>WaterRefill</c> nodes name
/// their source: <c>pond</c>, <c>river</c>, <c>lake</c>.
/// </summary>
public static class WaterSourceCodes
{
    /// <summary>The code of <paramref name="source"/>, e.g. <c>river</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="WaterSource"/> member.</exception>
    public static string Code(WaterSource source) => throw new NotImplementedException();

    /// <summary>The source whose code is exactly <paramref name="code"/> (case-sensitive); false for anything else.</summary>
    public static bool TryParse(string? code, out WaterSource source) => throw new NotImplementedException();
}
