using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>
/// The farmhouse's parts (#104, ART_DIRECTION §7.1 "grown, not built"). Pure C#, no Godot: the Godot builder
/// <c>FarmhouseView</c> only merges these parts into one mesh under the shared palette material, so the whole house is
/// one opaque draw with no glow (REQ-14). Only the run-down state is built; the restored state (Phase 2, the carpenter)
/// is a list of its own to sit beside <see cref="RunDown"/>.
/// <para>
/// <b>Conventions.</b> The <see cref="CropPart"/> conventions (sizes, Godot's Euler YXZ rotations about the part's
/// centre, a positive pitch tipping +Y towards +Z, a positive roll lifting +X, yaw 0 facing +Z and yaw 90 +X), in
/// house-local metres: the origin is the centre of the footprint <b>on the ground</b> (y 0), the front (the door, facing
/// the plot) is <b>+Z</b>, the footprint is x −3..3, z −2..2 and the walls are 3 m tall, the same box as the house's
/// collision. A roof part lying on a slope is placed by its distance <c>s</c> up the slope from the eave edge and its
/// lift off the roof deck; its local Y runs down the slope and its pitch is ±(90° + the slope angle), less a 6° shingle
/// tilt for the leaves so each leaf's lower end lifts over the row below. Parts are named by group, and the tests read
/// the prefixes: <c>Roof*</c>, <c>Patch*</c>, <c>Chimney*</c>, <c>Door</c>, <c>Step</c>, <c>Weed*</c>,
/// <c>FallenLeaf*</c>, <c>Creeper*</c>.
/// </para>
/// <para>
/// <b>Solid, with dark gaps.</b> Behind the planks a <c>timber-trim</c> core fills the walls 0.03 m in from their outer
/// faces, and a <c>timber-trim</c> attic prism fills the roof, so every gap between planks, every missing plank and every
/// gap in the leaves shows a dark, solid face, never the sky. The core is recessed 0.57 m behind the door so the ajar door
/// can turn inwards (−Z) and keep the front inside z 2.1.
/// </para>
/// <list type="table">
/// <listheader><term>Group</term><description>Parts (run-down)</description></listheader>
/// <item><term>Core</term><description><c>CoreBack</c>, <c>CoreFrontWest</c>, <c>CoreFrontEast</c>, <c>CoreSill</c>,
/// <c>CoreLintel</c>: Boxes filling x ±2.97, z ±1.97, y 0..3, with the doorway (x ±0.55, y 0.35..2.41) recessed to
/// z 1.4, <c>timber-trim</c>.</description></item>
/// <item><term>Stone base</term><description><c>Plinth&lt;wall&gt;&lt;i&gt;</c>: Boxes 0.14 deep, 0.34–0.36 tall, around
/// the walls out to x ±3.06, z ±2.06, <c>stone</c>; <c>PlinthRock&lt;wall&gt;&lt;i&gt;</c>: proud blocks 0.32 x 0.2,
/// <c>rock</c>.</description></item>
/// <item><term>Walls</term><description><c>Wall&lt;S|N|E|W&gt;&lt;row&gt;_&lt;board&gt;</c>: nine rows of planks
/// 0.27 tall and 0.06 thick from y 0.35 to 3.0, 0.024 m apart, split into two or three boards with 0.02 m seams,
/// alternating <c>wall-grey</c> and <c>timber-side</c> by row, each a few millimetres proud or rolled 0.4° for the
/// weathered look. The front rows 0–6 stop at the doorway. Three boards are missing (front row 1 west, east row 2
/// middle, back row 5 middle), showing the core. <c>Grain*</c>: thin <c>timber-grain</c> lines, a wave on some planks
/// and a line in some seams.</description></item>
/// <item><term>Corner posts</term><description><c>Post&lt;c&gt;Seg0</c>–<c>Seg3</c>: four six-sided Cylinders stacked
/// at (±2.9, ±1.9) from the ground to y 3.45, where the post comes through the roof's corner, 0.30 tapering to 0.20
/// across, turned −15°, 0°, 15° and 30°, <c>trunk-grey</c>; <c>Post&lt;c&gt;Twig0</c>–<c>Twig2</c>: dead twigs, Boxes
/// 0.035 across and 0.35–0.45 long rising up and out from the post top, <c>trunk-grey</c>.</description></item>
/// <item><term>Door</term><description>In front of the core's dark recess: <c>Door</c> Box 1.0 x 1.48 x 0.05 and
/// <c>DoorArch</c> six-sided Cylinder 1.155 across (a pointed arch 1.0 wide at its shoulders), y 0.35..2.41,
/// <c>door-faded</c>, hinged on the west jamb (x −0.53, z 1.95) and turned 24° inwards; <c>DoorBrace0</c>/<c>1</c>,
/// <c>timber-grain</c>; <c>DoorKnob</c>, <c>rock</c>; <c>DoorJambWest</c>/<c>East</c> and the two
/// <c>DoorArchBoard</c>s over the opening, <c>timber-trim</c>.</description></item>
/// <item><term>Windows</term><description>At x ∓1.75, y 0.98..2.14: <c>WindowWestFrame</c>/<c>FrameArch</c> and
/// <c>WindowEastFrame</c>/<c>FrameArch</c>, <c>timber-trim</c>; the west one boarded up with three tilted
/// <c>WindowWestBoard</c>s (<c>timber-side</c>, <c>wall-grey</c>); the east one <c>WindowEastGlass</c>/<c>GlassArch</c>
/// (<c>glass-dull</c>), a <c>WindowEastMullion</c> and a zigzag <c>WindowEastCrack0</c>/<c>1</c> across the east pane
/// (<c>timber-trim</c>); a <c>timber-side</c> sill under each.</description></item>
/// <item><term>Step</term><description><c>Step</c>: Box 1.2 x 0.12 x 0.27 at z 2.02..2.29, <c>stone</c>.</description></item>
/// <item><term>Gables</term><description><c>Attic</c>: Prism 4.6 x 1.5 x 5.94 yawed 90°, y 3.0..4.5, z ±2.3, the
/// roof deck, <c>timber-trim</c>; <c>Gable&lt;E|W&gt;&lt;row&gt;_&lt;board&gt;</c>: five rows of planks cut to the
/// slope; <c>AtticWindow&lt;E|W&gt;Frame</c>/<c>Glass</c>/<c>Rib</c>: a leaf-shaped (pointed six-sided) window at
/// y 3.72.</description></item>
/// <item><term>Leaf roof</term><description>Ridge (deck apex) at y 4.5 running east–west, eaves at y 3.0 at
/// z ±2.3 (0.3 m out from the walls), gable edges at x ±3.3; slope 33.1°, 2.75 m long. Per slope (<c>F</c> front,
/// <c>B</c> back) six rows (s 0.45 + 0.4·row) of overlapping leaves, eight 0.9 m leaves in even rows and seven plus two
/// 0.45 m half leaves at the gable ends in odd rows: <c>RoofLeaf&lt;F|B&gt;&lt;row&gt;_&lt;i&gt;</c> Box 0.9 x 0.45 x
/// 0.04 and <c>RoofTip&lt;F|B&gt;&lt;row&gt;_&lt;i&gt;</c> Prism 0.9 x 0.28 x 0.04 pointing down the slope, in
/// <c>roof-wilted</c> (even rows) or <c>tips-dry</c> (odd rows and every tip), each rolled up to ±5°. Thinning rows
/// (front 4, back 3) have narrower 0.62 m leaves. Three gaps (two leaves gone in two rows): front x −1.43, rows 2–3
/// (under the patch), front x 1.25, rows 1–2, and back x −0.6, rows 3–4, each with three <c>RoofBatten</c>s showing,
/// <c>timber-trim</c>. <c>RoofRidge&lt;i&gt;</c>: eight Prism caps 0.8 x 0.35 along the ridge, top y 4.65;
/// <c>RoofFringe&lt;F|B&gt;&lt;i&gt;</c> and <c>RoofFringe&lt;E|W&gt;&lt;F|B&gt;&lt;i&gt;</c>: drooping dry tips
/// (Prisms pointing down, 0.15–0.25 long) under the eaves and the gable edges; <c>RoofFrond0</c>–<c>3</c>: one dry
/// frond rising from the ridge at x 0.9 and drooping over the front slope, top about y 4.83, <c>tips-dry</c>.</description></item>
/// <item><term>Hull-panel patch</term><description>On the front slope at x −1.43, s 1.35, 0.16 m off the deck so it sits
/// above the leaves, rolled 4°: <c>PatchPanel</c> Box 1.2 x 0.9 x 0.03, <c>ship-metal</c>; <c>PatchStripe</c> 1.2 x
/// 0.14, <c>navy</c>; <c>PatchRivet0</c>–<c>7</c>, <c>rock</c>, round its edge.</description></item>
/// <item><term>Chimney</term><description>On the back slope at (1.6, −1.15): <c>ChimneyStack</c> Box 0.6 x 1.52 x 0.6
/// from y 3.3 to 4.82 (about 0.8 m above the roof where it leaves it), <c>stone</c>; <c>ChimneyRock0</c>–<c>2</c>
/// proud stones and <c>ChimneyCap</c> 0.74 x 0.12 x 0.74, <c>rock</c>; <c>ChimneyFlue</c>, <c>timber-trim</c>. No
/// smoke.</description></item>
/// <item><term>At the base</term><description><c>Weed&lt;i&gt;A</c>/<c>B</c>: eight tufts of two crossed blade
/// Prisms along the front and sides, <c>foliage</c> or <c>grass-shade</c>; <c>Creeper&lt;i&gt;</c> and
/// <c>CreeperLeaf&lt;i&gt;</c>: a thin <c>foliage</c> creeper climbing the front-west post and onto the wall;
/// <c>FallenLeaf&lt;i&gt;</c>: eight flat leaves (<c>tips-dry</c>, <c>roof-wilted</c>) on the ground along the front
/// and sides, none on the step or the 1.2 m path in front of the door, all within z 2.3.</description></item>
/// </list>
/// <para>
/// Sizes differ from the issue's stated defaults within its 25%: the deck's ridge is y 4.5 so the ridge caps top out at
/// 4.65 and the frond at about 4.83 (the issue's "about 4.6"); the corner posts rise to 3.45 to come through the roof's
/// corners with their twigs (the part below 2.7 stays within 0.1 m of the footprint); the door is 2.06 m tall.
/// </para>
/// </summary>
public static class FarmhouseArt
{
    private const float HalfWidth = 3f;
    private const float HalfDepth = 2f;
    private const float Inset = 0.03f;
    private const float PlankDepth = 0.06f;
    private const float PlankHeight = 0.27f;
    private const int PlankRows = 9;
    private const float PlinthTop = 0.35f;
    private const float Eaves = 3f;
    private const float RidgeY = 4.5f;
    private const float EaveZ = 2.3f;
    private const float GableX = 3.3f;
    private const float Seam = 0.02f;

