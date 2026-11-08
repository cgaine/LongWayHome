using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Art;

/// <summary>
/// The gathering tools bought from Maggie in Phase 2 (design doc, Tools: the axe for 50 gold, the pickaxe for 75), drawn
/// ahead of the game having them: they are not <see cref="ToolId"/> values yet, so nothing in play can hold them. The art
/// sandbox shows them in the farmer's hand through <see cref="HeldItemArt.PartsFor(GatherTool)"/>.
/// </summary>
public enum GatherTool
{
    /// <summary>The axe: wood and sap from trees.</summary>
    Axe,

    /// <summary>The pickaxe: stone and ore from rocks.</summary>
    Pickaxe,
}

/// <summary>
/// The parts of the item in the farmer's right hand (#102), in the hand's space (origin at the <c>RightHand</c> bone,
/// the centre of the fist; y up from the grip, +Z the farmer's front). Pure C#, no Godot; the Godot builder
/// <c>FarmerModel</c> merges each item into one mesh under the shared palette material.
/// <list type="table">
/// <listheader><term>Item</term><description>Parts</description></listheader>
/// <item><term>Hoe (basic)</term><description>A short, thick <c>timber</c> handle (Cylinder 0.05 x 0.75) held near
/// its lower end, from y −0.22 to 0.53, with <c>timber-side</c> bands above the fist and at the butt; at the top a
/// <c>rock</c> socket, a plate jutting 0.16 m forward and a blade turning down and tipped back 12°. The head tops out
/// about 0.56 m above the grip, about chest high when he holds it.</description></item>
/// <item><term>Watering can (basic)</term><description>A <c>rock</c> bail handle through the fist on two posts, a
/// round body (Cylinder 0.28 x 0.22) hanging below it with a rim, a long spout rising forward at 55° from low on the
/// body and a rose at its tip; one small dent, a flattened facet tilted on the body's left side.</description></item>
/// <item><term>Seeds</term><description>A <c>seed-pouch</c> cloth bag (Sphere 0.15) hanging below the fist on a
/// cord, tied at the neck (<c>timber-side</c>), its mouth flaring open with three seeds peeking out in the crop's
/// colour (<c>ember</c>, <c>dusktuber</c>, <c>moonmelon</c>, <c>sunbell-gold</c>, <c>starcorn</c>), and a
/// <c>timber</c> patch on its front.</description></item>
/// <item><term>Axe (basic)</term><description>The hoe's short, thick handle and bands, with a <c>rock</c> head on top
/// (ART_DIRECTION §6.4): a socket round the handle, a short <c>rock</c> poll behind and a single wedge jutting forward
/// that deepens towards its edge, with a pale <c>ship-metal</c> sharpened edge 0.2 m in front of the handle.</description></item>
/// <item><term>Pickaxe (basic)</term><description>The same handle with a curved, double-pointed <c>rock</c> head across
/// its top, forward and back: each arm three tapering pieces bending down towards a <c>ship-metal</c> point, 0.5 m
/// tip to tip, the widest silhouette of the tools.</description></item>
/// </list>
/// </summary>
public static class HeldItemArt
{
    private static readonly IReadOnlyList<CropPart> HoeParts = BuildHoe();

    private static readonly IReadOnlyList<CropPart> CanParts = BuildCan();

    private static readonly IReadOnlyList<CropPart> AxeParts = BuildAxe();

    private static readonly IReadOnlyList<CropPart> PickaxeParts = BuildPickaxe();

    private static readonly Dictionary<CropTypeId, IReadOnlyList<CropPart>> PouchParts = Enum.GetValues<CropTypeId>()
        .ToDictionary(crop => crop, crop => (IReadOnlyList<CropPart>)BuildPouch(SeedColour(crop)));

    /// <summary>The parts of the basic hoe, the basic watering can, the axe, the pickaxe or a seed pouch in the crop's colour.</summary>
    /// <exception cref="ArgumentException">The item is neither a tool nor a seed, or an undefined tool or crop.</exception>
    public static IReadOnlyList<CropPart> PartsFor(HeldItem held)
    {
        ArgumentNullException.ThrowIfNull(held);
        if (held.Tool is { } tool)
        {
            return tool switch
            {
                ToolId.Hoe => HoeParts,
                ToolId.WateringCan => CanParts,
                ToolId.Axe => AxeParts,
                ToolId.Pickaxe => PickaxeParts,
                _ => throw new ArgumentException($"No held-item art for tool {tool}.", nameof(held)),
            };
        }

        if (held.Seed is { } seed && PouchParts.TryGetValue(seed, out var pouch))
        {
            return pouch;
        }

        throw new ArgumentException($"No held-item art for tool {held.Tool}, seed {held.Seed}.", nameof(held));
    }

