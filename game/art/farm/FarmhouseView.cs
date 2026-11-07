#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The run-down farmhouse (#104, ART_DIRECTION §7.1), the script of <c>res://art/farm/Farmhouse.tscn</c>. On ready it
/// builds its one <c>Model</c> mesh from <see cref="FarmhouseArt.RunDown"/>: each part becomes a low-poly primitive
/// (Box → <see cref="BoxMesh"/>, Sphere → <see cref="SphereMesh"/> 8 x 4, Cylinder → six-sided
/// <see cref="CylinderMesh"/>, Prism → <see cref="PrismMesh"/>, as <see cref="CropModels"/> does) and
/// <see cref="ArtMeshes.Merge"/> merges them into one mesh whose UVs point at each part's palette swatch, so the whole
/// house is one opaque draw under <see cref="ArtMaterials.Shared"/>, casting shadows, with no glow (REQ-14). Its origin
/// is the house's footprint centre on the ground. It has no collision (the farm's <c>House</c> body keeps its own box)
/// and reads no game state. The merged mesh is built once and shared, so loading the farm again does not rebuild it.
/// </summary>
public partial class FarmhouseView : Node3D
{
    private static ArrayMesh? _runDown;

    public override void _Ready()
    {
        var model = GetNode<MeshInstance3D>("Model");
        model.Mesh = _runDown ??= ArtMeshes.Merge(FarmhouseArt.RunDown.Select(ToMeshPart));
        model.MaterialOverride = ArtMaterials.Shared;
        model.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
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
            RadialSegments = 6,
            Rings = 1,
        },
        CropPartShape.Prism => new PrismMesh { Size = new Vector3(part.SizeX, part.SizeY, part.SizeZ) },
        _ => throw new System.ArgumentOutOfRangeException(nameof(part), part.Shape, "Unknown farmhouse part shape."),
    };
}
