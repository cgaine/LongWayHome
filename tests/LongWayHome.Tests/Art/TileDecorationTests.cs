using LongWayHome.Application.Art;
using LongWayHome.Domain.Farming;

namespace LongWayHome.Tests.Art;

/// <summary>
/// AC9 of #78: a grass Tile's tufts and native plant come from a hash of its grid position, so the same Tile always
/// looks the same, across saves and loads too.
/// </summary>
public class TileDecorationTests
{
    private static IEnumerable<GridPosition> AllPositions() =>
        from y in Enumerable.Range(0, FarmPlot.Height)
        from x in Enumerable.Range(0, FarmPlot.Width)
        select new GridPosition(x, y);

    [Fact]
    public void AC9_hash_uses_the_spec_formula()
    {
        Assert.Equal((3 * 73856093) ^ (4 * 19349663), TileDecoration.Hash(new GridPosition(3, 4)));
        Assert.Equal(0, TileDecoration.Hash(new GridPosition(0, 0)));
        Assert.Equal(unchecked((11 * 73856093) ^ (8 * 19349663)), TileDecoration.Hash(new GridPosition(11, 8)));
    }

    [Fact]
    public void AC9_same_position_gives_same_decoration()
    {
        foreach (var position in AllPositions())
        {
            var first = TileDecoration.For(position);
            var second = TileDecoration.For(position);

            Assert.Equal(first.NativePlant, second.NativePlant);
            Assert.Equal(first.TuftOffsets, second.TuftOffsets);
        }
    }

    [Fact]
    public void AC9_tuft_offsets_are_three_within_0_35_of_centre()
    {
        foreach (var position in AllPositions())
        {
            var offsets = TileDecoration.For(position).TuftOffsets;

            Assert.Equal(3, offsets.Count);
            Assert.All(offsets, o =>
            {
                Assert.InRange(o.X, -0.35f, 0.35f);
                Assert.InRange(o.Z, -0.35f, 0.35f);
            });
        }
    }

    [Fact]
    public void AC9_about_one_grass_tile_in_six_has_a_native_plant()
    {
        var kinds = AllPositions().Select(p => TileDecoration.For(p).NativePlant).ToList();

        var withPlant = kinds.Count(k => k != NativePlantKind.None);
        Assert.InRange(withPlant, 10, 30);
        Assert.Contains(NativePlantKind.Frond, kinds);
        Assert.Contains(NativePlantKind.Puffball, kinds);
    }

    [Fact]
    public void AC9_tuft_offsets_differ_between_tiles()
    {
        var distinct = AllPositions()
            .Select(p => string.Join(";", TileDecoration.For(p).TuftOffsets.Select(o => $"{o.X:R},{o.Z:R}")))
            .Distinct()
            .Count();

        Assert.True(distinct > 1, "every tile has the same tuft offsets");
    }
}