    /// <summary>The parts of a gathering tool (basic level), in the hand's space like the hoe.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined tool.</exception>
    public static IReadOnlyList<CropPart> PartsFor(GatherTool tool) => tool switch
    {
        GatherTool.Axe => AxeParts,
        GatherTool.Pickaxe => PickaxeParts,
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown gathering tool."),
    };

    /// <summary>The crop's own colour role, as on its seed and ripe models (#90–#92) and its icons (#95).</summary>
    private static string SeedColour(CropTypeId crop) => crop switch
    {
        CropTypeId.Emberroot => "ember",
        CropTypeId.Dusktuber => "dusktuber",
        CropTypeId.Moonmelon => "moonmelon",
        CropTypeId.Sunbell => "sunbell-gold",
        CropTypeId.Starcorn => "starcorn",
        _ => throw new ArgumentOutOfRangeException(nameof(crop), crop, "Unknown crop type."),
    };

    private static List<CropPart> BuildHoe() =>
    [
        Part("Handle", CropPartShape.Cylinder, 0.05f, 0.75f, 0.05f, 0f, 0.155f, 0f, "timber"),
        Part("HandleBand", CropPartShape.Cylinder, 0.058f, 0.05f, 0.058f, 0f, 0.11f, 0f, "timber-side"),
        Part("HandleButt", CropPartShape.Cylinder, 0.058f, 0.04f, 0.058f, 0f, -0.20f, 0f, "timber-side"),
        Part("Socket", CropPartShape.Box, 0.07f, 0.07f, 0.09f, 0f, 0.52f, 0.01f, "rock"),
        Part("Plate", CropPartShape.Box, 0.15f, 0.025f, 0.16f, 0f, 0.545f, 0.10f, "rock"),
        Part("Blade", CropPartShape.Box, 0.17f, 0.13f, 0.025f, 0f, 0.48f, 0.18f, "rock", pitch: 12f),
    ];

    private static List<CropPart> BuildCan()
    {
        // The spout rises forward from low on the body's front, pitched 55°; its centre and tip are the base plus
        // the rotated half and full length.
        const float spoutLength = 0.26f;
        const float spoutPitch = 55f;
        var (sin, cos) = MathF.SinCos(spoutPitch * MathF.PI / 180f);
        var (baseY, baseZ) = (-0.26f, 0.12f);

        return
        [
            Part("Bail", CropPartShape.Box, 0.035f, 0.035f, 0.18f, 0f, 0f, 0f, "rock"),
            Part("BailPostFront", CropPartShape.Box, 0.03f, 0.08f, 0.03f, 0f, -0.05f, 0.08f, "rock"),
            Part("BailPostBack", CropPartShape.Box, 0.03f, 0.08f, 0.03f, 0f, -0.05f, -0.08f, "rock"),
            Part("Rim", CropPartShape.Cylinder, 0.26f, 0.03f, 0.22f, 0f, -0.085f, 0f, "rock"),
            Part("Body", CropPartShape.Cylinder, 0.28f, 0.22f, 0.28f, 0f, -0.21f, 0f, "rock"),
            Part(
                "Spout", CropPartShape.Cylinder, 0.05f, spoutLength, 0.035f,
                0f, baseY + cos * spoutLength / 2f, baseZ + sin * spoutLength / 2f, "rock", pitch: spoutPitch),
            Part(
                "Rose", CropPartShape.Cylinder, 0.06f, 0.03f, 0.09f,
                0f, baseY + cos * spoutLength, baseZ + sin * spoutLength, "rock", pitch: spoutPitch),
            // The dent: a small flat facet, tilted against the round side so it catches the light differently.
            Part("Dent", CropPartShape.Box, 0.012f, 0.07f, 0.08f, 0.128f, -0.22f, -0.04f, "rock", yaw: 20f, roll: 12f),
        ];
    }

    /// <summary>The handle the hoe, axe and pickaxe share: timber, held near its lower end, with two bands.</summary>
    private static List<CropPart> Handle() =>
    [
        Part("Handle", CropPartShape.Cylinder, 0.05f, 0.75f, 0.05f, 0f, 0.155f, 0f, "timber"),
        Part("HandleBand", CropPartShape.Cylinder, 0.058f, 0.05f, 0.058f, 0f, 0.11f, 0f, "timber-side"),
        Part("HandleButt", CropPartShape.Cylinder, 0.058f, 0.04f, 0.058f, 0f, -0.20f, 0f, "timber-side"),
    ];

