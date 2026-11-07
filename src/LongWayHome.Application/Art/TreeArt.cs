using System.Numerics;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

/// <summary>A tree's growth or chopping state (ART_DIRECTION §5.1, "Chopping and regrowth").</summary>
public enum TreeStage
{
    /// <summary>A small version of the tree: what regrows every couple of days after a chop.</summary>
    Sapling,

    /// <summary>A full tree: about 5 wood with the basic axe.</summary>
    Full,

    /// <summary>An old giant, about 1.3 times bigger: needs the steel axe, gives the most wood.</summary>
    Giant,

    /// <summary>What a chop leaves: glowing sap rings, a dribble of sap, wood chips and a new shoot.</summary>
    Stump,
}

/// <summary>
/// The Twinbough, the meadow's oak-like native tree (ART_DIRECTION §5.1 and the concept art's Trees page): a twisted
/// violet trunk that splits in two, a puffy blue-green canopy with a violet underside, and an amber sap seam in the bark
/// that glows at night. Pure C#, no Godot: the sandbox's builder <c>NatureModels</c> merges these parts into one mesh
/// under the shared palette material. For now it is shown only in the art sandbox.
/// <para>
/// <b>Conventions.</b> The <see cref="CropPart"/> conventions (sizes in metres, Godot's Euler YXZ rotations about the
/// part's centre), with the origin at the foot of the trunk <b>on the ground</b> (y 0). A full tree is about 4.9 m tall
/// with a canopy about 3.6 m across; a sapling is the same tree at 0.32 times the size and a giant at 1.28 times.
/// </para>
/// <para>
/// <b>Detail.</b> An eight-sided trunk in four twisting segments with spiral bark grooves and ridges, five tapering root
/// flares, two limbs that bend apart and fork into branches reaching up into the canopy, and a canopy of nine big
/// twelve-sided puffs, each roughened by smaller clumps, in three tones of the season's colour: lighter on top, darker
/// underneath, over a violet underside.
/// </para>
/// <para>
/// <b>Seasons.</b> Spring: a lighter canopy with clusters of pink-violet blossom. Summer: the full blue-green canopy.
/// Fall: a magenta canopy with a dark violet underside and fallen leaves round the foot. Winter: bare branches forking
/// into twigs, with snow along them and in the forks. The stump is the same in every season. Only the sap glows
/// (<c>lantern</c>: the seams on the trunk and the stump's rings and dribble).
/// </para>
/// Parts are named by group: <c>Trunk*</c>, <c>Root*</c>, <c>Limb*</c>, <c>Groove*</c>, <c>Ridge*</c>, <c>Sap*</c>,
/// <c>Canopy*</c>, <c>Clump*</c>, <c>Underside</c>, <c>Blossom*</c>, <c>Branch*</c>, <c>Twig*</c>, <c>Snow*</c>,
/// <c>FallenLeaf*</c>, <c>Stump*</c>, <c>Ring*</c>, <c>Crack*</c>, <c>Shoot*</c>, <c>Chip*</c>.
/// </summary>
public static class TreeArt
{
    /// <summary>The size of a sapling against a full tree.</summary>
    public const float SaplingScale = 0.32f;

    /// <summary>The size of an old giant against a full tree.</summary>
    public const float GiantScale = 1.28f;

    private const float TrunkTop = 1.3f;

    /// <summary>The canopy's big puffs: centre and diameter, in a full tree's metres.</summary>
    private static readonly (Vector3 At, float Size)[] Puffs =
    [
        (new Vector3(-1.1f, 2.4f, 0.1f), 1.35f),
        (new Vector3(1.13f, 2.48f, -0.1f), 1.42f),
        (new Vector3(-0.76f, 3.18f, -0.15f), 1.76f),
        (new Vector3(0.71f, 3.32f, 0.12f), 1.92f),
        (new Vector3(-0.04f, 4.08f, 0f), 1.68f),
        (new Vector3(0.05f, 2.95f, -0.85f), 1.7f),
        (new Vector3(-0.05f, 2.85f, 0.85f), 1.6f),
        (new Vector3(-0.3f, 3.7f, 0.5f), 1.4f),
        (new Vector3(0.4f, 3.6f, -0.55f), 1.5f),
    ];

    /// <summary>Where each puff's small clumps sit on it: yaw round it and rise above its middle, in degrees.</summary>
    private static readonly (float Yaw, float Rise)[] ClumpSpots = [(20f, 50f), (140f, 35f), (260f, 40f), (80f, 10f)];

