using System.Numerics;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Art;

/// <summary>
/// The detail on a grass Tile's slab (ART_DIRECTION §7.2), so the plot reads as a lawn rather than a grid of identical
/// squares: flat turf patches in a lighter and a darker tone of the Season's grass, a dozen tufts of blades of mixed
/// height and tone (some leaning out over the slab's edge to soften the grooves) and, by variant, clover, small flowers,
/// pebbles or a taller seeding clump. Fall scatters a few fallen leaves; winter lays snow drifts with grass tips poking
/// through. Pure C#, no Godot: <c>GrassTileModels</c> merges each (variant, Season) into one mesh under the shared
/// palette material, shared by every Tile showing it.
/// <para>
/// <b>Variation.</b> There are <see cref="Variants"/> layouts. <see cref="PickFor"/> gives each grid position a layout and
/// a quarter turn from a hash of the position (the same mixer as <see cref="TileDecoration"/>), so neighbouring Tiles
/// rarely match, and the same Tile looks the same after a save and load.
/// </para>
/// <para>
/// <b>Conventions.</b> The <see cref="CropPart"/> conventions, with the origin at the centre of the slab's top (y 0) and
/// the slab 0.96 m square: parts stay within ±0.5 m of the centre, so a blade may lean 4 cm over the groove at most, and
/// nothing reaches more than 0.3 m above the slab. Nothing glows. Parts are named by group: <c>Patch*</c>,
/// <c>Tuft*</c>, <c>Clover*</c>, <c>Flower*</c>, <c>Pebble*</c>, <c>Seed*</c>, <c>Leaf*</c>, <c>Drift*</c>.
/// </para>
/// </summary>
public static class GrassTileArt
{
    /// <summary>How many layouts there are.</summary>
    public const int Variants = 8;

    /// <summary>How far from the slab's centre any part may reach, on each axis.</summary>
    public const float MaxReach = 0.5f;

    private const float Spread = 0.42f;

    private static readonly Dictionary<(int, SeasonGrass), IReadOnlyList<CropPart>> Cache = new();

    /// <summary>The layout and quarter turn for a grid position; the same position always gives the same pair.</summary>
    public static (int Variant, int QuarterTurns) PickFor(GridPosition position)
    {
        var state = unchecked((uint)TileDecoration.Hash(position) ^ 0xA5A5A5A5u);
        var variant = (int)(Next(ref state) % Variants);
        var turns = (int)(Next(ref state) % 4);
        return (variant, turns);
    }

    /// <summary>The detail parts of <paramref name="variant"/> in the Season's <paramref name="grass"/>. The same list for the same arguments.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The variant is not 0 to <see cref="Variants"/> − 1.</exception>
    public static IReadOnlyList<CropPart> Parts(int variant, SeasonGrass grass)
    {
        ArgumentNullException.ThrowIfNull(grass);
        if (variant is < 0 or >= Variants)
        {
            throw new ArgumentOutOfRangeException(nameof(variant), variant, $"A grass layout is 0 to {Variants - 1}.");
        }

        lock (Cache)
        {
            if (!Cache.TryGetValue((variant, grass), out var parts))
            {
                parts = Build(variant, grass);
                Cache[(variant, grass)] = parts;
            }

            return parts;
        }
    }

    /// <summary>The tones of a Season's grass: lighter and darker than its ground, and its flowers' colours.</summary>
    private sealed record Tones(string Light, string Dark, string[] Flowers, bool Snow);

    private static Tones TonesFor(SeasonGrass grass) => grass.Ground switch
    {
        "grass-mown" => new("grass-spring-mown", "grass-lit", ["blossom", "starcorn-sparkle", "sunbell-gold"], false),
        "grass-fall" => new("grass-fall-mown", "roof-wilted", ["sunbell-orange", "ember"], false),
        "snow" => new("starcorn-sparkle", "ship-metal", [], true),
        "grass-lit" => new("grass-mown", "grass-shade", ["starcorn-sparkle", "sunbell-gold", "leaf-underside"], false),
        _ => new(grass.Ground, "grass-shade", ["starcorn-sparkle"], false),
    };

