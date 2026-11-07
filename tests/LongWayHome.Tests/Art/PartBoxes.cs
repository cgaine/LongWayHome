using System.Numerics;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The rotated box of a part, worked out independently of the production code with the <see cref="CropArt"/>
/// conventions: the unrotated box is SizeX x SizeY x SizeZ for a Box or Prism, SizeX x SizeY x SizeX for a Sphere and
/// max(SizeX, SizeZ) x SizeY x max(SizeX, SizeZ) for a Cylinder, rotated about its centre by Godot's Euler YXZ
/// (<see cref="Quaternion.CreateFromYawPitchRoll"/>).
/// </summary>
internal static class PartBoxes
{
    public static IReadOnlyList<Vector3> Corners(FarmerPart part) => Corners(
        part.Shape, part.SizeX, part.SizeY, part.SizeZ, new Vector3(part.X, part.Y, part.Z),
        part.PitchDegrees, part.YawDegrees, part.RollDegrees);

    public static IReadOnlyList<Vector3> Corners(CropPart part) => Corners(
        part.Shape, part.SizeX, part.SizeY, part.SizeZ, new Vector3(part.X, part.Y, part.Z),
        part.PitchDegrees, part.YawDegrees, part.RollDegrees);

    public static (Vector3 Min, Vector3 Max) Extents(IEnumerable<IReadOnlyList<Vector3>> boxes)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var corner in boxes.SelectMany(box => box))
        {
            min = Vector3.Min(min, corner);
            max = Vector3.Max(max, corner);
        }

        return (min, max);
    }

    private static IReadOnlyList<Vector3> Corners(
        CropPartShape shape, float sizeX, float sizeY, float sizeZ, Vector3 centre, float pitch, float yaw, float roll)
    {
        var footprint = shape switch
        {
            CropPartShape.Sphere => (X: sizeX, Z: sizeX),
            CropPartShape.Cylinder => (X: MathF.Max(sizeX, sizeZ), Z: MathF.Max(sizeX, sizeZ)),
            _ => (X: sizeX, Z: sizeZ),
        };
        var half = new Vector3(footprint.X / 2f, sizeY / 2f, footprint.Z / 2f);
        var rotation = Quaternion.CreateFromYawPitchRoll(Radians(yaw), Radians(pitch), Radians(roll));
        var corners = new List<Vector3>(8);
        for (var i = 0; i < 8; i++)
        {
            var sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
            corners.Add(centre + Vector3.Transform(half * sign, rotation));
        }

        return corners;
    }

    private static float Radians(float degrees) => degrees * MathF.PI / 180f;
}
