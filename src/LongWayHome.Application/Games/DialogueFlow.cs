namespace LongWayHome.Application.Games;

/// <summary>
/// The line-stepping of a dialogue box (#138): one speaker and their lines, advanced one at a time or skipped, raising
/// <see cref="Finished"/> exactly once per conversation.
/// </summary>
public sealed class DialogueFlow
{
    /// <summary>Raised once when a conversation closes, by advancing past its last line or by skipping.</summary>
#pragma warning disable CS0067 // Red-phase stub: raised once the flow is implemented.
    public event Action? Finished;
#pragma warning restore CS0067

    public bool IsOpen => throw new NotImplementedException();

    public string Speaker => throw new NotImplementedException();

    public string CurrentLine => throw new NotImplementedException();

    public bool IsLastLine => throw new NotImplementedException();

    /// <summary>Opens a conversation at its first line.</summary>
    public void Start(string speaker, IReadOnlyList<string> lines) => throw new NotImplementedException();

    /// <summary>Moves to the next line, or closes after the last. Ignored while not open.</summary>
    public void Advance() => throw new NotImplementedException();

    /// <summary>Closes at once, skipping the remaining lines. Ignored while not open.</summary>
    public void Skip() => throw new NotImplementedException();
}
