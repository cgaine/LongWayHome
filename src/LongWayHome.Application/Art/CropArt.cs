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
/// <b>The models.</b> "Colour" is the crop's own role: <c>ember</c> for emberroot, <c>dusktuber</c> for dusktuber.
/// </para>
/// <list type="table">
/// <listheader><term>Model</term><description>Parts</description></listheader>
/// <item><term>Seed (both)</term><description><c>Mound</c> Sphere 0.36 x 0.10 at y 0.02, <c>dirt-path</c>;
/// <c>Seed</c> Sphere 0.10 x 0.07 at y 0.09, colour. About 0.36 wide, 0.13 tall.</description></item>
/// <item><term>Sprout (both)</term><description><c>Stem</c> Cylinder 0.035 x 0.19 x 0.035 at y 0.095,
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
/// </list>
/// <para>
/// The Seed, Sprout and Young models are up to 25% larger than the issue's starting sizes (the issue allows it), so
/// the early stages still read from the farthest camera zoom.
/// </para>
/// <para>
/// Moonmelon, sunbell and starcorn have no parts yet (<see cref="Parts"/> returns <c>null</c>); the game falls back
/// to their prototype models until the summer and fall features draw them.
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
        foreach (var (crop, colour) in new[] { (CropTypeId.Emberroot, "ember"), (CropTypeId.Dusktuber, "dusktuber") })
        {
            models[(crop, GrowthStage.Seed)] = Seed(colour);
            models[(crop, GrowthStage.Sprout)] = Sprout(colour);
        }

        models[(CropTypeId.Emberroot, GrowthStage.Young)] = EmberrootYoung();
        models[(CropTypeId.Emberroot, GrowthStage.Ripe)] = EmberrootRipe();
        models[(CropTypeId.Dusktuber, GrowthStage.Young)] = DusktuberYoung();
        models[(CropTypeId.Dusktuber, GrowthStage.Ripe)] = DusktuberRipe();
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
    /// An upright Prism whose foot is at <paramref name="root"/>, pitched <paramref name="pitch"/> outward and then
    /// yawed to <paramref name="yaw"/>: its centre is the root plus the rotated half-length.
    /// </summary>
    private static CropPart RisingFrom(
        string name, float sizeX, float sizeY, float sizeZ, Vector3 root, float pitch, float yaw, string role)
    {
        var centre = root + Vector3.Transform(new Vector3(0f, sizeY / 2f, 0f), Rotation(pitch, yaw, 0f));
        return new CropPart(name, CropPartShape.Prism, sizeX, sizeY, sizeZ, centre.X, centre.Y, centre.Z, pitch, yaw, 0f, role);
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
