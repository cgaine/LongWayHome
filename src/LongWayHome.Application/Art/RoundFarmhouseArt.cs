using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>
/// The round farmhouse from the concept art's Farmhouse page (ART_DIRECTION §7.1, "Round farmhouse drawing"): a round
/// drum of mauve timber staves under a tall cone of overlapping leaves, living violet trunk ribs climbing the wall and
/// running up the roof to a sprouting crown, a stone chimney at the back, the ship's hull-panel patch on the roof, an
/// arched ember-orange door facing the plot and two arched windows. Pure C#, no Godot: the sandbox's builder
/// <c>RoundFarmhouseModels</c> merges these parts into one mesh under the shared palette material. For now it is shown
/// only in the art sandbox; the farm still uses the rectangular <see cref="FarmhouseArt"/> house.
/// <para>
/// <b>Conventions.</b> The <see cref="FarmhouseArt"/> conventions (sizes, Godot's Euler YXZ rotations about the part's
/// centre, yaw 0 facing +Z and yaw 90 +X), in house-local metres: the origin is the centre of the drum <b>on the
/// ground</b> (y 0) and the door faces <b>+Z</b>. An angle θ round the house is a yaw: 0 is the front, 90 east (+X),
/// 180 the back. The drum is 5 m across (radius 2.5) with 3 m walls, so it sits inside the rectangular house's 6 x 4 m
/// footprint's width; the cone's eaves reach out to radius 3.2 and its apex is at y 6.3. A cylinder 1.5 m or more across
/// (the drum's core and stone base, the roof's cone) is meant to be drawn round (many sides); smaller ones keep the
/// game's six sides, so the door and window arches are pointed like the rectangular house's.
/// </para>
/// <para>
/// <b>Run-down and restored.</b> Both states share the same shape. Run-down (day one): grey staves with missing pieces,
/// a wilted olive-brown leaf roof with gaps showing the dark roof core, sparse dry eave tips, bare grey ribs with dead
/// twigs, the left window boarded up and the right one cracked, the faded door left ajar, weeds, a creeper and fallen
/// leaves. Nothing glows. Restored: mauve staves, a full green leaf roof with fresh tips round the hull patch, living
/// ribs with leaf clusters, a sprouting crown, clear glass, the ember door with its spiral carving, a flower box, a
/// lantern by the door and chimney smoke. At night the eave tips, the crown and the lantern glow;
/// <see cref="RestoredNightLights"/> are the warm window panes, shown only after dark.
/// </para>
/// Parts are named by group, and the tests read the prefixes: <c>Core</c>, <c>Base*</c>, <c>Stave*</c>,
/// <c>Grain*</c>, <c>Rib*</c>, <c>Door*</c>, <c>Window*</c>, <c>Step</c>, <c>Roof*</c>, <c>Leaf*</c>, <c>Tip*</c>,
/// <c>Crown*</c>, <c>Patch*</c>, <c>Chimney*</c>, <c>Smoke*</c>, <c>Lantern*</c>, <c>FlowerBox*</c>, <c>Weed*</c>,
/// <c>Creeper*</c>, <c>FallenLeaf*</c>, <c>NightWindow*</c>.
/// </summary>
public static class RoundFarmhouseArt
{
    /// <summary>The drum's outer radius: 5 m across.</summary>
    public const float WallRadius = 2.5f;

    /// <summary>The height of the drum's walls, where the roof's eaves start.</summary>
    public const float WallTop = 3f;

    /// <summary>How far the roof's eaves reach from the centre.</summary>
    public const float EaveRadius = 3.2f;

    /// <summary>The height of the roof cone's apex.</summary>
    public const float ApexY = 6.3f;

    private const float BaseTop = 0.3f;
    private const float StaveDepth = 0.08f;
    private const int Staves = 24;

    private const float RoofHeight = ApexY - WallTop;

    private const float DoorHalf = 0.5f;
    private const float DoorShoulder = 1.8f;
    private const float WindowAngle = 35f;
    private const float WindowSill = 0.85f;
    private const float WindowShoulder = 1.5f;

    private const float LeafLength = 1.0f;
    private const float LeafTilt = 6f;
    private const float RingStart = 0.45f;
    private const float RingStep = 0.68f;
    private const int Rings = 5;

    private const float ChimneyAngle = 150f;
    private const float ChimneyRadius = 1.35f;
    private const float PatchAngle = 25f;
    private const float PatchS = 1.85f;

    /// <summary>The angles of the four living ribs, clear of the door, the windows and the chimney.</summary>
    private static readonly float[] RibAngles = [-62f, 62f, 120f, -120f];

    private static readonly float SlopeLength = MathF.Sqrt(EaveRadius * EaveRadius + RoofHeight * RoofHeight);
    private static readonly float SlopeCos = EaveRadius / SlopeLength;
    private static readonly float SlopeSin = RoofHeight / SlopeLength;
    private static readonly float SlopeDegrees = MathF.Atan2(RoofHeight, EaveRadius) * 180f / MathF.PI;

