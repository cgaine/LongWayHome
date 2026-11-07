using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>The carpenter's rebuild stages (design doc, Carpenter; ART_DIRECTION §7.3).</summary>
public enum CarpenterStage
{
    /// <summary>Stage 0: empty and wilted, waiting for its first rebuild, with a rebuild sign out front.</summary>
    Empty,

    /// <summary>Stage 1: Tom reopens: planks and sealed planks, fences and chests; the Hollis family moves in upstairs.</summary>
    Reopened,

    /// <summary>Stage 2: a side shed with a workbench and sap barrels that glow amber (sealed planks).</summary>
    Workshop,

    /// <summary>Stage 3: timber frames for new homes and stacks of sealed planks.</summary>
    Builder,
}

/// <summary>
/// Tom's carpenter, in the round, grown style of the concept art's "Round buildings" page ("Two drums grown together: the
/// workshop with braced double doors, and a taller drum behind for the family upstairs") and ART_DIRECTION §7.3 ("Big
/// braced double doors, plank stacks, saw sign"). Pure C#, no Godot: the sandbox's builder merges these parts into one
/// mesh under the shared palette material. For now it is shown only in the art sandbox.
/// <para>
/// <b>Shape.</b> Building-local metres, origin at the workshop drum's centre on the ground, the doors facing +Z. The
/// workshop drum is 5.4 m across with 3 m walls under a leaf cone reaching 3.6 m out to an apex at 6.5 m; its braced
/// double doors (1.9 m wide, 2.1 m tall) face the front, a small window on each side. The family's drum, 3.6 m across
/// with 4.4 m walls (two storeys, a window over a window) and its apex at 7.6 m, is grown into the workshop behind its
/// west side. A round saw sign hangs beside the doors and a plank stack leans by them.
/// </para>
/// <para>
/// <b>Stages.</b> Empty: both drums run-down (grey staves with gaps, wilted roofs, dead twigs), faded doors with a bar
/// across, boarded windows, weeds, no sign or planks. Reopened: alive, ember doors, the saw sign and a plank stack; the
/// windows lit at night. Workshop: plus an open shed on the east side (four living posts under a leaf roof) over a
/// workbench with a saw and planks, and three sap barrels whose lids and bands glow amber. Builder: plus the timber frame
/// of a new round home on the west front (posts, a ring beam, rafters to a peak) and two stacks of sealed planks with
/// amber-sealed ends.
/// </para>
/// Parts are named by group: the workshop's carry no prefix (<c>Stave*</c>, <c>Leaf*</c>, <c>Door*</c>,
/// <c>Window*</c>, <c>Sign*</c>, <c>Planks*</c>, <c>Shed*</c>, <c>Bench*</c>, <c>Barrel*</c>, <c>Frame*</c>,
/// <c>Sealed*</c>, <c>Weed*</c>); the family's drum starts <c>House</c>.
/// </summary>
public static class CarpenterArt
{
    /// <summary>The workshop drum's outer radius: 5.4 m across.</summary>
    public const float WallRadius = 2.7f;

    /// <summary>The family drum's apex: the tallest point of the building.</summary>
    public const float HouseApexY = 7.6f;

    private const float DoorWidth = 1.9f;
    private const float DoorHeight = 2.1f;
    private const float WindowAngle = 62f;

    private static readonly RoundBuilding Workshop = new("", Vector3.Zero, WallRadius, 3.0f, 3.6f, 6.5f);

    /// <summary>The family's taller drum, grown into the workshop behind its west side.</summary>
    private static readonly RoundBuilding House = new("House", new Vector3(-2.5f, 0f, -2.4f), 1.8f, 4.4f, 2.5f, HouseApexY);

    private static readonly Dictionary<CarpenterStage, IReadOnlyList<CropPart>> Cache = new();
    private static readonly Dictionary<CarpenterStage, IReadOnlyList<CropPart>> LightCache = new();

    /// <summary>The carpenter at <paramref name="stage"/>. The same list for the same stage.</summary>
    public static IReadOnlyList<CropPart> For(CarpenterStage stage)
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

