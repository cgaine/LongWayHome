using System.Numerics;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The grass Tile detail (ART_DIRECTION §7.2): every layout in every Season stays on its slab, uses the Season's
/// grass, and the plot's Tiles spread across the layouts and turns so neighbours rarely match. Parts are in slab-top
/// metres: origin at the centre of the slab's top.
/// </summary>
public class GrassTileArtTests
{
    public static TheoryData<int, Season> EveryLayout()
    {
        var data = new TheoryData<int, Season>();
        for (var variant = 0; variant < GrassTileArt.Variants; variant++)
        {
            foreach (var season in Enum.GetValues<Season>())
            {
                data.Add(variant, season);
            }
        }

        return data;
    }

    private static IReadOnlyList<CropPart> Parts(int variant, Season season) => GrassTileArt.Parts(variant, SeasonGrass.For(season));

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    private static IEnumerable<GridPosition> Plot() =>
        from x in Enumerable.Range(0, FarmPlot.Width) from y in Enumerable.Range(0, FarmPlot.Height) select new GridPosition(x, y);

    [Theory]
    [MemberData(nameof(EveryLayout))]
    public void Every_layout_uses_palette_roles_with_unique_names_and_nothing_glows(int variant, Season season)
    {
        var parts = Parts(variant, season);
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        Assert.Equal(parts.Count, parts.Select(part => part.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(parts, part => Assert.Equal(0f, part.Glow));
        Assert.All(parts, part => Assert.True(part.SizeX > 0f && part.SizeY > 0f && part.SizeZ > 0f, $"{part.Name} has a zero size"));
    }

    [Theory]
    [MemberData(nameof(EveryLayout))]
    public void Every_layout_stays_on_its_slab_and_low(int variant, Season season)
    {
        var (min, max) = Extent(Parts(variant, season));

        Assert.True(min.X >= -GrassTileArt.MaxReach && max.X <= GrassTileArt.MaxReach && min.Z >= -GrassTileArt.MaxReach && max.Z <= GrassTileArt.MaxReach,
            $"layout {variant} in {season} spans x {min.X:0.###}..{max.X:0.###}, z {min.Z:0.###}..{max.Z:0.###}");
        Assert.True(min.Y >= -0.04f, $"layout {variant} in {season} sinks to y {min.Y:0.###}, through the slab");
        Assert.True(max.Y <= 0.3f, $"layout {variant} in {season} stands {max.Y:0.###} m tall");
    }

    [Theory]
    [MemberData(nameof(EveryLayout))]
    public void Every_layout_is_detailed_with_patches_and_many_blades(int variant, Season season)
    {
        var parts = Parts(variant, season);
        var blades = parts.Count(part => Named(part, "Tuft"));

        Assert.True(parts.Count(part => Named(part, "Patch") || Named(part, "Drift")) >= 4, "too few turf patches");
        Assert.True(blades >= (season == Season.Winter ? 20 : 33), $"only {blades} blades");
        Assert.InRange(parts.Count, 30, 120);
    }

    [Theory]
    [InlineData(Season.Spring)]
    [InlineData(Season.Summer)]
    [InlineData(Season.Fall)]
    public void Patches_are_lighter_and_darker_than_the_ground_and_blades_take_the_seasons_tuft_role(Season season)
    {
        var grass = SeasonGrass.For(season);
        var parts = Parts(0, season);
        var patchRoles = parts.Where(part => Named(part, "Patch")).Select(part => part.Role).Distinct().ToList();

        Assert.Equal(2, patchRoles.Count);
        Assert.DoesNotContain(grass.Ground, patchRoles);
        Assert.Contains(parts, part => Named(part, "Tuft") && part.Role == grass.Tufts);
    }

    [Fact]
    public void Winter_is_snow_drifts_with_short_green_tips_and_no_flowers()
    {
        for (var variant = 0; variant < GrassTileArt.Variants; variant++)
        {
            var winter = Parts(variant, Season.Winter);
            var summer = Parts(variant, Season.Summer);

            Assert.Contains(winter, part => Named(part, "Drift"));
            Assert.DoesNotContain(winter, part => Named(part, "Patch") || Named(part, "Flower") || Named(part, "Clover") || Named(part, "Seed"));
            Assert.All(winter.Where(part => Named(part, "Tuft")), part => Assert.Contains(part.Role, new[] { "grass-shade", "foliage" }));
            Assert.True(Extent(winter.Where(part => Named(part, "Tuft"))).Max.Y < Extent(summer.Where(part => Named(part, "Tuft"))).Max.Y,
                "winter tips aren't shorter");
        }
    }

    [Fact]
    public void The_layouts_differ_and_bring_clover_flowers_pebbles_and_seed_heads()
    {
        var summer = Enumerable.Range(0, GrassTileArt.Variants).Select(variant => Parts(variant, Season.Summer)).ToList();

        Assert.Equal(GrassTileArt.Variants, summer.Select(parts => string.Join("|", parts.Select(part => $"{part.Name}@{part.X:0.000},{part.Z:0.000}"))).Distinct().Count());
        Assert.Contains(summer, parts => parts.Any(part => Named(part, "Clover")));
        Assert.Contains(summer, parts => parts.Any(part => Named(part, "Flower")));
        Assert.Contains(summer, parts => parts.Any(part => Named(part, "Pebble")));
        Assert.Contains(summer, parts => parts.Any(part => Named(part, "Seed")));
        Assert.All(Enumerable.Range(0, GrassTileArt.Variants), variant =>
            Assert.Contains(Parts(variant, Season.Fall), part => Named(part, "Leaf")));
    }

    [Fact]
    public void The_same_arguments_give_the_same_parts()
    {
        Assert.Same(Parts(3, Season.Fall), Parts(3, Season.Fall));
        Assert.Equal(GrassTileArt.PickFor(new GridPosition(4, 7)), GrassTileArt.PickFor(new GridPosition(4, 7)));
    }

    [Fact]
    public void The_plot_uses_every_layout_and_turn_and_few_neighbours_match()
    {
        var picks = Plot().ToDictionary(position => position, GrassTileArt.PickFor);

        Assert.Equal(GrassTileArt.Variants, picks.Values.Select(pick => pick.Variant).Distinct().Count());
        Assert.Equal(4, picks.Values.Select(pick => pick.QuarterTurns).Distinct().Count());
        Assert.All(picks.Values, pick => Assert.InRange(pick.QuarterTurns, 0, 3));

        // Matching east and south neighbours (same layout and turn) should be rare: well under one pair in eight.
        var pairs = 0;
        var same = 0;
        foreach (var (position, pick) in picks)
        {
            foreach (var (x, y) in new[] { (position.X + 1, position.Y), (position.X, position.Y + 1) })
            {
                // A GridPosition must lie on the plot, so stop at its east and south edges.
                if (x >= FarmPlot.Width || y >= FarmPlot.Height) continue;
                var other = picks[new GridPosition(x, y)];
                pairs++;
                if (other == pick) same++;
            }
        }

        Assert.True(same * 8 < pairs, $"{same} of {pairs} neighbouring pairs match");
    }

    [Fact]
    public void An_undefined_layout_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GrassTileArt.Parts(-1, SeasonGrass.For(Season.Summer)));
        Assert.Throws<ArgumentOutOfRangeException>(() => GrassTileArt.Parts(GrassTileArt.Variants, SeasonGrass.For(Season.Summer)));
    }
}