    private static List<CropPart> Build(int variant, SeasonGrass grass)
    {
        var tones = TonesFor(grass);
        var parts = new List<CropPart>();
        var state = 0x9E3779B9u * (uint)(variant + 1);

        // Turf patches: low, flat lumps a shade lighter or darker than the ground, so the slab is mottled, not flat.
        // In winter they are snow drifts in white and a cool blue shadow.
        var patches = 4 + (int)(Next(ref state) % 3);
        for (var i = 0; i < patches; i++)
        {
            var width = 0.16f + 0.18f * Unit(ref state);
            var at = Spot(ref state, Spread - width / 2f);
            var role = i % 2 == 0 ? tones.Light : tones.Dark;
            var name = tones.Snow ? $"Drift{i}" : $"Patch{i}";
            var height = tones.Snow ? 0.05f : 0.026f;
            parts.Add(new CropPart(name, CropPartShape.Sphere, width, height, width * (0.6f + 0.4f * Unit(ref state)),
                at.X, 0f, at.Y, 0f, 360f * Unit(ref state), 0f, role, Sides: 7));
        }

        // Tufts of blades, fanned and leaning out, in three tones; a few near the edge lean over the groove.
        var tufts = tones.Snow ? 7 : 11 + (int)(Next(ref state) % 4);
        // In winter the tips through the snow keep to greens, never the snow's own tones.
        string[] bladeRoles = tones.Snow ? [grass.Tufts, "foliage", grass.Tufts] : [grass.Tufts, tones.Dark, tones.Light];
        for (var t = 0; t < tufts; t++)
        {
            var edge = t < 3;
            var at = edge ? EdgeSpot(ref state) : Spot(ref state, Spread);
            var blades = 3 + (int)(Next(ref state) % 3);
            var baseHeight = (0.07f + 0.07f * Unit(ref state)) * grass.TuftHeightScale;
            var baseYaw = 360f * Unit(ref state);
            for (var b = 0; b < blades; b++)
            {
                var height = baseHeight * (0.75f + 0.45f * Unit(ref state));
                var yaw = baseYaw + 360f / blades * b + 20f * (Unit(ref state) - 0.5f);
                var tilt = 12f + 22f * Unit(ref state);
                parts.Add(Blade($"Tuft{t}_{b}", at, height, 0.026f + 0.01f * Unit(ref state), yaw, tilt, bladeRoles[(b + t) % 3]));
            }
        }

        if (tones.Snow)
        {
            Pebbles(parts, ref state, variant % 2 == 0 ? 2 : 1);
            return parts;
        }

        switch (variant % 4)
        {
            case 0:
                Clover(parts, ref state, tones);
                break;
            case 1:
                Flowers(parts, ref state, tones, 4 + variant / 4);
                break;
            case 2:
                Pebbles(parts, ref state, 3);
                break;
            default:
                Seeding(parts, ref state, grass, tones);
                break;
        }

        if (grass.Ground == "grass-fall")
        {
            var leaves = 2 + variant % 3;
            for (var i = 0; i < leaves; i++)
            {
                var at = Spot(ref state, Spread);
                parts.Add(new CropPart($"Leaf{i}", CropPartShape.Box, 0.1f, 0.008f, 0.055f, at.X, 0.006f, at.Y,
                    0f, 360f * Unit(ref state), 6f * (Unit(ref state) - 0.5f), i % 2 == 0 ? "canopy-fall" : "sunbell-orange"));
            }
        }

        return parts;
    }

    // ---- the extras ----

    /// <summary>Two or three clusters of round clover leaves lying on the grass, with a white flower among one.</summary>
    private static void Clover(List<CropPart> parts, ref uint state, Tones tones)
    {
        var clusters = 2 + (int)(Next(ref state) % 2);
        for (var c = 0; c < clusters; c++)
        {
            var at = Spot(ref state, Spread - 0.06f);
            for (var l = 0; l < 3; l++)
            {
                var angle = (120f * l + 360f * Unit(ref state)) * MathF.PI / 180f;
                var leaf = at + new Vector2(0.035f * MathF.Cos(angle), 0.035f * MathF.Sin(angle));
                parts.Add(new CropPart($"Clover{c}_{l}", CropPartShape.Sphere, 0.06f, 0.014f, 0.06f, leaf.X, 0.03f, leaf.Y,
                    0f, 0f, 0f, l == 0 ? "foliage" : "grass-shade", Sides: 6));
            }

            if (c == 0 && tones.Flowers.Length > 0)
            {
                parts.Add(new CropPart("CloverFlowerStem", CropPartShape.Box, 0.008f, 0.06f, 0.008f, at.X, 0.05f, at.Y, 0f, 0f, 0f, "grass-shade"));
                parts.Add(new CropPart("CloverFlower", CropPartShape.Sphere, 0.04f, 0.035f, 0.04f, at.X, 0.085f, at.Y, 0f, 0f, 0f, tones.Flowers[0], Sides: 6));
            }
        }
    }

