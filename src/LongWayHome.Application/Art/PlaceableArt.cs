using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Art;

/// <summary>
/// The placeables set down on the farm, as pure parts like <see cref="RockArt"/>: a 1 x 1 m fence section (two
/// <c>Post*</c>, two <c>Rail*</c>, timber) and a 0.8 x 0.55 x 0.55 m chest (<c>Box*</c>, <c>Lid*</c>, a <c>Trim*</c>
/// band). The origin is at the object's centre on the ground (y 0).
/// </summary>
public static class PlaceableArt
{
    /// <summary>The parts of <paramref name="placeable"/>.</summary>
    public static IReadOnlyList<CropPart> For(PlaceableId placeable) => throw new NotImplementedException();
}