    private const float DoorHalf = 0.55f;
    private const float RecessZ = 1.4f;
    private const float DoorYaw = 24f;
    private const float WindowX = 1.75f;

    private const float LeafTilt = 6f;
    private const float LeafLength = 0.45f;
    private const float TipLength = 0.28f;
    private const float RowStart = 0.45f;
    private const float RowStep = 0.4f;
    private const int LeafRows = 6;

    private const float PlankPitch = (Eaves - PlinthTop) / PlankRows;

    /// <summary>The top of the doorway: the bottom of front plank row 7.</summary>
    private const float OpeningTop = PlinthTop + 7 * PlankPitch;

    private static readonly float SlopeSin = (RidgeY - Eaves) / MathF.Sqrt(EaveZ * EaveZ + (RidgeY - Eaves) * (RidgeY - Eaves));
    private static readonly float SlopeCos = EaveZ / MathF.Sqrt(EaveZ * EaveZ + (RidgeY - Eaves) * (RidgeY - Eaves));
    private static readonly float SlopeDegrees = MathF.Atan2(RidgeY - Eaves, EaveZ) * 180f / MathF.PI;

    /// <summary>The roof gaps: slope side (+1 front, −1 back), centre x and the first of the two rows with no leaves.</summary>
    private static readonly (int Side, float X, int Row)[] Gaps = [(1, -1.43f, 2), (1, 1.25f, 1), (-1, -0.6f, 3)];