    /// <summary>
    /// The run-down roof's gaps: the ring and the angle range (degrees, −180..180) with no leaves. The first is the
    /// worst gap, under the hull-panel patch.
    /// </summary>
    private static readonly (int Ring, float From, float To)[] RoofGaps =
    [
        (1, 10f, 42f), (2, 12f, 40f), (2, -150f, -120f), (3, -150f, -125f), (3, 70f, 100f),
    ];

    private static readonly IReadOnlyList<CropPart> RunDownParts = Build(restored: false);
    private static readonly IReadOnlyList<CropPart> RestoredParts = Build(restored: true);
    private static readonly IReadOnlyList<CropPart> NightLightParts = BuildNightLights();

    /// <summary>The round farmhouse run-down, as on day one. Nothing glows.</summary>
    public static IReadOnlyList<CropPart> RunDown => RunDownParts;

    /// <summary>The round farmhouse restored. Its eave tips, crown and lantern glow at night.</summary>
    public static IReadOnlyList<CropPart> Restored => RestoredParts;

    /// <summary>
    /// The restored house's lit window panes, in <c>lantern</c>, just in front of the glass: drawn only at night, so
    /// by day the windows show their glass.
    /// </summary>
    public static IReadOnlyList<CropPart> RestoredNightLights => NightLightParts;

    private static List<CropPart> Build(bool restored)
    {
        var parts = new List<CropPart>();
        Drum(parts, restored);
        Ribs(parts, restored);
        Door(parts, restored);
        Windows(parts, restored);
        parts.Add(Box("Step", 1.4f, 0.14f, 0.4f, new Vector3(0f, 0.07f, WallRadius + 0.2f), "stone"));
        Roof(parts, restored);
        Patch(parts, restored);
        Chimney(parts, restored);
        Crown(parts, restored);
        if (restored)
        {
            FlowerBox(parts);
            Lantern(parts);
        }
        else
        {
            Clutter(parts);
        }

        return parts;
    }

    // ---- the drum: core, stone base, staves ----

    private static void Drum(List<CropPart> parts, bool restored)
    {
        // A dark solid core just inside the staves, so a missing stave piece shows a dark face, never the sky.
        parts.Add(new CropPart("Core", CropPartShape.Cylinder, 2f * (WallRadius - StaveDepth), WallTop, 2f * (WallRadius - StaveDepth),
            0f, WallTop / 2f, 0f, 0f, 0f, 0f, "timber-trim"));
        parts.Add(new CropPart("Base", CropPartShape.Cylinder, 2f * WallRadius + 0.16f, BaseTop + 0.02f, 2f * WallRadius + 0.14f,
            0f, (BaseTop + 0.02f) / 2f, 0f, 0f, 0f, 0f, "stone"));
        float[] rocks = [-140f, -95f, -48f, 52f, 100f, 160f];
        for (var i = 0; i < rocks.Length; i++)
        {
            parts.Add(Box($"BaseRock{i}", 0.34f, 0.18f, 0.05f, OnWall(rocks[i], 0f, 0.15f, 0.1f), "rock", yaw: rocks[i]));
        }

        // Each stave is two pieces, split at a height that steps round the drum, with a 2 cm seam.
        var width = 2f * WallRadius * MathF.Sin(MathF.PI / Staves) - 0.015f;
        int[] missingLower = restored ? [] : [6, 18];
        int[] missingUpper = restored ? [] : [11];
        for (var i = 0; i < Staves; i++)
        {
            var angle = 360f * i / Staves;
            var split = 1.15f + 0.3f * (i * 7 % 5);
            var lower = restored
                ? (i % 4 == 2 ? "timber-side" : "timber")
                : (i % 3 == 1 ? "timber-side" : "wall-grey");
            var upper = restored
                ? (i % 4 == 0 ? "timber-side" : "timber")
                : (i % 3 == 2 ? "timber-side" : "wall-grey");
            var tilt = restored ? 0f : 0.6f * (i % 3 - 1);
            if (!missingLower.Contains(i))
            {
                var height = split - BaseTop - 0.01f;
                parts.Add(Box($"Stave{i}Lower", width, height, StaveDepth, OnWall(angle, 0f, BaseTop + height / 2f, -StaveDepth / 2f), lower, yaw: angle, roll: tilt));
            }

            if (!missingUpper.Contains(i))
            {
                var height = WallTop - split - 0.01f;
                parts.Add(Box($"Stave{i}Upper", width, height, StaveDepth, OnWall(angle, 0f, WallTop - height / 2f, -StaveDepth / 2f), upper, yaw: angle, roll: -tilt));
            }

            // Two short lines of wavy grain on each stave, at heights that vary round the drum.
            for (var g = 0; g < 2; g++)
            {
                var y = 0.75f + 1.05f * g + 0.17f * (i * 3 % 4);
                if (Missing(i, y, split, missingLower, missingUpper)) continue;
                parts.Add(Box($"Grain{i}_{g}", width * 0.6f, 0.016f, 0.01f, OnWall(angle, 0.04f * (g * 2 - 1), y, 0.003f), "timber-grain",
                    yaw: angle, roll: 3f * (g * 2 - 1)));
            }
        }
    }

