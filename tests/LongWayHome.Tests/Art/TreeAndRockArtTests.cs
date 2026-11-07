using System.Numerics;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The Twinbough's part tables at every stage and season (ART_DIRECTION §5.1) and the rocks the pickaxe breaks
/// (§7.6). Every check works on the parts' rotated corners (<see cref="PartBoxes"/>).
/// </summary>
public class TreeAndRockArtTests
{
    public static TheoryData<TreeStage, Season> EveryTree()
    {
        var data = new TheoryData<TreeStage, Season>();
        foreach (var stage in Enum.GetValues<TreeStage>())
        {
            foreach (var season in Enum.GetValues<Season>())
            {
                data.Add(stage, season);
            }
        }

        return data;
    }

    public static TheoryData<RockKind> EveryRock() => new(Enum.GetValues<RockKind>());

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    private static void AssertWellFormed(IReadOnlyList<CropPart> parts)
    {
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        Assert.Equal(parts.Count, parts.Select(part => part.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(parts, part => Assert.True(part.SizeX > 0f && part.SizeY > 0f && part.SizeZ > 0f, $"{part.Name} has a zero size"));
    }

    // ---- the Twinbough ----

    [Theory]
    [MemberData(nameof(EveryTree))]
    public void Every_tree_uses_palette_roles_with_unique_names(TreeStage stage, Season season) =>
        AssertWellFormed(TreeArt.Twinbough(stage, season));

    [Theory]
    [MemberData(nameof(EveryTree))]
    public void Only_sap_glows_and_it_glows_amber(TreeStage stage, Season season)
    {
        var parts = TreeArt.Twinbough(stage, season);

        Assert.Contains(parts, part => part.Glow > 0f);
        Assert.All(parts.Where(part => part.Glow > 0f), part => Assert.Equal("lantern", part.Role));
    }

    [Theory]
    [MemberData(nameof(EveryTree))]
    public void Nothing_sinks_more_than_a_few_centimetres_into_the_ground(TreeStage stage, Season season)
    {
        foreach (var part in TreeArt.Twinbough(stage, season))
        {
            var (min, _) = Extent([part]);
            Assert.True(min.Y >= -0.08f, $"{part.Name} reaches y {min.Y:0.###}");
        }
    }

    [Fact]
    public void A_full_tree_is_about_5_m_tall_with_a_canopy_about_3_6_m_across()
    {
        var parts = TreeArt.Twinbough(TreeStage.Full, Season.Summer);
        var (min, max) = Extent(parts);

        Assert.InRange(max.Y, 4.5f, 5.3f);
        Assert.InRange(max.X - min.X, 3.2f, 4.2f);
        Assert.InRange(max.Z - min.Z, 3.0f, 4.2f);
    }

    [Fact]
    public void A_sapling_and_a_giant_are_the_full_tree_scaled()
    {
        var full = Extent(TreeArt.Twinbough(TreeStage.Full, Season.Summer)).Max.Y;

        Assert.Equal(full * TreeArt.SaplingScale, Extent(TreeArt.Twinbough(TreeStage.Sapling, Season.Summer)).Max.Y, 0.01f);
        Assert.Equal(full * TreeArt.GiantScale, Extent(TreeArt.Twinbough(TreeStage.Giant, Season.Summer)).Max.Y, 0.01f);
    }

    [Fact]
    public void The_trunk_splits_into_two_limbs()
    {
        var parts = TreeArt.Twinbough(TreeStage.Full, Season.Summer);

        var west = parts.Single(part => part.Name == "LimbWest");
        var east = parts.Single(part => part.Name == "LimbEast");
        Assert.True(west.X < 0f && east.X > 0f, "the limbs don't lean apart");
    }

    [Theory]
    [InlineData(Season.Spring, "canopy-spring")]
    [InlineData(Season.Summer, "foliage")]
    [InlineData(Season.Fall, "canopy-fall")]
    public void The_canopy_takes_its_season_colour(Season season, string role)
    {
        var canopy = TreeArt.Twinbough(TreeStage.Full, season).Where(part => Named(part, "Canopy")).ToList();

        Assert.NotEmpty(canopy);
        Assert.All(canopy, part => Assert.Equal(role, part.Role));
    }

    [Fact]
    public void Spring_has_blossom_fall_has_fallen_leaves_and_winter_is_bare_with_snow()
    {
        Assert.Contains(TreeArt.Twinbough(TreeStage.Full, Season.Spring), part => Named(part, "Blossom") && part.Role == "blossom");
        Assert.DoesNotContain(TreeArt.Twinbough(TreeStage.Full, Season.Summer), part => Named(part, "Blossom") || Named(part, "FallenLeaf"));
        Assert.Contains(TreeArt.Twinbough(TreeStage.Full, Season.Fall), part => Named(part, "FallenLeaf"));
        Assert.Equal("underside-fall", TreeArt.Twinbough(TreeStage.Full, Season.Fall).Single(part => part.Name == "Underside").Role);

        var winter = TreeArt.Twinbough(TreeStage.Full, Season.Winter);
        Assert.DoesNotContain(winter, part => Named(part, "Canopy") || part.Name == "Underside");
        Assert.Contains(winter, part => Named(part, "Branch"));
        Assert.Contains(winter, part => Named(part, "Snow") && part.Role == "snow");
    }

    [Theory]
    [InlineData(Season.Spring)]
    [InlineData(Season.Winter)]
    public void The_stump_has_glowing_rings_a_shoot_and_chips_in_every_season(Season season)
    {
        var stump = TreeArt.Twinbough(TreeStage.Stump, season);

        Assert.Equal("wood-cut", stump.Single(part => part.Name == "StumpFace").Role);
        Assert.True(stump.Count(part => Named(part, "Ring") && part.Glow > 0f) >= 2, "the stump has fewer than two glowing rings");
        Assert.Contains(stump, part => Named(part, "Shoot"));
        Assert.Contains(stump, part => Named(part, "Chip"));
        Assert.True(Extent(stump).Max.Y < 1.2f, "the stump is too tall");
        Assert.Equal(TreeArt.Twinbough(TreeStage.Stump, Season.Summer), stump);
    }

    // ---- the rocks ----

    [Theory]
    [MemberData(nameof(EveryRock))]
    public void Every_rock_uses_palette_roles_with_unique_names_and_nothing_glows(RockKind kind)
    {
        var parts = RockArt.For(kind);

        AssertWellFormed(parts);
        Assert.All(parts, part => Assert.Equal(0f, part.Glow));
        Assert.Contains(parts, part => Named(part, "Body") && part.Role == "stone");
    }

    [Theory]
    [MemberData(nameof(EveryRock))]
    public void Every_rock_sits_on_the_ground_sunk_no_more_than_a_little(RockKind kind)
    {
        var (min, max) = Extent(RockArt.For(kind));

        // Sunk by no more than half its height above ground (measured on the lumps' rotated boxes, which overstate it).
        Assert.True(max.Y > 0.4f, $"{kind} is only {max.Y:0.##} m tall");
        Assert.InRange(min.Y, -0.5f * max.Y, 0.01f);
    }

    [Fact]
    public void A_rock_is_about_0_9_m_across_and_a_boulder_about_twice_that()
    {
        var (rockMin, rockMax) = Extent(RockArt.For(RockKind.Rock));
        var (boulderMin, boulderMax) = Extent(RockArt.For(RockKind.Boulder));

        Assert.InRange(rockMax.X - rockMin.X, 0.8f, 1.4f);
        Assert.InRange((boulderMax.X - boulderMin.X) / (rockMax.X - rockMin.X), 1.7f, 2.1f);
        Assert.Contains(RockArt.For(RockKind.Boulder), part => Named(part, "Crack"));
    }

    [Fact]
    public void Ore_rocks_show_their_ore()
    {
        Assert.True(RockArt.For(RockKind.RoskOre).Count(part => part.Role == "rosk") >= 6, "Rosk ore has too few veins");
        Assert.True(RockArt.For(RockKind.GrothOre).Count(part => part.Role == "groth") >= 3, "Groth ore has too few crystals");
        Assert.DoesNotContain(RockArt.For(RockKind.Rock), part => part.Role is "rosk" or "groth");
    }
}
