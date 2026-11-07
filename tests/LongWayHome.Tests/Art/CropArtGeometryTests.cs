using System.Numerics;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The placement rules of #90 that <see cref="CropArtTests"/> leaves to the table: parts that "rise from" a base point
/// are stored by their centre, so their foot lands back on that point; curls sit on their leaf's outer tip; seed leaves
/// lift at their outer ends; and <see cref="CropArt.Bounds"/> of no parts is empty.
/// </summary>
public class CropArtGeometryTests
{
    private const float Tolerance = 1e-4f;

    private static Quaternion Rotation(CropPart part) => Quaternion.CreateFromYawPitchRoll(
        part.YawDegrees * MathF.PI / 180f,
        part.PitchDegrees * MathF.PI / 180f,
        part.RollDegrees * MathF.PI / 180f);

    private static Vector3 Centre(CropPart part) => new(part.X, part.Y, part.Z);

    /// <summary>The point at <paramref name="local"/> in the part's own (unrotated, centred) frame.</summary>
    private static Vector3 At(CropPart part, Vector3 local) => Centre(part) + Vector3.Transform(local, Rotation(part));

    private static IReadOnlyList<CropPart> PartsOf(CropTypeId crop, GrowthStage stage) =>
        CropArt.Parts(crop, stage) ?? throw new InvalidOperationException($"{crop} {stage} has no parts");

    private static void AssertNear(Vector3 expected, Vector3 actual, string what) =>
        Assert.True(Vector3.Distance(expected, actual) <= Tolerance, $"{what}: expected {expected}, got {actual}");

    [Fact]
    public void Bounds_of_no_parts_are_zero() => Assert.Equal((0f, 0f), CropArt.Bounds([]));

    [Fact]
    public void Bounds_reject_null() => Assert.Throws<ArgumentNullException>(() => CropArt.Bounds(null!));

    [Theory]
    [InlineData(CropTypeId.Emberroot, GrowthStage.Young, 0.04f, 25f)]
    [InlineData(CropTypeId.Emberroot, GrowthStage.Ripe, 0.13f, 22f)]
    public void Fronds_stand_on_their_root_and_lean_outward(CropTypeId crop, GrowthStage stage, float rootY, float pitch)
    {
        var fronds = PartsOf(crop, stage).Where(part => part.Name.StartsWith("Frond", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(fronds);
        foreach (var frond in fronds)
        {
            AssertNear(new Vector3(0f, rootY, 0f), At(frond, new Vector3(0f, -frond.SizeY / 2f, 0f)), $"{frond.Name} foot");
            Assert.Equal(pitch, frond.PitchDegrees, Tolerance);
            var tip = At(frond, new Vector3(0f, frond.SizeY / 2f, 0f));
            Assert.True(new Vector2(tip.X, tip.Z).Length() > 0.1f, $"{frond.Name} tip {tip} does not lean outward");
        }
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, GrowthStage.Young, 5)]
    [InlineData(CropTypeId.Emberroot, GrowthStage.Ripe, 7)]
    public void Fronds_are_spread_evenly_round_the_root(CropTypeId crop, GrowthStage stage, int count)
    {
        var yaws = PartsOf(crop, stage)
            .Where(part => part.Name.StartsWith("Frond", StringComparison.Ordinal))
            .OrderBy(part => part.Name, StringComparer.Ordinal)
            .Select(part => part.YawDegrees)
            .ToList();

        Assert.Equal(count, yaws.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i * 360f / count, yaws[i], 1e-3f);
        }
    }