    private static bool Missing(int stave, float y, float split, int[] missingLower, int[] missingUpper) =>
        (y < split && missingLower.Contains(stave)) || (y >= split && missingUpper.Contains(stave));

    // ---- the living ribs ----

    private static void Ribs(List<CropPart> parts, bool restored)
    {
        var role = restored ? "trunk" : "trunk-grey";
        (float Bottom, float Top, float BottomD, float TopD, float Twist)[] segments =
        [
            (0f, 0.8f, 0.34f, 0.31f, -15f),
            (0.8f, 1.6f, 0.31f, 0.29f, 0f),
            (1.6f, 2.35f, 0.29f, 0.27f, 15f),
            (2.35f, 2.95f, 0.27f, 0.25f, 30f),
        ];
        for (var r = 0; r < RibAngles.Length; r++)
        {
            var angle = RibAngles[r];
            for (var i = 0; i < segments.Length; i++)
            {
                var (bottom, top, bottomD, topD, twist) = segments[i];
                // A slight wobble from segment to segment, so the trunk reads grown, not turned.
                var at = OnWall(angle + 0.8f * (i % 2 * 2 - 1), 0f, (bottom + top) / 2f, 0.1f);
                parts.Add(new CropPart($"Rib{r}Seg{i}", CropPartShape.Cylinder, bottomD, top - bottom, topD,
                    at.X, at.Y, at.Z, 0f, twist + 11f * r, 0f, role));
            }

            // Under the eaves the trunk bends out to the eave edge, then runs up the roof above the leaves.
            var elbowFrom = Radial(angle, WallRadius + 0.1f, 2.9f);
            var elbowTo = OnRoof(angle, 0f, 0.3f);
            parts.Add(Beam($"Rib{r}Elbow", 0.24f, 0.22f, elbowFrom, elbowTo, angle, role));
            // Lifted clear of the leaves, whose lower ends stand up to about 0.23 m off the cone.
            float[] along = [0f, 1.1f, 2.2f, 3.3f];
            for (var i = 0; i < along.Length - 1; i++)
            {
                var from = OnRoof(angle, along[i], 0.3f);
                var to = OnRoof(angle, along[i + 1] + 0.05f, 0.3f);
                var thickness = 0.24f - 0.035f * i;
                parts.Add(Beam($"Rib{r}Roof{i}", thickness, thickness, from, to, angle, role));
            }

            var tip = Radial(angle, EaveRadius + 0.08f, WallTop + 0.1f);
            if (restored)
            {
                // A cluster of blue-green leaves with a violet underside where the rib meets the eaves.
                parts.Add(new CropPart($"Rib{r}Cluster", CropPartShape.Sphere, 0.46f, 0.36f, 0.46f, tip.X, tip.Y, tip.Z, 0f, angle, 0f, "roof-tips"));
                var under = Radial(angle + 4f, EaveRadius + 0.22f, WallTop + 0.02f);
                parts.Add(new CropPart($"Rib{r}ClusterUnder", CropPartShape.Sphere, 0.28f, 0.22f, 0.28f, under.X, under.Y, under.Z, 0f, angle, 0f, "leaf-underside"));
                var shoot = Radial(angle - 5f, EaveRadius + 0.05f, WallTop + 0.3f);
                parts.Add(new CropPart($"Rib{r}Shoot", CropPartShape.Prism, 0.12f, 0.32f, 0.04f, shoot.X, shoot.Y, shoot.Z, 0f, angle, -18f, "foliage"));
            }
            else
            {
                // Dead twigs instead of leaves.
                (float Yaw, float Tilt, float Length)[] twigs = [(-40f, 55f, 0.42f), (5f, 35f, 0.36f), (45f, 60f, 0.3f)];
                for (var i = 0; i < twigs.Length; i++)
                {
                    parts.Add(Rising($"Rib{r}Twig{i}", 0.035f, twigs[i].Length, 0.035f, tip, twigs[i].Tilt, angle + twigs[i].Yaw, "trunk-grey"));
                }
            }
        }
    }

    // ---- the door, the windows ----

