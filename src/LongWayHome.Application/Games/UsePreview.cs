using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>What Use would do (<see cref="Action"/>) or why it can't (<see cref="Rejection"/>); exactly one is set.</summary>
public readonly record struct UsePreview(UseActionKind? Action, Rejection? Rejection);
