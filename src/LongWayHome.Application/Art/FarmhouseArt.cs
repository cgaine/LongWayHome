namespace LongWayHome.Application.Art;

/// <summary>
/// The farmhouse's parts (#104), in house-local metres: origin at the centre of the footprint on the ground, the door
/// side +Z. Pure C#, no Godot. Not built yet (red).
/// </summary>
public static class FarmhouseArt
{
    /// <summary>The run-down farmhouse of ART_DIRECTION §7.1.</summary>
    public static IReadOnlyList<CropPart> RunDown => throw new NotImplementedException();

    /// <summary>The axis-aligned bounds of the parts' rotated boxes.</summary>
    public static (float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ) Bounds(IEnumerable<CropPart> parts) =>
        throw new NotImplementedException();
}