    [Theory]
    [InlineData(CropTypeId.Dusktuber, GrowthStage.Young)]
    [InlineData(CropTypeId.Dusktuber, GrowthStage.Ripe)]
    public void Each_curl_sits_on_its_leafs_outer_tip_and_the_tip_is_lifted(CropTypeId crop, GrowthStage stage)
    {
        var parts = PartsOf(crop, stage);
        var leaves = parts.Where(part => part.Name.StartsWith("Leaf", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(leaves);
        foreach (var leaf in leaves)
        {
            var curl = parts.Single(part => part.Name == "Curl" + leaf.Name["Leaf".Length..]);
            var outer = At(leaf, new Vector3(leaf.SizeX / 2f, 0f, 0f));
            var inner = At(leaf, new Vector3(-leaf.SizeX / 2f, 0f, 0f));
            AssertNear(outer, Centre(curl), $"{curl.Name}");
            Assert.True(outer.Y > inner.Y, $"{leaf.Name} outer end {outer} is not above its inner end {inner}");
            Assert.True(
                new Vector2(outer.X, outer.Z).Length() > new Vector2(inner.X, inner.Z).Length(),
                $"{leaf.Name} outer end {outer} is not farther out than its inner end {inner}");
        }
    }

    [Fact]
    public void Ripe_dusktuber_leaves_rise_from_the_bulb_top()
    {
        foreach (var leaf in PartsOf(CropTypeId.Dusktuber, GrowthStage.Ripe)
                     .Where(part => part.Name.StartsWith("Leaf", StringComparison.Ordinal)))
        {
            AssertNear(new Vector3(0f, 0.26f, 0f), At(leaf, new Vector3(-leaf.SizeX / 2f, 0f, 0f)), $"{leaf.Name} inner end");
        }
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    public void Seed_leaves_meet_at_the_stem_and_lift_into_a_v(CropTypeId crop)
    {
        var parts = PartsOf(crop, GrowthStage.Sprout);
        var stem = parts.Single(part => part.Name == "Stem");
        var stemTop = stem.Y + (stem.SizeY / 2f);

        foreach (var (name, side) in new[] { ("SeedLeafLeft", -1f), ("SeedLeafRight", 1f) })
        {
            var leaf = parts.Single(part => part.Name == name);
            var ends = new[] { At(leaf, new Vector3(-leaf.SizeX / 2f, 0f, 0f)), At(leaf, new Vector3(leaf.SizeX / 2f, 0f, 0f)) };
            var inner = ends.MinBy(end => MathF.Abs(end.X));
            var outer = ends.MaxBy(end => MathF.Abs(end.X));

            Assert.Equal(0f, inner.X, 1e-3f);
            Assert.True(inner.Y <= stemTop + Tolerance, $"{name} inner end {inner} floats above the stem top {stemTop}");
            Assert.True(MathF.Sign(outer.X) == side, $"{name} outer end {outer} is on the wrong side");
            Assert.True(outer.Y > inner.Y + 0.05f, $"{name} outer end {outer} does not lift above {inner}");
        }
    }

    // ---- #91: sunbell and starcorn ----

    [Fact]
    public void Young_sunbell_canes_rise_from_the_base_and_lean_outward()
    {
        var canes = PartsOf(CropTypeId.Sunbell, GrowthStage.Young)
            .Where(part => part.Name.StartsWith("Cane", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(3, canes.Count);
        foreach (var cane in canes)
        {
            Assert.Equal(CropPartShape.Cylinder, cane.Shape);
            AssertNear(Vector3.Zero, At(cane, new Vector3(0f, -cane.SizeY / 2f, 0f)), $"{cane.Name} foot");
            var tip = At(cane, new Vector3(0f, cane.SizeY / 2f, 0f));
            Assert.True(new Vector2(tip.X, tip.Z).Length() > 0.03f, $"{cane.Name} tip {tip} does not lean outward");
        }
    }

    [Fact]
    public void Ripe_sunbell_sepals_are_two_crossed_boxes_on_each_berry()
    {
        var parts = PartsOf(CropTypeId.Sunbell, GrowthStage.Ripe);

        for (var i = 0; i < 6; i++)
        {
            var top = parts.Single(part => part.Name == $"Berry{i}Top");
            var a = parts.Single(part => part.Name == $"Berry{i}SepalsA");
            var b = parts.Single(part => part.Name == $"Berry{i}SepalsB");

            foreach (var sepal in new[] { a, b })
            {
                Assert.Equal(CropPartShape.Box, sepal.Shape);
                Assert.Equal(top.X, sepal.X, 1e-3f);
                Assert.Equal(top.Z, sepal.Z, 1e-3f);
            }

            Assert.Equal(90f, b.YawDegrees - a.YawDegrees, Tolerance);
        }
    }

    [Theory]
    [InlineData(GrowthStage.Young)]
    [InlineData(GrowthStage.Ripe)]
    public void Starcorn_leaves_start_at_the_stalk_and_droop_at_the_outer_end(GrowthStage stage)
    {
        var leaves = PartsOf(CropTypeId.Starcorn, stage)
            .Where(part => part.Name.StartsWith("Leaf", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(leaves);
        foreach (var leaf in leaves)
        {
            var inner = At(leaf, new Vector3(-leaf.SizeX / 2f, 0f, 0f));
            var outer = At(leaf, new Vector3(leaf.SizeX / 2f, 0f, 0f));
            Assert.True(new Vector2(inner.X, inner.Z).Length() <= Tolerance, $"{leaf.Name} inner end {inner} is off the stalk");
            Assert.True(outer.Y < inner.Y - 0.05f, $"{leaf.Name} outer end {outer} does not droop below {inner}");
            var lowestEdge = At(leaf, new Vector3(leaf.SizeX / 2f, -leaf.SizeY / 2f, 0f)).Y;
            Assert.True(lowestEdge > 0f, $"{leaf.Name} outer edge dips to y {lowestEdge}, into the soil");
        }
    }

    [Fact]
    public void Ripe_starcorn_cob_leans_away_from_the_stalk_and_its_husks_stand_on_its_foot()
    {
        var parts = PartsOf(CropTypeId.Starcorn, GrowthStage.Ripe);
        var cob = parts.Single(part => part.Name == "Cob");
        var cobFoot = At(cob, new Vector3(0f, -cob.SizeY / 2f, 0f));
        var cobTop = At(cob, new Vector3(0f, cob.SizeY / 2f, 0f));

        Assert.True(cobTop.X > cobFoot.X + 0.05f, $"the cob top {cobTop} does not lean out past its foot {cobFoot}");
        foreach (var (name, side) in new[] { ("HuskLeft", -1f), ("HuskRight", 1f) })
        {
            var husk = parts.Single(part => part.Name == name);
            var foot = At(husk, new Vector3(0f, -husk.SizeY / 2f, 0f));
            var tip = At(husk, new Vector3(0f, husk.SizeY / 2f, 0f));
            Assert.Equal(cobFoot.Y, foot.Y, 1e-2f);
            Assert.True(MathF.Sign(foot.Z) == side, $"{name} foot {foot} is on the wrong side of the cob");
            Assert.True(MathF.Abs(tip.Z) > MathF.Abs(foot.Z), $"{name} tip {tip} does not peel back from the cob");
        }
    }

    [Fact]
    public void Ripe_starcorn_sparkles_are_diamonds_on_the_side_facing_out_from_the_stalk()
    {
        var parts = PartsOf(CropTypeId.Starcorn, GrowthStage.Ripe);
        var cob = parts.Single(part => part.Name == "Cob");
        var sparkles = parts.Where(part => part.Name.StartsWith("Sparkle", StringComparison.Ordinal)).ToList();

        Assert.Equal(8, sparkles.Count);
        Assert.All(sparkles, sparkle =>
        {
            Assert.Equal(45f, sparkle.PitchDegrees, Tolerance);
            Assert.Equal(45f, sparkle.RollDegrees, Tolerance);
            Assert.True(sparkle.X > cob.X, $"{sparkle.Name} at x {sparkle.X} faces the stalk, not out from it");
        });
        var heights = sparkles.Select(sparkle => sparkle.Y).ToList();
        Assert.True(heights.Max() - heights.Min() > cob.SizeY / 2f, "the sparkles are not spread over the cob's length");
    }

    [Fact]
    public void Ripe_starcorn_tassels_rise_from_the_stalk_top()
    {
        var parts = PartsOf(CropTypeId.Starcorn, GrowthStage.Ripe);
        var stalk = parts.Single(part => part.Name == "Stalk");
        var stalkTop = stalk.Y + (stalk.SizeY / 2f);
        var tassels = parts.Where(part => part.Name.StartsWith("Tassel", StringComparison.Ordinal)).ToList();

        Assert.Equal(3, tassels.Count);
        foreach (var tassel in tassels)
        {
            var foot = At(tassel, new Vector3(0f, -tassel.SizeY / 2f, 0f));
            Assert.True(new Vector2(foot.X, foot.Z).Length() <= Tolerance, $"{tassel.Name} foot {foot} is off the stalk");
            Assert.InRange(foot.Y, stalkTop - 0.03f, stalkTop);
            Assert.Equal(1.0f, tassel.Y, 1e-2f);
        }
    }

    [Fact]
    public void Ripe_dusktuber_speckles_sit_on_the_bulbs_upper_half()
    {
        var parts = PartsOf(CropTypeId.Dusktuber, GrowthStage.Ripe);
        var bulb = parts.Single(part => part.Name == "Bulb");
        var rx = bulb.SizeX / 2f;
        var ry = bulb.SizeY / 2f;

        foreach (var speckle in parts.Where(part => part.Name.StartsWith("Speckle", StringComparison.Ordinal)))
        {
            var dx = new Vector2(speckle.X, speckle.Z).Length() / rx;
            var dy = (speckle.Y - bulb.Y) / ry;
            var onSurface = MathF.Sqrt((dx * dx) + (dy * dy));
            Assert.True(speckle.Y > bulb.Y, $"{speckle.Name} is below the bulb's middle");
            Assert.InRange(onSurface, 0.85f, 1.15f);
        }
    }
}
