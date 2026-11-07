using System.Numerics;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

/// <summary>The primitive a <see cref="CropPart"/> is drawn as (#90).</summary>
public enum CropPartShape
{
    Box,
    Sphere,
    Cylinder,
    Prism,
}

/// <summary>
/// One part of a crop model (#90). Sizes are full extents in metres (for a Sphere, <c>SizeX</c> is the diameter and
/// <c>SizeY</c> the height; for a Cylinder, <c>SizeX</c> is the bottom diameter, <c>SizeZ</c> the top diameter and
/// <c>SizeY</c> the height). The position is the part's centre with y 0 at the slab top. Rotations follow Godot's
/// <c>Node3D.RotationDegrees</c> (Euler YXZ), applied about the part's centre.
/// </summary>
public sealed record CropPart(
    string Name,
    CropPartShape Shape,
    float SizeX,
    float SizeY,
    float SizeZ,
    float X,
    float Y,
    float Z,
    float PitchDegrees,
    float YawDegrees,
    float RollDegrees,
    string Role);

/// <summary>
/// The parts of each (crop, stage) model drawn to ART_DIRECTION §4 (#90). Pure C#, no Godot: the Godot builder
/// <c>CropModels</c> only turns these parts into one merged mesh, drawn with the shared palette material.
/// <para>
/// <b>Conventions.</b> Sizes and positions are world metres on the 0.96 m slab (no model scale), y 0 at the slab top,
/// and every part stays inside the slab footprint (|X|, |Z| ≤ 0.46 at every corner). A part's rotation is Godot's
/// Euler YXZ, the same as <c>Node3D.RotationDegrees</c>: R = Ry(yaw)·Rx(pitch)·Rz(roll), so the part is rolled
/// first, then pitched, then yawed, about the world axes through its centre (in System.Numerics,
/// <see cref="Quaternion.CreateFromYawPitchRoll"/>). This differs from a literal "pitch, then yaw, then roll": that
/// order would spin a leaf lying along Z about its own long axis and leave the dusktuber leaves flat. A positive pitch
/// tips a part's +Y end towards +Z; a positive roll lifts its +X end. The unrotated box of a part, used for
/// <see cref="Bounds"/> and the footprint, is SizeX x SizeY x SizeZ for a Box or Prism, SizeX x SizeY x SizeX for a
/// Sphere and max(SizeX, SizeZ) x SizeY x max(SizeX, SizeZ) for a Cylinder.
/// </para>
/// <para>
/// Fronds and leaves that "rise from" a base point are stored by their centre, worked out here as the base plus the
/// rotated half-length: base + R·(0, length/2, 0) for an upright frond, base + R·(length/2, 0, 0) for a leaf lying
/// along X.
/// </para>
/// <para>
/// <b>The models.</b> "Colour" is the crop's own role: <c>ember</c> for emberroot, <c>dusktuber</c> for dusktuber,
/// <c>sunbell-gold</c> for sunbell (#91) and <c>starcorn</c> for starcorn (#91). A part placed "at radius r, yawed a"
/// is centred at Ry(a)·(0, y, r): yaw 0 is +Z, yaw 90 is +X.
/// </para>
/// <list type="table">
/// <listheader><term>Model</term><description>Parts</description></listheader>
/// <item><term>Seed (all four)</term><description><c>Mound</c> Sphere 0.36 x 0.10 at y 0.02, <c>dirt-path</c>;
/// <c>Seed</c> Sphere 0.10 x 0.07 at y 0.09, colour. About 0.36 wide, 0.13 tall.</description></item>
/// <item><term>Sprout (all four)</term><description><c>Stem</c> Cylinder 0.035 x 0.19 x 0.035 at y 0.095,
/// <c>foliage</c>; <c>SeedLeafLeft</c>/<c>SeedLeafRight</c> Boxes 0.17 x 0.02 x 0.085 rising from the stem top
/// (0, 0.18, 0) to the left and right, rolled so the outer ends lift 25° (a V), centres about (∓0.077, 0.22, 0),
/// <c>grass-mown</c>; <c>Bud</c> Sphere 0.075 at y 0.22, colour. About 0.32 wide, 0.26 tall.</description></item>
/// <item><term>Emberroot Young</term><description><c>Shoulder</c> Sphere 0.15 x 0.07 at y 0.02, <c>ember</c>;
/// <c>Frond0</c>–<c>Frond4</c> Prisms 0.07 x 0.36 x 0.015 rising from (0, 0.04, 0), yawed i x 72°, pitched 25°
/// outward, <c>foliage</c>. About 0.33 wide, 0.37 tall.</description></item>
/// <item><term>Emberroot Ripe</term><description><c>Shoulder</c> Cylinder bottom 0.14, top 0.22, height 0.14 at y
/// 0.05, <c>ember</c>; <c>Core</c> Sphere 0.10 x 0.04 at y 0.12, <c>lantern</c>; <c>Frond0</c>–<c>Frond6</c> Prisms
/// 0.07 x 0.45 x 0.015 rising from (0, 0.13, 0), yawed i x 51.43°, pitched 22° outward, <c>foliage</c> except
/// <c>Frond1</c> and <c>Frond4</c>, <c>leaf-underside</c>. About 0.40 wide, 0.55 tall.</description></item>
/// <item><term>Dusktuber Young</term><description><c>Bulb</c> Sphere 0.17 x 0.10 at y 0.03, <c>dusktuber</c>;
/// <c>Leaf0</c>–<c>Leaf4</c> Boxes 0.21 x 0.02 x 0.09 centred 0.14 m out at y 0.08, yawed i x 72°, rolled 20° up at
/// the outer end, <c>foliage</c>; <c>Curl0</c>–<c>Curl4</c> Spheres 0.06 at each leaf's outer tip,
/// <c>leaf-underside</c>. About 0.51 wide, 0.15 tall.</description></item>
/// <item><term>Dusktuber Ripe</term><description><c>Bulb</c> Sphere 0.40 x 0.30 at y 0.10, <c>dusktuber</c>;
/// <c>Speckle0</c>–<c>Speckle7</c> Spheres 0.035, four at y 0.17 at radius 0.18 yawed i x 90°, four at y 0.22 at
/// radius 0.13 yawed 45° + i x 90°, <c>moonmelon</c>; <c>Leaf0</c>–<c>Leaf3</c> Boxes 0.26 x 0.02 x 0.09 rising
/// from the bulb top (0, 0.26, 0), yawed i x 90° + 20°, rolled 35° up, <c>foliage</c>; <c>Curl0</c>–<c>Curl3</c>
/// Spheres 0.06 at each leaf's outer tip, <c>leaf-underside</c>. About 0.46 wide, 0.44 tall.</description></item>
/// <item><term>Sunbell Young</term><description><c>Cane0</c>–<c>Cane2</c> Cylinders bottom 0.025, top 0.015,
/// height 0.22 rising from (0, 0, 0), yawed i x 120°, pitched 15° outward, <c>foliage</c>; <c>Leaf0</c>–<c>Leaf5</c>
/// Prisms 0.12 x 0.16 x 0.03 at radius 0.10, yawed i x 60°, at y 0.18 (even i) or 0.26 (odd i), pitched 40°
/// outward, <c>foliage</c>; <c>Berry</c> Sphere 0.05 at (0.08, 0.16, 0), <c>grass-mown</c>. About 0.34 wide, 0.33
/// tall.</description></item>
/// <item><term>Sunbell Ripe</term><description><c>Foliage0</c> Sphere 0.22 x 0.18 at y 0.30 and
/// <c>Foliage1</c>–<c>Foliage4</c> Spheres 0.22 x 0.18 at y 0.20, radius 0.16, yawed i x 90°, <c>foliage</c>; six
/// berries at radius 0.25, yawed i x 60°, each <c>Berry&lt;i&gt;Top</c> Cylinder bottom 0.06, top 0.09, height 0.05
/// at y 0.17, <c>sunbell-gold</c>, <c>Berry&lt;i&gt;Tip</c> Cylinder bottom 0, top 0.06, height 0.05 straight under it
/// at y 0.12, <c>sunbell-orange</c> (the point, hanging down), and <c>Berry&lt;i&gt;SepalsA</c>/<c>SepalsB</c>
/// Boxes 0.09 x 0.01 x 0.02 at y 0.20, yawed with the berry and 90° across it, <c>foliage</c>. No glow. About 0.59
/// wide, 0.39 tall.</description></item>
/// <item><term>Starcorn Young</term><description><c>Stalk</c> Cylinder bottom 0.05, top 0.035, height 0.40 at y
/// 0.20, <c>foliage</c>; <c>Leaf0</c>–<c>Leaf3</c> Boxes 0.30 x 0.02 x 0.06 from the stalk axis at y 0.12, 0.20,
/// 0.28 and 0.36, yawed i x 90° + 30°, the outer end drooping (rolled −25°), <c>foliage</c>. About 0.51 wide, 0.40
/// tall.</description></item>
/// <item><term>Starcorn Ripe</term><description><c>Stalk</c> Cylinder bottom 0.08, top 0.05, height 0.95 at y
/// 0.475, <c>foliage</c>; <c>Leaf0</c>–<c>Leaf4</c> Boxes 0.36 x 0.02 x 0.07 from the stalk axis at y 0.15, 0.30,
/// 0.45, 0.60 and 0.75, yawed i x 72°, drooping (rolled −25°), <c>foliage</c>; <c>Cob</c> Cylinder bottom 0.15, top
/// 0.13, height 0.32 centred at (0.09, 0.62, 0), rolled −15° so its top leans away from the stalk, <c>starcorn</c>;
/// <c>HuskLeft</c>/<c>HuskRight</c> Prisms 0.08 x 0.26 x 0.02 rising from the cob's foot on its −Z and +Z sides,
/// rolled −15° with the cob and pitched ∓20° to peel back, <c>grass-mown</c>; <c>Sparkle0</c>–<c>Sparkle7</c>
/// Boxes 0.025 pitched 45° and rolled 45° (diamonds), centred 0.005 m proud of the cob's tapering surface, spread
/// from 0.125 m below to 0.125 m above its middle and round its outer side (−63° to 63° from the cob's +X),
/// <c>starcorn-sparkle</c>; <c>Tassel0</c>–<c>Tassel2</c> Prisms 0.03 x 0.14 x 0.01 rising from (0, 0.935, 0) so
/// they are centred at y 1.0, yawed i x 120°, pitched 20° outward, <c>sunbell-gold</c>. About 0.65 wide, 1.07
/// tall.</description></item>
/// </list>
/// <para>
/// The Seed, Sprout and Young models of emberroot and dusktuber are up to 25% larger than #90's starting sizes (the
/// issue allows it), so the early stages still read from the farthest camera zoom. Ripe sunbell's outer foliage
/// (radius 0.16, not 0.13) and berries (radius 0.25, not 0.20) sit further out than #91's starting sizes, so the bush
/// is at least 0.1 m wider than ripe dusktuber (AC4).
/// </para>
/// <para>
/// Moonmelon has no parts yet (<see cref="Parts"/> returns <c>null</c>); the game falls back to its prototype model
/// until the fall feature (#92) draws it.
/// </para>
/// </summary>
public static class CropArt
{
    private static readonly Dictionary<(CropTypeId Crop, GrowthStage Stage), IReadOnlyList<CropPart>> Models = Build();

