#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// Turns code-built <see cref="CropPart"/> tables into one merged mesh, for the art sandbox's round farmhouse, trees and
/// rocks: each part becomes a low-poly primitive (Box → <see cref="BoxMesh"/>, Sphere → <see cref="SphereMesh"/> 8 x 4,
/// Cylinder → <see cref="CylinderMesh"/>, Prism → <see cref="PrismMesh"/>) placed by the part's Euler YXZ rotation, and
/// <see cref="ArtMeshes.Merge"/> merges them with UVs on each part's palette swatch (and its night glow), to draw with
/// <see cref="ArtMaterials.Shared"/>. A cylinder <see cref="RoundDiameter"/> or more across gets
/// <see cref="RoundSides"/> sides so it reads round; smaller ones keep the game's six.
/// </summary>
public static class PartMeshes
{
    /// <summary>The sides of a cylinder <see cref="RoundDiameter"/> or more across.</summary>
    public const int RoundSides = 24;

    /// <summary>The diameter from which a cylinder is drawn round.</summary>
    public const float RoundDiameter = 1.5f;

    /// <summary>The parts merged into one mesh.</summary>
    public static ArrayMesh Merge(IEnumerable<CropPart> parts) => ArtMeshes.Merge(parts.Select(ToMeshPart));

    private static MeshPart ToMeshPart(CropPart part)
    {
        // CropPart rotations are Godot's Euler YXZ, the same as Node3D.RotationDegrees, about the part's centre.
        var euler = new Vector3(
            Mathf.DegToRad(part.PitchDegrees),
            Mathf.DegToRad(part.YawDegrees),
            Mathf.DegToRad(part.RollDegrees));
        var at = new Transform3D(Basis.FromEuler(euler, EulerOrder.Yxz), new Vector3(part.X, part.Y, part.Z));
        return new MeshPart(Primitive(part), at, part.Role, part.Glow, part.Twinkle);
    }

    private static Mesh Primitive(CropPart part) => part.Shape switch
    {
        CropPartShape.Box => new BoxMesh { Size = new Vector3(part.SizeX, part.SizeY, part.SizeZ) },
        CropPartShape.Sphere => new SphereMesh
        {
            Radius = part.SizeX / 2f,
            Height = part.SizeY,
            RadialSegments = 8,
            Rings = 4,
        },
        CropPartShape.Cylinder => new CylinderMesh
        {
            BottomRadius = part.SizeX / 2f,
            TopRadius = part.SizeZ / 2f,
            Height = part.SizeY,
            RadialSegments = Mathf.Max(part.SizeX, part.SizeZ) >= RoundDiameter ? RoundSides : 6,
            Rings = 1,
        },
        CropPartShape.Prism => new PrismMesh { Size = new Vector3(part.SizeX, part.SizeY, part.SizeZ) },
        _ => throw new System.ArgumentOutOfRangeException(nameof(part), part.Shape, "Unknown part shape."),
    };
}