    /// <summary>The limbs' paths: from the split, through a bend, to the fork.</summary>
    private static readonly (string Name, Vector3[] Path, float[] Diameters)[] Limbs =
    [
        ("LimbWest", [new(-0.08f, TrunkTop - 0.1f, 0f), new(-0.4f, 1.85f, 0.06f), new(-0.72f, 2.4f, 0.1f)], [0.44f, 0.36f, 0.27f]),
        ("LimbEast", [new(0.08f, TrunkTop - 0.1f, 0f), new(0.44f, 1.8f, -0.06f), new(0.78f, 2.38f, -0.1f)], [0.44f, 0.36f, 0.27f]),
    ];

    /// <summary>The branches from the forks (and the middle of the split) up into the canopy.</summary>
    private static readonly (string Name, Vector3 From, Vector3 To, float Diameter)[] CanopyBranches =
    [
        ("BranchWestOut", new(-0.72f, 2.4f, 0.1f), new(-1.15f, 2.75f, 0.3f), 0.18f),
        ("BranchWestUp", new(-0.72f, 2.4f, 0.1f), new(-0.55f, 3.2f, -0.35f), 0.18f),
        ("BranchEastOut", new(0.78f, 2.38f, -0.1f), new(1.2f, 2.75f, -0.3f), 0.18f),
        ("BranchEastUp", new(0.78f, 2.38f, -0.1f), new(0.55f, 3.25f, 0.4f), 0.18f),
        ("BranchMiddle", new(0f, 1.9f, 0f), new(0.05f, 3.6f, 0.05f), 0.2f),
    ];

    private static readonly Dictionary<(TreeStage, Season), IReadOnlyList<CropPart>> Cache = new();

