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
/// faceted lump of <c>stone</c> with lighter <c>rock</c> facets, sunk a little into the ground. Pure C#, no Godot: the
/// sandbox's builder <c>NatureModels</c> merges these parts into one mesh under the shared palette material. For now
/// they are shown only in the art sandbox.
/// <para>
/// <b>Conventions.</b> The <see cref="CropPart"/> conventions, with the origin at the rock's centre <b>on the
/// ground</b> (y 0). A rock is about 0.9 m across and 0.5 m tall; a boulder about 1.7 m across and 1 m tall. Nothing
/// glows. Parts are named by group: <c>Body*</c>, <c>Facet*</c>, <c>Crack*</c>, <c>Vein*</c>, <c>Fleck*</c>,
/// <c>Crystal*</c>, <c>Pebble*</c>.
/// </para>
/// </summary>
public static class RockArt
{
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
                    _ => Body(1f),
                };
                Cache[kind] = parts;
            }

            return parts;
        }
    }

    /// <summary>
    /// The rock's body at <paramref name="scale"/>: a big flattened lump, a smaller one leaning on it, a few lighter
    /// facets standing proud of the surface, and a couple of pebbles at the foot.
    /// </summary>
    private static List<CropPart> Body(float scale)
    {
        var parts = new List<CropPart>
        {
            Lump("Body0", 0.9f, 0.62f, new Vector3(0f, 0.2f, 0f), 0f, 10f, 6f, "stone"),
            Lump("Body1", 0.56f, 0.44f, new Vector3(0.3f, 0.14f, 0.16f), 12f, 40f, -10f, "stone"),
            Lump("Body2", 0.46f, 0.36f, new Vector3(-0.28f, 0.1f, -0.14f), -8f, 70f, 12f, "stone"),
        };

        // Lighter lumps set into the top and the sun side, so the rock reads faceted from above.
        parts.Add(Lump("Facet0", 0.5f, 0.3f, new Vector3(-0.06f, 0.36f, 0.04f), 10f, 35f, -6f, "rock"));
        parts.Add(Lump("Facet1", 0.34f, 0.26f, new Vector3(0.22f, 0.3f, 0.2f), -14f, 5f, 18f, "rock"));

        (float X, float Z, float Size)[] pebbles = [(0.52f, -0.22f, 0.14f), (-0.5f, 0.3f, 0.11f)];
        for (var i = 0; i < pebbles.Length; i++)
        {
            var (x, z, size) = pebbles[i];
            parts.Add(Lump($"Pebble{i}", size, size * 0.7f, new Vector3(x, size * 0.25f, z), 0f, 30f * i, 0f, "rock"));
        }

        return scale == 1f ? parts : Scaled(parts, scale);
    }

    private static List<CropPart> Boulder()
    {
        var parts = Body(1.9f);
        // Dark cracks across the front and the top: it takes a stronger pickaxe.
        (Vector3 At, float Pitch, float Yaw, float Roll, float Length)[] cracks =
        [
            (new Vector3(-0.1f, 0.62f, 0.72f), -35f, 0f, 30f, 0.5f),
            (new Vector3(0.16f, 0.45f, 0.8f), -40f, 0f, -45f, 0.4f),
            (new Vector3(0.05f, 0.96f, 0.15f), 0f, 25f, 0f, 0.6f),
            (new Vector3(-0.25f, 0.92f, 0.3f), 0f, -40f, 0f, 0.35f),
        ];
        for (var i = 0; i < cracks.Length; i++)
        {
            var (at, pitch, yaw, roll, length) = cracks[i];
            parts.Add(new CropPart($"Crack{i}", CropPartShape.Box, length, 0.025f, 0.045f, at.X, at.Y, at.Z, pitch, yaw, roll, "timber-trim"));
        }

        return parts;
    }

    private static List<CropPart> Rosk()
    {
        var parts = Body(1f);
        // Rose-copper veins that curl into little spirals, lying over the top and front.
        (Vector3 Centre, float Yaw, float Pitch)[] spirals =
        [
            (new Vector3(-0.05f, 0.5f, 0.05f), 0f, 4f),
            (new Vector3(0.22f, 0.38f, 0.32f), 35f, 32f),
            (new Vector3(-0.32f, 0.3f, 0.2f), -50f, 30f),
        ];
        for (var s = 0; s < spirals.Length; s++)
        {
            var (centre, yaw, pitch) = spirals[s];
            for (var i = 0; i < 5; i++)
            {
                var turn = 1.1f * i;
                var radius = 0.04f + 0.032f * i;
                var local = new Vector3(radius * MathF.Cos(turn), 0.035f, radius * MathF.Sin(turn));
                var at = centre + PartBox.Rotate(local, pitch, yaw, 0f);
                parts.Add(new CropPart($"Vein{s}_{i}", CropPartShape.Box, 0.09f + 0.016f * i, 0.035f, 0.045f, at.X, at.Y, at.Z,
                    pitch, yaw - turn * 180f / MathF.PI + 90f, 0f, "rosk"));
            }
        }

        // Bright flecks.
        Vector3[] flecks = [new(0.08f, 0.5f, -0.12f), new(-0.2f, 0.45f, -0.05f), new(0.32f, 0.33f, 0.05f), new(0f, 0.36f, 0.33f)];
        for (var i = 0; i < flecks.Length; i++)
        {
            parts.Add(new CropPart($"Fleck{i}", CropPartShape.Box, 0.04f, 0.04f, 0.04f, flecks[i].X, flecks[i].Y, flecks[i].Z, 30f, 45f * i, 20f, "starcorn-sparkle"));
        }

        return parts;
    }

    private static List<CropPart> Groth()
    {
        var parts = Body(1f);
        // Blocky dark blue-grey crystals pushing out of the top and sides.
        (Vector3 Foot, float Tilt, float Yaw, float Length, float Width)[] crystals =
        [
            (new Vector3(0.02f, 0.38f, 0f), 8f, 0f, 0.5f, 0.17f),
            (new Vector3(0.12f, 0.34f, 0.08f), 28f, 50f, 0.38f, 0.14f),
            (new Vector3(-0.1f, 0.34f, 0.06f), 32f, -40f, 0.34f, 0.13f),
            (new Vector3(0.3f, 0.24f, 0.22f), 48f, 40f, 0.28f, 0.12f),
            (new Vector3(-0.3f, 0.2f, -0.08f), 52f, -100f, 0.26f, 0.11f),
        ];
        for (var i = 0; i < crystals.Length; i++)
        {
            var (foot, tilt, yaw, length, width) = crystals[i];
            var centre = foot + PartBox.Rotate(new Vector3(0f, length / 2f, 0f), tilt, yaw, 0f);
            parts.Add(new CropPart($"Crystal{i}", CropPartShape.Box, width, length, width, centre.X, centre.Y, centre.Z, tilt, yaw, 0f, "groth"));
        }

        return parts;
    }

    // ---- helpers ----

    /// <summary>A low-poly lump: a sphere of the given width and height, turned so its facets don't line up.</summary>
    private static CropPart Lump(string name, float width, float height, Vector3 at, float pitch, float yaw, float roll, string role) =>
        new(name, CropPartShape.Sphere, width, height, width, at.X, at.Y, at.Z, pitch, yaw, roll, role);

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
