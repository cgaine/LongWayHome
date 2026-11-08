namespace LongWayHome.Application.Games;

/// <summary>
/// The Opening's step order over a <see cref="GameSession"/> (BR-I1). Stub for #139 (red): every member throws.
/// </summary>
public sealed class OpeningFlow : IDisposable
{
    public OpeningFlow(GameSession session) => throw new NotImplementedException();

    public event Action? StepChanged;

    public OpeningStep Current => throw new NotImplementedException();

    public bool IsActive => throw new NotImplementedException();

    public string Speaker => throw new NotImplementedException();

    public IReadOnlyList<string> Lines => throw new NotImplementedException();

    public bool ClockRuns => throw new NotImplementedException();

    public string? Hint(InputDeviceKind device) => throw new NotImplementedException();

    public void CrashFinished() => throw new NotImplementedException();

    public void LinesFinished() => throw new NotImplementedException();

    public void WalkArrived() => throw new NotImplementedException();

    public void Dispose() => throw new NotImplementedException();

    private void Raise() => StepChanged?.Invoke();
}
