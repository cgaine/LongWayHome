using LongWayHome.Domain.Residents;

namespace LongWayHome.Application.Art;

/// <summary>
/// The townspeople of ART_DIRECTION §6.2 (#138): a static part table per Resident, in the farmer's chunky proportions,
/// with the <see cref="CropPart"/> conventions (model space in metres, y 0 at the soles, facing +Z). Pure C#, no Godot:
/// the Godot builder <c>ResidentModels</c> merges each table into one mesh under the shared palette material.
/// <para>
/// <b>Shared traits.</b> About 3 heads tall, like the farmer (§6.1). Each has two pointed ears, triangular prisms named
/// <c>EarLeft</c>/<c>EarRight</c>, mirrored in x, tipped out and back (roll ∓30°, pitch −20°): the not-quite-human cue
/// that reads from the overhead camera. The underside of the chin is the Resident's cool undertone. No night glow or
/// cheek marks yet (out of scope for #138).
/// </para>
/// <para>
/// <b>Maggie</b> (1.32 m to the top of her bun, smaller than Clyde and the farmer): a lavender-silver bun and hair,
/// round <c>glass-dull</c> glasses, a <c>cardigan-lavender</c> cardigan, a long <c>skirt-teal</c> skirt and a
/// <c>apron-cream</c> shop apron (bib, skirt and waist tie). <b>Clyde</b> (1.43 m to the top of his cap, a little
/// stooped): a <c>knit-cap-teal</c> knit cap, a big <c>beard-white</c> beard, an <c>oilskin-mustard</c> oilskin coat,
/// <c>slate</c> trousers and a fishing rod held in his right hand and resting over his right shoulder (the
/// <c>Rod*</c> parts, which rise behind him past his height).
/// </para>
/// </summary>
public static class ResidentArt
{
    private static readonly IReadOnlyList<CropPart> MaggieParts = BuildMaggie();

    private static readonly IReadOnlyList<CropPart> ClydeParts = BuildClyde();

