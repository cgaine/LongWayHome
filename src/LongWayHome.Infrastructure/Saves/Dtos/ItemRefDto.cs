using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>
/// What a Slot holds: <c>{ "kind": "seed", "id": "emberroot" }</c>. The id is the CropTypeId code for <c>seed</c> and
/// <c>crop</c>, the ToolId code for <c>tool</c>.
/// </summary>
internal sealed record ItemRefDto
{
    public required ItemKind Kind { get; init; }

    public required string Id { get; init; }
}