    private static List<CropPart> BuildAxe() =>
    [
        .. Handle(),
        Part("Socket", CropPartShape.Box, 0.065f, 0.12f, 0.08f, 0f, 0.48f, 0f, "rock"),
        Part("Poll", CropPartShape.Box, 0.06f, 0.08f, 0.05f, 0f, 0.48f, -0.06f, "rock"),
        // The wedge: a cheek next to the socket, then a deeper blade, then the pale edge.
        Part("Cheek", CropPartShape.Box, 0.05f, 0.11f, 0.07f, 0f, 0.48f, 0.07f, "rock"),
        Part("Blade", CropPartShape.Box, 0.042f, 0.19f, 0.07f, 0f, 0.475f, 0.14f, "rock"),
        Part("BladeTop", CropPartShape.Box, 0.042f, 0.05f, 0.09f, 0f, 0.555f, 0.125f, "rock", pitch: -24f),
        Part("BladeBottom", CropPartShape.Box, 0.042f, 0.05f, 0.09f, 0f, 0.395f, 0.125f, "rock", pitch: 24f),
        Part("Edge", CropPartShape.Box, 0.03f, 0.22f, 0.025f, 0f, 0.475f, 0.185f, "ship-metal"),
        Part("Wedge", CropPartShape.Box, 0.012f, 0.06f, 0.06f, 0f, 0.535f, 0f, "timber-grain"),
    ];

    private static List<CropPart> BuildPickaxe()
    {
        var parts = Handle();
        parts.Add(Part("Socket", CropPartShape.Box, 0.07f, 0.09f, 0.08f, 0f, 0.5f, 0f, "rock"));
        // Each arm bends down in three tapering pieces towards a pale point, forward (+Z) and back (−Z).
        (float Droop, float Length, float Height, float Width)[] pieces = [(8f, 0.09f, 0.05f, 0.05f), (22f, 0.09f, 0.04f, 0.042f), (40f, 0.08f, 0.03f, 0.034f)];
        foreach (var (side, name) in new[] { (1f, "Front"), (-1f, "Back") })
        {
            var at = (Y: 0.51f, Z: side * 0.03f);
            for (var i = 0; i < pieces.Length; i++)
            {
                var (droop, length, height, width) = pieces[i];
                var (sin, cos) = MathF.SinCos(droop * MathF.PI / 180f);
                var end = (Y: at.Y - sin * length, Z: at.Z + side * cos * length);
                // The piece lies along Z, pitched so its far end drops by the droop.
                parts.Add(Part($"Arm{name}{i}", CropPartShape.Box, width, height, length + 0.01f, 0f, (at.Y + end.Y) / 2f, (at.Z + end.Z) / 2f,
                    i == pieces.Length - 1 ? "ship-metal" : "rock", pitch: side * droop));
                at = end;
            }
        }

        return parts;
    }

    private static List<CropPart> BuildPouch(string colour) =>
    [
        Part("Cord", CropPartShape.Box, 0.015f, 0.06f, 0.015f, 0f, -0.09f, 0f, "timber-side"),
        Part("Mouth", CropPartShape.Cylinder, 0.06f, 0.04f, 0.10f, 0f, -0.13f, 0f, "seed-pouch"),
        Part("SeedA", CropPartShape.Sphere, 0.035f, 0.035f, 0.035f, 0.022f, -0.108f, 0.01f, colour),
        Part("SeedB", CropPartShape.Sphere, 0.035f, 0.035f, 0.035f, -0.022f, -0.11f, -0.012f, colour),
        Part("SeedC", CropPartShape.Sphere, 0.035f, 0.035f, 0.035f, 0f, -0.104f, 0.028f, colour),
        Part("Tie", CropPartShape.Cylinder, 0.065f, 0.018f, 0.065f, 0f, -0.155f, 0f, "timber-side"),
        Part("Bag", CropPartShape.Sphere, 0.15f, 0.15f, 0.15f, 0f, -0.23f, 0f, "seed-pouch"),
        Part("Patch", CropPartShape.Box, 0.06f, 0.05f, 0.012f, 0f, -0.235f, 0.072f, "timber"),
    ];

    private static CropPart Part(
        string name,
        CropPartShape shape,
        float sizeX,
        float sizeY,
        float sizeZ,
        float x,
        float y,
        float z,
        string role,
        float pitch = 0f,
        float yaw = 0f,
        float roll = 0f) =>
        new(name, shape, sizeX, sizeY, sizeZ, x, y, z, pitch, yaw, roll, role);
}