    private static readonly IReadOnlyList<CropPart> RunDownParts = BuildRunDown();

    /// <summary>The run-down farmhouse of ART_DIRECTION §7.1, as on day one. Nothing glows.</summary>
    public static IReadOnlyList<CropPart> RunDown => RunDownParts;

    /// <summary>
    /// The axis-aligned bounds of the parts' rotated boxes (each part's unrotated box as in <see cref="CropArt"/>, rotated
    /// about its centre); all zero when there are no parts.
    /// </summary>
    public static (float MinX, float MaxX, float MinY, float MaxY, float MinZ, float MaxZ) Bounds(IEnumerable<CropPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var (min, max) = PartBox.Extents(parts.SelectMany(part => PartBox.Corners(
            part.Shape, part.SizeX, part.SizeY, part.SizeZ, new Vector3(part.X, part.Y, part.Z),
            part.PitchDegrees, part.YawDegrees, part.RollDegrees)));
        return (min.X, max.X, min.Y, max.Y, min.Z, max.Z);
    }

    private static List<CropPart> BuildRunDown()
    {
        var parts = new List<CropPart>();
        Core(parts);
        Plinth(parts);
        Walls(parts);
        Grain(parts);
        Posts(parts);
        Door(parts);
        Windows(parts);
        parts.Add(Box("Step", 1.2f, 0.12f, 0.27f, new Vector3(0f, 0.06f, 2.155f), "stone"));
        Gables(parts);
        Leaves(parts);
        RoofEdges(parts);
        Patch(parts);
        Chimney(parts);
        Base(parts);
        return parts;
    }

    // ---- the solid core, the stone base and the walls ----

    private static void Core(List<CropPart> parts)
    {
        const float inX = HalfWidth - Inset;
        const float inZ = HalfDepth - Inset;
        const float frontDepth = inZ - RecessZ;
        const float frontZ = (inZ + RecessZ) / 2f;
        const float sideWidth = inX - DoorHalf;
        parts.Add(Box("CoreBack", 2f * inX, Eaves, inZ + RecessZ, new Vector3(0f, Eaves / 2f, (RecessZ - inZ) / 2f), "timber-trim"));
        parts.Add(Box("CoreFrontWest", sideWidth, Eaves, frontDepth, new Vector3(-(inX + DoorHalf) / 2f, Eaves / 2f, frontZ), "timber-trim"));
        parts.Add(Box("CoreFrontEast", sideWidth, Eaves, frontDepth, new Vector3((inX + DoorHalf) / 2f, Eaves / 2f, frontZ), "timber-trim"));
        parts.Add(Box("CoreSill", 2f * DoorHalf, PlinthTop, frontDepth, new Vector3(0f, PlinthTop / 2f, frontZ), "timber-trim"));
        parts.Add(Box("CoreLintel", 2f * DoorHalf, Eaves - OpeningTop, frontDepth, new Vector3(0f, (Eaves + OpeningTop) / 2f, frontZ), "timber-trim"));
    }

    private static void Plinth(List<CropPart> parts)
    {
        const float depth = 0.14f;
        const float outX = HalfWidth + 0.06f;
        const float outZ = HalfDepth + 0.06f;
        float[] heights = [0.35f, 0.36f, 0.34f];
        float[] alongX = [-outX, -1.0f, 1.1f, outX];
        float[] alongZ = [-outZ, -0.3f, outZ];
        for (var side = -1; side <= 1; side += 2)
        {
            var wall = side > 0 ? "S" : "N";
            for (var i = 0; i < alongX.Length - 1; i++)
            {
                var h = heights[i];
                parts.Add(Box($"Plinth{wall}{i}", alongX[i + 1] - alongX[i] - 0.01f, h, depth,
                    new Vector3((alongX[i] + alongX[i + 1]) / 2f, h / 2f, side * (outZ - depth / 2f)), "stone"));
            }

            parts.Add(Box($"PlinthRock{wall}0", 0.32f, 0.2f, 0.03f, new Vector3(side * -2.3f, 0.17f, side * (outZ + 0.015f)), "rock"));
            parts.Add(Box($"PlinthRock{wall}1", 0.28f, 0.18f, 0.03f, new Vector3(side * 1.9f, 0.19f, side * (outZ + 0.015f)), "rock"));

            var end = side > 0 ? "E" : "W";
            for (var i = 0; i < alongZ.Length - 1; i++)
            {
                var h = heights[(i + 1) % heights.Length];
                parts.Add(Box($"Plinth{end}{i}", depth, h, alongZ[i + 1] - alongZ[i] - 0.01f,
                    new Vector3(side * (outX - depth / 2f), h / 2f, (alongZ[i] + alongZ[i + 1]) / 2f), "stone"));
            }

            parts.Add(Box($"PlinthRock{end}0", 0.03f, 0.2f, 0.34f, new Vector3(side * (outX + 0.015f), 0.18f, side * 0.9f), "rock"));
        }
    }

    private static float RowY(int row) => PlinthTop + row * PlankPitch + PlankHeight / 2f;

