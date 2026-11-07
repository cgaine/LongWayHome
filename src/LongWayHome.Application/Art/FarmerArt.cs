using System.Numerics;

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

/// <summary>
/// The day-one farmer of ART_DIRECTION §6.1 (#102): his skeleton and parts. Pure C#, no Godot: the Godot builder
/// <c>FarmerModel</c> turns the bones into a <c>Skeleton3D</c> and merges every part into one skinned mesh under the
/// shared palette material, each vertex weighted fully to its part's bone.
/// <para>
/// <b>Conventions.</b> Model space in metres, y 0 at the soles, the farmer facing +Z, his own left on +X. Sizes,
/// shapes and rotations are those of <see cref="CropPart"/>. He stands in his rest pose: an "A" pose with the arms a
/// little out (rolled 6°), no bone rotated. Every left/right pair is built once on the left and mirrored (x, yaw and
/// roll negated); the wrist unit, the Earth patch and the chest pocket are one-sided.
/// </para>
/// <para>
/// <b>Proportions.</b> 1.60 m to the top of the cap, so he fills the 1.6 m collision capsule. The head and cap run
/// from the chin at y 1.07 to 1.60 (0.53 m, a third of his height: about 3 heads tall). Torso y 0.46–1.05; short legs
/// y 0.18–0.55; boots y 0–0.18, 0.2 wide and 0.32 long, toes forward. Hands are 0.14 m boxes at y 0.56. He is about
/// 0.79 m wide (hand to hand) and 0.59 m deep (hair to brim tip). Within the issue's 25% allowance the torso is 0.48 m
/// wide (not 0.62) and 0.38 m deep (not 0.42), so the A-posed arms and oversized hands fit the 0.8 m width without
/// sinking into the body.
/// </para>
/// <para>
/// <b>Outfit.</b> The one-piece coverall is <c>ship-metal</c>, with <c>coverall-shade</c> side facets on the torso
/// and legs, folds at the elbows and knees, knee pads, the zip and the chest pocket; the collar, belt, sleeve bands
/// and pocket flap are <c>navy</c>. The Earth patch (an <c>earth-blue</c> disc with two <c>earth-green</c> lands)
/// sits on the left chest. Flight boots (<c>boots</c>) have <c>rock</c> toe caps, a <c>navy</c> sole and a
/// <c>coverall-shade</c> lace strip. The <c>navy</c> mission cap has an <c>earth-blue</c> front patch and a 0.17 m
/// brim tipped 10° down, reaching z 0.38: the facing cue. The face is <c>skin</c> with two <c>navy</c> eyes,
/// <c>hair</c> brows, a small nose and round ears, <c>skin-shade</c> under the brim and at the chin, and <c>hair</c>
/// below the cap at the sides and back, down to the nape so his back never reads as a face. The
/// <c>navy</c>/<c>wrist-screen</c> wrist unit is on the left forearm.
/// </para>
/// </summary>
public static class FarmerArt
{
    /// <summary>The A pose's outward tilt of the arms, in degrees of roll.</summary>
    private const float ArmRoll = 6f;

    private static readonly IReadOnlyList<FarmerBone> BoneList = BuildBones();

    private static readonly IReadOnlyList<FarmerPart> PartList = BuildParts();

    /// <summary>The skeleton, parents before children; <c>Hips</c> is the root.</summary>
    public static IReadOnlyList<FarmerBone> Bones => BoneList;

    /// <summary>The day-one farmer's parts.</summary>
    public static IReadOnlyList<FarmerPart> Parts => PartList;

    /// <summary>
    /// The axis-aligned extents of every part's rotated box (see <see cref="CropArt"/> for the unrotated box of each
    /// shape); all zero for no parts.
    /// </summary>
    public static (float Width, float Depth, float Height) Bounds(IEnumerable<FarmerPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var (min, max) = PartBox.Extents(parts.SelectMany(Corners));
        return (max.X - min.X, max.Z - min.Z, max.Y - min.Y);
    }

    private static IEnumerable<Vector3> Corners(FarmerPart part) => PartBox.Corners(
        part.Shape, part.SizeX, part.SizeY, part.SizeZ, new Vector3(part.X, part.Y, part.Z),
        part.PitchDegrees, part.YawDegrees, part.RollDegrees);

