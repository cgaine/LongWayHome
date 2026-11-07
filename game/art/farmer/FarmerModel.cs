#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Game.Art;

/// <summary>
/// The farmer (#102, ART_DIRECTION §6.1), the script of <c>res://art/farmer/Farmer.tscn</c>. On <see cref="_Ready"/>
/// it builds, under the <c>Farmer</c> node:
/// <list type="bullet">
/// <item>a <see cref="Skeleton3D"/> <c>Skeleton</c> from <see cref="FarmerArt.Bones"/>, each rest pose the bone's
/// position relative to its parent and no rotation (the rest pose is the "A" pose);</item>
/// <item>under it one skinned <see cref="MeshInstance3D"/> <c>Body</c>: every <see cref="FarmerArt.Parts"/> entry
/// merged by <see cref="ArtMeshes.MergeSkinned"/>, each vertex weighted fully to its part's bone, with a
/// <see cref="Skin"/> binding each bone at rest. It draws with a copy of the shared palette material whose next pass
/// is the inverted-hull outline (<c>res://art/shaders/outline.gdshader</c>), so only the farmer is outlined;</item>
/// <item>a <c>Shadow</c>: a flat 0.6 m dark disc just above the tile slabs (y 0.065), unshaded and 35% opaque,
/// casting no shadow;</item>
/// <item>a <see cref="BoneAttachment3D"/> <c>HeldItem</c> on the <c>RightHand</c> bone holding one mesh per item kind,
/// built on first show from <see cref="HeldItemArt.PartsFor"/>, shown one at a time by <see cref="ShowHeld"/>.</item>
/// </list>
/// It also gives the scene's <c>Animations</c> <see cref="AnimationPlayer"/> the clips of
/// <see cref="FarmerAnimations.Build"/> as its default library (#103); <c>FarmerAnimator</c> on the Player, or the art
/// sandbox, plays them.
/// The meshes, skin and materials are built once and shared by every farmer (the art sandbox shows several). The
/// farmer faces +Z with his left on +X, so <c>PlayerController</c> keeps rotating its <c>Model</c> as before. A
/// handful of draws: the body, its outline, the shadow and the held item (REQ-14).
/// </summary>
public partial class FarmerModel : Node3D
{
    private const string OutlineShaderPath = "res://art/shaders/outline.gdshader";

    private const string HandBone = "RightHand";

    /// <summary>The scene's <see cref="AnimationPlayer"/>, given <see cref="FarmerAnimations.Build"/> as its default library.</summary>
    private const string AnimationsNode = "Animations";

    private const float ShadowDiameter = 0.6f;

    /// <summary>
    /// Just above the farm's tile slab tops (0.06 m, <c>TileView</c>): the Player stands at y 0, so a shadow at the
    /// issue's y 0.01 would be buried in every Tile he walks on. Off the plot it floats a few centimetres over the
    /// meadow, which the raised camera can't tell.
    /// </summary>
    private const float ShadowHeight = 0.065f;

    private static readonly Dictionary<HeldItem, ArrayMesh> HeldMeshes = new();

    private static ArrayMesh? _bodyMesh;

    private static Skin? _skin;

    private static ShaderMaterial? _bodyMaterial;

    private static Mesh? _shadowMesh;

    private static StandardMaterial3D? _shadowMaterial;

    /// <summary>The held-item meshes this farmer has shown, one per item kind.</summary>
    private readonly Dictionary<HeldItem, MeshInstance3D> _heldViews = new();

    private BoneAttachment3D? _hand;

    /// <summary>What to hold once built: <see cref="ShowHeld"/> may be called before <see cref="_Ready"/>.</summary>
    private HeldItem _held = HeldItem.Of(ToolId.Hoe);

    /// <summary>The farmer's skeleton, for the animation feature to drive.</summary>
    public Skeleton3D Skeleton { get; private set; } = null!;

