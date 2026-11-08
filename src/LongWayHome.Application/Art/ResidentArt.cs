using LongWayHome.Domain.Residents;

namespace LongWayHome.Application.Art;

/// <summary>
/// The townspeople of ART_DIRECTION §6.2 (#138): a static part table per Resident, in the farmer's chunky proportions,
/// with the <see cref="CropPart"/> conventions (model space in metres, y 0 at the soles, facing +Z). Pure C#, no Godot.
/// </summary>
public static class ResidentArt
{
    /// <summary>The parts of <paramref name="id"/>'s model. Only Maggie and Clyde have one so far.</summary>
    public static IReadOnlyList<CropPart> For(ResidentId id) => throw new NotImplementedException();
}