    private static List<FarmerBone> BuildBones()
    {
        var bones = new List<FarmerBone>
        {
            new("Hips", null, 0f, 0.55f, 0f),
            new("Spine", "Hips", 0f, 0.64f, 0f),
            new("Chest", "Spine", 0f, 0.78f, 0f),
            new("Neck", "Chest", 0f, 1.04f, 0f),
            new("Head", "Neck", 0f, 1.11f, 0f),
        };

        foreach (var side in new[] { Side.Left, Side.Right })
        {
            bones.Add(Bone(side, "Shoulder", "Chest", 0.10f, 1.00f, 0f));
            bones.Add(Bone(side, "UpperArm", $"{side}Shoulder", 0.27f, 0.97f, 0f));
            bones.Add(Bone(side, "LowerArm", $"{side}UpperArm", 0.30f, 0.78f, 0f));
            bones.Add(Bone(side, "Hand", $"{side}LowerArm", 0.325f, 0.56f, 0f));
        }

        foreach (var side in new[] { Side.Left, Side.Right })
        {
            bones.Add(Bone(side, "UpperLeg", "Hips", 0.12f, 0.55f, 0f));
            bones.Add(Bone(side, "LowerLeg", $"{side}UpperLeg", 0.12f, 0.35f, 0f));
            bones.Add(Bone(side, "Foot", $"{side}LowerLeg", 0.12f, 0.10f, 0.02f));
        }

        return bones;
    }

    /// <summary>A bone on <paramref name="side"/>, given at its left (+X) position.</summary>
    private static FarmerBone Bone(Side side, string name, string parent, float x, float y, float z) =>
        new($"{side}{name}", parent, side.Sign() * x, y, z);

    private static List<FarmerPart> BuildParts()
    {
        var parts = new List<FarmerPart>();
        AddTorso(parts);
        AddHead(parts);
        foreach (var side in new[] { Side.Left, Side.Right })
        {
            AddArm(parts, side);
            AddLeg(parts, side);
        }

        AddWristUnit(parts);
        return parts;
    }

    private static void AddTorso(List<FarmerPart> parts)
    {
        // Hips: the seat of the coverall and the belt with its buckle.
        parts.Add(Box("Seat", 0.46f, 0.14f, 0.36f, 0f, 0.53f, 0f, "ship-metal", "Hips"));
        parts.Add(Box("Belt", 0.50f, 0.06f, 0.40f, 0f, 0.60f, 0f, "navy", "Hips"));
        parts.Add(Box("Buckle", 0.08f, 0.05f, 0.02f, 0f, 0.60f, 0.205f, "rock", "Hips"));

        // Spine: the belly, its sides in shade.
        parts.Add(Box("Belly", 0.47f, 0.14f, 0.37f, 0f, 0.69f, 0f, "ship-metal", "Spine"));
        foreach (var side in new[] { Side.Left, Side.Right })
        {
            parts.Add(Mirror(side, Box("BellySide", 0.012f, 0.14f, 0.35f, 0.241f, 0.69f, 0f, "coverall-shade", "Spine")));
        }

        // Chest: the barrel chest, its sides in shade, the collar, the zip, the pocket and the Earth patch.
        parts.Add(Box("Chest", 0.48f, 0.30f, 0.38f, 0f, 0.90f, 0f, "ship-metal", "Chest"));
        foreach (var side in new[] { Side.Left, Side.Right })
        {
            parts.Add(Mirror(side, Box("ChestSide", 0.012f, 0.30f, 0.36f, 0.246f, 0.90f, 0f, "coverall-shade", "Chest")));
            // The collar's points: a V at the throat, each outer end lifted.
            parts.Add(Mirror(side, Box("CollarPoint", 0.10f, 0.07f, 0.015f, 0.06f, 1.015f, 0.193f, "navy", "Chest", roll: 25f)));
        }

        parts.Add(Box("Collar", 0.30f, 0.06f, 0.28f, 0f, 1.06f, -0.01f, "navy", "Chest"));
        parts.Add(Box("Zip", 0.02f, 0.34f, 0.012f, 0f, 0.81f, 0.193f, "coverall-shade", "Chest"));
        parts.Add(Box("ZipPull", 0.035f, 0.05f, 0.015f, 0f, 0.97f, 0.198f, "rock", "Chest"));
        parts.Add(Box("Pocket", 0.12f, 0.10f, 0.012f, -0.12f, 0.88f, 0.196f, "coverall-shade", "Chest"));
        parts.Add(Box("PocketFlap", 0.13f, 0.03f, 0.016f, -0.12f, 0.935f, 0.198f, "navy", "Chest"));
        parts.Add(new FarmerPart(
            "EarthPatch", CropPartShape.Cylinder, 0.11f, 0.012f, 0.11f, 0.12f, 0.90f, 0.196f, 90f, 0f, 0f,
            "earth-blue", "Chest"));
        parts.Add(Box("EarthLandNorth", 0.045f, 0.035f, 0.01f, 0.105f, 0.915f, 0.203f, "earth-green", "Chest"));
        parts.Add(Box("EarthLandSouth", 0.03f, 0.025f, 0.01f, 0.14f, 0.88f, 0.203f, "earth-green", "Chest"));

        parts.Add(new FarmerPart(
            "Neck", CropPartShape.Cylinder, 0.17f, 0.08f, 0.17f, 0f, 1.08f, 0f, 0f, 0f, 0f, "skin", "Neck"));
    }

