using LongWayHome.Application.Art;
using LongWayHome.Domain.Farming;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Art;

/// <summary>
/// #150 (REQ-14): <see cref="GrassBatches"/> plans the batched grass. Every grass Tile is in exactly one batch, keyed
/// by its <see cref="GrassTileArt.PickFor"/> layout and with its quarter turns; a tilled Tile is in none.
/// </summary>
public class GrassBatchesTests
{
    private static readonly GridPosition Tilled = new(3, 4);

    private static List<(GridPosition Position, int QuarterTurns)> Flatten(
        IReadOnlyDictionary<int, IReadOnlyList<(GridPosition Position, int QuarterTurns)>> batches) =>
        batches.Values.SelectMany(batch => batch).ToList();

    [Fact]
    public void AC9_batches_cover_every_grass_tile_once()
    {
        var plot = new FarmPlot();

        var entries = Flatten(GrassBatches.ByLayout(plot));

        Assert.Equal(FarmPlot.Width * FarmPlot.Height, entries.Count);
        Assert.Equal(
            plot.Tiles.Select(tile => (tile.Position.X, tile.Position.Y)).Order().ToList(),
            entries.Select(entry => (entry.Position.X, entry.Position.Y)).Order().ToList());
    }

    [Fact]
    public void AC9_each_tile_is_batched_under_its_grass_layout_with_its_turns()
    {
        var batches = GrassBatches.ByLayout(new FarmPlot());
        Assert.NotEmpty(batches);

        foreach (var (variant, batch) in batches)
        {
            Assert.InRange(variant, 0, GrassTileArt.Variants - 1);
            Assert.NotEmpty(batch);
            foreach (var (position, quarterTurns) in batch)
            {
                Assert.Equal(GrassTileArt.PickFor(position), (variant, quarterTurns));
            }
        }
    }

    [Fact]
    public void AC9_a_tilled_tile_leaves_the_batches()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Tilled, null));

        var entries = Flatten(GrassBatches.ByLayout(game.FarmPlot));

        Assert.DoesNotContain(entries, entry => entry.Position == Tilled);
        Assert.Equal(game.FarmPlot.Tiles.Count - 1, entries.Count);
    }

    [Fact]
    public void AC9_grass_positions_are_every_grass_tile_in_row_major_order()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Tilled, null));

        var positions = GrassBatches.GrassPositions(game.FarmPlot);

        Assert.Equal(
            game.FarmPlot.Tiles.Where(tile => tile.State == TileState.Grass).Select(tile => tile.Position).ToList(),
            positions);
        Assert.DoesNotContain(Tilled, positions);
    }

    [Fact]
    public void BR_T8_tilled_tile_never_returns_to_batches()
    {
        var session = TestGames.NewSession();
        Assert.True(session.Till(Tilled).IsOk);

        // A Night (when the batches are rebuilt for the new Day's Season) leaves the Tile tilled and out of them.
        Assert.True(session.Sleep().IsOk);

        Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Tilled].State);
        Assert.DoesNotContain(Flatten(GrassBatches.ByLayout(session.Game.FarmPlot)), entry => entry.Position == Tilled);
        Assert.DoesNotContain(Tilled, GrassBatches.GrassPositions(session.Game.FarmPlot));
    }
}