    private static void Walls(List<CropPart> parts)
    {
        for (var row = 0; row < PlankRows; row++)
        {
            float[][] front = row < 7
                ? [[-HalfWidth, -2.0f + 0.35f * (row % 3 - 1)], [-2.0f + 0.35f * (row % 3 - 1), -DoorHalf], [DoorHalf, 1.7f - 0.3f * (row % 2)], [1.7f - 0.3f * (row % 2), HalfWidth]]
                : [[-HalfWidth, -1.0f + 0.3f * (row % 2)], [-1.0f + 0.3f * (row % 2), 1.1f], [1.1f, HalfWidth]];
            // The doorway splits the front into separate boards, so only abutting boards leave a seam.
            Planks(parts, "S", row, front, missing: row == 1 ? 0 : -1);

            float[] backSplits = [-HalfWidth, -1.0f + 0.3f * (row % 3 - 1), 0.8f + 0.25f * (row % 2), HalfWidth];
            Planks(parts, "N", row, Spans(backSplits), missing: row == 5 ? 1 : -1);

            const float sideHalf = HalfDepth - PlankDepth;
            float[] sideSplits = [-sideHalf, -0.6f + 0.25f * (row % 2), 0.7f - 0.2f * (row % 3 - 1), sideHalf];
            Planks(parts, "E", row, Spans(sideSplits), missing: row == 2 ? 1 : -1);
            Planks(parts, "W", row, Spans(sideSplits.Select(z => -z).Reverse().ToArray()), missing: -1);
        }
    }

    private static float[][] Spans(float[] splits) =>
        Enumerable.Range(0, splits.Length - 1).Select(i => new[] { splits[i], splits[i + 1] }).ToArray();

    /// <summary>One row of boards on a wall (S front, N back, E, W), each span from–to along the wall, seams between.</summary>
    private static void Planks(List<CropPart> parts, string wall, int row, float[][] spans, int missing)
    {
        var y = RowY(row);
        var colourRow = wall is "E" or "W" ? row + 1 : row;
        var role = colourRow % 2 == 0 ? "wall-grey" : "timber-side";
        for (var b = 0; b < spans.Length; b++)
        {
            if (b == missing) continue;
            var (from, to) = (spans[b][0], spans[b][1]);
            // Trim half a seam where a board meets its neighbour, not where it meets a corner or the doorway.
            var startsAtNeighbour = b > 0 && MathF.Abs(spans[b - 1][1] - from) < 1e-4f;
            var endsAtNeighbour = b < spans.Length - 1 && MathF.Abs(spans[b + 1][0] - to) < 1e-4f;
            var start = from + (startsAtNeighbour ? Seam / 2f : 0f);
            var end = to - (endsAtNeighbour ? Seam / 2f : 0f);
            var length = end - start;
            var middle = (start + end) / 2f;
            var proud = 0.003f * ((row * 5 + b * 3) % 3);
            var tilt = 0.4f * ((row + b) % 3 - 1);
            var name = $"Wall{wall}{row}_{b}";
            parts.Add(wall switch
            {
                "S" => Box(name, length, PlankHeight, PlankDepth, new Vector3(middle, y, HalfDepth - PlankDepth / 2f + proud), role, roll: tilt),
                "N" => Box(name, length, PlankHeight, PlankDepth, new Vector3(middle, y, -(HalfDepth - PlankDepth / 2f + proud)), role, roll: tilt),
                "E" => Box(name, PlankDepth, PlankHeight, length, new Vector3(HalfWidth - PlankDepth / 2f + proud, y, middle), role, pitch: tilt),
                _ => Box(name, PlankDepth, PlankHeight, length, new Vector3(-(HalfWidth - PlankDepth / 2f + proud), y, middle), role, pitch: tilt),
            });
        }
    }

    /// <summary>Wavy grain on some planks and a lighter line in some seams, <c>timber-grain</c>.</summary>
    private static void Grain(List<CropPart> parts)
    {
        const float face = HalfDepth + 0.008f;
        (float X, int Row, float Length)[] waves = [(-2.5f, 3, 0.9f), (2.3f, 4, 1.0f), (-1.0f, 8, 1.2f), (0.9f, 2, 0.8f)];
        for (var i = 0; i < waves.Length; i++)
        {
            var (x, row, length) = waves[i];
            var y = RowY(row) + 0.03f;
            parts.Add(Box($"GrainS{i}a", length / 2f, 0.014f, 0.008f, new Vector3(x - length / 4f, y, face), "timber-grain", roll: 4f));
            parts.Add(Box($"GrainS{i}b", length / 2f, 0.014f, 0.008f, new Vector3(x + length / 4f, y, face), "timber-grain", roll: -4f));
            parts.Add(Box($"GrainN{i}", length, 0.014f, 0.008f, new Vector3(-x, y - 0.05f, -face), "timber-grain", roll: 2f));
        }

        // Lines in the seams (a little behind the plank faces): the seam below rows 2, 5 and 7.
        int[] seams = [2, 5, 7];
        for (var i = 0; i < seams.Length; i++)
        {
            var y = PlinthTop + seams[i] * PlankPitch - (PlankPitch - PlankHeight) / 2f;
            parts.Add(Box($"GrainSeamE{i}", 0.01f, 0.012f, 1.6f, new Vector3(HalfWidth - 0.012f, y, -0.6f + 0.5f * i), "timber-grain"));
            parts.Add(Box($"GrainSeamW{i}", 0.01f, 0.012f, 1.4f, new Vector3(-(HalfWidth - 0.012f), y, 0.5f - 0.4f * i), "timber-grain"));
        }
    }

    // ---- the living (here dead) corner posts ----