    private static void AddHead(List<FarmerPart> parts)
    {
        // The head: a big rounded-off block, its biggest part, shaded at the chin and under the brim.
        parts.Add(Box("Head", 0.40f, 0.32f, 0.38f, 0f, 1.27f, 0f, "skin", "Head"));
        parts.Add(Box("Chin", 0.36f, 0.04f, 0.34f, 0f, 1.09f, 0f, "skin-shade", "Head"));
        parts.Add(Box("BrimShade", 0.36f, 0.04f, 0.012f, 0f, 1.40f, 0.194f, "skin-shade", "Head"));
        parts.Add(Box("Nose", 0.05f, 0.05f, 0.035f, 0f, 1.235f, 0.2075f, "skin", "Head"));
        foreach (var side in new[] { Side.Left, Side.Right })
        {
            parts.Add(Mirror(side, Box("Eye", 0.05f, 0.07f, 0.016f, 0.085f, 1.29f, 0.195f, "navy", "Head")));
            parts.Add(Mirror(side, Box("Brow", 0.07f, 0.018f, 0.014f, 0.085f, 1.345f, 0.196f, "hair", "Head")));
            parts.Add(Mirror(side, new FarmerPart(
                "Ear", CropPartShape.Sphere, 0.07f, 0.11f, 0.07f, 0.215f, 1.27f, 0f, 0f, 0f, 0f, "skin", "Head")));
            // Hair behind the ears and a short sideburn in front of each, below the cap.
            parts.Add(Mirror(side, Box("HairSide", 0.025f, 0.16f, 0.12f, 0.2075f, 1.33f, -0.11f, "hair", "Head")));
            parts.Add(Mirror(side, Box("Sideburn", 0.02f, 0.08f, 0.04f, 0.205f, 1.37f, 0.06f, "hair", "Head")));
        }

        // The back of the head is hair down to the nape, so from behind he never reads as facing the camera.
        parts.Add(Box("HairBack", 0.42f, 0.26f, 0.05f, 0f, 1.30f, -0.19f, "hair", "Head"));

        // The mission cap, its patch and the brim: the brim is the frontmost thing on him above the shoulders.
        parts.Add(Box("CapCrown", 0.43f, 0.15f, 0.41f, 0f, 1.495f, -0.01f, "navy", "Head"));
        parts.Add(Box("CapTop", 0.37f, 0.02f, 0.35f, 0f, 1.58f, -0.01f, "navy", "Head"));
        parts.Add(Box("CapButton", 0.06f, 0.01f, 0.06f, 0f, 1.595f, -0.01f, "navy", "Head"));
        parts.Add(Box("CapPatch", 0.10f, 0.07f, 0.012f, 0f, 1.50f, 0.199f, "earth-blue", "Head"));
        parts.Add(Box("CapBrim", 0.36f, 0.035f, 0.17f, 0f, 1.425f, 0.29f, "navy", "Head", pitch: 10f));
    }

    /// <summary>One arm, built on the left (+X) and mirrored for the right, in the A pose.</summary>
    private static void AddArm(List<FarmerPart> parts, Side side)
    {
        FarmerPart[] arm =
        [
            new("Shoulder", CropPartShape.Sphere, 0.18f, 0.16f, 0.18f, 0.25f, 0.975f, 0f, 0f, 0f, 0f, "ship-metal", "Shoulder"),
            Box("UpperArm", 0.13f, 0.22f, 0.15f, 0.29f, 0.87f, 0f, "ship-metal", "UpperArm", roll: ArmRoll),
            Box("ElbowFold", 0.135f, 0.035f, 0.155f, 0.30f, 0.775f, 0f, "coverall-shade", "LowerArm", roll: ArmRoll),
            Box("Forearm", 0.12f, 0.15f, 0.14f, 0.312f, 0.69f, 0f, "ship-metal", "LowerArm", roll: ArmRoll),
            Box("SleeveBand", 0.13f, 0.04f, 0.15f, 0.322f, 0.635f, 0f, "navy", "LowerArm", roll: ArmRoll),
            Box("Hand", 0.14f, 0.13f, 0.14f, 0.325f, 0.56f, 0f, "skin", "Hand"),
            Box("Thumb", 0.045f, 0.06f, 0.05f, 0.29f, 0.575f, 0.08f, "skin", "Hand"),
        ];
        parts.AddRange(arm.Select(part => Mirror(side, part with { Bone = $"{side}{part.Bone}" })));
    }

