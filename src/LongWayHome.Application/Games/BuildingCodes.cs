using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Games;

/// <summary>
/// The canonical codes of <see cref="BuildingId"/> (canonical-values.md): <c>general_store</c>, <c>carpenter</c>,
/// <c>miras_house</c>, <c>blacksmith</c>.
/// </summary>
public static class BuildingCodes
{
    private static readonly Dictionary<BuildingId, string> CodeById = new()
    {
        [BuildingId.GeneralStore] = "general_store",
        [BuildingId.Carpenter] = "carpenter",
        [BuildingId.MirasHouse] = "miras_house",
        [BuildingId.Blacksmith] = "blacksmith",
    };

    private static readonly Dictionary<string, BuildingId> IdByCode =
        CodeById.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    /// <summary>The code of <paramref name="id"/>, e.g. <c>general_store</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="BuildingId"/> member.</exception>
    public static string Code(BuildingId id) =>
        CodeById.TryGetValue(id, out var code)
            ? code
            : throw new ArgumentOutOfRangeException(nameof(id), id, "Not a Building.");

    /// <summary>The Building whose code is exactly <paramref name="code"/> (case-sensitive); false for anything else.</summary>
    public static bool TryParse(string? code, out BuildingId id)
    {
        if (code is not null && IdByCode.TryGetValue(code, out id))
        {
            return true;
        }

        id = default;
        return false;
    }
}