    private static void Posts(List<CropPart> parts)
    {
        (float Bottom, float Top, float BottomD, float TopD, float Yaw)[] segments =
        [
            (0f, 0.9f, 0.30f, 0.28f, -15f),
            (0.9f, 1.8f, 0.28f, 0.27f, 0f),
            (1.8f, 2.71f, 0.27f, 0.25f, 15f),
            (2.71f, 3.45f, 0.25f, 0.20f, 30f),
        ];
        var c = 0;
        foreach (var (sx, sz) in new[] { (-1, 1), (1, 1), (1, -1), (-1, -1) })
        {
            for (var i = 0; i < segments.Length; i++)
            {
                var (bottom, top, bottomD, topD, yaw) = segments[i];
                // A slight inward wobble from segment to segment, so the stack reads twisted, not turned.
                var at = new Vector3(sx * (2.9f - 0.015f * (i % 2)), (bottom + top) / 2f, sz * (1.9f - 0.012f * ((i + 1) % 2)));
                parts.Add(new CropPart($"Post{c}Seg{i}", CropPartShape.Cylinder, bottomD, top - bottom, topD,
                    at.X, at.Y, at.Z, 0f, yaw + 7f * c, 0f, "trunk-grey"));
            }

            var outward = MathF.Atan2(sx, sz) * 180f / MathF.PI;
            var tip = new Vector3(sx * 2.9f, 3.42f, sz * 1.9f);
            (float Yaw, float Tilt, float Length)[] twigs = [(outward - 55f, 55f, 0.45f), (outward + 5f, 62f, 0.38f), (outward + 60f, 42f, 0.35f)];
            for (var i = 0; i < twigs.Length; i++)
            {
                parts.Add(Rising($"Post{c}Twig{i}", 0.035f, twigs[i].Length, 0.035f, tip, twigs[i].Tilt, twigs[i].Yaw, "trunk-grey"));
            }

            c++;
        }
    }

    // ---- the front: the ajar door, the windows ----

    private static void Door(List<CropPart> parts)
    {
        const float hingeX = -0.53f;
        const float hingeZ = 1.95f;
        const float width = 1.0f;
        const float thickness = 0.05f;
        const float archDiameter = 1.155f;
        const float archCentre = 1.83f;
        var hinge = new Vector3(hingeX, 0f, hingeZ);
        Vector3 OnDoor(float along, float y, float outward) => hinge + PartBox.Rotate(new Vector3(along, 0f, outward), 0f, DoorYaw, 0f) + new Vector3(0f, y, 0f);

        var leaf = OnDoor(width / 2f, (PlinthTop + archCentre) / 2f, 0f);
        parts.Add(Box("Door", width, archCentre - PlinthTop, thickness, leaf, "door-faded", yaw: DoorYaw));
        var arch = OnDoor(width / 2f, archCentre, 0f);
        parts.Add(new CropPart("DoorArch", CropPartShape.Cylinder, archDiameter, thickness, archDiameter, arch.X, arch.Y, arch.Z, 90f, DoorYaw, 0f, "door-faded"));
        parts.Add(Box("DoorBrace0", 0.86f, 0.07f, 0.02f, OnDoor(width / 2f, 0.7f, thickness / 2f + 0.01f), "timber-grain", yaw: DoorYaw));
        parts.Add(Box("DoorBrace1", 0.86f, 0.07f, 0.02f, OnDoor(width / 2f, 1.55f, thickness / 2f + 0.01f), "timber-grain", yaw: DoorYaw));
        var knob = OnDoor(0.85f, 1.12f, thickness / 2f + 0.03f);
        parts.Add(new CropPart("DoorKnob", CropPartShape.Sphere, 0.07f, 0.07f, 0.07f, knob.X, knob.Y, knob.Z, 0f, 0f, 0f, "rock"));

        // The frame round the opening: jambs, and two boards whose lower edges make the pointed arch head.
        const float frameZ = HalfDepth + 0.015f;
        const float shoulder = 2.10f;
        parts.Add(Box("DoorJambWest", 0.1f, shoulder - PlinthTop, 0.03f, new Vector3(-DoorHalf - 0.05f, (PlinthTop + shoulder) / 2f, frameZ), "timber-trim"));
        parts.Add(Box("DoorJambEast", 0.1f, shoulder - PlinthTop, 0.03f, new Vector3(DoorHalf + 0.05f, (PlinthTop + shoulder) / 2f, frameZ), "timber-trim"));
        var rise = 0.35f;
        var run = DoorHalf + 0.05f;
        var angle = MathF.Atan2(rise, run) * 180f / MathF.PI;
        var length = MathF.Sqrt(rise * rise + run * run) + 0.05f;
        const float boardHeight = 0.3f;
        for (var side = -1; side <= 1; side += 2)
        {
            // Lower edge from the shoulder (side·run, shoulder) to the apex (0, shoulder + rise); the board above it.
            var mid = new Vector3(side * run / 2f, shoulder + rise / 2f, frameZ + 0.002f);
            var up = PartBox.Rotate(new Vector3(0f, boardHeight / 2f, 0f), 0f, 0f, -side * angle);
            parts.Add(Box(side < 0 ? "DoorArchBoardWest" : "DoorArchBoardEast", length, boardHeight, 0.03f, mid + up, "timber-trim", roll: -side * angle));
        }
    }

