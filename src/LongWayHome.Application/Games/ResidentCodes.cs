using LongWayHome.Domain.Residents;

namespace LongWayHome.Application.Games;

/// <summary>The canonical codes of <see cref="ResidentId"/> (canonical-values.md): <c>maggie</c>, <c>clyde</c>, …</summary>
public static class ResidentCodes
{
    /// <summary>The code of <paramref name="id"/>, e.g. <c>maggie</c>.</summary>
    public static string Code(ResidentId id) => throw new NotImplementedException();

    /// <summary>The Resident whose code is exactly <paramref name="code"/> (case-sensitive); false for anything else.</summary>
    public static bool TryParse(string? code, out ResidentId id) => throw new NotImplementedException();
}