    /// <summary>The warm window panes lit after dark: none while empty, the workshop's and the family's windows after.</summary>
    public static IReadOnlyList<CropPart> NightLights(CarpenterStage stage)
    {
        lock (LightCache)
        {
            if (!LightCache.TryGetValue(stage, out var parts))
            {
                var lights = new List<CropPart>();
                if (stage != CarpenterStage.Empty)
                {
                    foreach (var (name, angle) in new[] { ("NightWindowWest", -WindowAngle), ("NightWindowEast", WindowAngle) })
                    {
                        Workshop.WindowLight(lights, name, angle, 0.62f, 0.95f, 1.6f, roundTop: false);
                    }

                    House.WindowLight(lights, "NightWindowLow", HouseWindowAngle, 0.62f, 0.95f, 1.6f, roundTop: false);
                    House.WindowLight(lights, "NightWindowHigh", HouseWindowAngle, 0.62f, 2.65f, 3.3f, roundTop: false);
                }

                parts = lights;
                LightCache[stage] = parts;
            }

            return parts;
        }
    }

    /// <summary>The family drum's windows face its free side, west of the front, clear of the workshop's roof.</summary>
    private const float HouseWindowAngle = -55f;

    private static List<CropPart> Build(CarpenterStage stage)
    {
        var parts = new List<CropPart>();
        var alive = stage != CarpenterStage.Empty;
        Func<float, bool> life = _ => alive;

        (int, bool)[] missing = alive ? [] : [(5, false), (11, true), (17, false)];
        Workshop.Drum(parts, 26, life, missing);
        House.Drum(parts, 18, life, alive ? [] : [(9, true)]);
        float[] ribs = [-100f, 100f, 150f];
        for (var i = 0; i < ribs.Length; i++)
        {
            Workshop.Rib(parts, i, ribs[i], alive);
        }

        // The family drum's ribs stand on its free sides, outside the workshop.
        House.Rib(parts, 0, -110f, alive);
        House.Rib(parts, 1, 160f, alive);
        (int, float, float)[] gaps = [(1, 20f, 50f), (2, -120f, -90f), (3, 60f, 95f)];
        Workshop.Roof(parts, life, gaps, crownAlive: alive);
        House.Roof(parts, life, [(1, -40f, -10f), (2, 100f, 130f)], crownAlive: alive);

        DoubleDoors(parts, alive);
        foreach (var (name, angle) in new[] { ("WindowWest", -WindowAngle), ("WindowEast", WindowAngle) })
        {
            Workshop.Window(parts, name, angle, 0.62f, 0.95f, 1.6f, alive ? "wrist-screen" : "glass-dull", roundTop: false, mullions: 1);
        }

        House.Window(parts, "WindowLow", HouseWindowAngle, 0.62f, 0.95f, 1.6f, alive ? "wrist-screen" : "glass-dull", roundTop: false, mullions: 1);
        House.Window(parts, "WindowHigh", HouseWindowAngle, 0.62f, 2.65f, 3.3f, alive ? "wrist-screen" : "glass-dull", roundTop: false, mullions: 1);

        if (!alive)
        {
            Boards(parts);
            Weeds(parts);
            return parts;
        }

        SawSign(parts);
        PlankStack(parts, "Planks", Workshop.Radial(42f, WallRadius + 0.7f, 0f), 42f + 90f, sealed_: false);

        if (stage is CarpenterStage.Workshop or CarpenterStage.Builder)
        {
            Shed(parts);
        }

        if (stage == CarpenterStage.Builder)
        {
            NewHomeFrame(parts);
            PlankStack(parts, "SealedA", new Vector3(3.4f, 0f, 3.3f), 10f, sealed_: true);
            PlankStack(parts, "SealedB", new Vector3(4.5f, 0f, 2.6f), 35f, sealed_: true);
        }

        return parts;
    }

    // ---- the doors ----

