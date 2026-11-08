using LongWayHome.Domain.Residents;

namespace LongWayHome.Application.Games;

/// <summary>The canonical codes of <see cref="ResidentId"/> (canonical-values.md): <c>maggie</c>, <c>clyde</c>, …</summary>
public static class ResidentCodes
{
    private static readonly Dictionary<ResidentId, string> CodeById = new()
    {
        [ResidentId.Maggie] = "maggie",
        [ResidentId.Clyde] = "clyde",
        [ResidentId.Tom] = "tom",
        [ResidentId.June] = "june",
        [ResidentId.Jesse] = "jesse",
        [ResidentId.Mira] = "mira",
    };

    private static readonly Dictionary<string, ResidentId> IdByCode =
        CodeById.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    /// <summary>The code of <paramref name="id"/>, e.g. <c>maggie</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a <see cref="ResidentId"/> member.</exception>
    public static string Code(ResidentId id) =>
        CodeById.TryGetValue(id, out var code)
            ? code
            : throw new ArgumentOutOfRangeException(nameof(id), id, "Not a resident.");

    /// <summary>The Resident whose code is exactly <paramref name="code"/> (case-sensitive); false for anything else.</summary>
    public static bool TryParse(string? code, out ResidentId id)
    {
        if (code is not null && IdByCode.TryGetValue(code, out id))
        {
            return true;
        }

        id = default;
        return false;
    }
}
