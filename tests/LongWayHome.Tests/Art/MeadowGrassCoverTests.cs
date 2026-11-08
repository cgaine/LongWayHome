using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The meadow's wild grass cover (ART_DIRECTION §7.2): 1 m patches of <see cref="GrassTileArt.MeadowParts"/> scattered
/// over the meadow ground north of the 72 x 36 grid (#150) by <see cref="MeadowLayout.GrassCover"/>, off the grid, the path, the pond and
/// the buildings, with no grid showing.
/// </summary>
public class MeadowGrassCoverTests
{
    private static readonly IReadOnlyList<MeadowProp> Cover = MeadowLayout.GrassCover();

    private static float Reach => MeadowLayout.MaxGrassCoverReach;

    [Fact]
    public void The_cover_fills_most_of_the_open_meadow_with_every_layout()
    {
        // #150: the open meadow is the strip north of the 72 x 36 grid (about 72 x 8.5 m); the path, the pond and the
        // buildings take out some of it.
        Assert.InRange(Cover.Count, 250, 650);
        Assert.Contains(Cover, patch => patch.X < -30f);
        Assert.Contains(Cover, patch => patch.X > 30f);
        var kinds = Cover.Select(patch => patch.Kind).Distinct().ToList();
        Assert.Equal(GrassTileArt.Variants, kinds.Count);
        Assert.All(kinds, kind => Assert.Matches("^Grass[0-7]$", kind));
    }

    [Fact]
    public void The_same_cover_every_call() =>
        Assert.Equal(Cover, MeadowLayout.GrassCover());

    [Fact]
    public void Patches_are_jittered_turned_and_scaled_so_no_grid_shows()
    {
        Assert.Contains(Cover, patch => MathF.Abs(patch.X - MathF.Round(patch.X)) > 0.2f);
        Assert.True(Cover.Select(patch => MathF.Round(patch.YawDegrees / 10f)).Distinct().Count() > 30, "the patches don't turn freely");
        Assert.All(Cover, patch => Assert.InRange(patch.Scale, 0.9f, 1.25f));
        Assert.All(Cover, patch => Assert.InRange(patch.YawDegrees, 0f, 360f));
    }

    [Fact]
    public void No_patch_reaches_the_grid_the_path_the_pond_or_the_ground_edge()
    {
        Assert.NotEmpty(Cover);
        foreach (var patch in Cover)
        {
            var (x, z) = (patch.X, patch.Z);
            // #150: the grid, x -36..36, z -4.5..31.5, is farm ground.
            var gridDx = MathF.Max(0f, MathF.Abs(x) - 36f);
            var gridDz = MathF.Max(0f, MathF.Max(-4.5f - z, z - 31.5f));
            Assert.True(MathF.Sqrt(gridDx * gridDx + gridDz * gridDz) >= Reach - 1e-4f, $"a patch at ({x:0.##}, {z:0.##}) reaches the grid");
            var pathDx = MathF.Max(0f, MathF.Abs(x) - 0.6f);
            var pathDz = MathF.Max(0f, MathF.Max(-7f - z, z + 4.5f));
            Assert.True(MathF.Sqrt(pathDx * pathDx + pathDz * pathDz) >= Reach - 1e-4f, $"a patch at ({x:0.##}, {z:0.##}) reaches the path");
            Assert.True(MathF.Sqrt((x - 10f) * (x - 10f) + (z + 9f) * (z + 9f)) >= 2.5f + Reach - 1e-4f, $"a patch at ({x:0.##}, {z:0.##}) reaches the pond");
            Assert.True(x - Reach >= -36f - 1e-4f && x + Reach <= 36.5f + 1e-4f && z - Reach >= -13f - 1e-4f, $"a patch at ({x:0.##}, {z:0.##}) hangs off the ground");
            Assert.False(x is >= -3f and <= 3f && z is >= -11f and <= -7f, $"a patch at ({x:0.##}, {z:0.##}) is in the house");
            Assert.False(x is >= -11f and <= -7f && z is >= -11f and <= -7f, $"a patch at ({x:0.##}, {z:0.##}) is in the moved shop stall");
        }
    }

    [Theory]
    [InlineData(Season.Spring)]
    [InlineData(Season.Summer)]
    [InlineData(Season.Fall)]
    [InlineData(Season.Winter)]
    public void Meadow_grass_is_wilder_than_the_plots_but_stays_on_its_patch(Season season)
    {
        var grass = SeasonGrass.For(season);
        for (var variant = 0; variant < GrassTileArt.Variants; variant++)
        {
            var wild = GrassTileArt.MeadowParts(variant, grass);
            var tile = GrassTileArt.Parts(variant, grass);
            var wildExtent = PartBoxes.Extents(wild.Select(PartBoxes.Corners));
            var tileExtent = PartBoxes.Extents(tile.Where(part => part.Name.StartsWith("Tuft", StringComparison.Ordinal)).Select(PartBoxes.Corners));

            Assert.True(wild.Count(part => part.Name.StartsWith("Tuft", StringComparison.Ordinal)) > tile.Count(part => part.Name.StartsWith("Tuft", StringComparison.Ordinal)),
                $"layout {variant} in {season} isn't denser in the meadow");
            Assert.True(PartBoxes.Extents(wild.Where(part => part.Name.StartsWith("Tuft", StringComparison.Ordinal)).Select(PartBoxes.Corners)).Max.Y > tileExtent.Max.Y,
                $"layout {variant} in {season} isn't taller in the meadow");
            Assert.True(wildExtent.Max.Y <= 0.45f, $"layout {variant} in {season} stands {wildExtent.Max.Y:0.##} m tall");
            Assert.True(MathF.Max(MathF.Abs(wildExtent.Min.X), wildExtent.Max.X) <= GrassTileArt.MaxReach
                && MathF.Max(MathF.Abs(wildExtent.Min.Z), wildExtent.Max.Z) <= GrassTileArt.MaxReach, $"layout {variant} in {season} leaves its patch");
            Assert.Same(wild, GrassTileArt.MeadowParts(variant, grass));
        }
    }
}
