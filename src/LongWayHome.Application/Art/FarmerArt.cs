namespace LongWayHome.Application.Art;

/// <summary>
/// One bone of the farmer's skeleton (#102): its rest position in model space, in metres, y 0 at the feet, +Z the
/// front. Names are Godot's <c>SkeletonProfileHumanoid</c> names; "Left" is the farmer's own left (+X).
/// </summary>
public sealed record FarmerBone(string Name, string? Parent, float X, float Y, float Z);

/// <summary>
/// One part of the farmer model (#102), with the same size and rotation conventions as <see cref="CropPart"/>
/// (positions in model space at rest, Euler YXZ about the part's centre), following exactly one <see cref="Bone"/>.
/// </summary>
public sealed record FarmerPart(
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
    string Role,
    string Bone);

/// <summary>The day-one farmer of ART_DIRECTION §6.1 (#102): his skeleton and parts. Pure C#, no Godot.</summary>
public static class FarmerArt
{
    /// <summary>The skeleton, parents before children; <c>Hips</c> is the root.</summary>
    public static IReadOnlyList<FarmerBone> Bones => throw new NotImplementedException();

    /// <summary>The day-one farmer's parts.</summary>
    public static IReadOnlyList<FarmerPart> Parts => throw new NotImplementedException();

    /// <summary>The axis-aligned extents of every part's rotated box.</summary>
    public static (float Width, float Depth, float Height) Bounds(IEnumerable<FarmerPart> parts) =>
        throw new NotImplementedException();
}
