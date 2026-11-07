using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>The blacksmith's rebuild stages (design doc, Blacksmith; ART_DIRECTION §7.3).</summary>
public enum BlacksmithStage
{
    /// <summary>Stage 0: empty, a cold, dark forge.</summary>
    Empty,

    /// <summary>Stage 1: Mira reopens and the forge glows again: tool upgrades and Rosk pumps.</summary>
    Reopened,

    /// <summary>Stage 2: a smelting furnace on the side and more smoke: Groth.</summary>
    Forge,

    /// <summary>Stage 3: a teal weathervane and finished metal parts on display.</summary>
    MasterSmith,
}

/// <summary>
/// Mira's blacksmith, in the round, grown style of the concept art's "Round buildings" page ("A stone lower ring, a
/// glowing forge arch, an anvil, a tall stone chimney and a teal weathervane on the peak") and ART_DIRECTION §7.3 ("The
/// heaviest building: a stone lower storey, an open forge arch that glows orange, anvil, quench trough, hammer sign, big
/// stone chimney"). Pure C#, no Godot: the sandbox's builder merges these parts into one mesh under the shared palette
/// material. For now it is shown only in the art sandbox.
/// <para>
/// <b>Shape.</b> Building-local metres, origin at the drum's centre on the ground, the forge arch facing +Z. The drum is
/// 5.8 m across with 3.2 m walls, stone up to 1.5 m and mauve staves above, under a leaf cone reaching 3.8 m out to an
/// apex at 6.6 m. The forge arch is a deep stone surround built out from the front wall round a round-topped opening
/// 1.7 m wide, with a hearth of coals and flames in it. The big stone chimney rises from the ground at the back right
/// to 7.6 m, above the roof. An anvil stands at the front left, a stone quench trough at the front right, a round hammer
/// sign hangs beside the arch and a window sits on the left above the stone.
/// </para>
/// <para>
/// <b>Stages.</b> Empty: the staves grey, the roof wilted with gaps, the forge dark and its coals cold grey, no flames
/// or smoke, the trough dry, the window boarded, weeds, no sign. Reopened: alive, the coals and flames glowing (by day
/// and brighter at night), smoke from the chimney, water in the trough, the hammer sign; the window lit at night.
/// Forge: plus a domed stone smelting furnace on the right with a glowing mouth and its own chimney, and more smoke.
/// Master smith: plus a teal weathervane on the peak and a rack of finished metal parts on the left: pipes, a pump, a
/// sprinkler and plates.
/// </para>
/// Parts are named by group: <c>Stone*</c> and <c>Stave*</c> (the drum), <c>Forge*</c>, <c>Coal*</c>, <c>Flame*</c>,
/// <c>Anvil*</c>, <c>Trough*</c>, <c>Sign*</c>, <c>Chimney*</c>, <c>Smoke*</c>, <c>Furnace*</c>, <c>Rack*</c>,
/// <c>Vane*</c>, <c>Weed*</c>.
/// </summary>
public static class BlacksmithArt
{
    /// <summary>The drum's outer radius: 5.8 m across.</summary>
    public const float WallRadius = 2.9f;

    /// <summary>Where the stone storey ends and the staves begin.</summary>
    public const float StoneTo = 1.5f;

    /// <summary>The chimney's top: the tallest point of the building.</summary>
    public const float ChimneyTop = 7.6f;

    private const float ArchHalf = 0.85f;
    private const float ArchShoulder = 1.55f;
    private const float WindowAngle = -62f;
    private const float ChimneyAngle = 145f;

    private static readonly RoundBuilding Smithy = new("", Vector3.Zero, WallRadius, 3.2f, 3.8f, 6.6f);

    private static readonly Dictionary<BlacksmithStage, IReadOnlyList<CropPart>> Cache = new();
    private static readonly Dictionary<BlacksmithStage, IReadOnlyList<CropPart>> LightCache = new();

