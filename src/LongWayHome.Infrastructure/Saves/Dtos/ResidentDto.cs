namespace LongWayHome.Infrastructure.Saves.Dtos;

/// <summary>One Resident in the save (version 2, #138): <c>{ "id": "&lt;ResidentId code&gt;", "present": &lt;bool&gt; }</c>.</summary>
internal sealed record ResidentDto
{
    public required string Id { get; init; }

    public required bool Present { get; init; }
}
