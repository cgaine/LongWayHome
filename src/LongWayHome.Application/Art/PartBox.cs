using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>
/// The rotated box of a code-built part (#90, #102), shared by <see cref="CropArt"/> and <see cref="FarmerArt"/>. The
/// unrotated box is SizeX x SizeY x SizeZ for a Box or Prism, SizeX x SizeY x SizeX for a Sphere and
/// max(SizeX, SizeZ) x SizeY x max(SizeX, SizeZ) for a Cylinder, rotated about its centre by Godot's Euler YXZ,
/// Ry(yaw)·Rx(pitch)·Rz(roll) (<see cref="Quaternion.CreateFromYawPitchRoll"/>).
/// </summary>
internal static class PartBox
{
    /// <summary>The eight corners of a part's unrotated box, rotated about its centre.</summary>
    public static IEnumerable<Vector3> Corners(
        CropPartShape shape,
        float sizeX,
        float sizeY,
        float sizeZ,
        Vector3 centre,
        float pitchDegrees,
        float yawDegrees,
        float rollDegrees)
    {
        var footprint = shape switch
        {
            CropPartShape.Sphere => (X: sizeX, Z: sizeX),
            CropPartShape.Cylinder => (X: MathF.Max(sizeX, sizeZ), Z: MathF.Max(sizeX, sizeZ)),
            _ => (X: sizeX, Z: sizeZ),
        };
        var half = new Vector3(footprint.X / 2f, sizeY / 2f, footprint.Z / 2f);
        var rotation = Quaternion.CreateFromYawPitchRoll(
            Radians(yawDegrees), Radians(pitchDegrees), Radians(rollDegrees));
        for (var i = 0; i < 8; i++)
        {
            var sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
            yield return centre + Vector3.Transform(half * sign, rotation);
        }
    }

    /// <summary>The axis-aligned extents of a set of corners; (0, 0, 0) to (0, 0, 0) when there are none.</summary>
    public static (Vector3 Min, Vector3 Max) Extents(IEnumerable<Vector3> corners)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var any = false;
        foreach (var corner in corners)
        {
            min = Vector3.Min(min, corner);
            max = Vector3.Max(max, corner);
            any = true;
        }

        return any ? (min, max) : (Vector3.Zero, Vector3.Zero);
    }

    private static float Radians(float degrees) => degrees * MathF.PI / 180f;
}