    private static void Door(List<CropPart> parts, bool restored)
    {
        const float frameZ = WallRadius + 0.005f;
        const float doorZ = WallRadius + 0.06f;
        const float archDiameter = 2f * DoorHalf / 0.866f;
        // The frame: a wider pointed arch of timber-trim behind the door, bridging the curve of the wall.
        parts.Add(Box("DoorFrame", 2f * DoorHalf + 0.2f, DoorShoulder - BaseTop, 0.1f,
            new Vector3(0f, (BaseTop + DoorShoulder) / 2f, frameZ), "timber-trim"));
        parts.Add(new CropPart("DoorFrameArch", CropPartShape.Cylinder, archDiameter + 0.23f, 0.1f, archDiameter + 0.23f,
            0f, DoorShoulder, frameZ, 90f, 0f, 0f, "timber-trim"));

        if (restored)
        {
            parts.Add(Box("Door", 2f * DoorHalf, DoorShoulder - BaseTop, 0.05f, new Vector3(0f, (BaseTop + DoorShoulder) / 2f, doorZ), "ember"));
            parts.Add(new CropPart("DoorArch", CropPartShape.Cylinder, archDiameter, 0.05f, archDiameter, 0f, DoorShoulder, doorZ, 90f, 0f, 0f, "ember"));
            var knob = new Vector3(0.36f, 1.12f, doorZ + 0.05f);
            parts.Add(new CropPart("DoorKnob", CropPartShape.Sphere, 0.08f, 0.08f, 0.08f, knob.X, knob.Y, knob.Z, 0f, 0f, 0f, "rock"));
            // The spiral carving: short pale strokes along a widening spiral.
            for (var i = 0; i < 9; i++)
            {
                var turn = 0.9f * i;
                var radius = 0.03f + 0.022f * i;
                var at = new Vector3(radius * MathF.Cos(turn), 1.25f + radius * MathF.Sin(turn), doorZ + 0.028f);
                parts.Add(Box($"DoorSpiral{i}", 0.06f + 0.012f * i, 0.025f, 0.008f, at, "seed-pouch", roll: turn * 180f / MathF.PI + 90f));
            }
        }
        else
        {
            // The dark doorway, and the faded door hanging ajar from its west hinge, swung a little way out.
            parts.Add(Box("DoorwayDark", 2f * DoorHalf, DoorShoulder - BaseTop, 0.02f, new Vector3(0f, (BaseTop + DoorShoulder) / 2f, frameZ + 0.06f), "timber-trim"));
            parts.Add(new CropPart("DoorwayDarkArch", CropPartShape.Cylinder, archDiameter, 0.02f, archDiameter, 0f, DoorShoulder, frameZ + 0.06f, 90f, 0f, 0f, "timber-trim"));
            const float yaw = -22f;
            var hinge = new Vector3(-DoorHalf, 0f, doorZ + 0.01f);
            Vector3 OnDoor(float along, float y, float outward) => hinge + PartBox.Rotate(new Vector3(along, 0f, outward), 0f, yaw, 0f) + new Vector3(0f, y, 0f);
            parts.Add(Box("Door", 2f * DoorHalf, DoorShoulder - BaseTop, 0.05f, OnDoor(DoorHalf, (BaseTop + DoorShoulder) / 2f, 0f), "door-faded", yaw: yaw));
            var arch = OnDoor(DoorHalf, DoorShoulder, 0f);
            parts.Add(new CropPart("DoorArch", CropPartShape.Cylinder, archDiameter, 0.05f, archDiameter, arch.X, arch.Y, arch.Z, 90f, yaw, 0f, "door-faded"));
            parts.Add(Box("DoorBrace0", 0.86f, 0.07f, 0.02f, OnDoor(DoorHalf, 0.7f, 0.035f), "timber-grain", yaw: yaw));
            parts.Add(Box("DoorBrace1", 0.86f, 0.07f, 0.02f, OnDoor(DoorHalf, 1.5f, 0.035f), "timber-grain", yaw: yaw));
            var knob = OnDoor(0.86f, 1.12f, 0.05f);
            parts.Add(new CropPart("DoorKnob", CropPartShape.Sphere, 0.07f, 0.07f, 0.07f, knob.X, knob.Y, knob.Z, 0f, 0f, 0f, "rock"));
        }
    }

    private static void Windows(List<CropPart> parts, bool restored)
    {
        const float frameWidth = 0.82f;
        const float glassWidth = 0.68f;
        foreach (var (side, name) in new[] { (-1, "West"), (1, "East") })
        {
            var angle = side * WindowAngle;
            parts.Add(Box($"Window{name}Frame", frameWidth, WindowShoulder - WindowSill, 0.05f,
                OnWall(angle, 0f, (WindowSill + WindowShoulder) / 2f, 0.03f), "timber-trim", yaw: angle));
            parts.Add(Pane($"Window{name}FrameArch", frameWidth / 0.866f, 0.05f, OnWall(angle, 0f, WindowShoulder, 0.03f), angle, "timber-trim"));
            parts.Add(Box($"Window{name}Sill", frameWidth + 0.12f, 0.06f, 0.12f, OnWall(angle, 0f, WindowSill - 0.03f, 0.06f), "timber-side", yaw: angle));

            // The glass: clear when restored, dull when run-down.
            var glass = restored ? "wrist-screen" : "glass-dull";
            parts.Add(Box($"Window{name}Glass", glassWidth, WindowShoulder - WindowSill - 0.06f, 0.01f,
                OnWall(angle, 0f, (WindowSill + 0.06f + WindowShoulder) / 2f, 0.06f), glass, yaw: angle));
            parts.Add(Pane($"Window{name}GlassArch", glassWidth / 0.866f, 0.01f, OnWall(angle, 0f, WindowShoulder, 0.06f), angle, glass));
            parts.Add(Box($"Window{name}Mullion", 0.04f, 0.95f, 0.014f, OnWall(angle, 0f, 1.37f, 0.07f), "timber-trim", yaw: angle));
        }

        if (restored) return;

        // West: boarded up with two crossed boards. East: cracked across the glass.
        parts.Add(Box("WindowWestBoard0", 0.95f, 0.14f, 0.03f, OnWall(-WindowAngle, 0f, 1.3f, 0.1f), "wall-grey", yaw: -WindowAngle, roll: 28f));
        parts.Add(Box("WindowWestBoard1", 0.95f, 0.14f, 0.03f, OnWall(-WindowAngle, 0f, 1.32f, 0.13f), "timber-side", yaw: -WindowAngle, roll: -24f));
        parts.Add(Box("WindowEastCrack0", 0.2f, 0.016f, 0.006f, OnWall(WindowAngle, 0.12f, 1.3f, 0.068f), "timber-trim", yaw: WindowAngle, roll: 40f));
        parts.Add(Box("WindowEastCrack1", 0.18f, 0.016f, 0.006f, OnWall(WindowAngle, 0.2f, 1.18f, 0.068f), "timber-trim", yaw: WindowAngle, roll: -35f));
    }

