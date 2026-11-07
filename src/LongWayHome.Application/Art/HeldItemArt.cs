using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Art;

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
/// </list>
/// </summary>
public static class HeldItemArt
{
    private static readonly IReadOnlyList<CropPart> HoeParts = BuildHoe();

    private static readonly IReadOnlyList<CropPart> CanParts = BuildCan();

    private static readonly Dictionary<CropTypeId, IReadOnlyList<CropPart>> PouchParts = Enum.GetValues<CropTypeId>()
        .ToDictionary(crop => crop, crop => (IReadOnlyList<CropPart>)BuildPouch(SeedColour(crop)));

    /// <summary>The parts of the basic hoe, the basic watering can or a seed pouch in the crop's colour.</summary>
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
                _ => throw new ArgumentException($"No held-item art for tool {tool}.", nameof(held)),
            };
        }

        if (held.Seed is { } seed && PouchParts.TryGetValue(seed, out var pouch))
        {
            return pouch;
        }

        throw new ArgumentException($"No held-item art for tool {held.Tool}, seed {held.Seed}.", nameof(held));
    }

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