    /// <summary>A few small flowers on thin stems, in the Season's flower colours.</summary>
    private static void Flowers(List<CropPart> parts, ref uint state, Tones tones, int count)
    {
        if (tones.Flowers.Length == 0) return;
        for (var i = 0; i < count; i++)
        {
            var at = Spot(ref state, Spread);
            var height = 0.07f + 0.06f * Unit(ref state);
            var lean = 8f * (Unit(ref state) - 0.5f);
            parts.Add(new CropPart($"Flower{i}Stem", CropPartShape.Box, 0.008f, height, 0.008f, at.X, height / 2f, at.Y, lean, 0f, 0f, tones.Dark));
            var role = tones.Flowers[i % tones.Flowers.Length];
            parts.Add(new CropPart($"Flower{i}Head", CropPartShape.Sphere, 0.045f, 0.03f, 0.045f, at.X, height + 0.01f, at.Y, 0f, 0f, 0f, role, Sides: 6));
            parts.Add(new CropPart($"Flower{i}Eye", CropPartShape.Sphere, 0.016f, 0.016f, 0.016f, at.X, height + 0.025f, at.Y, 0f, 0f, 0f,
                role == "sunbell-gold" ? "ember" : "sunbell-gold", Sides: 4));
        }
    }

    /// <summary>Small stones half sunk in the turf.</summary>
    private static void Pebbles(List<CropPart> parts, ref uint state, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var size = 0.04f + 0.04f * Unit(ref state);
            var at = Spot(ref state, Spread - size);
            parts.Add(new CropPart($"Pebble{i}", CropPartShape.Sphere, size, size * 0.6f, size, at.X, size * 0.12f, at.Y,
                10f * (Unit(ref state) - 0.5f), 360f * Unit(ref state), 0f, i % 2 == 0 ? "rock" : "stone", Sides: 6));
        }
    }

    /// <summary>A clump of taller blades going to seed, each with a small seed head.</summary>
    private static void Seeding(List<CropPart> parts, ref uint state, SeasonGrass grass, Tones tones)
    {
        var at = Spot(ref state, Spread - 0.08f);
        for (var b = 0; b < 5; b++)
        {
            var height = (0.18f + 0.07f * Unit(ref state)) * grass.TuftHeightScale;
            var yaw = 72f * b + 20f * Unit(ref state);
            var tilt = 6f + 12f * Unit(ref state);
            parts.Add(Blade($"Seed{b}Stalk", at, height, 0.018f, yaw, tilt, b % 2 == 0 ? grass.Tufts : tones.Dark));
            var tip = new Vector3(at.X, 0f, at.Y) + PartBox.Rotate(new Vector3(0f, height, 0f), tilt, yaw, 0f);
            parts.Add(new CropPart($"Seed{b}Head", CropPartShape.Sphere, 0.026f, 0.05f, 0.026f, tip.X, tip.Y, tip.Z, tilt, yaw, 0f, tones.Light, Sides: 5));
        }
    }

    // ---- helpers ----

    /// <summary>A blade standing at <paramref name="at"/> (x, z), leaning <paramref name="tilt"/>° towards <paramref name="yaw"/>.</summary>
    private static CropPart Blade(string name, Vector2 at, float height, float width, float yaw, float tilt, string role)
    {
        var centre = new Vector3(at.X, 0f, at.Y) + PartBox.Rotate(new Vector3(0f, height / 2f, 0f), tilt, yaw, 0f);
        return new CropPart(name, CropPartShape.Prism, width, height, 0.006f, centre.X, centre.Y, centre.Z, tilt, yaw, 0f, role);
    }

    /// <summary>A point within ±<paramref name="reach"/> of the centre on both axes.</summary>
    private static Vector2 Spot(ref uint state, float reach) =>
        new((2f * Unit(ref state) - 1f) * reach, (2f * Unit(ref state) - 1f) * reach);

    /// <summary>A point along one of the slab's edges, 0.36–0.44 m out, for tufts that soften the grooves.</summary>
    private static Vector2 EdgeSpot(ref uint state)
    {
        var along = (2f * Unit(ref state) - 1f) * 0.4f;
        var out_ = 0.36f + 0.08f * Unit(ref state);
        return (Next(ref state) % 4) switch
        {
            0 => new Vector2(along, out_),
            1 => new Vector2(along, -out_),
            2 => new Vector2(out_, along),
            _ => new Vector2(-out_, along),
        };
    }

    /// <summary>A value in 0..1 from the next 24 random bits.</summary>
    private static float Unit(ref uint state) => (Next(ref state) >> 8) / (float)(1 << 24);

    /// <summary>The next value of a Weyl sequence through the murmur3 finaliser (as <see cref="TileDecoration"/>): stable everywhere.</summary>
    private static uint Next(ref uint state)
    {
        unchecked
        {
            state += 0x9E3779B9u;
            var z = state;
            z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
            z = (z ^ (z >> 13)) * 0xC2B2AE35u;
            return z ^ (z >> 16);
        }
    }
}
