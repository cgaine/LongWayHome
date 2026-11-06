namespace LongWayHome.Infrastructure.Data;

/// <summary>
/// The game's reference data failed to parse or validate. <see cref="Errors"/> lists every problem found
/// across all three files; <see cref="Exception.Message"/> is those errors joined with newlines.
/// </summary>
public sealed class GameDataException : Exception
{
    public GameDataException(IReadOnlyList<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
