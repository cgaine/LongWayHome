using System.Text.Json.Serialization;
using LongWayHome.Domain.Players;

namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>
/// What a Slot holds: <c>{ "kind": "seed", "id": "emberroot" }</c>. The id is the CropTypeId code for <c>seed</c> and
/// <c>crop</c>, the ToolId code for <c>tool</c>. A material is <c>{ "kind": "material", "materialId": "wood" }</c>
/// (#152), with no <c>id</c>; older saves never hold one, so the save version is unchanged.
/// </summary>
internal sealed record ItemRefDto
{
    public required ItemKind Kind { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MaterialId { get; init; }
}
