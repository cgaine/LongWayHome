namespace LongWayHome.Application.Games;

/// <summary>
/// The line-stepping of a dialogue box (#138): one speaker and their lines, advanced one at a time or skipped, raising
/// <see cref="Finished"/> exactly once per conversation.
/// </summary>
public sealed class DialogueFlow
{
    private string[] _lines = [];
    private int _index;

    /// <summary>Raised once when a conversation closes, by advancing past its last line or by skipping.</summary>
    public event Action? Finished;

    public bool IsOpen { get; private set; }

    /// <summary>The speaker of the current (or last) conversation; empty before the first.</summary>
    public string Speaker { get; private set; } = "";

    /// <summary>The line showing; empty while not open.</summary>
    public string CurrentLine => IsOpen ? _lines[_index] : "";

    public bool IsLastLine => IsOpen && _index == _lines.Length - 1;

    /// <summary>
    /// Opens a conversation at its first line. Starting while one is open replaces it without raising
    /// <see cref="Finished"/> for the replaced one.
    /// </summary>
    /// <exception cref="ArgumentException">No speaker, or no lines.</exception>
    public void Start(string speaker, IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
        {
            throw new ArgumentException("A dialogue has at least one line.", nameof(lines));
        }

        Speaker = speaker;
        _lines = [.. lines];
        _index = 0;
        IsOpen = true;
    }

    /// <summary>Moves to the next line, or closes after the last. Ignored while not open.</summary>
    public void Advance()
    {
        if (!IsOpen)
        {
            return;
        }

        if (IsLastLine)
        {
            Close();
            return;
        }

        _index++;
    }

    /// <summary>Closes at once, skipping the remaining lines. Ignored while not open.</summary>
    public void Skip()
    {
        if (IsOpen)
        {
            Close();
        }
    }

    private void Close()
    {
        IsOpen = false;
        Finished?.Invoke();
    }
}