    /// <summary>The parts of a crop's model at a growth stage, or <c>null</c> for a crop not drawn to the art yet.</summary>
    public static IReadOnlyList<CropPart>? Parts(CropTypeId crop, GrowthStage stage) =>
        Models.TryGetValue((crop, stage), out var parts) ? parts : null;

    /// <summary>
    /// Width is the larger of the X and Z extents of the parts' axis-aligned bounds (each part's rotated box); Height
    /// is the top of the highest part above y 0.
    /// </summary>
    public static (float Width, float Height) Bounds(IReadOnlyList<CropPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Count == 0)
        {
            return (0f, 0f);
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var corner in parts.SelectMany(Corners))
        {
            min = Vector3.Min(min, corner);
            max = Vector3.Max(max, corner);
        }

        return (MathF.Max(max.X - min.X, max.Z - min.Z), max.Y);
    }

    /// <summary>The eight corners of a part's unrotated box, rotated about its centre.</summary>
    private static IEnumerable<Vector3> Corners(CropPart part)
    {
        var footprint = part.Shape switch
        {
            CropPartShape.Sphere => (X: part.SizeX, Z: part.SizeX),
            CropPartShape.Cylinder => (X: MathF.Max(part.SizeX, part.SizeZ), Z: MathF.Max(part.SizeX, part.SizeZ)),
            _ => (X: part.SizeX, Z: part.SizeZ),
        };
        var half = new Vector3(footprint.X / 2f, part.SizeY / 2f, footprint.Z / 2f);
        var rotation = Rotation(part.PitchDegrees, part.YawDegrees, part.RollDegrees);
        var centre = new Vector3(part.X, part.Y, part.Z);
        for (var i = 0; i < 8; i++)
        {
            var sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
            yield return centre + Vector3.Transform(half * sign, rotation);
        }
    }

    /// <summary>Godot's Euler YXZ rotation, Ry(yaw)·Rx(pitch)·Rz(roll).</summary>
    private static Quaternion Rotation(float pitchDegrees, float yawDegrees, float rollDegrees) =>
        Quaternion.CreateFromYawPitchRoll(Radians(yawDegrees), Radians(pitchDegrees), Radians(rollDegrees));

    private static float Radians(float degrees) => degrees * MathF.PI / 180f;

    private static Dictionary<(CropTypeId Crop, GrowthStage Stage), IReadOnlyList<CropPart>> Build()
    {
        var models = new Dictionary<(CropTypeId, GrowthStage), IReadOnlyList<CropPart>>();
        var colours = new[]
        {
            (CropTypeId.Emberroot, "ember"),
            (CropTypeId.Dusktuber, "dusktuber"),
            (CropTypeId.Sunbell, "sunbell-gold"),
            (CropTypeId.Starcorn, "starcorn"),
        };
        foreach (var (crop, colour) in colours)
        {
            models[(crop, GrowthStage.Seed)] = Seed(colour);
            models[(crop, GrowthStage.Sprout)] = Sprout(colour);
        }

        models[(CropTypeId.Emberroot, GrowthStage.Young)] = EmberrootYoung();
        models[(CropTypeId.Emberroot, GrowthStage.Ripe)] = EmberrootRipe();
        models[(CropTypeId.Dusktuber, GrowthStage.Young)] = DusktuberYoung();
        models[(CropTypeId.Dusktuber, GrowthStage.Ripe)] = DusktuberRipe();
        models[(CropTypeId.Sunbell, GrowthStage.Young)] = SunbellYoung();
        models[(CropTypeId.Sunbell, GrowthStage.Ripe)] = SunbellRipe();
        models[(CropTypeId.Starcorn, GrowthStage.Young)] = StarcornYoung();
        models[(CropTypeId.Starcorn, GrowthStage.Ripe)] = StarcornRipe();
        return models;
    }

    /// <summary>A light dirt mound, which reads on dry and watered soil, with a seed in the crop's colour.</summary>
    private static CropPart[] Seed(string colour) =>
    [
        Upright("Mound", CropPartShape.Sphere, 0.36f, 0.10f, 0.36f, 0.02f, "dirt-path"),
        Upright("Seed", CropPartShape.Sphere, 0.10f, 0.07f, 0.10f, 0.09f, colour),
    ];

    /// <summary>A short stem with two seed leaves in a V and a bud in the crop's colour.</summary>
    private static CropPart[] Sprout(string colour)
    {
        var stemTop = new Vector3(0f, 0.18f, 0f);
        return
        [
            Upright("Stem", CropPartShape.Cylinder, 0.035f, 0.19f, 0.035f, 0.095f, "foliage"),
            LyingFrom("SeedLeafLeft", 0.17f, 0.02f, 0.085f, stemTop, yaw: 180f, roll: 25f, "grass-mown"),
            LyingFrom("SeedLeafRight", 0.17f, 0.02f, 0.085f, stemTop, yaw: 0f, roll: 25f, "grass-mown"),
            Upright("Bud", CropPartShape.Sphere, 0.075f, 0.075f, 0.075f, 0.22f, colour),
        ];
    }

    /// <summary>The orange shoulder just showing, under five feathery fronds.</summary>
    private static List<CropPart> EmberrootYoung()
    {
        var parts = new List<CropPart> { Upright("Shoulder", CropPartShape.Sphere, 0.15f, 0.07f, 0.15f, 0.02f, "ember") };
        var root = new Vector3(0f, 0.04f, 0f);
        for (var i = 0; i < 5; i++)
        {
            parts.Add(RisingFrom($"Frond{i}", 0.07f, 0.36f, 0.015f, root, pitch: 25f, yaw: i * 72f, "foliage"));
        }

        return parts;
    }

    /// <summary>A wide orange shoulder half out of the soil, a warm core, and a tall tuft of seven fronds.</summary>
    private static List<CropPart> EmberrootRipe()
    {
        var parts = new List<CropPart>
        {
            Upright("Shoulder", CropPartShape.Cylinder, 0.14f, 0.14f, 0.22f, 0.05f, "ember"),
            Upright("Core", CropPartShape.Sphere, 0.10f, 0.04f, 0.10f, 0.12f, "lantern"),
        };
        var root = new Vector3(0f, 0.13f, 0f);
        for (var i = 0; i < 7; i++)
        {
            var role = i is 1 or 4 ? "leaf-underside" : "foliage";
            parts.Add(RisingFrom($"Frond{i}", 0.07f, 0.45f, 0.015f, root, pitch: 22f, yaw: i * 360f / 7f, role));
        }

        return parts;
    }

    /// <summary>A small purple bulb under a low rosette of five leaves curling violet at the tips.</summary>
    private static List<CropPart> DusktuberYoung()
    {
        var parts = new List<CropPart> { Upright("Bulb", CropPartShape.Sphere, 0.17f, 0.10f, 0.17f, 0.03f, "dusktuber") };
        for (var i = 0; i < 5; i++)
        {
            var yaw = i * 72f;
            var centre = Vector3.Transform(new Vector3(0.14f, 0.08f, 0f), Rotation(0f, yaw, 0f));
            var leaf = Lying($"Leaf{i}", 0.21f, 0.02f, 0.09f, centre, yaw, roll: 20f, "foliage");
            parts.Add(leaf);
            parts.Add(CurlAtTip($"Curl{i}", 0.06f, leaf));
        }

        return parts;
    }

    /// <summary>A big night-purple bulb half out of the soil, speckled with pale stars, under four curling leaves.</summary>
    private static List<CropPart> DusktuberRipe()
    {
        var parts = new List<CropPart> { Upright("Bulb", CropPartShape.Sphere, 0.40f, 0.30f, 0.40f, 0.10f, "dusktuber") };
        for (var i = 0; i < 4; i++)
        {
            parts.Add(Speckle($"Speckle{i}", radius: 0.18f, y: 0.17f, yaw: i * 90f));
            parts.Add(Speckle($"Speckle{i + 4}", radius: 0.13f, y: 0.22f, yaw: 45f + (i * 90f)));
        }

        var bulbTop = new Vector3(0f, 0.26f, 0f);
        for (var i = 0; i < 4; i++)
        {
            var leaf = LyingFrom($"Leaf{i}", 0.26f, 0.02f, 0.09f, bulbTop, yaw: (i * 90f) + 20f, roll: 35f, "foliage");
            parts.Add(leaf);
            parts.Add(CurlAtTip($"Curl{i}", 0.06f, leaf));
        }

        return parts;
    }

    /// <summary>Three young canes leaning out, a ring of leaves round them and one small green berry.</summary>
    private static List<CropPart> SunbellYoung()
    {
        var parts = new List<CropPart>();
        for (var i = 0; i < 3; i++)
        {
            parts.Add(RisingFrom(
                $"Cane{i}", 0.025f, 0.22f, 0.015f, Vector3.Zero, pitch: 15f, yaw: i * 120f, "foliage", CropPartShape.Cylinder));
        }

        for (var i = 0; i < 6; i++)
        {
            var y = 0.18f + (0.08f * (i % 2));
            parts.Add(Around($"Leaf{i}", CropPartShape.Prism, 0.12f, 0.16f, 0.03f, radius: 0.10f, y, yaw: i * 60f, pitch: 40f, "foliage"));
        }

        parts.Add(new CropPart("Berry", CropPartShape.Sphere, 0.05f, 0.05f, 0.05f, 0.08f, 0.16f, 0f, 0f, 0f, 0f, "grass-mown"));
        return parts;
    }

    /// <summary>
    /// A rounded raspberry bush with six sun-coloured berries hanging point down under its edge, each gold at the top
    /// shading to an orange point under a green star of sepals. Nothing on it is a bell.
    /// </summary>
    private static List<CropPart> SunbellRipe()
    {
        const float berryRadius = 0.25f;
        var parts = new List<CropPart> { Upright("Foliage0", CropPartShape.Sphere, 0.22f, 0.18f, 0.22f, 0.30f, "foliage") };
        for (var i = 0; i < 4; i++)
        {
            parts.Add(Around(
                $"Foliage{i + 1}", CropPartShape.Sphere, 0.22f, 0.18f, 0.22f, radius: 0.16f, y: 0.20f, yaw: i * 90f, pitch: 0f, "foliage"));
        }

        for (var i = 0; i < 6; i++)
        {
            var yaw = i * 60f;
            parts.Add(Around($"Berry{i}Top", CropPartShape.Cylinder, 0.06f, 0.05f, 0.09f, berryRadius, 0.17f, yaw, pitch: 0f, "sunbell-gold"));
            parts.Add(Around($"Berry{i}Tip", CropPartShape.Cylinder, 0f, 0.05f, 0.06f, berryRadius, 0.12f, yaw, pitch: 0f, "sunbell-orange"));
            parts.Add(Around($"Berry{i}SepalsA", CropPartShape.Box, 0.09f, 0.01f, 0.02f, berryRadius, 0.20f, yaw, pitch: 0f, "foliage"));
            parts.Add(Around(
                $"Berry{i}SepalsB", CropPartShape.Box, 0.09f, 0.01f, 0.02f, berryRadius, 0.20f, yaw + 90f, pitch: 0f, "foliage", positionYaw: yaw));
        }

        return parts;
    }

    /// <summary>A young corn stalk with four long leaves drooping at their outer ends.</summary>
    private static List<CropPart> StarcornYoung()
    {
        var parts = new List<CropPart> { Upright("Stalk", CropPartShape.Cylinder, 0.05f, 0.40f, 0.035f, 0.20f, "foliage") };
        for (var i = 0; i < 4; i++)
        {
            var root = new Vector3(0f, 0.12f + (0.08f * i), 0f);
            parts.Add(LyingFrom($"Leaf{i}", 0.30f, 0.02f, 0.06f, root, yaw: (i * 90f) + 30f, roll: -25f, "foliage"));
        }

        return parts;
    }

    /// <summary>
    /// A tall corn stalk, by far the tallest crop on the farm, with five drooping leaves, a big golden cob leaning out
    /// from it between two peeled-back husks, eight white diamond sparkles round the cob's outer side and a gold tassel.
    /// </summary>
    private static List<CropPart> StarcornRipe()
    {
        var parts = new List<CropPart> { Upright("Stalk", CropPartShape.Cylinder, 0.08f, 0.95f, 0.05f, 0.475f, "foliage") };
        for (var i = 0; i < 5; i++)
        {
            var root = new Vector3(0f, 0.15f * (i + 1), 0f);
            parts.Add(LyingFrom($"Leaf{i}", 0.36f, 0.02f, 0.07f, root, yaw: i * 72f, roll: -25f, "foliage"));
        }

        // The cob stands 0.09 m out from the stalk (+X) and leans 15° away from it: a negative roll tips its top to +X.
        const float cobBottom = 0.15f;
        const float cobTop = 0.13f;
        const float cobLength = 0.32f;
        const float cobRoll = -15f;
        var cobCentre = new Vector3(0.09f, 0.62f, 0f);
        var cobRotation = Rotation(0f, 0f, cobRoll);
        parts.Add(new CropPart(
            "Cob", CropPartShape.Cylinder, cobBottom, cobLength, cobTop, cobCentre.X, cobCentre.Y, cobCentre.Z, 0f, 0f, cobRoll, "starcorn"));

        // The husks stand on the cob's foot either side of it (the cob's ±Z) and peel 20° back from it.
        foreach (var (name, side) in new[] { ("HuskLeft", -1f), ("HuskRight", 1f) })
        {
            var foot = cobCentre + Vector3.Transform(new Vector3(0f, -cobLength / 2f, side * cobBottom / 2f), cobRotation);
            parts.Add(RisingFrom(name, 0.08f, 0.26f, 0.02f, foot, pitch: side * 20f, yaw: 0f, "grass-mown", roll: cobRoll));
        }

        // Eight sparkles spread up the cob and round its outer side, just proud of its tapering surface.
        for (var i = 0; i < 8; i++)
        {
            var along = -0.125f + (i * 0.25f / 7f);
            var t = (along + (cobLength / 2f)) / cobLength;
            var radius = (cobBottom / 2f) + (((cobTop - cobBottom) / 2f) * t) + 0.005f;
            var around = Radians((((i * 3) % 8) - 3.5f) * 18f);
            var at = cobCentre + Vector3.Transform(
                new Vector3(radius * MathF.Cos(around), along, radius * MathF.Sin(around)), cobRotation);
            parts.Add(new CropPart(
                $"Sparkle{i}", CropPartShape.Box, 0.025f, 0.025f, 0.025f, at.X, at.Y, at.Z, 45f, 0f, 45f, "starcorn-sparkle"));
        }

        // The tassel rises from just inside the stalk top, so its three prisms are centred at y 1.0.
        var tasselRoot = new Vector3(0f, 0.935f, 0f);
        for (var i = 0; i < 3; i++)
        {
            parts.Add(RisingFrom($"Tassel{i}", 0.03f, 0.14f, 0.01f, tasselRoot, pitch: 20f, yaw: i * 120f, "sunbell-gold"));
        }

        return parts;
    }

    private static CropPart Speckle(string name, float radius, float y, float yaw)
    {
        var at = Vector3.Transform(new Vector3(radius, y, 0f), Rotation(0f, yaw, 0f));
        return new CropPart(name, CropPartShape.Sphere, 0.035f, 0.035f, 0.035f, at.X, at.Y, at.Z, 0f, 0f, 0f, "moonmelon");
    }

    /// <summary>An unrotated part centred on the slab's centre line at height <paramref name="y"/>.</summary>
    private static CropPart Upright(
        string name, CropPartShape shape, float sizeX, float sizeY, float sizeZ, float y, string role) =>
        new(name, shape, sizeX, sizeY, sizeZ, 0f, y, 0f, 0f, 0f, 0f, role);

    /// <summary>
    /// An upright part (a Prism unless <paramref name="shape"/> says otherwise) whose foot is at <paramref name="root"/>,
    /// rolled <paramref name="roll"/>, pitched <paramref name="pitch"/> outward and then yawed to <paramref name="yaw"/>:
    /// its centre is the root plus the rotated half-length.
    /// </summary>
    private static CropPart RisingFrom(
        string name,
        float sizeX,
        float sizeY,
        float sizeZ,
        Vector3 root,
        float pitch,
        float yaw,
        string role,
        CropPartShape shape = CropPartShape.Prism,
        float roll = 0f)
    {
        var centre = root + Vector3.Transform(new Vector3(0f, sizeY / 2f, 0f), Rotation(pitch, yaw, roll));
        return new CropPart(name, shape, sizeX, sizeY, sizeZ, centre.X, centre.Y, centre.Z, pitch, yaw, roll, role);
    }

    /// <summary>
    /// A part centred <paramref name="radius"/> out from the slab's centre line at height <paramref name="y"/>, towards
    /// <paramref name="positionYaw"/> (by default <paramref name="yaw"/>; yaw 0 is +Z), then pitched
    /// <paramref name="pitch"/> outward and yawed to <paramref name="yaw"/>.
    /// </summary>
    private static CropPart Around(
        string name,
        CropPartShape shape,
        float sizeX,
        float sizeY,
        float sizeZ,
        float radius,
        float y,
        float yaw,
        float pitch,
        string role,
        float? positionYaw = null)
    {
        var at = Vector3.Transform(new Vector3(0f, y, radius), Rotation(0f, positionYaw ?? yaw, 0f));
        return new CropPart(name, shape, sizeX, sizeY, sizeZ, at.X, at.Y, at.Z, pitch, yaw, 0f, role);
    }

    /// <summary>
    /// A Box lying along X whose inner end is at <paramref name="root"/>, its outer end rolled up by
    /// <paramref name="roll"/> and then yawed to <paramref name="yaw"/>: its centre is the root plus the rotated
    /// half-length.
    /// </summary>
    private static CropPart LyingFrom(
        string name, float sizeX, float sizeY, float sizeZ, Vector3 root, float yaw, float roll, string role)
    {
        var centre = root + Vector3.Transform(new Vector3(sizeX / 2f, 0f, 0f), Rotation(0f, yaw, roll));
        return Lying(name, sizeX, sizeY, sizeZ, centre, yaw, roll, role);
    }

    private static CropPart Lying(
        string name, float sizeX, float sizeY, float sizeZ, Vector3 centre, float yaw, float roll, string role) =>
        new(name, CropPartShape.Box, sizeX, sizeY, sizeZ, centre.X, centre.Y, centre.Z, 0f, yaw, roll, role);

    /// <summary>A small sphere centred on the outer (+X) end of a lying <paramref name="leaf"/>.</summary>
    private static CropPart CurlAtTip(string name, float diameter, CropPart leaf)
    {
        var tip = new Vector3(leaf.X, leaf.Y, leaf.Z)
            + Vector3.Transform(new Vector3(leaf.SizeX / 2f, 0f, 0f), Rotation(0f, leaf.YawDegrees, leaf.RollDegrees));
        return new CropPart(name, CropPartShape.Sphere, diameter, diameter, diameter, tip.X, tip.Y, tip.Z, 0f, 0f, 0f, "leaf-underside");
    }
}