    /// <summary>One leg and flight boot, built on the left (+X) and mirrored for the right.</summary>
    private static void AddLeg(List<FarmerPart> parts, Side side)
    {
        FarmerPart[] leg =
        [
            Box("Thigh", 0.19f, 0.20f, 0.24f, 0.12f, 0.45f, 0f, "ship-metal", "UpperLeg"),
            Box("ThighSide", 0.012f, 0.18f, 0.22f, 0.221f, 0.45f, 0f, "coverall-shade", "UpperLeg"),
            Box("KneeFold", 0.185f, 0.03f, 0.225f, 0.12f, 0.36f, 0f, "coverall-shade", "LowerLeg"),
            Box("KneePad", 0.14f, 0.09f, 0.025f, 0.12f, 0.335f, 0.12f, "coverall-shade", "LowerLeg"),
            Box("Shin", 0.18f, 0.18f, 0.22f, 0.12f, 0.27f, 0f, "ship-metal", "LowerLeg"),
            Box("ShinSide", 0.012f, 0.16f, 0.20f, 0.216f, 0.27f, 0f, "coverall-shade", "LowerLeg"),
            Box("Boot", 0.20f, 0.11f, 0.32f, 0.12f, 0.055f, 0.04f, "boots", "Foot"),
            Box("BootShaft", 0.20f, 0.09f, 0.22f, 0.12f, 0.135f, -0.01f, "boots", "Foot"),
            Box("Sole", 0.21f, 0.025f, 0.33f, 0.12f, 0.0125f, 0.04f, "navy", "Foot"),
            Box("ToeCap", 0.21f, 0.08f, 0.07f, 0.12f, 0.045f, 0.17f, "rock", "Foot"),
            Box("Laces", 0.07f, 0.08f, 0.012f, 0.12f, 0.13f, 0.106f, "coverall-shade", "Foot"),
            Box("LacesTop", 0.07f, 0.012f, 0.035f, 0.12f, 0.116f, 0.1175f, "coverall-shade", "Foot"),
        ];
        parts.AddRange(leg.Select(part => Mirror(side, part with { Bone = $"{side}{part.Bone}" })));
    }

    /// <summary>The wrist unit on the left forearm, its screen facing forward.</summary>
    private static void AddWristUnit(List<FarmerPart> parts)
    {
        parts.Add(Box("WristUnit", 0.085f, 0.065f, 0.03f, 0.318f, 0.675f, 0.08f, "navy", "LeftLowerArm", roll: ArmRoll));
        parts.Add(Box(
            "WristScreen", 0.065f, 0.045f, 0.01f, 0.318f, 0.675f, 0.097f, "wrist-screen", "LeftLowerArm", roll: ArmRoll));
    }

    private static FarmerPart Box(
        string name,
        float sizeX,
        float sizeY,
        float sizeZ,
        float x,
        float y,
        float z,
        string role,
        string bone,
        float pitch = 0f,
        float yaw = 0f,
        float roll = 0f) =>
        new(name, CropPartShape.Box, sizeX, sizeY, sizeZ, x, y, z, pitch, yaw, roll, role, bone);

    /// <summary>
    /// A part drawn on the left, placed on <paramref name="side"/>: the name gets the side as a prefix, and on the
    /// right x, yaw and roll are negated (a mirror in the YZ plane). The bone is kept as given (already sided, or a
    /// centre bone).
    /// </summary>
    private static FarmerPart Mirror(Side side, FarmerPart left)
    {
        var sign = side.Sign();
        return left with
        {
            Name = $"{side}{left.Name}",
            X = sign * left.X,
            YawDegrees = sign * left.YawDegrees,
            RollDegrees = sign * left.RollDegrees,
        };
    }

    private enum Side
    {
        Left,
        Right,
    }

    private static float Sign(this Side side) => side == Side.Left ? 1f : -1f;
}
