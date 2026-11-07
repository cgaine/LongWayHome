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
/// <b>Seasons.</b> Spring: a lighter canopy with pink-violet blossom. Summer: the full blue-green canopy. Fall: a magenta
/// canopy with a dark violet underside and fallen leaves round the foot. Winter: bare branches with snow along them. The
/// stump is the same in every season. Only the sap glows (<c>lantern</c>, the seam on the trunk and the stump's rings
/// and dribble).
/// </para>
/// Parts are named by group: <c>Trunk*</c>, <c>Root*</c>, <c>Limb*</c>, <c>Groove*</c>, <c>Sap*</c>, <c>Canopy*</c>,
/// <c>Underside</c>, <c>Blossom*</c>, <c>Branch*</c>, <c>Snow*</c>, <c>FallenLeaf*</c>, <c>Stump*</c>, <c>Ring*</c>,
/// <c>Shoot*</c>, <c>Chip*</c>.
/// </summary>
public static class TreeArt
{
    /// <summary>The size of a sapling against a full tree.</summary>
    public const float SaplingScale = 0.32f;

    /// <summary>The size of an old giant against a full tree.</summary>
    public const float GiantScale = 1.28f;

    private const float TrunkTop = 1.35f;

    /// <summary>The canopy's puffs: centre and diameter, in a full tree's metres.</summary>
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
            Canopy(parts, season);
        }

        if (season == Season.Fall) FallenLeaves(parts);
        return parts;
    }

    private static void Trunk(List<CropPart> parts)
    {
        // Three stacked six-sided segments, each turned a little further, so the trunk reads twisted.
        (float Bottom, float Top, float BottomD, float TopD, float Yaw)[] segments =
        [
            (0f, 0.5f, 0.78f, 0.7f, 0f),
            (0.5f, 0.95f, 0.7f, 0.62f, 20f),
            (0.95f, TrunkTop, 0.62f, 0.56f, 40f),
        ];
        for (var i = 0; i < segments.Length; i++)
        {
            var (bottom, top, bottomD, topD, yaw) = segments[i];
            parts.Add(new CropPart($"Trunk{i}", CropPartShape.Cylinder, bottomD, top - bottom, topD, 0f, (bottom + top) / 2f, 0f, 0f, yaw, 0f, "trunk"));
        }

        // Root flares spreading out at the foot.
        float[] roots = [20f, 140f, 260f];
        for (var i = 0; i < roots.Length; i++)
        {
            var dir = Direction(roots[i]);
            var at = dir * 0.38f + new Vector3(0f, 0.09f, 0f);
            parts.Add(new CropPart($"Root{i}", CropPartShape.Box, 0.2f, 0.18f, 0.42f, at.X, at.Y, at.Z, 18f, roots[i], 0f, "trunk"));
        }

        // The split: two limbs leaning apart, each a little twisted.
        parts.Add(Beam("LimbWest", 0.46f, 0.36f, new Vector3(-0.08f, TrunkTop - 0.15f, 0f), new Vector3(-0.55f, 2.35f, 0.05f), "trunk"));
        parts.Add(Beam("LimbEast", 0.46f, 0.36f, new Vector3(0.08f, TrunkTop - 0.15f, 0f), new Vector3(0.6f, 2.3f, -0.05f), "trunk"));

        // Spiral grooves climbing the trunk (the twist), on the front and back.
        parts.Add(new CropPart("Groove0", CropPartShape.Box, 0.5f, 0.04f, 0.03f, 0f, 0.35f, 0.38f, 0f, 0f, 28f, "timber-trim"));
        parts.Add(new CropPart("Groove1", CropPartShape.Box, 0.46f, 0.04f, 0.03f, 0f, 0.95f, 0.33f, 0f, 0f, 28f, "timber-trim"));
        parts.Add(new CropPart("Groove2", CropPartShape.Box, 0.5f, 0.04f, 0.03f, 0f, 0.6f, -0.36f, 0f, 0f, -28f, "timber-trim"));

        // The amber sap seam in the bark, on the front: glows at night.
        parts.Add(new CropPart("Sap0", CropPartShape.Box, 0.06f, 0.26f, 0.03f, -0.13f, 0.5f, 0.36f, -5f, 0f, 12f, "lantern", Glow: 1f));
        parts.Add(new CropPart("Sap1", CropPartShape.Box, 0.06f, 0.22f, 0.03f, -0.1f, 0.73f, 0.34f, -5f, 0f, -10f, "lantern", Glow: 1f));
    }

    private static void Canopy(List<CropPart> parts, Season season)
    {
        var canopy = season switch
        {
            Season.Spring => "canopy-spring",
            Season.Fall => "canopy-fall",
            _ => "foliage",
        };
        var underside = season == Season.Fall ? "underside-fall" : "leaf-underside";

        // The violet underside: a broad, deep puff tucked under the canopy, showing as a band below its edge.
        parts.Add(new CropPart("Underside", CropPartShape.Sphere, 3.0f, 1.1f, 3.0f, 0f, 2.4f, 0f, 0f, 15f, 0f, underside));
        for (var i = 0; i < Puffs.Length; i++)
        {
            var (at, size) = Puffs[i];
            parts.Add(new CropPart($"Canopy{i}", CropPartShape.Sphere, size, size * 0.92f, size, at.X, at.Y, at.Z, 0f, 23f * i, 0f, canopy));
        }

        if (season != Season.Spring) return;

        // Blossom dotted over the canopy's outside.
        (int Puff, float Yaw, float Rise)[] blooms =
        [
            (0, -60f, 20f), (1, 70f, 10f), (2, -30f, 40f), (3, 40f, 35f), (4, 0f, 55f), (4, 140f, 30f),
            (5, 170f, 20f), (6, -10f, 15f), (7, 20f, 30f), (8, 200f, 25f), (2, -120f, 10f), (3, 120f, 15f),
        ];
        for (var i = 0; i < blooms.Length; i++)
        {
            var (puff, yaw, rise) = blooms[i];
            var (centre, size) = Puffs[puff];
            var dir = Vector3.Normalize(Direction(yaw) * MathF.Cos(rise * MathF.PI / 180f) + new Vector3(0f, MathF.Sin(rise * MathF.PI / 180f), 0f));
            var at = centre + dir * (size / 2f * 0.95f);
            var bloom = i % 3 == 0 ? 0.26f : 0.2f;
            parts.Add(new CropPart($"Blossom{i}", CropPartShape.Sphere, bloom, bloom * 0.8f, bloom, at.X, at.Y, at.Z, 0f, 0f, 0f, "blossom"));
        }
    }

    private static void Bare(List<CropPart> parts)
    {
        // Bare branches out of the limbs, with twigs, and snow lying along the tops of the big ones.
        (string Name, Vector3 From, Vector3 To, float Thickness)[] branches =
        [
            ("BranchEast", new Vector3(0.55f, 2.2f, -0.05f), new Vector3(1.3f, 3.2f, 0f), 0.17f),
            ("BranchWest", new Vector3(-0.5f, 2.25f, 0.05f), new Vector3(-1.15f, 3.35f, 0f), 0.17f),
            ("BranchMiddle", new Vector3(0.02f, 1.75f, 0f), new Vector3(-0.08f, 4.2f, 0f), 0.15f),
            ("BranchBack", new Vector3(0.1f, 2.0f, -0.05f), new Vector3(0.25f, 3.15f, -0.95f), 0.14f),
            ("BranchFront", new Vector3(-0.1f, 2.0f, 0.05f), new Vector3(-0.25f, 3.05f, 0.95f), 0.14f),
            ("BranchTwigEast", new Vector3(0.95f, 2.75f, 0f), new Vector3(1.4f, 2.75f, 0.1f), 0.07f),
            ("BranchTwigWest", new Vector3(-0.85f, 2.8f, 0f), new Vector3(-1.35f, 2.85f, -0.1f), 0.07f),
            ("BranchTwigTop0", new Vector3(0f, 3.35f, 0f), new Vector3(0.32f, 3.65f, 0.05f), 0.07f),
            ("BranchTwigTop1", new Vector3(-0.05f, 3.7f, 0f), new Vector3(-0.35f, 3.95f, -0.05f), 0.07f),
        ];
        foreach (var (name, from, to, thickness) in branches)
        {
            parts.Add(Beam(name, thickness, thickness, from, to, "trunk"));
        }

        (string Name, Vector3 From, Vector3 To)[] snowy =
        [
            ("SnowEast", new Vector3(0.75f, 2.5f, -0.04f), new Vector3(1.15f, 3.0f, 0f)),
            ("SnowWest", new Vector3(-0.7f, 2.6f, 0.04f), new Vector3(-1.05f, 3.15f, 0f)),
            ("SnowMiddle", new Vector3(-0.03f, 3.3f, 0f), new Vector3(-0.07f, 3.9f, 0f)),
        ];
        foreach (var (name, from, to) in snowy)
        {
            var lift = new Vector3(0f, 0.1f, 0f);
            parts.Add(Beam(name, 0.2f, 0.18f, from + lift, to + lift, "snow"));
        }

        parts.Add(new CropPart("SnowCrook", CropPartShape.Sphere, 0.5f, 0.14f, 0.5f, 0f, TrunkTop + 0.42f, 0f, 0f, 0f, 0f, "snow"));
    }

    private static void FallenLeaves(List<CropPart> parts)
    {
        (float Yaw, float Distance, float Turn)[] leaves =
        [
            (10f, 1.2f, 20f), (70f, 1.6f, -30f), (130f, 1.1f, 10f), (190f, 1.5f, 40f), (250f, 1.3f, -20f), (310f, 1.7f, 60f),
        ];
        for (var i = 0; i < leaves.Length; i++)
        {
            var at = Direction(leaves[i].Yaw) * leaves[i].Distance + new Vector3(0f, 0.012f, 0f);
            parts.Add(new CropPart($"FallenLeaf{i}", CropPartShape.Box, 0.22f, 0.012f, 0.12f, at.X, at.Y, at.Z, 0f, leaves[i].Turn, 4f, "canopy-fall"));
        }
    }

    // ---- the stump ----

    private static List<CropPart> Stump()
    {
        var parts = new List<CropPart>();
        const float height = 0.75f;
        parts.Add(new CropPart("StumpBark", CropPartShape.Cylinder, 0.9f, height, 0.82f, 0f, height / 2f, 0f, 0f, 10f, 0f, "trunk"));
        float[] roots = [30f, 150f, 270f];
        for (var i = 0; i < roots.Length; i++)
        {
            var root = Direction(roots[i]) * 0.45f + new Vector3(0f, 0.08f, 0f);
            parts.Add(new CropPart($"StumpRoot{i}", CropPartShape.Box, 0.2f, 0.16f, 0.4f, root.X, root.Y, root.Z, 18f, roots[i], 0f, "trunk"));
        }

        // The cut face: pale wood with growth rings of sap that glow at night, stacked a few millimetres apart.
        (float Diameter, string Role)[] rings = [(0.8f, "wood-cut"), (0.6f, "lantern"), (0.52f, "wood-cut"), (0.34f, "lantern"), (0.26f, "wood-cut"), (0.07f, "lantern")];
        for (var i = 0; i < rings.Length; i++)
        {
            var (diameter, role) = rings[i];
            var y = height + 0.01f + 0.004f * i;
            var part = new CropPart(i == 0 ? "StumpFace" : $"Ring{i}", CropPartShape.Cylinder, diameter, 0.02f, diameter, 0f, y, 0f, 0f, 0f, 0f, role);
            parts.Add(role == "lantern" ? part with { Glow = 1f } : part);
        }

        // A dribble of sap down the side.
        parts.Add(new CropPart("SapDribble0", CropPartShape.Box, 0.06f, 0.2f, 0.03f, 0.3f, height - 0.08f, 0.29f, -8f, 45f, 0f, "lantern", Glow: 1f));
        parts.Add(new CropPart("SapDribble1", CropPartShape.Box, 0.05f, 0.14f, 0.03f, 0.31f, height - 0.24f, 0.3f, -8f, 45f, 10f, "lantern", Glow: 1f));

        // A new shoot already sprouting from the cut face.
        var shootFoot = new Vector3(-0.2f, height + 0.03f, -0.05f);
        var at = shootFoot;
        float[] bends = [10f, 25f, 50f];
        for (var i = 0; i < bends.Length; i++)
        {
            var length = 0.14f - 0.02f * i;
            var dir = PartBox.Rotate(new Vector3(0f, length, 0f), bends[i], 200f, 0f);
            var centre = at + dir / 2f;
            parts.Add(new CropPart($"Shoot{i}", CropPartShape.Box, 0.035f, length, 0.035f, centre.X, centre.Y, centre.Z, bends[i], 200f, 0f, "foliage"));
            at += dir;
        }

        parts.Add(new CropPart("ShootLeaf", CropPartShape.Sphere, 0.16f, 0.05f, 0.16f, at.X, at.Y, at.Z, 0f, 0f, -30f, "roof-tips"));

        // Wood chips on the ground round the foot.
        (float Yaw, float Distance)[] chips = [(80f, 0.75f), (200f, 0.8f), (320f, 0.7f), (20f, 0.95f)];
        for (var i = 0; i < chips.Length; i++)
        {
            var c = Direction(chips[i].Yaw) * chips[i].Distance + new Vector3(0f, 0.015f, 0f);
            parts.Add(new CropPart($"Chip{i}", CropPartShape.Prism, 0.12f, 0.03f, 0.06f, c.X, c.Y, c.Z, 0f, chips[i].Yaw + 40f, 0f, "wood-cut"));
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

    /// <summary>A box running from <paramref name="from"/> to <paramref name="to"/> along its local Y.</summary>
    private static CropPart Beam(string name, float sizeX, float sizeZ, Vector3 from, Vector3 to, string role)
    {
        var d = to - from;
        var length = d.Length();
        var horizontal = MathF.Sqrt(d.X * d.X + d.Z * d.Z);
        // Yaw turns the box to face along the run; pitch then tips its Y from upright towards that run.
        var yaw = horizontal < 1e-5f ? 0f : MathF.Atan2(d.X, d.Z) * 180f / MathF.PI;
        var pitch = MathF.Atan2(horizontal, d.Y) * 180f / MathF.PI;
        var centre = (from + to) / 2f;
        return new CropPart(name, CropPartShape.Box, sizeX, length, sizeZ, centre.X, centre.Y, centre.Z, pitch, yaw, 0f, role);
    }
}
