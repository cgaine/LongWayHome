using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// The canonical snake_case codes of <see cref="AreaId"/> (canonical-values.md), as the area scenes name their exits'
/// targets: <c>farm</c>, <c>forest_path</c>, <c>town</c>, <c>lakeshore</c>, <c>across_river</c>, <c>mine</c>.
/// </summary>
public static class AreaCodes
{
    private static readonly Dictionary<AreaId, string> CodeByArea = new()
    {
        [AreaId.Farm] = "farm",
        [AreaId.ForestPath] = "forest_path",
        [AreaId.Town] = "town",
        [AreaId.Lakeshore] = "lakeshore",
        [AreaId.AcrossRiver] = "across_river",
        [AreaId.Mine] = "mine",
    };

    private static readonly Dictionary<string, AreaId> AreaByCode =
        CodeByArea.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    /// <summary>The code of <paramref name="area"/>, e.g. <c>forest_path</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not an <see cref="AreaId"/> member.</exception>
    public static string Code(AreaId area) =>
        CodeByArea.TryGetValue(area, out var code)
            ? code
            : throw new ArgumentOutOfRangeException(nameof(area), area, "Not an area.");

    /// <summary>The area whose code is exactly <paramref name="code"/> (case-sensitive); false for anything else.</summary>
    public static bool TryParse(string? code, out AreaId area)
    {
        if (code is not null && AreaByCode.TryGetValue(code, out area))
        {
            return true;
        }

        area = default;
        return false;
    }
}