    private static void Windows(List<CropPart> parts)
    {
        const float sill = 0.98f;
        const float archCentre = 1.6f;
        const float frameZ = HalfDepth + 0.015f;
        const float glassZ = HalfDepth + 0.035f;
        foreach (var (side, name) in new[] { (-1, "West"), (1, "East") })
        {
            var x = side * WindowX;
            parts.Add(Box($"Window{name}Frame", 0.94f, archCentre - sill, 0.03f, new Vector3(x, (sill + archCentre) / 2f, frameZ), "timber-trim"));
            parts.Add(new CropPart($"Window{name}FrameArch", CropPartShape.Cylinder, 1.08f, 0.03f, 1.08f, x, archCentre, frameZ, 90f, 0f, 0f, "timber-trim"));
            parts.Add(Box($"Window{name}Sill", 1.04f, 0.06f, 0.09f, new Vector3(x, sill - 0.03f, HalfDepth + 0.045f), "timber-side"));
        }

        // West: boarded up, three boards nailed across at slight angles.
        (float Y, float Width, float Roll, string Role)[] boards = [(1.18f, 1.02f, 7f, "timber-side"), (1.52f, 1.0f, -5f, "wall-grey"), (1.86f, 0.7f, 9f, "timber-side")];
        for (var i = 0; i < boards.Length; i++)
        {
            var (y, width, roll, role) = boards[i];
            parts.Add(Box($"WindowWestBoard{i}", width, 0.15f, 0.03f, new Vector3(-WindowX + 0.02f * (i - 1), y, HalfDepth + 0.045f), role, roll: roll));
        }

        // East: dull glass in two panes, cracked across the east one.
        parts.Add(Box("WindowEastGlass", 0.74f, archCentre - sill - 0.05f, 0.01f, new Vector3(WindowX, (sill + 0.05f + archCentre) / 2f, glassZ), "glass-dull"));
        parts.Add(new CropPart("WindowEastGlassArch", CropPartShape.Cylinder, 0.86f, 0.01f, 0.86f, WindowX, archCentre, glassZ, 90f, 0f, 0f, "glass-dull"));
        parts.Add(Box("WindowEastMullion", 0.04f, 1.0f, 0.012f, new Vector3(WindowX, 1.52f, glassZ + 0.011f), "timber-trim"));
        parts.Add(Box("WindowEastCrack0", 0.2f, 0.014f, 0.006f, new Vector3(WindowX + 0.12f, 1.42f, glassZ + 0.008f), "timber-trim", roll: 35f));
        parts.Add(Box("WindowEastCrack1", 0.18f, 0.014f, 0.006f, new Vector3(WindowX + 0.26f, 1.44f, glassZ + 0.008f), "timber-trim", roll: -30f));
    }

    // ---- the gables, the leaf roof, the patch and the chimney ----

    private static void Gables(List<CropPart> parts)
    {
        parts.Add(new CropPart("Attic", CropPartShape.Prism, 2f * EaveZ, RidgeY - Eaves, 2f * (HalfWidth - Inset),
            0f, (Eaves + RidgeY) / 2f, 0f, 0f, 90f, 0f, "timber-trim"));
        const float rowPitch = 0.25f;
        const float rowHeight = 0.23f;
        for (var side = -1; side <= 1; side += 2)
        {
            var end = side > 0 ? "E" : "W";
            var x = side * (HalfWidth - PlankDepth / 2f);
            for (var row = 0; row < 5; row++)
            {
                var bottom = Eaves + row * rowPitch;
                var top = bottom + rowHeight;
                var half = EaveZ * (RidgeY - top) / (RidgeY - Eaves) - 0.04f;
                var role = row % 2 == 0 ? "timber-side" : "wall-grey";
                var split = row < 3 ? 0.3f * side * (row - 1) : float.NaN;
                if (float.IsNaN(split))
                {
                    parts.Add(Box($"Gable{end}{row}_0", PlankDepth, rowHeight, 2f * half, new Vector3(x, bottom + rowHeight / 2f, 0f), role));
                    continue;
                }

                parts.Add(Box($"Gable{end}{row}_0", PlankDepth, rowHeight, split + half - Seam / 2f, new Vector3(x, bottom + rowHeight / 2f, (split - half - Seam / 2f) / 2f), role));
                parts.Add(Box($"Gable{end}{row}_1", PlankDepth, rowHeight, half - split - Seam / 2f, new Vector3(x, bottom + rowHeight / 2f, (split + half + Seam / 2f) / 2f), role, pitch: 0.5f));
            }

            const float windowY = 3.72f;
            parts.Add(new CropPart($"AtticWindow{end}Frame", CropPartShape.Cylinder, 0.56f, 0.03f, 0.56f, side * (HalfWidth + 0.015f), windowY, 0f, 90f, 90f, 0f, "timber-trim"));
            parts.Add(new CropPart($"AtticWindow{end}Glass", CropPartShape.Cylinder, 0.42f, 0.01f, 0.42f, side * (HalfWidth + 0.035f), windowY, 0f, 90f, 90f, 0f, "glass-dull"));
            parts.Add(Box($"AtticWindow{end}Rib", 0.012f, 0.4f, 0.02f, new Vector3(side * (HalfWidth + 0.043f), windowY, 0f), "timber-trim"));
        }
    }

    /// <summary>A point on a roof slope (+1 front, −1 back): <paramref name="s"/> metres up from the eave edge, lifted off the deck.</summary>
    private static Vector3 OnSlope(int side, float x, float s, float lift) =>
        new(x, Eaves + s * SlopeSin + lift * SlopeCos, side * (EaveZ - s * SlopeCos + lift * SlopeSin));

    /// <summary>The pitch that lays a part's local Y down a slope, its local Z across the deck, less a shingle tilt.</summary>
    private static float SlopePitch(int side, float tilt) => side * (90f + SlopeDegrees - tilt);

    private static bool InGap(int side, int row, float x) =>
        Gaps.Any(gap => gap.Side == side && (row == gap.Row || row == gap.Row + 1) && MathF.Abs(x - gap.X) < 0.5f);

