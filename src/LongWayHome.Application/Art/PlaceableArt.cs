using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Art;

/// <summary>
/// The placeables set down on the farm, as pure parts like <see cref="RockArt"/>: a 1 x 1 m fence section (two
/// <c>Post*</c>, two <c>Rail*</c>, timber) and a 0.8 x 0.55 x 0.55 m chest (<c>Box*</c>, <c>Lid*</c>, a <c>Trim*</c>
/// band). The origin is at the object's centre on the ground (y 0). Nothing glows.
/// </summary>
public static class PlaceableArt
{
    private static readonly IReadOnlyList<CropPart> Fence =
    [
        Part("PostLeft", 0.1f, 1f, 0.1f, -0.42f, 0.5f, 0f, "timber"),
        Part("PostRight", 0.1f, 1f, 0.1f, 0.42f, 0.5f, 0f, "timber"),
        Part("RailLow", 0.94f, 0.08f, 0.05f, 0f, 0.38f, 0.06f, "timber-side"),
        Part("RailHigh", 0.94f, 0.08f, 0.05f, 0f, 0.76f, 0.06f, "timber-side"),
    ];

    private static readonly IReadOnlyList<CropPart> Chest =
    [
        Part("Box", 0.78f, 0.4f, 0.53f, 0f, 0.2f, 0f, "timber"),
        Part("Lid", 0.8f, 0.15f, 0.55f, 0f, 0.475f, 0f, "timber-side"),
        Part("TrimBand", 0.8f, 0.05f, 0.55f, 0f, 0.38f, 0f, "ember"),
        Part("TrimLatch", 0.08f, 0.1f, 0.02f, 0f, 0.4f, 0.28f, "timber-trim"),
    ];

    /// <summary>The parts of <paramref name="placeable"/>. The same list for the same placeable.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined placeable.</exception>
    public static IReadOnlyList<CropPart> For(PlaceableId placeable) => placeable switch
    {
        PlaceableId.Fence => Fence,
        PlaceableId.Chest => Chest,
        _ => throw new ArgumentOutOfRangeException(nameof(placeable), placeable, "Unknown placeable."),
    };

    private static CropPart Part(string name, float sizeX, float sizeY, float sizeZ, float x, float y, float z, string role) =>
        new(name, CropPartShape.Box, sizeX, sizeY, sizeZ, x, y, z, 0f, 0f, 0f, role);
}