    private static List<CropPart> BuildNightLights()
    {
        var parts = new List<CropPart>();
        foreach (var (side, name) in new[] { (-1, "West"), (1, "East") })
        {
            var angle = side * WindowAngle;
            parts.Add(Box($"NightWindow{name}", 0.66f, WindowShoulder - WindowSill - 0.07f, 0.01f,
                OnWall(angle, 0f, (WindowSill + 0.07f + WindowShoulder) / 2f, 0.075f), "lantern", yaw: angle) with { Glow = 1f });
            parts.Add(Pane($"NightWindow{name}Arch", 0.66f / 0.866f, 0.01f, OnWall(angle, 0f, WindowShoulder, 0.075f), angle, "lantern") with { Glow = 1f });
        }

        return parts;
    }

    // ---- the leaf roof ----

    private static void Roof(List<CropPart> parts, bool restored)
    {
        // The roof's solid cone under the leaves: dark where the run-down roof has gaps, dark green under the restored one.
        parts.Add(new CropPart("RoofCore", CropPartShape.Cylinder, 2f * EaveRadius, RoofHeight, 0f,
            0f, WallTop + RoofHeight / 2f, 0f, 0f, 0f, 0f, restored ? "roof-far" : "timber-trim"));

        // Rings of overlapping leaves from the eaves up. Each leaf lies on the slope with its lower end lifted over the
        // ring below; neighbours alternate in lift so their overlaps never fight.
        for (var ring = 0; ring < Rings; ring++)
        {
            var s = RingStart + RingStep * ring;
            var radius = EaveRadius - s * SlopeCos;
            var count = (int)MathF.Round(2f * MathF.PI * radius / 0.8f);
            var width = 2f * MathF.PI * radius / count * 1.25f;
            var role = restored
                ? (ring % 2 == 0 ? "foliage" : "roof-rows")
                : (ring % 2 == 0 ? "roof-wilted" : "tips-dry");
            for (var i = 0; i < count; i++)
            {
                var angle = Wrap(360f * i / count + (ring % 2) * 180f / count);
                if (!restored && InGap(ring, angle)) continue;
                var lift = 0.05f + 0.025f * ring + 0.012f * (i % 2);
                var wilt = restored ? 0f : 4f * ((i + ring) % 3 - 1);
                parts.Add(OnSlope($"Leaf{ring}_{i}", width, LeafLength, 0.04f, angle, s, lift, role, tilt: LeafTilt, roll: wilt));
            }
        }

        // The fringe of leaf tips hanging over the eaves: every one when restored (and they glow at night), only a few,
        // longer and drooping, when run-down.
        const int tips = 30;
        for (var i = 0; i < tips; i++)
        {
            if (!restored && i % 3 != 0) continue;
            var angle = 360f * i / tips + 6f;
            var length = restored ? 0.3f : 0.42f;
            var at = Radial(angle, EaveRadius + 0.03f, WallTop + 0.02f - length / 2f);
            var tip = new CropPart($"Tip{i}", CropPartShape.Prism, 0.3f, length, 0.03f, at.X, at.Y, at.Z,
                restored ? -8f : -22f, angle, 180f, restored ? "roof-tips" : "tips-dry");
            parts.Add(restored ? tip with { Glow = 0.9f } : tip);
        }
    }

    private static bool InGap(int ring, float angle) =>
        RoofGaps.Any(gap => gap.Ring == ring && angle >= gap.From && angle <= gap.To);