    private static void Leaves(List<CropPart> parts)
    {
        const float width = 0.9f;
        const float step = (2f * GableX - width) / 7f;
        for (var side = -1; side <= 1; side += 2)
        {
            var slope = side > 0 ? "F" : "B";
            var thinRow = side > 0 ? 4 : 3;
            var pitch = SlopePitch(side, LeafTilt);
            for (var row = 0; row < LeafRows; row++)
            {
                var s = RowStart + row * RowStep;
                var role = row % 2 == 0 ? "roof-wilted" : "tips-dry";
                var leaves = new List<(float X, float Width)>();
                if (row % 2 == 0)
                {
                    for (var i = 0; i < 8; i++) leaves.Add((-GableX + width / 2f + i * step, width));
                }
                else
                {
                    leaves.Add((-GableX + width / 4f, width / 2f));
                    for (var i = 0; i < 7; i++) leaves.Add((-GableX + width / 2f + step / 2f + i * step, width));
                    leaves.Add((GableX - width / 4f, width / 2f));
                }

                for (var i = 0; i < leaves.Count; i++)
                {
                    var (x, w) = leaves[i];
                    if (InGap(side, row, x)) continue;
                    if (row == thinRow && MathF.Abs(x) < 2.4f) w = 0.62f;
                    var roll = 2.5f * ((i * 7 + row * 3 + (side > 0 ? 0 : 2)) % 5 - 2);
                    var lift = 0.05f + 0.008f * (i % 2);
                    var centre = OnSlope(side, x, s, lift);
                    parts.Add(Box($"RoofLeaf{slope}{row}_{i}", w, LeafLength, 0.04f, centre, role, pitch: pitch, roll: roll));
                    var tip = centre + PartBox.Rotate(new Vector3(0f, LeafLength / 2f + TipLength / 2f, 0f), pitch, 0f, roll);
                    parts.Add(new CropPart($"RoofTip{slope}{row}_{i}", CropPartShape.Prism, w, TipLength, 0.04f,
                        tip.X, tip.Y, tip.Z, pitch, 0f, roll, "tips-dry"));
                }
            }
        }

        // Battens showing through each gap, across the band the missing leaves leave bare.
        for (var g = 0; g < Gaps.Length; g++)
        {
            var (side, x, row) = Gaps[g];
            var bare = 0.51f + RowStep * row;
            for (var b = 0; b < 3; b++)
            {
                var at = OnSlope(side, x, bare + 0.17f * (b - 1), 0.02f);
                parts.Add(Box($"RoofBatten{g}_{b}", 1.3f, 0.07f, 0.035f, at, "timber-trim", pitch: SlopePitch(side, 0f)));
            }
        }
    }

    private static void RoofEdges(List<CropPart> parts)
    {
        // Ridge caps: Prisms across the ridge, extruded along it.
        const float capLength = 0.86f;
        for (var i = 0; i < 8; i++)
        {
            var x = -GableX + capLength / 2f + i * (2f * GableX - capLength) / 7f;
            parts.Add(new CropPart($"RoofRidge{i}", CropPartShape.Prism, 0.8f, 0.35f, capLength, x, 4.475f, 0f, 0f, 90f, 0f,
                i % 2 == 0 ? "roof-wilted" : "tips-dry"));
        }

        float[] lengths = [0.15f, 0.2f, 0.25f, 0.18f];
        // Drooping dry tips under the front and back eaves, pointing down and a little out.
        for (var side = -1; side <= 1; side += 2)
        {
            var slope = side > 0 ? "F" : "B";
            for (var i = 0; i < 19; i++)
            {
                var x = -3.15f + i * 0.35f;
                var h = lengths[(i * 3 + (side > 0 ? 0 : 1)) % lengths.Length];
                var at = new Vector3(x, Eaves + 0.03f - h / 2f * MathF.Cos(12f * MathF.PI / 180f), side * (EaveZ + 0.02f + h / 2f * MathF.Sin(12f * MathF.PI / 180f)));
                parts.Add(new CropPart($"RoofFringe{slope}{i}", CropPartShape.Prism, 0.22f, h, 0.02f, at.X, at.Y, at.Z, -side * 12f, 0f, 180f, "tips-dry"));
            }
        }

        // ...and under the gable edges, facing out east and west.
        for (var end = -1; end <= 1; end += 2)
        {
            var gable = end > 0 ? "E" : "W";
            for (var side = -1; side <= 1; side += 2)
            {
                var slope = side > 0 ? "F" : "B";
                for (var i = 0; i < 6; i++)
                {
                    var s = 0.25f + i * 0.4f;
                    var h = lengths[(i + (side > 0 ? 1 : 3) + (end > 0 ? 0 : 2)) % lengths.Length];
                    var edge = OnSlope(side, end * (GableX + 0.01f), s, 0.04f);
                    parts.Add(new CropPart($"RoofFringe{gable}{slope}{i}", CropPartShape.Prism, 0.2f, h, 0.02f,
                        edge.X, edge.Y - h / 2f, edge.Z, 0f, 90f, 180f, "tips-dry"));
                }
            }
        }

        // One dry frond rising from the ridge and drooping over the front slope.
        var at0 = new Vector3(0.9f, 4.6f, 0f);
        (float Tilt, float Width)[] frond = [(35f, 0.12f), (75f, 0.1f), (110f, 0.08f), (140f, 0.06f)];
        const float segment = 0.2f;
        for (var i = 0; i < frond.Length; i++)
        {
            var part = Rising($"RoofFrond{i}", frond[i].Width, segment, 0.02f, at0, frond[i].Tilt, 0f, "tips-dry");
            parts.Add(part);
            at0 += PartBox.Rotate(new Vector3(0f, segment, 0f), frond[i].Tilt, 0f, 0f);
        }
    }

    private static void Patch(List<CropPart> parts)
    {
        const float x = -1.43f;
        const float s = 1.35f;
        const float lift = 0.16f;
        const float roll = 4f;
        var pitch = SlopePitch(1, 0f);
        var normal = new Vector3(0f, SlopeCos, SlopeSin);
        var centre = OnSlope(1, x, s, lift);
        Vector3 OnPanel(float across, float down, float above) => centre + PartBox.Rotate(new Vector3(across, down, 0f), pitch, 0f, roll) + normal * above;

        parts.Add(Box("PatchPanel", 1.2f, 0.9f, 0.03f, centre, "ship-metal", pitch: pitch, roll: roll));
        parts.Add(Box("PatchStripe", 1.2f, 0.14f, 0.012f, OnPanel(0f, 0.12f, 0.018f), "navy", pitch: pitch, roll: roll));
        (float Across, float Down)[] rivets = [(-0.54f, -0.39f), (0f, -0.39f), (0.54f, -0.39f), (0.54f, 0f), (0.54f, 0.39f), (0f, 0.39f), (-0.54f, 0.39f), (-0.54f, 0f)];
        for (var i = 0; i < rivets.Length; i++)
        {
            parts.Add(Box($"PatchRivet{i}", 0.05f, 0.05f, 0.03f, OnPanel(rivets[i].Across, rivets[i].Down, 0.02f), "rock", pitch: pitch, roll: roll));
        }
    }

