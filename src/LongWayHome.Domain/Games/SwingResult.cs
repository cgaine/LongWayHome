namespace LongWayHome.Domain.Games;

/// <summary>
/// What <see cref="Game.Swing"/> did: its result, whether the node was cleared, and what each yield stack gained or
/// dropped (BR-N3, BR-K3).
/// </summary>
public sealed record SwingResult(ActionResult Result, bool Cleared, IReadOnlyList<GainResult> Gained);
