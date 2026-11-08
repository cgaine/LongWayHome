using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>A rock the pickaxe breaks (design doc, Gathering and the early mine; ART_DIRECTION §7.6).</summary>
public enum RockKind
{
    /// <summary>A meadow or mine rock: about 3 stone with the basic pickaxe.</summary>
    Rock,

    /// <summary>A boulder about twice the size, cracked: the iron pickaxe breaks it.</summary>
    Boulder,

    /// <summary>A rock with rose-copper veins that curl into little spirals, with bright flecks: Rosk ore.</summary>
    RoskOre,

    /// <summary>A rock with blocky dark blue-grey crystals: a small Groth deposit.</summary>
    GrothOre,
}

/// <summary>
/// The rocks the pickaxe breaks, drawn to ART_DIRECTION §7.6 ("Faceted lilac-grey", Rosk and Groth ore): a squat,
/// irregular lump of <c>stone</c> built from several faceted lumps, with lighter <c>rock</c> faces set into the top,
/// a few embedded stones and pebbles at the foot, sunk a little into the ground. Pure C#, no Godot: the sandbox's
/// builder <c>NatureModels</c> merges these parts into one mesh under the shared palette material. The art sandbox
/// shows them, and the lakeshore's shore (#136) uses the plain rock as scenery.
/// <para>
/// <b>Conventions.</b> The <see cref="CropPart"/> conventions, with the origin at the rock's centre <b>on the
/// ground</b> (y 0). A rock is about 0.9 m across and 0.5 m tall; a boulder about 1.7 m across and 1 m tall. The meadow
/// rock and the boulder have a patch of moss and a tuft of grass at the foot; the ore rocks, which belong in the mine,
/// don't. Nothing glows. Parts are named by group: <c>Body*</c>, <c>Facet*</c>, <c>Stone*</c>, <c>Pebble*</c>,
/// <c>Moss*</c>, <c>Tuft*</c>, <c>Crack*</c>, <c>Vein*</c>, <c>Fleck*</c>, <c>Crystal*</c>.
/// </para>
/// </summary>
public static class RockArt
{
    /// <summary>The sides round each faceted lump: enough to read rounded, few enough to stay faceted.</summary>
    private const int LumpSides = 10;

    private static readonly Dictionary<RockKind, IReadOnlyList<CropPart>> Cache = new();