    /// <summary>
    /// Big braced double doors at the front: a timber-trim frame, two leaves with a Z of bracing each, ring handles; ember
    /// when alive, faded and barred across when empty. The doors are flat, so the frame stands proud of the curved wall.
    /// </summary>
    private static void DoubleDoors(List<CropPart> parts, bool alive)
    {
        const float bottom = 0.3f;
        var role = alive ? "ember" : "door-faded";
        var (frameZ, doorZ) = (WallRadius + 0.02f, WallRadius + 0.08f);
        parts.Add(new CropPart("DoorFrame", CropPartShape.Box, DoorWidth + 0.3f, DoorHeight - bottom + 0.2f, 0.32f, 0f, (bottom + DoorHeight + 0.2f) / 2f, frameZ - 0.12f, 0f, 0f, 0f, "timber-trim"));
        parts.Add(new CropPart("DoorLintel", CropPartShape.Box, DoorWidth + 0.5f, 0.2f, 0.36f, 0f, DoorHeight + 0.2f, frameZ - 0.1f, 0f, 0f, 0f, "trunk"));
        foreach (var (name, side) in new[] { ("West", -1f), ("East", 1f) })
        {
            var x = side * DoorWidth / 4f;
            var leaf = alive ? role : (side < 0 ? "door-faded" : "wall-grey");
            parts.Add(new CropPart($"Door{name}", CropPartShape.Box, DoorWidth / 2f - 0.03f, DoorHeight - bottom, 0.06f, x, (bottom + DoorHeight) / 2f, doorZ, 0f, 0f, 0f, leaf));
            var braceRole = alive ? "timber-trim" : "timber-grain";
            foreach (var (brace, y) in new[] { ("Top", DoorHeight - 0.25f), ("Bottom", bottom + 0.25f) })
            {
                parts.Add(new CropPart($"Door{name}Rail{brace}", CropPartShape.Box, DoorWidth / 2f - 0.1f, 0.1f, 0.03f, x, y, doorZ + 0.045f, 0f, 0f, 0f, braceRole));
            }

            // The diagonal brace between the rails.
            var length = MathF.Sqrt(MathF.Pow(DoorWidth / 2f - 0.2f, 2) + MathF.Pow(DoorHeight - bottom - 0.5f, 2));
            var tilt = MathF.Atan2(DoorWidth / 2f - 0.2f, DoorHeight - bottom - 0.5f) * 180f / MathF.PI;
            parts.Add(new CropPart($"Door{name}Brace", CropPartShape.Box, 0.1f, length, 0.03f, x, (bottom + DoorHeight) / 2f, doorZ + 0.045f, 0f, 0f, side * tilt, braceRole));
            parts.Add(new CropPart($"Door{name}Ring", CropPartShape.Cylinder, 0.14f, 0.025f, 0.14f, side * 0.14f, 1.15f, doorZ + 0.06f, 90f, 0f, 0f, "rock", Sides: 10));
        }

        parts.Add(new CropPart("DoorStep", CropPartShape.Box, DoorWidth + 0.5f, 0.14f, 0.45f, 0f, 0.07f, WallRadius + 0.3f, 0f, 0f, 0f, "stone"));
        if (!alive)
        {
            parts.Add(new CropPart("DoorBar", CropPartShape.Box, DoorWidth + 0.4f, 0.16f, 0.06f, 0f, 1.2f, doorZ + 0.08f, 0f, 0f, -4f, "timber-side"));
        }
    }

    // ---- outside: sign, planks, shed, frames ----

    /// <summary>The round saw sign hanging from a bracket to the east of the doors.</summary>
    private static void SawSign(List<CropPart> parts)
    {
        const float angle = 30f;
        parts.Add(Workshop.Beam("SignBracket", 0.06f, 0.06f, Workshop.OnWall(angle, 0f, 2.55f, 0.02f), Workshop.OnWall(angle, 0f, 2.55f, 0.62f), "trunk"));
        foreach (var (name, along) in new[] { ("SignChainA", -0.2f), ("SignChainB", 0.2f) })
        {
            parts.Add(Workshop.Beam(name, 0.02f, 0.02f, Workshop.OnWall(angle, 0f, 2.55f, 0.55f + along), Workshop.OnWall(angle, 0f, 2.3f, 0.55f + along), "rock"));
        }

        var centre = Workshop.OnWall(angle, 0f, 2.08f, 0.55f);
        var yaw = angle + 90f;
        parts.Add(new CropPart("SignRim", CropPartShape.Cylinder, 0.78f, 0.06f, 0.78f, centre.X, centre.Y, centre.Z, 90f, yaw, 0f, "timber-trim", Sides: 16));
        parts.Add(new CropPart("SignFace", CropPartShape.Cylinder, 0.66f, 0.08f, 0.66f, centre.X, centre.Y, centre.Z, 90f, yaw, 0f, "wood-cut", Sides: 16));
        var face = PartBox.Rotate(Vector3.UnitZ, 0f, yaw, 0f);
        foreach (var (side, name) in new[] { (1f, "A"), (-1f, "B") })
        {
            // A saw: a pale blade with a row of teeth, and a timber handle, painted on each face.
            var at = centre + face * (0.045f * side);
            Vector3 On(float x, float y) => at + PartBox.Rotate(new Vector3(x, y, 0f), 0f, yaw, 0f);
            var blade = On(0.04f, 0.02f);
            parts.Add(new CropPart($"SignSaw{name}Blade", CropPartShape.Box, 0.36f, 0.1f, 0.012f, blade.X, blade.Y, blade.Z, 0f, yaw, -12f, "ship-metal"));
            for (var t = 0; t < 4; t++)
            {
                var tooth = On(-0.1f + 0.08f * t, -0.05f + 0.017f * t);
                parts.Add(new CropPart($"SignSaw{name}Tooth{t}", CropPartShape.Prism, 0.05f, 0.04f, 0.012f, tooth.X, tooth.Y, tooth.Z, 0f, yaw, 168f, "ship-metal"));
            }

            var handle = On(-0.19f, 0.06f);
            parts.Add(new CropPart($"SignSaw{name}Handle", CropPartShape.Box, 0.1f, 0.12f, 0.014f, handle.X, handle.Y, handle.Z, 0f, yaw, -12f, "timber-side"));
        }
    }