    private static void Crown(List<CropPart> parts, bool restored)
    {
        // A small cone of leaves capping the apex, over the top ends of the last ring.
        const float capRadius = 0.95f;
        var bottom = WallTop + (EaveRadius - capRadius) * RoofHeight / EaveRadius + 0.08f;
        const float top = ApexY + 0.22f;
        parts.Add(new CropPart("CrownCap", CropPartShape.Cylinder, 2f * capRadius, top - bottom, 0f,
            0f, (bottom + top) / 2f, 0f, 0f, 0f, 0f, restored ? "roof-rows" : "roof-wilted"));

        var apex = new Vector3(0f, top - 0.05f, 0f);
        if (restored)
        {
            // A new frond curling up from the crown, and a small leaf; both glow at night.
            var at = apex;
            float[] bends = [10f, 30f, 60f, 100f, 140f];
            for (var i = 0; i < bends.Length; i++)
            {
                var length = 0.2f - 0.02f * i;
                var dir = PartBox.Rotate(new Vector3(0f, length, 0f), bends[i], -20f, 0f);
                var centre = at + dir / 2f;
                parts.Add(new CropPart($"CrownFrond{i}", CropPartShape.Box, 0.07f, length, 0.07f, centre.X, centre.Y, centre.Z,
                    bends[i], -20f, 0f, "foliage", Glow: 0.9f));
                at += dir;
            }

            var leaf = apex + new Vector3(-0.16f, 0.1f, 0.02f);
            parts.Add(new CropPart("CrownLeaf", CropPartShape.Sphere, 0.22f, 0.08f, 0.22f, leaf.X, leaf.Y, leaf.Z, 0f, 0f, -35f, "roof-tips", Glow: 0.9f));
        }
        else
        {
            (float Yaw, float Tilt, float Length)[] twigs = [(0f, 20f, 0.32f), (130f, 35f, 0.26f), (240f, 28f, 0.22f)];
            for (var i = 0; i < twigs.Length; i++)
            {
                parts.Add(Rising($"CrownTwig{i}", 0.035f, twigs[i].Length, 0.035f, apex, twigs[i].Tilt, twigs[i].Yaw, "trunk-grey"));
            }
        }
    }

    // ---- the hull-panel patch and the chimney ----

    private static void Patch(List<CropPart> parts, bool restored)
    {
        const float lift = 0.24f;
        const float roll = 12f;
        var pitch = SlopeDegrees - 90f;
        var centre = OnRoof(PatchAngle, PatchS, lift);
        Vector3 OnPanel(float across, float up, float above) =>
            centre + PartBox.Rotate(new Vector3(across, up, above), pitch, PatchAngle, roll);

        parts.Add(new CropPart("PatchPanel", CropPartShape.Box, 1.15f, 0.85f, 0.03f, centre.X, centre.Y, centre.Z, pitch, PatchAngle, roll, "ship-metal"));
        var stripe = OnPanel(0f, -0.1f, 0.018f);
        parts.Add(new CropPart("PatchStripe", CropPartShape.Box, 1.15f, 0.13f, 0.012f, stripe.X, stripe.Y, stripe.Z, pitch, PatchAngle, roll, "navy"));
        (float Across, float Up)[] rivets = [(-0.52f, -0.37f), (0f, -0.37f), (0.52f, -0.37f), (0.52f, 0f), (0.52f, 0.37f), (0f, 0.37f), (-0.52f, 0.37f), (-0.52f, 0f)];
        for (var i = 0; i < rivets.Length; i++)
        {
            var at = OnPanel(rivets[i].Across, rivets[i].Up, 0.022f);
            parts.Add(new CropPart($"PatchRivet{i}", CropPartShape.Box, 0.05f, 0.05f, 0.03f, at.X, at.Y, at.Z, pitch, PatchAngle, roll, "rock"));
        }

        if (!restored) return;

        // New leaves growing up round the patch's edges.
        (float Across, float Up, float Roll)[] fresh = [(-0.62f, -0.2f, 30f), (0.64f, 0.25f, -30f), (0.3f, -0.48f, 40f)];
        for (var i = 0; i < fresh.Length; i++)
        {
            var at = OnPanel(fresh[i].Across, fresh[i].Up, -0.02f);
            parts.Add(new CropPart($"PatchLeaf{i}", CropPartShape.Sphere, 0.32f, 0.06f, 0.16f, at.X, at.Y, at.Z, pitch, PatchAngle, roll + fresh[i].Roll, "roof-tips"));
        }
    }

    private static void Chimney(List<CropPart> parts, bool restored)
    {
        var centre = Radial(ChimneyAngle, ChimneyRadius, 0f);
        const float bottom = 4.4f;
        const float top = 6.0f;
        parts.Add(Box("ChimneyStack", 0.62f, top - bottom, 0.62f, centre with { Y = (bottom + top) / 2f }, "stone", yaw: ChimneyAngle));
        parts.Add(Box("ChimneyRock0", 0.22f, 0.14f, 0.04f, centre + PartBox.Rotate(new Vector3(-0.1f, 5.65f, 0.32f), 0f, ChimneyAngle, 0f), "rock", yaw: ChimneyAngle));
        parts.Add(Box("ChimneyRock1", 0.04f, 0.16f, 0.24f, centre + PartBox.Rotate(new Vector3(0.32f, 5.4f, 0.05f), 0f, ChimneyAngle, 0f), "rock", yaw: ChimneyAngle));
        parts.Add(Box("ChimneyCap", 0.76f, 0.12f, 0.76f, centre with { Y = top + 0.05f }, "rock", yaw: ChimneyAngle));
        parts.Add(Box("ChimneyFlue", 0.36f, 0.02f, 0.36f, centre with { Y = top + 0.115f }, "timber-trim", yaw: ChimneyAngle));

        if (!restored) return;

        // A wisp of smoke drifting up and away.
        (float Up, float Drift, float Size)[] puffs = [(0.45f, 0.05f, 0.26f), (0.9f, 0.22f, 0.34f), (1.45f, 0.1f, 0.4f)];
        for (var i = 0; i < puffs.Length; i++)
        {
            var at = centre + new Vector3(puffs[i].Drift, top + puffs[i].Up, -puffs[i].Drift * 0.5f);
            parts.Add(new CropPart($"Smoke{i}", CropPartShape.Sphere, puffs[i].Size, puffs[i].Size * 0.85f, puffs[i].Size, at.X, at.Y, at.Z, 0f, 20f * i, 0f, "snow"));
        }
    }