    /// <summary>The parts of <paramref name="kind"/>. The same list for the same kind.</summary>
    public static IReadOnlyList<CropPart> For(RockKind kind)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(kind, out var parts))
            {
                parts = kind switch
                {
                    RockKind.Boulder => Boulder(),
                    RockKind.RoskOre => Rosk(),
                    RockKind.GrothOre => Groth(),
                    _ => Meadow(Body(), 1f),
                };
                Cache[kind] = parts;
            }

            return parts;
        }
    }

    /// <summary>
    /// The rock's body: a big flattened lump with smaller lumps leaning on and into it for an irregular outline, lighter
    /// faces set into the top and sun side, two smaller stones bedded in its flanks, and pebbles at the foot.
    /// </summary>
    private static List<CropPart> Body()
    {
        var parts = new List<CropPart>
        {
            Lump("Body0", 0.9f, 0.62f, new Vector3(0f, 0.2f, 0f), 0f, 10f, 6f, "stone"),
            Lump("Body1", 0.56f, 0.44f, new Vector3(0.3f, 0.14f, 0.16f), 12f, 40f, -10f, "stone"),
            Lump("Body2", 0.46f, 0.36f, new Vector3(-0.28f, 0.1f, -0.14f), -8f, 70f, 12f, "stone"),
            Lump("Body3", 0.5f, 0.42f, new Vector3(-0.12f, 0.24f, 0.2f), 18f, 15f, -14f, "stone"),
            Lump("Body4", 0.42f, 0.34f, new Vector3(0.16f, 0.26f, -0.2f), -15f, 55f, 20f, "stone"),

            // Lighter faces set into the top and the sun side, so the rock reads faceted from above.
            Lump("Facet0", 0.5f, 0.3f, new Vector3(-0.06f, 0.36f, 0.04f), 10f, 35f, -6f, "rock"),
            Lump("Facet1", 0.34f, 0.26f, new Vector3(0.22f, 0.3f, 0.2f), -14f, 5f, 18f, "rock"),
            Lump("Facet2", 0.28f, 0.2f, new Vector3(-0.24f, 0.28f, 0.16f), 22f, 60f, -10f, "rock"),

            // Two smaller stones bedded in the flanks.
            Lump("Stone0", 0.22f, 0.18f, new Vector3(0.4f, 0.1f, -0.12f), 20f, 30f, 25f, "rock", sides: 6),
            Lump("Stone1", 0.18f, 0.15f, new Vector3(-0.38f, 0.08f, 0.2f), -25f, 80f, 15f, "rock", sides: 6),
        };

        (float X, float Z, float Size)[] pebbles = [(0.52f, -0.22f, 0.14f), (-0.5f, 0.3f, 0.11f), (0.15f, 0.5f, 0.09f), (-0.42f, -0.36f, 0.1f)];
        for (var i = 0; i < pebbles.Length; i++)
        {
            var (x, z, size) = pebbles[i];
            parts.Add(Lump($"Pebble{i}", size, size * 0.7f, new Vector3(x, size * 0.25f, z), 0f, 30f * i, 0f, "rock", sides: 6));
        }

        return parts;
    }

    /// <summary>A meadow rock: the body at <paramref name="scale"/>, with moss on its shaded side and grass at its foot.</summary>
    private static List<CropPart> Meadow(List<CropPart> body, float scale)
    {
        var parts = scale == 1f ? body : Scaled(body, scale);
        // Moss in the shade on the north side (−Z), low on the body.
        parts.Add(Lump("Moss0", 0.36f * scale, 0.1f * scale, new Vector3(-0.08f, 0.28f, -0.26f) * scale, -35f, 0f, 0f, "grass-shade"));
        parts.Add(Lump("Moss1", 0.22f * scale, 0.08f * scale, new Vector3(0.14f, 0.2f, -0.32f) * scale, -45f, 20f, 8f, "foliage"));

        // Tufts of grass round the foot, two crossed blades each.
        (float X, float Z, float Yaw)[] tufts = [(0.44f, 0.3f, 20f), (-0.5f, -0.05f, 70f), (0.05f, -0.48f, 130f)];
        for (var i = 0; i < tufts.Length; i++)
        {
            var (x, z, yaw) = tufts[i];
            var at = new Vector3(x, 0f, z) * scale;
            var height = 0.2f + 0.04f * i;
            parts.Add(new CropPart($"Tuft{i}A", CropPartShape.Prism, 0.14f, height, 0.02f, at.X, height / 2f, at.Z, 0f, yaw, 8f, "grass-lit"));
            parts.Add(new CropPart($"Tuft{i}B", CropPartShape.Prism, 0.12f, height * 0.8f, 0.02f, at.X + 0.03f, height * 0.4f, at.Z, 0f, yaw + 90f, -8f, "grass-shade"));
        }

        return parts;
    }

    private static List<CropPart> Boulder()
    {
        const float scale = 1.9f;
        var parts = Meadow(Body(), scale);
        // Dark cracks across the front and over the top, each in short zigzag runs: it takes a stronger pickaxe.
        (Vector3 Start, Vector3[] Steps)[] cracks =
        [
            (new Vector3(-0.22f, 0.86f, 0.62f), [new(0.1f, -0.14f, 0.07f), new(-0.04f, -0.15f, 0.06f), new(0.09f, -0.14f, 0.05f)]),
            (new Vector3(0.1f, 1.02f, 0.25f), [new(0.14f, -0.02f, 0.1f), new(0.12f, -0.06f, 0.12f), new(0.1f, -0.12f, 0.1f)]),
            (new Vector3(-0.1f, 1.05f, 0.1f), [new(-0.16f, -0.03f, -0.08f), new(-0.14f, -0.08f, -0.1f)]),
        ];
        for (var c = 0; c < cracks.Length; c++)
        {
            var (start, steps) = cracks[c];
            var at = start;
            for (var i = 0; i < steps.Length; i++)
            {
                var next = at + steps[i];
                parts.Add(Beam($"Crack{c}_{i}", 0.035f, 0.04f, at, next, "timber-trim"));
                at = next;
            }
        }

        return parts;
    }

    private static List<CropPart> Rosk()
    {
        var parts = Body();
        // Rose-copper veins lying over the top and front, each a continuous strip curling into a spiral at its end.
        (Vector3 Centre, float Yaw, float Pitch, float Turns)[] spirals =
        [
            (new Vector3(-0.05f, 0.5f, 0.05f), 0f, 4f, 1.6f),
            (new Vector3(0.22f, 0.4f, 0.3f), 35f, 32f, 1.3f),
            (new Vector3(-0.3f, 0.32f, 0.22f), -50f, 30f, 1.3f),
        ];
        for (var s = 0; s < spirals.Length; s++)
        {
            var (centre, yaw, pitch, turns) = spirals[s];
            const int steps = 9;
            Vector3? previous = null;
            for (var i = 0; i <= steps; i++)
            {
                var turn = turns * 2f * MathF.PI * i / steps;
                var radius = 0.025f + 0.11f * i / steps;
                var local = new Vector3(radius * MathF.Cos(turn), 0.04f, radius * MathF.Sin(turn));
                var point = centre + PartBox.Rotate(local, pitch, yaw, 0f);
                if (previous is { } from)
                {
                    parts.Add(Beam($"Vein{s}_{i}", 0.04f, 0.03f, from, point, "rosk"));
                }

                previous = point;
            }
        }

        // A tail of vein running down the front from the middle spiral.
        Vector3[] tail = [new(0.3f, 0.36f, 0.4f), new(0.36f, 0.24f, 0.44f), new(0.32f, 0.12f, 0.46f)];
        for (var i = 0; i < tail.Length - 1; i++)
        {
            parts.Add(Beam($"VeinTail{i}", 0.04f, 0.03f, tail[i], tail[i + 1], "rosk"));
        }

        // Bright flecks.
        Vector3[] flecks =
        [
            new(0.08f, 0.5f, -0.12f), new(-0.2f, 0.45f, -0.05f), new(0.32f, 0.33f, 0.05f), new(0f, 0.36f, 0.33f),
            new(-0.12f, 0.48f, 0.15f), new(0.18f, 0.46f, -0.02f), new(-0.34f, 0.28f, -0.1f), new(0.1f, 0.3f, 0.38f),
        ];
        for (var i = 0; i < flecks.Length; i++)
        {
            parts.Add(new CropPart($"Fleck{i}", CropPartShape.Box, 0.035f, 0.035f, 0.035f, flecks[i].X, flecks[i].Y, flecks[i].Z, 30f, 45f * i, 20f, "starcorn-sparkle"));
        }

        return parts;
    }

    private static List<CropPart> Groth()
    {
        var parts = Body();
        // Hexagonal crystals of dark blue-grey pushing out of the top and sides in two clusters, each a six-sided shaft
        // with a pointed tip that catches the light.
        (Vector3 Foot, float Tilt, float Yaw, float Length, float Width)[] crystals =
        [
            (new Vector3(0.02f, 0.38f, 0f), 6f, 0f, 0.48f, 0.15f),
            (new Vector3(0.1f, 0.36f, 0.08f), 26f, 50f, 0.36f, 0.12f),
            (new Vector3(-0.08f, 0.36f, 0.06f), 30f, -40f, 0.32f, 0.11f),
            (new Vector3(0.03f, 0.36f, -0.08f), 28f, 170f, 0.28f, 0.1f),
            (new Vector3(0.3f, 0.24f, 0.22f), 48f, 40f, 0.28f, 0.11f),
            (new Vector3(0.24f, 0.22f, 0.3f), 58f, 15f, 0.2f, 0.09f),
            (new Vector3(-0.3f, 0.2f, -0.08f), 52f, -100f, 0.26f, 0.1f),
        ];
        for (var i = 0; i < crystals.Length; i++)
        {
            var (foot, tilt, yaw, length, width) = crystals[i];
            var shaft = length * 0.72f;
            var tipLength = length - shaft;
            var shaftCentre = foot + PartBox.Rotate(new Vector3(0f, shaft / 2f, 0f), tilt, yaw, 0f);
            parts.Add(new CropPart($"Crystal{i}", CropPartShape.Cylinder, width, shaft, width, shaftCentre.X, shaftCentre.Y, shaftCentre.Z,
                tilt, yaw, 0f, "groth", Sides: 6));
            var tipCentre = foot + PartBox.Rotate(new Vector3(0f, shaft + tipLength / 2f, 0f), tilt, yaw, 0f);
            parts.Add(new CropPart($"CrystalTip{i}", CropPartShape.Cylinder, width, tipLength, 0.01f, tipCentre.X, tipCentre.Y, tipCentre.Z,
                tilt, yaw, 0f, "groth-light", Sides: 6));
        }

        return parts;
    }

    // ---- helpers ----

    /// <summary>A faceted lump: a sphere of the given width and height, turned so its facets don't line up.</summary>
    private static CropPart Lump(string name, float width, float height, Vector3 at, float pitch, float yaw, float roll, string role, int sides = LumpSides) =>
        new(name, CropPartShape.Sphere, width, height, width, at.X, at.Y, at.Z, pitch, yaw, roll, role, Sides: sides);

    /// <summary>A box running from <paramref name="from"/> to <paramref name="to"/> along its local Y.</summary>
    private static CropPart Beam(string name, float sizeX, float sizeZ, Vector3 from, Vector3 to, string role)
    {
        var d = to - from;
        var horizontal = MathF.Sqrt(d.X * d.X + d.Z * d.Z);
        var yaw = horizontal < 1e-5f ? 0f : MathF.Atan2(d.X, d.Z) * 180f / MathF.PI;
        var pitch = MathF.Atan2(horizontal, d.Y) * 180f / MathF.PI;
        var centre = (from + to) / 2f;
        // A little longer than the run, so consecutive pieces overlap at the joints.
        return new CropPart(name, CropPartShape.Box, sizeX, d.Length() + sizeX * 0.5f, sizeZ, centre.X, centre.Y, centre.Z, pitch, yaw, 0f, role);
    }

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
}