    /// <summary>The item now in his right hand.</summary>
    public HeldItem Held => _held;

    public override void _Ready()
    {
        Skeleton = BuildSkeleton();
        AddChild(Skeleton);

        Skeleton.AddChild(new MeshInstance3D
        {
            Name = "Body",
            Mesh = BodyMesh(),
            Skin = BodySkin(),
            Skeleton = new NodePath(".."),
            MaterialOverride = BodyMaterial(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
        });

        AddChild(new MeshInstance3D
        {
            Name = "Shadow",
            Mesh = ShadowMesh(),
            MaterialOverride = ShadowMaterial(),
            Position = new Vector3(0f, ShadowHeight, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        _hand = new BoneAttachment3D { Name = "HeldItem", BoneName = HandBone };
        Skeleton.AddChild(_hand);

        ShowHeld(_held);

        // #103: the clips, on the scene's AnimationPlayer. Its root node is this Farmer, so the tracks'
        // "Skeleton:<Bone>" paths resolve to the skeleton just built (the player resolves them when it first plays).
        if (GetNodeOrNull<AnimationPlayer>(AnimationsNode) is { } animations && !animations.HasAnimationLibrary(""))
        {
            animations.AddAnimationLibrary("", FarmerAnimations.Build());
        }
    }

    /// <summary>Shows <paramref name="held"/> in his right hand and hides the rest.</summary>
    public void ShowHeld(HeldItem held)
    {
        _held = held;
        if (_hand is null)
        {
            return;
        }

        if (!_heldViews.ContainsKey(held))
        {
            var view = new MeshInstance3D
            {
                Name = HeldName(held),
                Mesh = HeldMesh(held),
                MaterialOverride = ArtMaterials.Shared,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
            };
            _hand.AddChild(view);
            _heldViews[held] = view;
        }

        foreach (var (item, view) in _heldViews)
        {
            view.Visible = item == held;
        }
    }

    private static Skeleton3D BuildSkeleton()
    {
        var skeleton = new Skeleton3D { Name = "Skeleton" };
        var bones = FarmerArt.Bones;
        var positions = new Dictionary<string, Vector3>();
        foreach (var bone in bones)
        {
            var index = skeleton.AddBone(bone.Name);
            var position = new Vector3(bone.X, bone.Y, bone.Z);
            positions[bone.Name] = position;
            if (bone.Parent is { } parent)
            {
                skeleton.SetBoneParent(index, skeleton.FindBone(parent));
                // A rest pose is relative to the parent bone; the table gives model-space positions.
                skeleton.SetBoneRest(index, new Transform3D(Basis.Identity, position - positions[parent]));
            }
            else
            {
                skeleton.SetBoneRest(index, new Transform3D(Basis.Identity, position));
            }
        }

        skeleton.ResetBonePoses();
        return skeleton;
    }

    /// <summary>The merged body, every part weighted to its bone's index in <see cref="FarmerArt.Bones"/>.</summary>
    private static ArrayMesh BodyMesh()
    {
        if (_bodyMesh is null)
        {
            var boneIndex = BoneIndices();
            _bodyMesh = ArtMeshes.MergeSkinned(FarmerArt.Parts.Select(part => (
                ToMeshPart(part.Shape, part.SizeX, part.SizeY, part.SizeZ, part.X, part.Y, part.Z,
                    part.PitchDegrees, part.YawDegrees, part.RollDegrees, part.Role),
                boneIndex[part.Bone])));
        }

        return _bodyMesh;
    }

    /// <summary>One bind per bone: the inverse of its rest position in model space, so the rest pose is unchanged.</summary>
    private static Skin BodySkin()
    {
        if (_skin is null)
        {
            _skin = new Skin();
            var bones = FarmerArt.Bones;
            for (var i = 0; i < bones.Count; i++)
            {
                var rest = new Transform3D(Basis.Identity, new Vector3(bones[i].X, bones[i].Y, bones[i].Z));
                _skin.AddBind(i, rest.AffineInverse());
            }
        }

        return _skin;
    }

    /// <summary>A copy of <see cref="ArtMaterials.Shared"/> with the outline as its next pass, for the farmer only.</summary>
    private static ShaderMaterial BodyMaterial()
    {
        if (_bodyMaterial is null)
        {
            var outline = new ShaderMaterial { Shader = GD.Load<Shader>(OutlineShaderPath) };
            _bodyMaterial = (ShaderMaterial)ArtMaterials.Shared.Duplicate();
            _bodyMaterial.NextPass = outline;
        }

        return _bodyMaterial;
    }

    private static Mesh ShadowMesh() => _shadowMesh ??= new CylinderMesh
    {
        TopRadius = ShadowDiameter / 2f,
        BottomRadius = ShadowDiameter / 2f,
        Height = 0.002f,
        RadialSegments = 16,
        Rings = 1,
    };

    private static StandardMaterial3D ShadowMaterial() => _shadowMaterial ??= new StandardMaterial3D
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        AlbedoColor = new Color(0f, 0f, 0f, 0.35f),
    };

    /// <summary>The held item's merged mesh, in the hand's space, built on first use and shared.</summary>
    private static ArrayMesh HeldMesh(HeldItem held)
    {
        if (!HeldMeshes.TryGetValue(held, out var mesh))
        {
            mesh = ArtMeshes.Merge(HeldItemArt.PartsFor(held).Select(part => ToMeshPart(
                part.Shape, part.SizeX, part.SizeY, part.SizeZ, part.X, part.Y, part.Z,
                part.PitchDegrees, part.YawDegrees, part.RollDegrees, part.Role)));
            HeldMeshes[held] = mesh;
        }

        return mesh;
    }

    private static string HeldName(HeldItem held) => held.Tool is { } tool ? $"{tool}" : $"{held.Seed}Seeds";

    private static Dictionary<string, int> BoneIndices()
    {
        var indices = new Dictionary<string, int>();
        var bones = FarmerArt.Bones;
        for (var i = 0; i < bones.Count; i++)
        {
            indices[bones[i].Name] = i;
        }

        return indices;
    }

    /// <summary>A part as a primitive placed by Godot's Euler YXZ about its centre, the conventions of <see cref="CropPart"/>.</summary>
    private static MeshPart ToMeshPart(
        CropPartShape shape,
        float sizeX,
        float sizeY,
        float sizeZ,
        float x,
        float y,
        float z,
        float pitchDegrees,
        float yawDegrees,
        float rollDegrees,
        string role)
    {
        var euler = new Vector3(Mathf.DegToRad(pitchDegrees), Mathf.DegToRad(yawDegrees), Mathf.DegToRad(rollDegrees));
        var at = new Transform3D(Basis.FromEuler(euler, EulerOrder.Yxz), new Vector3(x, y, z));
        return new MeshPart(Primitive(shape, sizeX, sizeY, sizeZ), at, role);
    }

    private static Mesh Primitive(CropPartShape shape, float sizeX, float sizeY, float sizeZ) => shape switch
    {
        CropPartShape.Box => new BoxMesh { Size = new Vector3(sizeX, sizeY, sizeZ) },
        CropPartShape.Sphere => new SphereMesh { Radius = sizeX / 2f, Height = sizeY, RadialSegments = 8, Rings = 4 },
        CropPartShape.Cylinder => new CylinderMesh
        {
            BottomRadius = sizeX / 2f,
            TopRadius = sizeZ / 2f,
            Height = sizeY,
            RadialSegments = 8,
            Rings = 1,
        },
        CropPartShape.Prism => new PrismMesh { Size = new Vector3(sizeX, sizeY, sizeZ) },
        _ => throw new System.ArgumentOutOfRangeException(nameof(shape), shape, "Unknown part shape."),
    };
}
