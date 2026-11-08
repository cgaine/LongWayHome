using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// The canonical codes of <see cref="WaterSource"/> (canonical-values.md), as the scenes' <c>WaterRefill</c> nodes name
/// their source: <c>pond</c>, <c>river</c>, <c>lake</c>.
/// </summary>
public static class WaterSourceCodes
{
    private static readonly Dictionary<WaterSource, string> CodeBySource = new()
    {
        [WaterSource.Pond] = "pond",
        [WaterSource.River] = "river",
        [WaterSource.Lake] = "lake",
    };

    private static readonly Dictionary<string, WaterSource> SourceByCode =
        CodeBySource.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    /// <summary>The code of <paramref name="source"/>, e.g. <c>river</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="WaterSource"/> member.</exception>
    public static string Code(WaterSource source) =>
        CodeBySource.TryGetValue(source, out var code)
            ? code
            : throw new ArgumentOutOfRangeException(nameof(source), source, "Not a water source.");

    /// <summary>The source whose code is exactly <paramref name="code"/> (case-sensitive); false for anything else.</summary>
    public static bool TryParse(string? code, out WaterSource source)
    {
        if (code is not null && SourceByCode.TryGetValue(code, out source))
        {
            return true;
        }

        source = default;
        return false;
    }
}
