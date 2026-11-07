using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The grass by Season (#79, ART_DIRECTION §7.2): <see cref="SeasonGrass.For"/> gives each Season's palette roles,
/// tuft height and shade-patch visibility; every Season's ground still stands clear of tilled and watered soil; and the
/// Season, so the look, changes only across the Night after day 28 (BR-G1).
/// </summary>
public class SeasonGrassTests
{
    public static TheoryData<Season, string, string, string, string, float, bool> Table => new()
    {
        { Season.Spring, "grass-mown", "grass-spring-mown", "grass-mown", "grass-shade", 1.0f, true },
        { Season.Summer, "grass-lit", "grass-mown", "grass-lit", "grass-shade", 1.0f, true },
        { Season.Fall, "grass-fall", "grass-fall-mown", "grass-fall", "grass-shade", 1.0f, true },
        { Season.Winter, "snow", "snow", "grass-shade", "grass-shade", 0.5f, false },
    };

    public static TheoryData<Season> Seasons => new() { Season.Spring, Season.Summer, Season.Fall, Season.Winter };

    // ---- AC1: the lookup table ----

    [Theory]
    [MemberData(nameof(Table))]
    public void AC1_season_grass_matches_the_table(
        Season season, string ground, string margin, string tufts, string shadePatches, float tuftHeightScale, bool showShadePatches)
    {
        var grass = SeasonGrass.For(season);

        Assert.Equal(ground, grass.Ground);
        Assert.Equal(margin, grass.Margin);
        Assert.Equal(tufts, grass.Tufts);
        Assert.Equal(shadePatches, grass.ShadePatches);
        Assert.Equal(tuftHeightScale, grass.TuftHeightScale, 1e-6f);
        Assert.Equal(showShadePatches, grass.ShowShadePatches);
    }

    [Fact]
    public void AC1_every_role_exists_in_the_palette()
    {
        foreach (var season in Enum.GetValues<Season>())
        {
            var grass = SeasonGrass.For(season);
            foreach (var role in new[] { grass.Ground, grass.Margin, grass.Tufts, grass.ShadePatches })
            {
                var ex = Record.Exception(() => ArtPalette.Hex(role));
                Assert.True(ex is null, $"{season}: role '{role}' is not in ArtPalette ({ex?.Message})");
            }
        }
    }

    // ---- AC2: readability against soil ----

    [Theory]
    [MemberData(nameof(Seasons))]
    public void AC2_season_ground_contrasts_at_least_1_8_with_tilled_soil(Season season)
    {
        var ground = SeasonGrass.For(season).Ground;

        var ratio = ArtContrast.Ratio(ArtPalette.Hex(ground), ArtPalette.Hex("soil-tilled"));

        Assert.True(ratio >= 1.8, $"{season} ground {ground} vs soil-tilled contrast is {ratio:F2}, below 1.8");
    }

    [Theory]
    [MemberData(nameof(Seasons))]
    public void AC2_season_ground_contrasts_at_least_1_8_with_watered_soil(Season season)
    {
        var ground = SeasonGrass.For(season).Ground;

        var ratio = ArtContrast.Ratio(ArtPalette.Hex(ground), ArtPalette.Hex("soil-watered"));

        Assert.True(ratio >= 1.8, $"{season} ground {ground} vs soil-watered contrast is {ratio:F2}, below 1.8");
    }

    [Fact]
    public void AC2_ember_contrasts_at_least_2_with_the_winter_snow_ground()
    {
        // The winter ground is the snow the target marker's ember brackets must read against.
        var ground = SeasonGrass.For(Season.Winter).Ground;

        var ratio = ArtContrast.Ratio(ArtPalette.Hex("ember"), ArtPalette.Hex(ground));

        Assert.True(ratio >= 2.0, $"ember vs winter ground {ground} contrast is {ratio:F2}, below 2.0");
    }

    // ---- BR-G1: the Season changes only across the Night after day 28 ----

    [Fact]
    public void BR_G1_season_changes_only_across_the_night_after_day_28()
    {
        GameState game = TestGames.OnDate(new GameDate(1, Season.Spring, 28), null);
        var session = new GameSession(game, TestGames.Shipped);

        // A whole Day of actions on Spring 28 leaves the Season, so the grass, alone.
        session.AdvanceClock(TestGames.Shipped.Tuning.DayLengthRealSeconds / 2.0);
        session.Till(new GridPosition(2, 2));
        session.Water(new GridPosition(2, 2));
        Assert.Equal(Season.Spring, session.Game.Date.Season);

        Assert.True(session.Sleep().IsOk);

        Assert.Equal(new GameDate(1, Season.Summer, 1), session.Game.Date);
        Assert.Equal(SeasonGrass.For(Season.Summer), SeasonGrass.For(session.Game.Date.Season));
        Assert.Equal("grass-lit", SeasonGrass.For(session.Game.Date.Season).Ground);
    }
}