    // ---- restored extras: the flower box and the lantern ----

    private static void FlowerBox(List<CropPart> parts)
    {
        const float angle = WindowAngle;
        parts.Add(Box("FlowerBox", 0.92f, 0.2f, 0.22f, OnWall(angle, 0f, WindowSill - 0.17f, 0.14f), "timber-trim", yaw: angle));
        (float Along, float Size, string Role)[] blooms =
        [
            (-0.32f, 0.16f, "leaf-underside"), (-0.12f, 0.2f, "glow-rare"), (0.1f, 0.14f, "foliage"), (0.3f, 0.18f, "glow-rare"),
        ];
        for (var i = 0; i < blooms.Length; i++)
        {
            var at = OnWall(angle, blooms[i].Along, WindowSill - 0.02f + blooms[i].Size / 3f, 0.15f);
            parts.Add(new CropPart($"FlowerBoxBloom{i}", CropPartShape.Sphere, blooms[i].Size, blooms[i].Size * 0.8f, blooms[i].Size,
                at.X, at.Y, at.Z, 0f, 0f, 0f, blooms[i].Role));
        }
    }

    private static void Lantern(List<CropPart> parts)
    {
        // A post just east of the door with an arm and a hanging lantern that glows at night.
        var post = new Vector3(0.95f, 0f, WallRadius + 0.12f);
        parts.Add(new CropPart("LanternPost", CropPartShape.Cylinder, 0.09f, 1.75f, 0.07f, post.X, 0.875f, post.Z, 0f, 0f, 0f, "trunk"));
        parts.Add(Box("LanternArm", 0.32f, 0.05f, 0.05f, new Vector3(post.X + 0.14f, 1.7f, post.Z), "trunk"));
        parts.Add(Box("LanternBody", 0.2f, 0.3f, 0.2f, new Vector3(post.X + 0.26f, 1.48f, post.Z), "timber-trim"));
        parts.Add(new CropPart("LanternGlass", CropPartShape.Box, 0.21f, 0.2f, 0.21f, post.X + 0.26f, 1.47f, post.Z, 0f, 0f, 0f, "lantern", Glow: 1f));
    }

    // ---- run-down extras: weeds, the creeper, fallen leaves ----

    private static void Clutter(List<CropPart> parts)
    {
        float[] weeds = [-150f, -100f, -75f, -28f, 22f, 55f, 95f, 135f, 175f];
        for (var i = 0; i < weeds.Length; i++)
        {
            var at = Radial(weeds[i], WallRadius + 0.2f, 0f);
            var role = i % 2 == 0 ? "foliage" : "grass-shade";
            var height = 0.2f + 0.05f * (i % 3);
            parts.Add(new CropPart($"Weed{i}A", CropPartShape.Prism, 0.18f, height, 0.02f, at.X, height / 2f + 0.015f, at.Z, 0f, weeds[i] + 15f * i, 8f, role));
            parts.Add(new CropPart($"Weed{i}B", CropPartShape.Prism, 0.16f, height * 0.8f, 0.02f, at.X + 0.03f, height * 0.4f + 0.015f, at.Z, 0f, weeds[i] + 15f * i + 90f, -8f,
                i % 2 == 0 ? "grass-shade" : "foliage"));
        }

        // The creeper: a zigzag strand climbing the west rib by the boarded window.
        var angle = RibAngles[0];
        var at0 = OnWall(angle, 0.16f, 0.05f, 0.27f);
        const float strand = 0.4f;
        for (var i = 0; i < 6; i++)
        {
            var roll = i % 2 == 0 ? 18f : -18f;
            var dir = PartBox.Rotate(new Vector3(0f, strand, 0f), 0f, angle, roll);
            var centre = at0 + dir / 2f;
            parts.Add(Box($"Creeper{i}", 0.035f, strand, 0.015f, centre, "foliage", yaw: angle, roll: roll));
            if (i > 0) parts.Add(Box($"CreeperLeaf{i}", 0.09f, 0.06f, 0.015f, at0, "foliage", yaw: angle, roll: 30f));
            at0 += dir;
        }

        // Fallen leaves on the ground round the drum, clear of the step and the path in front of the door.
        (float Angle, float Radius, float Yaw)[] fallen = [(-160f, 2.95f, 30f), (-110f, 3.2f, -50f), (-55f, 2.9f, 70f), (-30f, 3.3f, -20f), (40f, 3.1f, 15f), (80f, 2.85f, 110f), (125f, 3.25f, 60f), (170f, 3.0f, -35f)];
        for (var i = 0; i < fallen.Length; i++)
        {
            var at = Radial(fallen[i].Angle, fallen[i].Radius, 0.012f);
            parts.Add(Box($"FallenLeaf{i}", 0.26f, 0.012f, 0.14f, at, i % 2 == 0 ? "tips-dry" : "roof-wilted", yaw: fallen[i].Yaw, roll: 4f));
        }
    }

