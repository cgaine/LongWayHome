using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

/// <summary>The primitive a <see cref="CropPart"/> is drawn as (#90).</summary>
public enum CropPartShape
{
    Box,
    Sphere,
    Cylinder,
    Prism,
}

/// <summary>
/// One part of a crop model (#90). Sizes are full extents in metres (for a Sphere, <c>SizeX</c> is the diameter and
/// <c>SizeY</c> the height; for a Cylinder, <c>SizeX</c> is the bottom diameter, <c>SizeZ</c> the top diameter and
/// <c>SizeY</c> the height). The position is the part's centre with y 0 at the slab top. Rotations follow Godot's
/// <c>Node3D.RotationDegrees</c> (Euler YXZ), applied about the part's centre.
/// </summary>
public sealed record CropPart(
    string Name,
    CropPartShape Shape,
    float SizeX,
    float SizeY,
    float SizeZ,
    float X,
    float Y,
    float Z,
    float PitchDegrees,
    float YawDegrees,
    float RollDegrees,
    string Role);

/// <summary>
/// The parts of each (crop, stage) model drawn to ART_DIRECTION §4 (#90). Pure C#, no Godot: the Godot builder
/// <c>CropModels</c> only turns these parts into one merged mesh.
/// </summary>
public static class CropArt
{
    /// <summary>The parts of a crop's model at a growth stage, or <c>null</c> for a crop not drawn to the art yet.</summary>
    public static IReadOnlyList<CropPart>? Parts(CropTypeId crop, GrowthStage stage) =>
        throw new NotImplementedException();

    /// <summary>
    /// Width is the larger of the X and Z extents of the parts' axis-aligned bounds (each part's rotated box); Height
    /// is the top of the highest part above y 0.
    /// </summary>
    public static (float Width, float Height) Bounds(IReadOnlyList<CropPart> parts) =>
        throw new NotImplementedException();
}