    /// <summary>The blacksmith at <paramref name="stage"/>. The same list for the same stage.</summary>
    public static IReadOnlyList<CropPart> For(BlacksmithStage stage)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(stage, out var parts))
            {
                parts = Build(stage);
                Cache[stage] = parts;
            }

            return parts;
        }
    }

    /// <summary>The warm window pane lit after dark: none while empty. The forge itself glows through its own parts.</summary>
    public static IReadOnlyList<CropPart> NightLights(BlacksmithStage stage)
    {
        lock (LightCache)
        {
            if (!LightCache.TryGetValue(stage, out var parts))
            {
                var lights = new List<CropPart>();
                if (stage != BlacksmithStage.Empty)
                {
                    Smithy.WindowLight(lights, "NightWindow", WindowAngle, 0.62f, 1.75f, 2.4f, roundTop: false);
                }

                parts = lights;
                LightCache[stage] = parts;
            }

            return parts;
        }
    }

    private static List<CropPart> Build(BlacksmithStage stage)
    {
        var parts = new List<CropPart>();
        var alive = stage != BlacksmithStage.Empty;
        Func<float, bool> life = _ => alive;

        Smithy.Drum(parts, 28, life, alive ? [] : [(9, true), (16, true), (22, true)], StoneTo);
        float[] ribs = [-110f, 100f, -160f];
        for (var i = 0; i < ribs.Length; i++)
        {
            Smithy.Rib(parts, i, ribs[i], alive);
        }

        (int, float, float)[] gaps = [(1, 25f, 55f), (2, -100f, -70f), (3, 60f, 90f)];
        Smithy.Roof(parts, life, gaps, crownAlive: alive);
        Smithy.Window(parts, "Window", WindowAngle, 0.62f, 1.75f, 2.4f, alive ? "wrist-screen" : "glass-dull", roundTop: false, mullions: 1);

        ForgeArch(parts, alive);
        Anvil(parts);
        Trough(parts, alive);
        Chimney(parts, alive ? (stage == BlacksmithStage.Reopened ? 3 : 5) : 0);

        if (!alive)
        {
            parts.Add(Smithy.Box("WindowBoard0", 0.85f, 0.13f, 0.03f, Smithy.OnWall(WindowAngle, 0f, 2.1f, 0.1f), "wall-grey", yaw: WindowAngle, roll: 25f));
            parts.Add(Smithy.Box("WindowBoard1", 0.85f, 0.13f, 0.03f, Smithy.OnWall(WindowAngle, 0f, 2.12f, 0.13f), "timber-side", yaw: WindowAngle, roll: -22f));
            Weeds(parts);
            return parts;
        }

        HammerSign(parts);
        if (stage is BlacksmithStage.Forge or BlacksmithStage.MasterSmith)
        {
            Furnace(parts);
        }

        if (stage == BlacksmithStage.MasterSmith)
        {
            Weathervane(parts);
            Rack(parts);
        }

        return parts;
    }

    // ---- the forge arch ----

    /// <summary>
    /// The forge: a deep stone surround built out from the front wall (a rectangular body under a round-topped stone
    /// arch with a keystone), the dark opening painted on its face, a stone hearth in front with a bed of coals, and,
    /// when the forge is lit, an orange glow at the back and flames on the coals. Cold, the glow and flames are gone and
    /// the coals are grey.
    /// </summary>
    private static void ForgeArch(List<CropPart> parts, bool lit)
    {
        const float frontZ = WallRadius + 0.32f;
        const float surround = 0.35f;
        var outer = 2f * (ArchHalf + surround);
        parts.Add(new CropPart("ForgeSurround", CropPartShape.Box, outer, ArchShoulder - 0.3f, 0.5f, 0f, (ArchShoulder + 0.3f) / 2f, WallRadius + 0.06f, 0f, 0f, 0f, "stone"));
        parts.Add(new CropPart("ForgeSurroundArch", CropPartShape.Cylinder, outer, 0.5f, outer, 0f, ArchShoulder, WallRadius + 0.06f, 90f, 0f, 0f, "stone", Sides: 24));
        parts.Add(new CropPart("ForgeKeystone", CropPartShape.Box, 0.3f, 0.34f, 0.54f, 0f, ArchShoulder + ArchHalf + surround - 0.12f, WallRadius + 0.07f, 0f, 0f, 0f, "rock"));
        foreach (var (name, x) in new[] { ("West", -1f), ("East", 1f) })
        {
            parts.Add(new CropPart($"ForgeQuoin{name}", CropPartShape.Box, 0.3f, 0.32f, 0.52f, x * (ArchHalf + surround - 0.15f), 0.75f, WallRadius + 0.07f, 0f, 0f, 0f, "rock"));
        }

        var dark = lit ? "boots" : "timber-trim";
        parts.Add(new CropPart("ForgeOpening", CropPartShape.Box, 2f * ArchHalf, ArchShoulder - 0.3f, 0.02f, 0f, (ArchShoulder + 0.3f) / 2f, frontZ, 0f, 0f, 0f, dark));
        parts.Add(new CropPart("ForgeOpeningArch", CropPartShape.Cylinder, 2f * ArchHalf, 0.02f, 2f * ArchHalf, 0f, ArchShoulder, frontZ, 90f, 0f, 0f, dark, Sides: 24));

        // The hearth: a low stone bench in the opening's mouth, with a bed of coals.
        parts.Add(new CropPart("ForgeHearth", CropPartShape.Box, 1.4f, 0.42f, 0.5f, 0f, 0.51f, frontZ + 0.2f, 0f, 0f, 0f, "stone"));
        for (var i = 0; i < 7; i++)
        {
            var x = -0.5f + 1f / 6f * i;
            var z = frontZ + 0.15f + 0.08f * (i % 2);
            var coal = new CropPart($"Coal{i}", CropPartShape.Sphere, 0.17f, 0.1f, 0.17f, x, 0.74f, z, 0f, 30f * i, 0f, lit ? (i % 3 == 0 ? "lantern" : "ember") : "stone", Sides: 6);
            parts.Add(lit ? coal with { Glow = 1f } : coal);
        }

        if (!lit) return;
        // The glow at the back of the forge, and flames licking up off the coals.
        parts.Add(new CropPart("ForgeGlow", CropPartShape.Cylinder, 1.3f, 0.02f, 1.3f, 0f, 1.25f, frontZ + 0.012f, 90f, 0f, 0f, "sunbell-orange", Glow: 0.7f, Sides: 24));
        (float X, float Height, float Width)[] flames = [(-0.32f, 0.36f, 0.16f), (-0.05f, 0.5f, 0.2f), (0.22f, 0.4f, 0.17f), (0.42f, 0.26f, 0.12f)];
        for (var i = 0; i < flames.Length; i++)
        {
            var (x, height, width) = flames[i];
            parts.Add(new CropPart($"Flame{i}", CropPartShape.Prism, width, height, 0.06f, x, 0.79f + height / 2f, frontZ + 0.2f, 0f, 12f * i, 0f, i % 2 == 0 ? "lantern" : "ember", Glow: 1f));
        }
    }

    // ---- outside ----

    /// <summary>The anvil at the front left: a dark iron block on a waist and foot, with a horn, and a hammer resting on it.</summary>
    private static void Anvil(List<CropPart> parts)
    {
        var at = Smithy.Radial(-38f, WallRadius + 1.15f, 0f);
        const float yaw = 52f;
        Vector3 On(float x, float y, float z) => at + PartBox.Rotate(new Vector3(x, y, z), 0f, yaw, 0f);
        var stump = On(0f, 0.18f, 0f);
        parts.Add(new CropPart("AnvilStump", CropPartShape.Cylinder, 0.52f, 0.36f, 0.48f, stump.X, stump.Y, stump.Z, 0f, yaw, 0f, "trunk", Sides: 10));
        var waist = On(0f, 0.46f, 0f);
        parts.Add(new CropPart("AnvilWaist", CropPartShape.Box, 0.22f, 0.2f, 0.18f, waist.X, waist.Y, waist.Z, 0f, yaw, 0f, "boots"));
        var face = On(0f, 0.62f, 0f);
        parts.Add(new CropPart("AnvilFace", CropPartShape.Box, 0.56f, 0.13f, 0.24f, face.X, face.Y, face.Z, 0f, yaw, 0f, "boots"));
        var horn = On(0.4f, 0.63f, 0f);
        parts.Add(new CropPart("AnvilHorn", CropPartShape.Prism, 0.22f, 0.26f, 0.1f, horn.X, horn.Y, horn.Z, 0f, yaw, -90f, "boots"));
        var hammer = On(-0.08f, 0.72f, 0.02f);
        parts.Add(new CropPart("AnvilHammerHead", CropPartShape.Box, 0.08f, 0.07f, 0.16f, hammer.X, hammer.Y, hammer.Z, 0f, yaw + 20f, 0f, "rock"));
        var handle = On(-0.08f, 0.71f, 0.2f);
        parts.Add(new CropPart("AnvilHammerHandle", CropPartShape.Box, 0.035f, 0.035f, 0.32f, handle.X, handle.Y, handle.Z, 0f, yaw + 20f, 0f, "timber"));
    }

    /// <summary>The quench trough at the front right: a long stone box, full of water once the forge is working.</summary>
    private static void Trough(List<CropPart> parts, bool full)
    {
        var at = Smithy.Radial(38f, WallRadius + 1.1f, 0f);
        const float yaw = -52f;
        parts.Add(new CropPart("Trough", CropPartShape.Box, 1.0f, 0.42f, 0.46f, at.X, 0.21f, at.Z, 0f, yaw, 0f, "stone"));
        parts.Add(new CropPart("TroughRim", CropPartShape.Box, 1.06f, 0.05f, 0.52f, at.X, 0.44f, at.Z, 0f, yaw, 0f, "rock"));
        parts.Add(new CropPart("TroughInside", CropPartShape.Box, 0.86f, 0.02f, 0.34f, at.X, 0.465f, at.Z, 0f, yaw, 0f, full ? "lake-deep" : "timber-trim"));
    }

    /// <summary>The big stone chimney at the back right, from the ground to above the roof, with smoke when lit.</summary>
    private static void Chimney(List<CropPart> parts, int puffs)
    {
        var at = Smithy.Radial(ChimneyAngle, WallRadius - 0.1f, 0f);
        parts.Add(new CropPart("ChimneyStack", CropPartShape.Box, 1.0f, ChimneyTop, 1.0f, at.X, ChimneyTop / 2f, at.Z, 0f, ChimneyAngle, 0f, "stone"));
        for (var i = 0; i < 4; i++)
        {
            var y = 1.4f + 1.6f * i;
            parts.Add(new CropPart($"ChimneyBand{i}", CropPartShape.Box, 1.06f, 0.12f, 1.06f, at.X, y, at.Z, 0f, ChimneyAngle, 0f, "rock"));
        }

        parts.Add(new CropPart("ChimneyCap", CropPartShape.Box, 1.24f, 0.16f, 1.24f, at.X, ChimneyTop + 0.08f, at.Z, 0f, ChimneyAngle, 0f, "rock"));
        parts.Add(new CropPart("ChimneyFlue", CropPartShape.Box, 0.6f, 0.02f, 0.6f, at.X, ChimneyTop + 0.17f, at.Z, 0f, ChimneyAngle, 0f, "boots"));
        for (var i = 0; i < puffs; i++)
        {
            var size = 0.35f + 0.1f * i;
            var drift = 0.2f * i;
            parts.Add(new CropPart($"Smoke{i}", CropPartShape.Sphere, size, size * 0.85f, size, at.X + drift, ChimneyTop + 0.5f + 0.5f * i, at.Z - drift * 0.4f, 0f, 25f * i, 0f, "snow", Sides: 8));
        }
    }

    /// <summary>The round hammer sign hanging from a bracket beside the forge arch.</summary>
    private static void HammerSign(List<CropPart> parts)
    {
        const float angle = 30f;
        parts.Add(Smithy.Beam("SignBracket", 0.06f, 0.06f, Smithy.OnWall(angle, 0f, 2.6f, 0.02f), Smithy.OnWall(angle, 0f, 2.6f, 0.62f), "boots"));
        foreach (var (name, along) in new[] { ("SignChainA", -0.2f), ("SignChainB", 0.2f) })
        {
            parts.Add(Smithy.Beam(name, 0.02f, 0.02f, Smithy.OnWall(angle, 0f, 2.6f, 0.55f + along), Smithy.OnWall(angle, 0f, 2.35f, 0.55f + along), "rock"));
        }

        var centre = Smithy.OnWall(angle, 0f, 2.13f, 0.55f);
        var yaw = angle + 90f;
        parts.Add(new CropPart("SignRim", CropPartShape.Cylinder, 0.78f, 0.06f, 0.78f, centre.X, centre.Y, centre.Z, 90f, yaw, 0f, "boots", Sides: 16));
        parts.Add(new CropPart("SignFace", CropPartShape.Cylinder, 0.66f, 0.08f, 0.66f, centre.X, centre.Y, centre.Z, 90f, yaw, 0f, "wood-cut", Sides: 16));
        var face = PartBox.Rotate(Vector3.UnitZ, 0f, yaw, 0f);
        foreach (var (side, name) in new[] { (1f, "A"), (-1f, "B") })
        {
            // A hammer, head up, painted on each face.
            var at = centre + face * (0.045f * side);
            Vector3 On(float x, float y) => at + PartBox.Rotate(new Vector3(x, y, 0f), 0f, yaw, 0f);
            var head = On(0f, 0.12f);
            parts.Add(new CropPart($"SignHammer{name}Head", CropPartShape.Box, 0.3f, 0.12f, 0.012f, head.X, head.Y, head.Z, 0f, yaw, -18f, "boots"));
            var handle = On(0.05f, -0.07f);
            parts.Add(new CropPart($"SignHammer{name}Handle", CropPartShape.Box, 0.05f, 0.32f, 0.012f, handle.X, handle.Y, handle.Z, 0f, yaw, -18f, "timber"));
        }
    }

    /// <summary>
    /// The smelting furnace on the right: a domed stone kiln with a glowing mouth facing out, its own short chimney and
    /// smoke.
    /// </summary>
    private static void Furnace(List<CropPart> parts)
    {
        const float angle = 75f;
        var at = Smithy.Radial(angle, WallRadius + 0.95f, 0f);
        parts.Add(new CropPart("FurnaceBody", CropPartShape.Cylinder, 1.6f, 1.1f, 1.5f, at.X, 0.55f, at.Z, 0f, angle, 0f, "stone", Sides: 14));
        parts.Add(new CropPart("FurnaceDome", CropPartShape.Sphere, 1.5f, 1.1f, 1.5f, at.X, 1.1f, at.Z, 0f, angle, 0f, "stone", Sides: 14));
        for (var i = 0; i < 3; i++)
        {
            parts.Add(new CropPart($"FurnaceBand{i}", CropPartShape.Cylinder, 1.64f - 0.02f * i, 0.08f, 1.64f - 0.02f * i, at.X, 0.25f + 0.35f * i, at.Z, 0f, 0f, 0f, "rock", Sides: 14));
        }

        var outward = Smithy.Radial(angle, 1f, 0f) - Smithy.Centre;
        var mouth = at + outward * 0.78f + new Vector3(0f, 0.5f, 0f);
        parts.Add(new CropPart("FurnaceMouthFrame", CropPartShape.Box, 0.62f, 0.52f, 0.12f, mouth.X, mouth.Y, mouth.Z, 0f, angle, 0f, "rock"));
        var glow = mouth + outward * 0.07f;
        parts.Add(new CropPart("FurnaceMouth", CropPartShape.Box, 0.44f, 0.36f, 0.02f, glow.X, glow.Y, glow.Z, 0f, angle, 0f, "ember", Glow: 1f));
        var stack = at - outward * 0.3f;
        parts.Add(new CropPart("FurnaceChimney", CropPartShape.Cylinder, 0.42f, 1.4f, 0.36f, stack.X, 2.2f, stack.Z, 0f, 0f, 0f, "stone", Sides: 8));
        for (var i = 0; i < 3; i++)
        {
            var size = 0.28f + 0.08f * i;
            parts.Add(new CropPart($"FurnaceSmoke{i}", CropPartShape.Sphere, size, size * 0.85f, size, stack.X + 0.12f * i, 3.1f + 0.4f * i, stack.Z, 0f, 0f, 0f, "snow", Sides: 8));
        }
    }

    /// <summary>The teal weathervane on the peak: a pole, an arrow with a tail, and a compass cross.</summary>
    private static void Weathervane(List<CropPart> parts)
    {
        const float bottom = 6.75f;
        parts.Add(new CropPart("VanePole", CropPartShape.Cylinder, 0.06f, 1.1f, 0.05f, 0f, bottom + 0.55f, 0f, 0f, 0f, 0f, "rock"));
        parts.Add(new CropPart("VaneCrossA", CropPartShape.Box, 0.6f, 0.03f, 0.03f, 0f, bottom + 0.7f, 0f, 0f, 0f, 0f, "rock"));
        parts.Add(new CropPart("VaneCrossB", CropPartShape.Box, 0.03f, 0.03f, 0.6f, 0f, bottom + 0.7f, 0f, 0f, 0f, 0f, "rock"));
        parts.Add(new CropPart("VaneArrow", CropPartShape.Box, 0.9f, 0.05f, 0.03f, 0f, bottom + 1.0f, 0f, 0f, 30f, 0f, "foliage"));
        var head = PartBox.Rotate(new Vector3(0.5f, 0f, 0f), 0f, 30f, 0f);
        parts.Add(new CropPart("VaneHead", CropPartShape.Prism, 0.2f, 0.18f, 0.03f, head.X, bottom + 1.0f, head.Z, 0f, 30f, -90f, "foliage"));
        var tail = PartBox.Rotate(new Vector3(-0.45f, 0f, 0f), 0f, 30f, 0f);
        parts.Add(new CropPart("VaneTail", CropPartShape.Box, 0.24f, 0.2f, 0.03f, tail.X, bottom + 1.03f, tail.Z, 0f, 30f, 0f, "grass-shade"));
    }

    /// <summary>
    /// The rack of finished metal parts on the left: two posts and a bar hung with pipes, a pump with its wheel, a
    /// sprinkler and plates leaning against it.
    /// </summary>
    private static void Rack(List<CropPart> parts)
    {
        const float angle = -80f;
        var at = Smithy.Radial(angle, WallRadius + 1.0f, 0f);
        const float yaw = angle + 90f;
        Vector3 On(float x, float y, float z) => at + PartBox.Rotate(new Vector3(x, y, z), 0f, yaw, 0f);
        foreach (var (name, x) in new[] { ("RackPostA", -0.75f), ("RackPostB", 0.75f) })
        {
            var post = On(x, 0.8f, 0f);
            parts.Add(new CropPart(name, CropPartShape.Cylinder, 0.1f, 1.6f, 0.09f, post.X, post.Y, post.Z, 0f, 0f, 0f, "timber-side"));
        }

        var bar = On(0f, 1.55f, 0f);
        parts.Add(new CropPart("RackBar", CropPartShape.Box, 1.7f, 0.08f, 0.08f, bar.X, bar.Y, bar.Z, 0f, yaw, 0f, "timber"));
        (string Name, float X, float Length, string Role)[] pipes = [("RackPipeA", -0.45f, 0.9f, "ship-metal"), ("RackPipeB", -0.15f, 0.7f, "rosk"), ("RackPipeC", 0.15f, 0.8f, "groth")];
        foreach (var (name, x, length, role) in pipes)
        {
            var pipe = On(x, 1.5f - length / 2f, 0.05f);
            parts.Add(new CropPart(name, CropPartShape.Cylinder, 0.1f, length, 0.1f, pipe.X, pipe.Y, pipe.Z, 0f, 0f, 0f, role, Sides: 8));
        }

        // The pump: a squat cylinder on the ground with a wheel on its side and a spout.
        var pump = On(0.45f, 0.3f, 0.3f);
        parts.Add(new CropPart("RackPump", CropPartShape.Cylinder, 0.42f, 0.6f, 0.38f, pump.X, pump.Y, pump.Z, 0f, 0f, 0f, "rosk", Sides: 10));
        var wheel = On(0.45f, 0.38f, 0.53f);
        parts.Add(new CropPart("RackPumpWheel", CropPartShape.Cylinder, 0.36f, 0.04f, 0.36f, wheel.X, wheel.Y, wheel.Z, 90f, yaw, 0f, "boots", Sides: 10));
        var spout = On(0.72f, 0.45f, 0.3f);
        parts.Add(new CropPart("RackPumpSpout", CropPartShape.Box, 0.22f, 0.07f, 0.07f, spout.X, spout.Y, spout.Z, 0f, yaw, 0f, "rosk"));

        // A sprinkler: a short post with two arms and a cap, ready for the farm.
        var sprinkler = On(-0.45f, 0.18f, 0.35f);
        parts.Add(new CropPart("RackSprinklerPost", CropPartShape.Cylinder, 0.08f, 0.36f, 0.07f, sprinkler.X, sprinkler.Y, sprinkler.Z, 0f, 0f, 0f, "ship-metal", Sides: 8));
        parts.Add(new CropPart("RackSprinklerArms", CropPartShape.Box, 0.42f, 0.04f, 0.04f, sprinkler.X, 0.38f, sprinkler.Z, 0f, yaw + 30f, 0f, "ship-metal"));
        parts.Add(new CropPart("RackSprinklerCap", CropPartShape.Sphere, 0.1f, 0.08f, 0.1f, sprinkler.X, 0.42f, sprinkler.Z, 0f, 0f, 0f, "rosk", Sides: 8));

        // Plates leaning against the posts.
        (string Name, float X, string Role)[] plates = [("RackPlateA", -0.75f, "groth"), ("RackPlateB", 0.75f, "ship-metal")];
        foreach (var (name, x, role) in plates)
        {
            var plate = On(x, 0.4f, -0.16f);
            parts.Add(new CropPart(name, CropPartShape.Box, 0.5f, 0.75f, 0.04f, plate.X, plate.Y, plate.Z, -14f, yaw, 0f, role));
        }
    }

    private static void Weeds(List<CropPart> parts)
    {
        float[] weeds = [-150f, -95f, -60f, 25f, 70f, 115f, 175f];
        for (var i = 0; i < weeds.Length; i++)
        {
            var at = Smithy.Radial(weeds[i], WallRadius + 0.25f, 0f);
            var height = 0.2f + 0.05f * (i % 3);
            parts.Add(new CropPart($"Weed{i}A", CropPartShape.Prism, 0.18f, height, 0.02f, at.X, height / 2f + 0.015f, at.Z, 0f, weeds[i] + 15f * i, 8f, i % 2 == 0 ? "foliage" : "grass-shade"));
            parts.Add(new CropPart($"Weed{i}B", CropPartShape.Prism, 0.16f, height * 0.8f, 0.02f, at.X + 0.03f, height * 0.4f + 0.015f, at.Z, 0f, weeds[i] + 15f * i + 90f, -8f, i % 2 == 0 ? "grass-shade" : "foliage"));
        }
    }
}