    // ---- placement helpers ----

    /// <summary>The point at <paramref name="radius"/> from the centre in the direction <paramref name="angle"/>, at height y.</summary>
    private static Vector3 Radial(float angle, float radius, float y)
    {
        var a = angle * MathF.PI / 180f;
        return new Vector3(radius * MathF.Sin(a), y, radius * MathF.Cos(a));
    }

    /// <summary>
    /// A point on the wall at <paramref name="angle"/>: <paramref name="along"/> metres across it (to the right seen from
    /// outside), at height <paramref name="y"/>, <paramref name="outward"/> metres out from the wall's outer face.
    /// </summary>
    private static Vector3 OnWall(float angle, float along, float y, float outward) =>
        PartBox.Rotate(new Vector3(along, 0f, WallRadius + outward), 0f, angle, 0f) + new Vector3(0f, y, 0f);

    /// <summary>The point <paramref name="s"/> metres up the roof slope from the eave at <paramref name="angle"/>, lifted off it.</summary>
    private static Vector3 OnRoof(float angle, float s, float lift)
    {
        var radius = EaveRadius - s * SlopeCos + lift * SlopeSin;
        var y = WallTop + s * SlopeSin + lift * SlopeCos;
        return Radial(angle, radius, y);
    }

    /// <summary>
    /// A box lying on the roof slope at <paramref name="angle"/>: its X across the slope, its Y up it, its Z out of it,
    /// with its lower end lifted <paramref name="tilt"/>° clear of the slope.
    /// </summary>
    private static CropPart OnSlope(string name, float width, float length, float thickness, float angle, float s, float lift, string role, float tilt, float roll)
    {
        var at = OnRoof(angle, s, lift);
        return new CropPart(name, CropPartShape.Box, width, length, thickness, at.X, at.Y, at.Z, SlopeDegrees - 90f - tilt, angle, roll, role);
    }

    /// <summary>A thin pointed-arch pane (a six-sided disc facing out) on the wall at <paramref name="angle"/>.</summary>
    private static CropPart Pane(string name, float diameter, float thickness, Vector3 at, float angle, string role) =>
        new(name, CropPartShape.Cylinder, diameter, thickness, diameter, at.X, at.Y, at.Z, 90f, angle, 0f, role);

    /// <summary>A box from <paramref name="from"/> to <paramref name="to"/>, both in the vertical plane at <paramref name="angle"/>.</summary>
    private static CropPart Beam(string name, float sizeX, float sizeZ, Vector3 from, Vector3 to, float angle, string role)
    {
        var d = to - from;
        var outward = d.X * MathF.Sin(angle * MathF.PI / 180f) + d.Z * MathF.Cos(angle * MathF.PI / 180f);
        // Tilted from upright towards the outside of the house by the angle of the run against the rise.
        var tilt = MathF.Atan2(outward, d.Y) * 180f / MathF.PI;
        var centre = (from + to) / 2f;
        return new CropPart(name, CropPartShape.Box, sizeX, d.Length(), sizeZ, centre.X, centre.Y, centre.Z, tilt, angle, 0f, role);
    }

    private static CropPart Box(
        string name, float sizeX, float sizeY, float sizeZ, Vector3 at, string role, float pitch = 0f, float yaw = 0f, float roll = 0f) =>
        new(name, CropPartShape.Box, sizeX, sizeY, sizeZ, at.X, at.Y, at.Z, pitch, yaw, roll, role);

    /// <summary>A box rising from <paramref name="from"/>: tilted <paramref name="tilt"/>° from upright towards its yaw.</summary>
    private static CropPart Rising(string name, float sizeX, float length, float sizeZ, Vector3 from, float tilt, float yaw, string role)
    {
        var centre = from + PartBox.Rotate(new Vector3(0f, length / 2f, 0f), tilt, yaw, 0f);
        return new CropPart(name, CropPartShape.Box, sizeX, length, sizeZ, centre.X, centre.Y, centre.Z, tilt, yaw, 0f, role);
    }

    private static float Wrap(float degrees)
    {
        var wrapped = (degrees + 180f) % 360f;
        return (wrapped < 0f ? wrapped + 360f : wrapped) - 180f;
    }
}