    /// <summary>The parts of <paramref name="id"/>'s model. Only Maggie and Clyde have one so far.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The Resident has no model yet, or the value is not a member.</exception>
    public static IReadOnlyList<CropPart> For(ResidentId id) => id switch
    {
        ResidentId.Maggie => MaggieParts,
        ResidentId.Clyde => ClydeParts,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "This resident has no model yet."),
    };

    private static List<CropPart> BuildMaggie()
    {
        var parts = new List<CropPart>();

        // Small shoes peeping out under the long skirt.
        AddPair(parts, Box("Shoe", 0.13f, 0.07f, 0.20f, 0.08f, 0.035f, 0.04f, "boots"));

        // The long teal skirt, flaring to the ankles, and the cream apron over its front.
        parts.Add(new CropPart(
            "Skirt", CropPartShape.Cylinder, 0.50f, 0.44f, 0.38f, 0f, 0.29f, 0f, 0f, 0f, 0f, "skirt-teal"));
        parts.Add(Box("ApronSkirt", 0.30f, 0.36f, 0.02f, 0f, 0.31f, 0.215f, "apron-cream", pitch: -7f));
        parts.Add(Box("ApronTie", 0.42f, 0.04f, 0.34f, 0f, 0.51f, 0f, "apron-cream"));

        // The cardigan: a compact torso, the apron's bib over its front, and short arms hanging a little out.
        parts.Add(Box("Cardigan", 0.38f, 0.28f, 0.30f, 0f, 0.66f, 0f, "cardigan-lavender"));
        parts.Add(Box("ApronBib", 0.22f, 0.18f, 0.02f, 0f, 0.65f, 0.155f, "apron-cream"));
        AddPair(parts, Box("Arm", 0.11f, 0.26f, 0.12f, 0.245f, 0.65f, 0f, "cardigan-lavender", roll: 6f));
        AddPair(parts, Box("Hand", 0.10f, 0.09f, 0.10f, 0.26f, 0.495f, 0.01f, "skin"));
        parts.Add(new CropPart(
            "Neck", CropPartShape.Cylinder, 0.12f, 0.06f, 0.12f, 0f, 0.82f, 0f, 0f, 0f, 0f, "skin"));

        // The head, a big block, its underside in her blue-lilac undertone.
        parts.Add(Box("Head", 0.36f, 0.30f, 0.34f, 0f, 1.00f, 0f, "skin"));
        parts.Add(Box("Chin", 0.32f, 0.03f, 0.30f, 0f, 0.865f, 0f, "undertone-blue-lilac"));
        parts.Add(Box("Nose", 0.045f, 0.045f, 0.03f, 0f, 0.975f, 0.185f, "skin"));

        // Round glasses: two lenses and the bridge, the eyes behind them showing as dots.
        AddPair(parts, new CropPart(
            "GlassesLens", CropPartShape.Cylinder, 0.09f, 0.012f, 0.09f, 0.075f, 1.02f, 0.176f, 90f, 0f, 0f,
            "glass-dull"));
        parts.Add(Box("GlassesBridge", 0.06f, 0.012f, 0.012f, 0f, 1.025f, 0.178f, "timber-trim"));
        AddPair(parts, Box("Eye", 0.035f, 0.045f, 0.01f, 0.075f, 1.02f, 0.186f, "navy"));

        // Lavender-silver hair: the top, the back, the sides, and the bun on the crown.
        parts.Add(Box("HairTop", 0.38f, 0.06f, 0.36f, 0f, 1.165f, -0.005f, "hair-lavender-silver"));
        parts.Add(Box("HairBack", 0.38f, 0.24f, 0.06f, 0f, 1.04f, -0.17f, "hair-lavender-silver"));
        AddPair(parts, Box("HairSide", 0.03f, 0.14f, 0.20f, 0.185f, 1.08f, -0.06f, "hair-lavender-silver"));
        parts.Add(new CropPart(
            "Bun", CropPartShape.Sphere, 0.18f, 0.16f, 0.18f, 0f, 1.24f, -0.06f, 0f, 0f, 0f, "hair-lavender-silver"));

        AddEars(parts, x: 0.20f, y: 1.03f, width: 0.07f, height: 0.14f);
        return parts;
    }

    private static List<CropPart> BuildClyde()
    {
        var parts = new List<CropPart>();

        // Boots and slate trousers.
        AddPair(parts, Box("Boot", 0.17f, 0.12f, 0.28f, 0.10f, 0.06f, 0.03f, "boots"));
        AddPair(parts, Box("Trouser", 0.17f, 0.30f, 0.20f, 0.10f, 0.27f, 0f, "slate"));

        // The mustard oilskin coat: a broad body, a flared hem and dark toggles down the front.
        parts.Add(Box("Coat", 0.52f, 0.50f, 0.38f, 0f, 0.66f, 0f, "oilskin-mustard"));
        parts.Add(Box("CoatHem", 0.56f, 0.10f, 0.42f, 0f, 0.44f, 0f, "oilskin-mustard"));
        for (var i = 0; i < 3; i++)
        {
            parts.Add(Box($"CoatToggle{i + 1}", 0.04f, 0.025f, 0.02f, 0f, 0.50f + 0.11f * i, 0.195f, "trunk"));
        }

        // The left arm hangs; the right reaches forward to hold the rod at his chest.
        parts.Add(Box("ArmLeft", 0.15f, 0.34f, 0.17f, 0.31f, 0.70f, 0f, "oilskin-mustard", roll: 6f));
        parts.Add(Box("HandLeft", 0.13f, 0.12f, 0.13f, 0.33f, 0.50f, 0f, "skin"));
        parts.Add(Box("ArmRight", 0.15f, 0.30f, 0.17f, -0.31f, 0.80f, 0.09f, "oilskin-mustard", pitch: -48f));
        parts.Add(Box("HandRight", 0.13f, 0.12f, 0.13f, -0.31f, 0.71f, 0.19f, "skin"));

        // The head, its underside in his blue undertone, the face mostly beard.
        parts.Add(Box("Head", 0.40f, 0.32f, 0.38f, 0f, 1.08f, 0f, "skin"));
        parts.Add(Box("Chin", 0.36f, 0.03f, 0.34f, 0f, 0.935f, 0f, "undertone-blue"));
        parts.Add(Box("Nose", 0.06f, 0.06f, 0.05f, 0f, 1.10f, 0.21f, "skin"));
        AddPair(parts, Box("Eye", 0.045f, 0.05f, 0.012f, 0.09f, 1.15f, 0.195f, "navy"));
        AddPair(parts, Box("Brow", 0.08f, 0.02f, 0.014f, 0.09f, 1.19f, 0.197f, "beard-white"));

        // The big white beard: a block over the jaw, sides up to the temples, a moustache and a point on the chest.
        parts.Add(Box("BeardMain", 0.42f, 0.24f, 0.12f, 0f, 0.97f, 0.16f, "beard-white"));
        AddPair(parts, Box("BeardSide", 0.05f, 0.18f, 0.22f, 0.195f, 1.04f, 0.06f, "beard-white"));
        parts.Add(Box("BeardMoustache", 0.24f, 0.05f, 0.04f, 0f, 1.065f, 0.225f, "beard-white"));
        parts.Add(new CropPart(
            "BeardPoint", CropPartShape.Prism, 0.32f, 0.16f, 0.10f, 0f, 0.79f, 0.19f, 0f, 0f, 180f, "beard-white"));

        // The teal knit cap: a folded cuff, the body over the crown and a rounded top.
        parts.Add(Box("CapCuff", 0.46f, 0.08f, 0.44f, 0f, 1.235f, -0.005f, "knit-cap-teal"));
        parts.Add(Box("CapBody", 0.43f, 0.14f, 0.41f, 0f, 1.33f, -0.01f, "knit-cap-teal"));
        parts.Add(Box("CapTop", 0.30f, 0.03f, 0.28f, 0f, 1.415f, -0.01f, "knit-cap-teal"));

        AddEars(parts, x: 0.215f, y: 1.12f, width: 0.08f, height: 0.15f);
        AddRod(parts);
        return parts;
    }

    /// <summary>
    /// The fishing rod in Clyde's right hand, rising back over his right shoulder (pitch −50°). Each part is placed
    /// along the rod's axis from the grip at the hand: centre = grip + t·(0, cos 50°, −sin 50°).
    /// </summary>
    private static void AddRod(List<CropPart> parts)
    {
        const float pitch = -50f;
        var up = MathF.Cos(50f * MathF.PI / 180f);
        var back = MathF.Sin(50f * MathF.PI / 180f);
        (float X, float Y, float Z) Along(float t) => (-0.31f, 0.71f + up * t, 0.19f - back * t);

        var grip = Along(0f);
        parts.Add(new CropPart(
            "RodGrip", CropPartShape.Cylinder, 0.05f, 0.22f, 0.05f, grip.X, grip.Y, grip.Z, pitch, 0f, 0f, "wood-cut"));
        var reel = Along(0.14f);
        parts.Add(new CropPart(
            "RodReel", CropPartShape.Cylinder, 0.09f, 0.04f, 0.09f, reel.X - 0.05f, reel.Y, reel.Z, pitch, 0f, 90f,
            "rock"));
        var pole = Along(0.95f);
        parts.Add(new CropPart(
            "RodPole", CropPartShape.Cylinder, 0.03f, 1.70f, 0.012f, pole.X, pole.Y, pole.Z, pitch, 0f, 0f, "trunk"));
    }

    /// <summary>Two pointed ears either side of the head, tipped out (roll) and back (pitch), mirrored in x.</summary>
    private static void AddEars(List<CropPart> parts, float x, float y, float width, float height)
    {
        AddPair(parts, new CropPart(
            "Ear", CropPartShape.Prism, width, height, 0.04f, x, y, -0.01f, -20f, 0f, -30f, "skin"));
    }

    /// <summary>
    /// Adds <paramref name="left"/> (drawn on the Resident's own left, +X) as <c>&lt;name&gt;Left</c> and its mirror in
    /// the YZ plane as <c>&lt;name&gt;Right</c>: x, yaw and roll negated.
    /// </summary>
    private static void AddPair(List<CropPart> parts, CropPart left)
    {
        parts.Add(left with { Name = left.Name + "Left" });
        parts.Add(left with
        {
            Name = left.Name + "Right",
            X = -left.X,
            YawDegrees = -left.YawDegrees,
            RollDegrees = -left.RollDegrees,
        });
    }

    private static CropPart Box(
        string name,
        float sizeX,
        float sizeY,
        float sizeZ,
        float x,
        float y,
        float z,
        string role,
        float pitch = 0f,
        float roll = 0f) =>
        new(name, CropPartShape.Box, sizeX, sizeY, sizeZ, x, y, z, pitch, 0f, roll, role);
}