    /// <summary>
    /// A stack of five planks at <paramref name="at"/> turned to <paramref name="yaw"/>: plain timber in alternating tones,
    /// or sealed, with an amber strip along each top and amber ends.
    /// </summary>
    private static void PlankStack(List<CropPart> parts, string name, Vector3 at, float yaw, bool sealed_)
    {
        for (var i = 0; i < 5; i++)
        {
            var y = 0.06f + 0.11f * i;
            var offset = PartBox.Rotate(new Vector3(0.03f * (i % 2 * 2 - 1), 0f, 0.02f * (i % 3 - 1)), 0f, yaw, 0f);
            var centre = at + offset + new Vector3(0f, y, 0f);
            var role = sealed_ ? "timber" : (i % 2 == 0 ? "wood-cut" : "timber");
            parts.Add(new CropPart($"{name}{i}", CropPartShape.Box, 1.6f, 0.1f, 0.26f, centre.X, centre.Y, centre.Z, 0f, yaw + 2f * (i % 3 - 1), 0f, role));
            if (!sealed_) continue;
            var top = centre + new Vector3(0f, 0.052f, 0f);
            parts.Add(new CropPart($"{name}{i}Seal", CropPartShape.Box, 1.5f, 0.008f, 0.07f, top.X, top.Y, top.Z, 0f, yaw + 2f * (i % 3 - 1), 0f, "lantern"));
            foreach (var (end, along) in new[] { ("A", -0.805f), ("B", 0.805f) })
            {
                var cap = centre + PartBox.Rotate(new Vector3(along, 0f, 0f), 0f, yaw + 2f * (i % 3 - 1), 0f);
                parts.Add(new CropPart($"{name}{i}End{end}", CropPartShape.Box, 0.012f, 0.1f, 0.26f, cap.X, cap.Y, cap.Z, 0f, yaw + 2f * (i % 3 - 1), 0f, "lantern"));
            }
        }
    }