    /// <summary>The Twinbough at <paramref name="stage"/> in <paramref name="season"/>. The same list for the same arguments.</summary>
    public static IReadOnlyList<CropPart> Twinbough(TreeStage stage, Season season)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue((stage, season), out var parts))
            {
                parts = stage switch
                {
                    TreeStage.Stump => Stump(),
                    TreeStage.Sapling => Scaled(Tree(season), SaplingScale),
                    TreeStage.Giant => Scaled(Tree(season), GiantScale),
                    _ => Tree(season),
                };
                Cache[(stage, season)] = parts;
            }

            return parts;
        }
    }

    // ---- the full tree ----

    private static List<CropPart> Tree(Season season)
    {
        var parts = new List<CropPart>();
        Trunk(parts);
        if (season == Season.Winter)
        {
            Bare(parts);
        }
        else
        {
            Branches(parts, withTwigs: false);
            Canopy(parts, season);
        }

        if (season == Season.Fall) FallenLeaves(parts);
        return parts;
    }

    private static void Trunk(List<CropPart> parts)
    {
        // Four stacked eight-sided segments, each turned a little further, so the trunk reads twisted.
        (float Bottom, float Top, float BottomD, float TopD, float Yaw)[] segments =
        [
            (0f, 0.4f, 0.84f, 0.74f, 0f),
            (0.4f, 0.75f, 0.74f, 0.67f, 15f),
            (0.75f, 1.05f, 0.67f, 0.61f, 30f),
            (1.05f, TrunkTop, 0.61f, 0.56f, 45f),
        ];
        for (var i = 0; i < segments.Length; i++)
        {
            var (bottom, top, bottomD, topD, yaw) = segments[i];
            parts.Add(new CropPart($"Trunk{i}", CropPartShape.Cylinder, bottomD, top - bottom, topD, 0f, (bottom + top) / 2f, 0f, 0f, yaw, 0f, "trunk", Sides: 8));
        }

        // Five root flares tapering out into the ground.
        float[] roots = [15f, 85f, 155f, 225f, 295f];
        for (var i = 0; i < roots.Length; i++)
        {
            var dir = Direction(roots[i]);
            var reach = 0.64f + 0.06f * (i % 2);
            parts.Add(Limb($"Root{i}", dir * 0.22f + new Vector3(0f, 0.3f, 0f), dir * reach + new Vector3(0f, 0.11f, 0f), 0.32f, 0.16f, "trunk"));
        }

        // Spiral bark: dark grooves and lighter ridges winding up the trunk, each in two pieces to follow its taper.
        for (var i = 0; i < 4; i++)
        {
            var yaw = 90f * i + 20f;
            for (var piece = 0; piece < 2; piece++)
            {
                var y = 0.32f + 0.55f * piece;
                var radius = 0.4f - 0.05f * piece;
                var at = Direction(yaw + 25f * piece) * radius + new Vector3(0f, y, 0f);
                parts.Add(new CropPart($"Groove{i}_{piece}", CropPartShape.Box, 0.045f, 0.6f, 0.03f, at.X, at.Y, at.Z, 0f, yaw + 25f * piece, 30f, "timber-trim"));
                var ridge = Direction(yaw + 45f + 25f * piece) * (radius + 0.005f) + new Vector3(0f, y + 0.05f, 0f);
                parts.Add(new CropPart($"Ridge{i}_{piece}", CropPartShape.Box, 0.06f, 0.5f, 0.035f, ridge.X, ridge.Y, ridge.Z, 0f, yaw + 45f + 25f * piece, 30f, "timber-side"));
            }
        }

        // The two limbs, each in two tapering pieces that bend apart.
        foreach (var (name, path, diameters) in Limbs)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                parts.Add(Limb($"{name}{i}", path[i], path[i + 1], diameters[i], diameters[i + 1], "trunk"));
            }
        }

        // The amber sap seam on the front, following a groove in three short runs: glows at night.
        Vector3[] seam = [new(-0.16f, 0.28f, 0.4f), new(-0.1f, 0.52f, 0.38f), new(-0.15f, 0.76f, 0.35f), new(-0.08f, 0.98f, 0.32f)];
        for (var i = 0; i < seam.Length - 1; i++)
        {
            parts.Add(Beam($"Sap{i}", 0.06f, 0.03f, seam[i], seam[i + 1], "lantern") with { Glow = 1f });
        }
    }

    /// <summary>The branches from the limbs up into the canopy (hidden by leaves but seen through gaps and from below).</summary>
    private static void Branches(List<CropPart> parts, bool withTwigs)
    {
        foreach (var (name, from, to, diameter) in CanopyBranches)
        {
            parts.Add(Limb(name, from, to, diameter, diameter * 0.6f, "trunk"));
            if (!withTwigs) continue;

            // Two twigs forking from each branch's tip, and a third further down.
            var run = Vector3.Normalize(to - from);
            var side = Vector3.Normalize(Vector3.Cross(run, Vector3.UnitY) + new Vector3(0.001f, 0f, 0f));
            var up = new Vector3(0f, 0.35f, 0f);
            parts.Add(Limb($"Twig{name}A", to, to + run * 0.35f + side * 0.3f + up, diameter * 0.5f, diameter * 0.25f, "trunk"));
            parts.Add(Limb($"Twig{name}B", to, to + run * 0.3f - side * 0.32f + up * 0.6f, diameter * 0.5f, diameter * 0.25f, "trunk"));
            var mid = Vector3.Lerp(from, to, 0.55f);
            parts.Add(Limb($"Twig{name}C", mid, mid + side * 0.42f + up * 0.9f, diameter * 0.45f, diameter * 0.2f, "trunk"));
        }
    }

    private static void Canopy(List<CropPart> parts, Season season)
    {
        var (shade, middle, light) = season switch
        {
            Season.Spring => ("foliage", "canopy-spring", "canopy-spring-light"),
            Season.Fall => ("canopy-fall-shade", "canopy-fall", "canopy-fall-light"),
            _ => ("grass-shade", "foliage", "roof-tips"),
        };
        var underside = season == Season.Fall ? "underside-fall" : "leaf-underside";

        // The violet underside: a broad, deep puff tucked under the canopy, showing as a band below its edge.
        parts.Add(new CropPart("Underside", CropPartShape.Sphere, 3.0f, 1.1f, 3.0f, 0f, 2.4f, 0f, 0f, 15f, 0f, underside, Sides: 12));

        for (var i = 0; i < Puffs.Length; i++)
        {
            var (centre, size) = Puffs[i];
            // Lighter on top, darker underneath: the puffs pick a tone by height.
            var tone = centre.Y >= 3.55f ? light : centre.Y < 2.6f ? shade : middle;
            parts.Add(new CropPart($"Canopy{i}", CropPartShape.Sphere, size, size * 0.92f, size, centre.X, centre.Y, centre.Z, 0f, 23f * i, 0f, tone, Sides: 12));

            // Smaller clumps half-sunk into the puff's surface, so its outline is lumpy rather than one smooth ball.
            for (var c = 0; c < ClumpSpots.Length; c++)
            {
                var (yaw, rise) = ClumpSpots[c];
                var at = centre + SurfaceDirection(yaw + 37f * i, rise) * (size * 0.42f);
                var clump = size * (0.34f + 0.04f * (c % 2));
                var clumpTone = rise >= 40f ? light : tone;
                parts.Add(new CropPart($"Clump{i}_{c}", CropPartShape.Sphere, clump, clump * 0.85f, clump, at.X, at.Y, at.Z, 0f, 40f * c, 0f, clumpTone, Sides: 10));
            }
        }

        if (season != Season.Spring) return;

        // Blossom in little clusters of three dotted over the canopy's outside.
        (int Puff, float Yaw, float Rise)[] blooms =
        [
            (0, -60f, 20f), (1, 70f, 10f), (2, -30f, 40f), (3, 40f, 35f), (4, 0f, 55f), (4, 140f, 30f),
            (5, 170f, 20f), (6, -10f, 15f), (7, 20f, 30f), (8, 200f, 25f), (2, -120f, 10f), (3, 120f, 15f),
            (0, 200f, 30f), (1, -90f, 35f), (5, 100f, 45f), (6, 60f, 40f),
        ];
        for (var i = 0; i < blooms.Length; i++)
        {
            var (puff, yaw, rise) = blooms[i];
            var (centre, size) = Puffs[puff];
            var dir = SurfaceDirection(yaw, rise);
            var at = centre + dir * (size / 2f * 0.97f);
            var across = Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY) + new Vector3(0.001f, 0f, 0f));
            Vector3[] petals = [Vector3.Zero, across * 0.09f + new Vector3(0f, 0.04f, 0f), -across * 0.08f + new Vector3(0f, -0.03f, 0f)];
            for (var p = 0; p < petals.Length; p++)
            {
                var bloom = p == 0 ? 0.16f : 0.12f;
                var b = at + petals[p];
                parts.Add(new CropPart($"Blossom{i}_{p}", CropPartShape.Sphere, bloom, bloom * 0.8f, bloom, b.X, b.Y, b.Z, 0f, 0f, 0f, "blossom"));
            }
        }
    }

    private static void Bare(List<CropPart> parts)
    {
        // Bare branches out of the limbs, each forking into twigs, with snow along the big ones and in the forks.
        Branches(parts, withTwigs: true);
        (string Name, Vector3 From, Vector3 To)[] snowy =
        [
            ("SnowWest", new Vector3(-0.42f, 1.92f, 0.06f), new Vector3(-0.7f, 2.42f, 0.1f)),
            ("SnowEast", new Vector3(0.46f, 1.88f, -0.06f), new Vector3(0.76f, 2.4f, -0.1f)),
            ("SnowWestOut", new Vector3(-0.8f, 2.5f, 0.14f), new Vector3(-1.1f, 2.76f, 0.28f)),
            ("SnowEastOut", new Vector3(0.86f, 2.48f, -0.14f), new Vector3(1.15f, 2.76f, -0.28f)),
        ];
        foreach (var (name, from, to) in snowy)
        {
            var lift = new Vector3(0f, 0.12f, 0f);
            parts.Add(Beam(name, 0.22f, 0.2f, from + lift, to + lift, "snow"));
        }

        parts.Add(new CropPart("SnowCrook", CropPartShape.Sphere, 0.55f, 0.16f, 0.55f, 0f, TrunkTop + 0.08f, 0f, 0f, 0f, 0f, "snow", Sides: 10));
        parts.Add(new CropPart("SnowForkWest", CropPartShape.Sphere, 0.26f, 0.1f, 0.26f, -0.72f, 2.5f, 0.1f, 0f, 0f, 0f, "snow"));
        parts.Add(new CropPart("SnowForkEast", CropPartShape.Sphere, 0.26f, 0.1f, 0.26f, 0.78f, 2.48f, -0.1f, 0f, 0f, 0f, "snow"));
    }

    private static void FallenLeaves(List<CropPart> parts)
    {
        (float Yaw, float Distance, float Turn)[] leaves =
        [
            (10f, 1.2f, 20f), (45f, 1.9f, 80f), (70f, 1.6f, -30f), (130f, 1.1f, 10f), (165f, 2.0f, -60f), (190f, 1.5f, 40f),
            (250f, 1.3f, -20f), (285f, 1.95f, 15f), (310f, 1.7f, 60f), (345f, 2.1f, -45f),
        ];
        for (var i = 0; i < leaves.Length; i++)
        {
            var at = Direction(leaves[i].Yaw) * leaves[i].Distance + new Vector3(0f, 0.012f, 0f);
            parts.Add(new CropPart($"FallenLeaf{i}", CropPartShape.Box, 0.22f, 0.012f, 0.12f, at.X, at.Y, at.Z, 0f, leaves[i].Turn, 4f,
                i % 3 == 0 ? "canopy-fall-light" : "canopy-fall"));
        }
    }

    // ---- the stump ----

    private static List<CropPart> Stump()
    {
        var parts = new List<CropPart>();
        const float height = 0.75f;
        parts.Add(new CropPart("StumpBark", CropPartShape.Cylinder, 0.9f, height, 0.82f, 0f, height / 2f, 0f, 0f, 10f, 0f, "trunk", Sides: 10));

        // Bark grooves and ridges round the side.
        for (var i = 0; i < 6; i++)
        {
            var yaw = 60f * i + 15f;
            var groove = Direction(yaw) * 0.44f + new Vector3(0f, 0.36f, 0f);
            parts.Add(new CropPart($"StumpGroove{i}", CropPartShape.Box, 0.04f, 0.6f, 0.03f, groove.X, groove.Y, groove.Z, 0f, yaw, 18f, "timber-trim"));
            var ridge = Direction(yaw + 30f) * 0.445f + new Vector3(0f, 0.4f, 0f);
            parts.Add(new CropPart($"StumpRidge{i}", CropPartShape.Box, 0.06f, 0.5f, 0.035f, ridge.X, ridge.Y, ridge.Z, 0f, yaw + 30f, 18f, "timber-side"));
        }

        float[] roots = [30f, 120f, 210f, 300f];
        for (var i = 0; i < roots.Length; i++)
        {
            var dir = Direction(roots[i]);
            parts.Add(Limb($"StumpRoot{i}", dir * 0.28f + new Vector3(0f, 0.28f, 0f), dir * 0.7f + new Vector3(0f, 0.1f, 0f), 0.3f, 0.15f, "trunk"));
        }

        // The cut face: pale wood with growth rings of sap that glow at night, stacked a few millimetres apart.
        (float Diameter, string Role)[] rings =
        [
            (0.8f, "wood-cut"), (0.62f, "lantern"), (0.56f, "wood-cut"), (0.4f, "lantern"), (0.34f, "wood-cut"),
            (0.18f, "lantern"), (0.13f, "wood-cut"), (0.05f, "lantern"),
        ];
        for (var i = 0; i < rings.Length; i++)
        {
            var (diameter, role) = rings[i];
            var y = height + 0.01f + 0.004f * i;
            var part = new CropPart(i == 0 ? "StumpFace" : $"Ring{i}", CropPartShape.Cylinder, diameter, 0.02f, diameter, 0f, y, 0f, 0f, 0f, 0f, role, Sides: 16);
            parts.Add(role == "lantern" ? part with { Glow = 1f } : part);
        }

        // Drying cracks running in from the edge of the cut face.
        float[] cracks = [70f, 200f, 320f];
        for (var i = 0; i < cracks.Length; i++)
        {
            var at = Direction(cracks[i]) * 0.3f + new Vector3(0f, height + 0.045f, 0f);
            parts.Add(new CropPart($"Crack{i}", CropPartShape.Box, 0.02f, 0.008f, 0.2f, at.X, at.Y, at.Z, 0f, cracks[i], 0f, "timber-trim"));
        }

        // A dribble of sap down the side, ending in a drop.
        parts.Add(new CropPart("SapDribble0", CropPartShape.Box, 0.06f, 0.2f, 0.03f, 0.3f, height - 0.08f, 0.29f, -8f, 45f, 0f, "lantern", Glow: 1f));
        parts.Add(new CropPart("SapDribble1", CropPartShape.Box, 0.05f, 0.14f, 0.03f, 0.31f, height - 0.24f, 0.3f, -8f, 45f, 10f, "lantern", Glow: 1f));
        parts.Add(new CropPart("SapDribbleDrop", CropPartShape.Sphere, 0.07f, 0.08f, 0.07f, 0.315f, height - 0.33f, 0.305f, 0f, 0f, 0f, "lantern", Glow: 1f));

        // A new shoot curling up from the cut face, with two leaves.
        var foot = new Vector3(-0.2f, height + 0.03f, -0.05f);
        var tip = foot;
        float[] bends = [8f, 20f, 36f, 56f];
        for (var i = 0; i < bends.Length; i++)
        {
            var length = 0.12f - 0.015f * i;
            var dir = PartBox.Rotate(new Vector3(0f, length, 0f), bends[i], 200f, 0f);
            parts.Add(Limb($"Shoot{i}", tip, tip + dir, 0.04f - 0.006f * i, 0.034f - 0.006f * i, "foliage"));
            tip += dir;
        }

        parts.Add(new CropPart("ShootLeaf0", CropPartShape.Sphere, 0.18f, 0.04f, 0.18f, tip.X, tip.Y, tip.Z, 0f, 0f, -30f, "roof-tips", Sides: 10));
        var lower = foot + new Vector3(0.05f, 0.14f, 0.06f);
        parts.Add(new CropPart("ShootLeaf1", CropPartShape.Sphere, 0.13f, 0.035f, 0.13f, lower.X, lower.Y, lower.Z, 0f, 0f, 35f, "foliage", Sides: 10));

        // Wood chips on the ground round the foot.
        (float Yaw, float Distance, float Size)[] chips = [(80f, 0.75f, 0.12f), (200f, 0.8f, 0.1f), (320f, 0.7f, 0.12f), (20f, 0.95f, 0.08f), (140f, 1.0f, 0.09f), (260f, 0.95f, 0.07f)];
        for (var i = 0; i < chips.Length; i++)
        {
            var c = Direction(chips[i].Yaw) * chips[i].Distance + new Vector3(0f, 0.015f, 0f);
            parts.Add(new CropPart($"Chip{i}", CropPartShape.Prism, chips[i].Size, 0.03f, chips[i].Size / 2f, c.X, c.Y, c.Z, 0f, chips[i].Yaw + 40f, 0f, "wood-cut"));
        }

        return parts;
    }

    // ---- helpers ----

    private static List<CropPart> Scaled(List<CropPart> parts, float scale) =>
        parts.Select(part => part with
        {
            SizeX = part.SizeX * scale,
            SizeY = part.SizeY * scale,
            SizeZ = part.SizeZ * scale,
            X = part.X * scale,
            Y = part.Y * scale,
            Z = part.Z * scale,
        }).ToList();

    /// <summary>The horizontal unit vector for a yaw: 0 is +Z, 90 is +X.</summary>
    private static Vector3 Direction(float yawDegrees)
    {
        var a = yawDegrees * MathF.PI / 180f;
        return new Vector3(MathF.Sin(a), 0f, MathF.Cos(a));
    }

    /// <summary>The unit vector at <paramref name="yawDegrees"/> round and <paramref name="riseDegrees"/> above the horizontal.</summary>
    private static Vector3 SurfaceDirection(float yawDegrees, float riseDegrees)
    {
        var rise = riseDegrees * MathF.PI / 180f;
        return Direction(yawDegrees) * MathF.Cos(rise) + new Vector3(0f, MathF.Sin(rise), 0f);
    }

    /// <summary>The pitch and yaw that turn a part's local Y from upright to run along <paramref name="d"/>.</summary>
    private static (float Pitch, float Yaw) Along(Vector3 d)
    {
        var horizontal = MathF.Sqrt(d.X * d.X + d.Z * d.Z);
        var yaw = horizontal < 1e-5f ? 0f : MathF.Atan2(d.X, d.Z) * 180f / MathF.PI;
        var pitch = MathF.Atan2(horizontal, d.Y) * 180f / MathF.PI;
        return (pitch, yaw);
    }

    /// <summary>A box running from <paramref name="from"/> to <paramref name="to"/> along its local Y.</summary>
    private static CropPart Beam(string name, float sizeX, float sizeZ, Vector3 from, Vector3 to, string role)
    {
        var (pitch, yaw) = Along(to - from);
        var centre = (from + to) / 2f;
        return new CropPart(name, CropPartShape.Box, sizeX, (to - from).Length(), sizeZ, centre.X, centre.Y, centre.Z, pitch, yaw, 0f, role);
    }

    /// <summary>
    /// A six-sided tapering cylinder from <paramref name="from"/> (<paramref name="fromDiameter"/> across) to
    /// <paramref name="to"/> (<paramref name="toDiameter"/> across), a little longer at each end so joints don't gap.
    /// </summary>
    private static CropPart Limb(string name, Vector3 from, Vector3 to, float fromDiameter, float toDiameter, string role)
    {
        var d = to - from;
        var (pitch, yaw) = Along(d);
        var length = d.Length() + 0.5f * MathF.Min(fromDiameter, toDiameter);
        var centre = (from + to) / 2f;
        return new CropPart(name, CropPartShape.Cylinder, fromDiameter, length, toDiameter, centre.X, centre.Y, centre.Z, pitch, yaw, 0f, role);
    }
}
