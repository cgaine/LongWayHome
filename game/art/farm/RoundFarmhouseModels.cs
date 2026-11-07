#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The round farmhouse's meshes (ART_DIRECTION §7.1, the concept art's round farmhouse), built from
/// <see cref="RoundFarmhouseArt"/> as <see cref="FarmhouseView"/> builds the rectangular house: each part becomes a
/// low-poly primitive and <see cref="ArtMeshes.Merge"/> merges them into one mesh whose UVs point at each part's palette
/// swatch, drawn with <see cref="ArtMaterials.Shared"/>. A cylinder 1.5 m or more across (the drum's core and stone base,
/// the roof cone) gets <see cref="RoundSides"/> sides so it reads round; smaller ones keep the game's six. Each mesh is
/// built once and shared. Shown only in the art sandbox for now; the farm still uses <see cref="FarmhouseView"/>.
/// </summary>
public static class RoundFarmhouseModels
{
    /// <summary>The sides of a cylinder 1.5 m or more across.</summary>
    public const int RoundSides = 24;

    private const float RoundDiameter = 1.5f;

    private static readonly Dictionary<string, ArrayMesh> Meshes = new();

    /// <summary>The round farmhouse, run-down (<paramref name="restored"/> false) or restored.</summary>
    public static ArrayMesh MeshFor(bool restored) =>
        Cached(restored ? "restored" : "rundown", restored ? RoundFarmhouseArt.Restored : RoundFarmhouseArt.RunDown);

    /// <summary>The restored house's lit window panes, to show only at night.</summary>
    public static ArrayMesh NightLights() => Cached("night", RoundFarmhouseArt.RestoredNightLights);

    private static ArrayMesh Cached(string key, IEnumerable<CropPart> parts)
    {
        if (!Meshes.TryGetValue(key, out var mesh))
        {
            mesh = ArtMeshes.Merge(parts.Select(ToMeshPart));
            Meshes[key] = mesh;
        }

        return mesh;
    }

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
        _ => throw new System.ArgumentOutOfRangeException(nameof(part), part.Shape, "Unknown farmhouse part shape."),
    };
}