    private static void Chimney(List<CropPart> parts)
    {
        const float x = 1.6f;
        const float z = -1.15f;
        const float bottom = 3.3f;
        const float top = 4.82f;
        parts.Add(Box("ChimneyStack", 0.6f, top - bottom, 0.6f, new Vector3(x, (bottom + top) / 2f, z), "stone"));
        parts.Add(Box("ChimneyRock0", 0.22f, 0.14f, 0.04f, new Vector3(x - 0.1f, 4.5f, z + 0.31f), "rock"));
        parts.Add(Box("ChimneyRock1", 0.04f, 0.16f, 0.24f, new Vector3(x + 0.31f, 4.35f, z + 0.05f), "rock"));
        parts.Add(Box("ChimneyRock2", 0.04f, 0.14f, 0.2f, new Vector3(x - 0.31f, 4.6f, z - 0.08f), "rock"));
        parts.Add(Box("ChimneyCap", 0.74f, 0.12f, 0.74f, new Vector3(x, top + 0.05f, z), "rock"));
        parts.Add(Box("ChimneyFlue", 0.34f, 0.02f, 0.34f, new Vector3(x, top + 0.115f, z), "timber-trim"));
    }

    // ---- at the base: weeds, the creeper, fallen leaves ----

    private static void Base(List<CropPart> parts)
    {
        (float X, float Z)[] weeds = [(-2.6f, 2.16f), (-1.15f, 2.17f), (1.15f, 2.16f), (2.55f, 2.17f), (3.18f, 1.2f), (3.17f, -0.9f), (-3.18f, 0.6f), (-3.17f, -1.4f)];
        for (var i = 0; i < weeds.Length; i++)
        {
            var (x, z) = weeds[i];
            var role = i % 2 == 0 ? "foliage" : "grass-shade";
            var height = 0.2f + 0.04f * (i % 3);
            parts.Add(new CropPart($"Weed{i}A", CropPartShape.Prism, 0.18f, height, 0.02f, x, height / 2f + 0.015f, z, 0f, 15f * i, 8f, role));
            parts.Add(new CropPart($"Weed{i}B", CropPartShape.Prism, 0.16f, height * 0.8f, 0.02f, x + 0.03f, height * 0.4f + 0.015f, z, 0f, 15f * i + 90f, -8f,
                i % 2 == 0 ? "grass-shade" : "foliage"));
        }

        // The creeper: a zigzag strand up the front face of the front-west post, and a second strand onto the wall.
        var at = new Vector3(-2.9f, 0.05f, 2.068f);
        const float strand = 0.42f;
        for (var i = 0; i < 5; i++)
        {
            var roll = i % 2 == 0 ? 20f : -20f;
            var dir = PartBox.Rotate(new Vector3(0f, strand, 0f), 0f, 0f, roll);
            var centre = at + dir / 2f;
            parts.Add(Box($"Creeper{i}", 0.035f, strand, 0.015f, centre, "foliage", roll: roll));
            if (i > 0) parts.Add(Box($"CreeperLeaf{i}", 0.09f, 0.06f, 0.015f, at + new Vector3(i % 2 == 0 ? 0.07f : 0.03f, 0f, 0.004f), "foliage", roll: 30f));
            at += dir;
        }

        var wall = new Vector3(-2.8f, 0.4f, HalfDepth + 0.012f);
        for (var i = 0; i < 3; i++)
        {
            var roll = -55f + 15f * i;
            var dir = PartBox.Rotate(new Vector3(0f, 0.36f, 0f), 0f, 0f, roll);
            parts.Add(Box($"CreeperWall{i}", 0.03f, 0.36f, 0.012f, wall + dir / 2f, "foliage", roll: roll));
            wall += dir;
        }

        // Fallen leaves along the front (clear of the step and the 1.2 m path) and down the sides.
        (float X, float Z, float Yaw)[] fallen = [(-2.25f, 2.13f, 30f), (-1.5f, 2.13f, -50f), (1.0f, 2.13f, 70f), (2.0f, 2.13f, -20f), (3.35f, 1.5f, 15f), (3.32f, -0.8f, 110f), (-3.35f, 0.9f, 60f), (-3.3f, -1.6f, -35f)];
        for (var i = 0; i < fallen.Length; i++)
        {
            var (x, z, yaw) = fallen[i];
            parts.Add(Box($"FallenLeaf{i}", 0.26f, 0.012f, 0.14f, new Vector3(x, 0.012f, z), i % 2 == 0 ? "tips-dry" : "roof-wilted", yaw: yaw, roll: 4f));
        }
    }

    // ---- helpers ----

    private static CropPart Box(
        string name, float sizeX, float sizeY, float sizeZ, Vector3 at, string role, float pitch = 0f, float yaw = 0f, float roll = 0f) =>
        new(name, CropPartShape.Box, sizeX, sizeY, sizeZ, at.X, at.Y, at.Z, pitch, yaw, roll, role);

    /// <summary>A box rising from <paramref name="from"/>: tilted <paramref name="tilt"/>° from upright towards its yaw.</summary>
    private static CropPart Rising(string name, float sizeX, float length, float sizeZ, Vector3 from, float tilt, float yaw, string role)
    {
        var centre = from + PartBox.Rotate(new Vector3(0f, length / 2f, 0f), tilt, yaw, 0f);
        return new CropPart(name, CropPartShape.Box, sizeX, length, sizeZ, centre.X, centre.Y, centre.Z, tilt, yaw, 0f, role);
    }
}