    /// <summary>
    /// The open shed on the east side: four living posts under a sloping leaf roof, over a workbench with a saw and
    /// planks on it, and three sap barrels whose lids and bands glow amber at night.
    /// </summary>
    private static void Shed(List<CropPart> parts)
    {
        var centre = new Vector3(4.1f, 0f, 0.4f);
        const float width = 2.6f;
        const float depth = 2.2f;
        (string Name, float X, float Z, float Height)[] posts =
        [
            ("ShedPostFrontWest", -width / 2f, depth / 2f, 2.3f), ("ShedPostFrontEast", width / 2f, depth / 2f, 2.1f),
            ("ShedPostBackWest", -width / 2f, -depth / 2f, 2.5f), ("ShedPostBackEast", width / 2f, -depth / 2f, 2.3f),
        ];
        foreach (var (name, x, z, height) in posts)
        {
            parts.Add(new CropPart(name, CropPartShape.Cylinder, 0.2f, height, 0.16f, centre.X + x, height / 2f, centre.Z + z, 0f, 25f, 0f, "trunk"));
            parts.Add(new CropPart($"{name}Bud", CropPartShape.Sphere, 0.28f, 0.2f, 0.28f, centre.X + x, height + 0.08f, centre.Z + z, 0f, 0f, 0f, "roof-tips"));
        }

        // The roof: overlapping leaves sloping down to the east and front, with a fringe of tips.
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                var x = -width / 2f + 0.15f + 0.78f * col;
                var z = depth / 2f + 0.15f - 0.85f * row;
                var y = 2.35f + 0.1f * row - 0.06f * col + 0.012f * ((row + col) % 2);
                parts.Add(new CropPart($"ShedLeaf{row}_{col}", CropPartShape.Box, 0.95f, 0.04f, 1.0f, centre.X + x, y, centre.Z + z, -8f, 0f, 4f,
                    (row + col) % 2 == 0 ? "foliage" : "roof-rows"));
            }
        }

        for (var i = 0; i < 9; i++)
        {
            var x = -width / 2f - 0.1f + 0.36f * i;
            parts.Add(new CropPart($"ShedTip{i}", CropPartShape.Prism, 0.26f, 0.24f, 0.03f, centre.X + x, 2.2f - 0.025f * i, centre.Z + depth / 2f + 0.62f, 0f, 0f, 180f, "roof-tips", Glow: 0.9f));
        }

        // The workbench: a thick top on four legs, a saw and two planks on it, a vice at one end.
        var bench = centre + new Vector3(0f, 0f, -0.3f);
        parts.Add(new CropPart("BenchTop", CropPartShape.Box, 1.6f, 0.1f, 0.6f, bench.X, 0.85f, bench.Z, 0f, 0f, 0f, "timber"));
        foreach (var (name, x, z) in new[] { ("BenchLegA", -0.7f, -0.24f), ("BenchLegB", 0.7f, -0.24f), ("BenchLegC", -0.7f, 0.24f), ("BenchLegD", 0.7f, 0.24f) })
        {
            parts.Add(new CropPart(name, CropPartShape.Box, 0.1f, 0.8f, 0.1f, bench.X + x, 0.4f, bench.Z + z, 0f, 0f, 0f, "timber-side"));
        }

        parts.Add(new CropPart("BenchShelf", CropPartShape.Box, 1.45f, 0.05f, 0.5f, bench.X, 0.25f, bench.Z, 0f, 0f, 0f, "timber-side"));
        parts.Add(new CropPart("BenchPlankA", CropPartShape.Box, 1.2f, 0.06f, 0.22f, bench.X - 0.1f, 0.93f, bench.Z - 0.1f, 0f, 4f, 0f, "wood-cut"));
        parts.Add(new CropPart("BenchPlankB", CropPartShape.Box, 1.0f, 0.06f, 0.2f, bench.X, 0.99f, bench.Z - 0.08f, 0f, -3f, 0f, "timber"));
        parts.Add(new CropPart("BenchSawBlade", CropPartShape.Box, 0.5f, 0.012f, 0.14f, bench.X + 0.35f, 0.91f, bench.Z + 0.17f, 0f, 20f, 0f, "ship-metal"));
        parts.Add(new CropPart("BenchSawHandle", CropPartShape.Box, 0.14f, 0.04f, 0.12f, bench.X + 0.08f, 0.92f, bench.Z + 0.08f, 0f, 20f, 0f, "timber-side"));
        parts.Add(new CropPart("BenchVice", CropPartShape.Box, 0.16f, 0.16f, 0.2f, bench.X - 0.75f, 0.95f, bench.Z + 0.25f, 0f, 0f, 0f, "rock"));

        // Sap barrels: timber staves (a ten-sided cylinder) with amber bands and lids that glow at night.
        (float X, float Z, float Height)[] barrels = [(0.85f, 0.55f, 0.85f), (0.3f, 0.75f, 0.8f), (-0.75f, 0.6f, 0.9f)];
        for (var i = 0; i < barrels.Length; i++)
        {
            var (x, z, height) = barrels[i];
            var at = centre + new Vector3(x, 0f, z);
            parts.Add(new CropPart($"Barrel{i}", CropPartShape.Cylinder, 0.56f, height, 0.5f, at.X, height / 2f, at.Z, 0f, 15f * i, 0f, "timber-side", Sides: 10));
            parts.Add(new CropPart($"Barrel{i}Bulge", CropPartShape.Cylinder, 0.6f, height * 0.4f, 0.6f, at.X, height / 2f, at.Z, 0f, 15f * i, 0f, "timber", Sides: 10));
            foreach (var (band, y) in new[] { ("Low", 0.18f), ("High", height - 0.16f) })
            {
                parts.Add(new CropPart($"Barrel{i}Band{band}", CropPartShape.Cylinder, 0.59f, 0.05f, 0.59f, at.X, y, at.Z, 0f, 0f, 0f, "lantern", Glow: 0.6f, Sides: 10));
            }

            parts.Add(new CropPart($"Barrel{i}Lid", CropPartShape.Cylinder, 0.46f, 0.03f, 0.46f, at.X, height + 0.01f, at.Z, 0f, 0f, 0f, "lantern", Glow: 1f, Sides: 10));
        }
    }

    /// <summary>
    /// The timber frame of a new round home on the west front: a ring of eight cut posts on a stone base, a ring beam on
    /// top and rafters rising to a peak, in plain timber because people cut it, not grew it (ART_DIRECTION §1).
    /// </summary>
    private static void NewHomeFrame(List<CropPart> parts)
    {
        var frame = new RoundBuilding("Frame", new Vector3(-4.6f, 0f, 3.6f), 1.4f, 2.4f, 1.4f, 4.0f);
        parts.Add(frame.Cylinder("Base", 2.96f, 0.22f, 2.9f, frame.Centre + new Vector3(0f, 0.11f, 0f), "stone") with { Sides = 16 });
        const int posts = 8;
        for (var i = 0; i < posts; i++)
        {
            var angle = 360f * i / posts + 22.5f;
            var foot = frame.Radial(angle, 1.35f, 0.22f);
            var top = frame.Radial(angle, 1.35f, 2.4f);
            parts.Add(frame.Beam($"Post{i}", 0.13f, 0.13f, foot, top, "timber"));
            var next = frame.Radial(angle + 360f / posts, 1.35f, 2.4f);
            parts.Add(frame.Beam($"Beam{i}", 0.12f, 0.1f, top, next, "timber-side"));
            parts.Add(frame.Beam($"Rafter{i}", 0.1f, 0.08f, top, frame.Centre + new Vector3(0f, 4.0f, 0f), "timber"));
        }

        parts.Add(frame.Cylinder("Peak", 0.2f, 0.18f, 0.2f, frame.Centre + new Vector3(0f, 4.0f, 0f), "timber-side"));
        // A few planks leaning against it, waiting to go up.
        parts.Add(frame.Box("Lean0", 0.22f, 1.8f, 0.05f, frame.Radial(30f, 1.6f, 0.86f), "wood-cut", pitch: -18f, yaw: 30f));
        parts.Add(frame.Box("Lean1", 0.22f, 1.7f, 0.05f, frame.Radial(40f, 1.62f, 0.82f), "timber", pitch: -20f, yaw: 40f));
    }

    // ---- run-down extras ----

    /// <summary>Boards across the run-down windows.</summary>
    private static void Boards(List<CropPart> parts)
    {
        foreach (var (name, building, angle, y) in new[] { ("WindowEast", Workshop, WindowAngle, 1.3f), ("WindowHigh", House, HouseWindowAngle, 3.0f) })
        {
            parts.Add(building.Box($"{name}Board0", 0.85f, 0.13f, 0.03f, building.OnWall(angle, 0f, y, 0.1f), "wall-grey", yaw: angle, roll: 25f));
            parts.Add(building.Box($"{name}Board1", 0.85f, 0.13f, 0.03f, building.OnWall(angle, 0f, y + 0.02f, 0.13f), "timber-side", yaw: angle, roll: -22f));
        }
    }

    private static void Weeds(List<CropPart> parts)
    {
        float[] weeds = [-150f, -75f, -35f, 30f, 75f, 120f, 170f];
        for (var i = 0; i < weeds.Length; i++)
        {
            var at = Workshop.Radial(weeds[i], WallRadius + 0.2f, 0f);
            var height = 0.2f + 0.05f * (i % 3);
            parts.Add(new CropPart($"Weed{i}A", CropPartShape.Prism, 0.18f, height, 0.02f, at.X, height / 2f + 0.015f, at.Z, 0f, weeds[i] + 15f * i, 8f, i % 2 == 0 ? "foliage" : "grass-shade"));
            parts.Add(new CropPart($"Weed{i}B", CropPartShape.Prism, 0.16f, height * 0.8f, 0.02f, at.X + 0.03f, height * 0.4f + 0.015f, at.Z, 0f, weeds[i] + 15f * i + 90f, -8f, i % 2 == 0 ? "grass-shade" : "foliage"));
        }
    }
}
