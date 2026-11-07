#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Game.Art;

/// <summary>
/// Builds the crop models of <see cref="CropArt"/> (#90): each (crop, stage) model's parts merged by
/// <see cref="ArtMeshes.Merge"/> into one <see cref="ArrayMesh"/> whose UVs point at each part's palette swatch, so a
/// model is one draw under <c>MaterialOverride = ArtMaterials.Shared</c> with the <c>swatch</c> instance parameter
/// unset. Each mesh is built once and cached, so all 108 Tiles share at most one mesh per (crop, stage). Only
/// <c>TileView</c> and the art sandbox use it (ART_DIRECTION §8).
/// </summary>
public static class CropModels
{
    private static readonly Dictionary<(CropTypeId Crop, GrowthStage Stage), ArrayMesh> Meshes = new();

    /// <summary>
    /// The merged mesh of a crop's model at a growth stage, standing on y 0 (the slab top). Every crop is drawn at
    /// every stage (#92), so there is always one.
    /// </summary>
    public static ArrayMesh MeshFor(CropTypeId crop, GrowthStage stage)
    {
        if (!Meshes.TryGetValue((crop, stage), out var mesh))
        {
            mesh = ArtMeshes.Merge(CropArt.Parts(crop, stage).Select(ToMeshPart));
            Meshes[(crop, stage)] = mesh;
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
        return new MeshPart(Primitive(part), at, part.Role);
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
            RadialSegments = 7,
            Rings = 1,
        },
        CropPartShape.Prism => new PrismMesh { Size = new Vector3(part.SizeX, part.SizeY, part.SizeZ) },
        _ => throw new System.ArgumentOutOfRangeException(nameof(part), part.Shape, "Unknown crop part shape."),
    };
}
