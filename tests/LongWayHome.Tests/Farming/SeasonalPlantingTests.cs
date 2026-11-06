using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Farming;

/// <summary>
/// Seeds plant only in their Crop type's Season (BR-T3, BR-C1); Plantings keep growing across a season change
/// (BR-T5); sunbell and starcorn are the summer crops (REQ-42) (#38).
/// </summary>
public class SeasonalPlantingTests
{
    private static readonly GridPosition Spot = new(3, 2);
    private static readonly GridPosition Grass = new(0, 0);
    private static readonly GridPosition Occupied = new(4, 2);

    private static GameDate SpringDay => new(1, Season.Spring, 1);
    private static GameDate SummerDay => new(1, Season.Summer, 1);
    private static GameDate FallDay => new(1, Season.Fall, 1);
    private static GameDate WinterDay => new(1, Season.Winter, 1);

    /// <summary>The canonical Crop type row, independent of the shipped data files.</summary>
    private static CropType Crop(CropTypeId id) => TestGames.CanonicalCrops[id];

    /// <summary>An inventory holding 3 seeds of every Crop type.</summary>
    private static Inventory EverySeed()
    {
        var inventory = new Inventory();
        foreach (var id in Enum.GetValues<CropTypeId>())
        {
            inventory.Add(ItemKind.Seed, id, 3);
        }

        return inventory;
    }

    private static Inventory Seeds(CropTypeId id, int quantity)
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, id, quantity);
        return inventory;
    }

    /// <summary>A Game on the date with every seed, one empty tilled Tile at Spot.</summary>
    private static GameState TilledOn(GameDate date, Inventory? inventory = null) =>
        TestGames.OnDate(date, inventory ?? EverySeed(), TestGames.TilledWith(Spot, null));

    /// <summary>A session (never-rain Nights) on the date with the inventory and an empty tilled Tile at Spot.</summary>
    private static GameSession SessionOn(GameDate date, Inventory inventory) =>
        new(TilledOn(date, inventory), TestGames.Shipped);

    // ---- AC3 / BR-C1 / BR-T3: moonmelon in spring ----

    [Fact]
    public void AC3_in_spring_planting_held_moonmelon_on_empty_tilled_soil_is_OutOfSeason_and_changes_nothing()
    {
        var game = TilledOn(SpringDay, Seeds(CropTypeId.Moonmelon, 2));
        var before = TestGames.FullSnapshot(game);

        var rejection = game.PlantRejection(Spot, Crop(CropTypeId.Moonmelon));
        var result = game.Plant(Spot, Crop(CropTypeId.Moonmelon));

        Assert.Equal(Rejection.OutOfSeason, rejection);
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), result);
        Assert.Null(game.FarmPlot[Spot].Planting);
        Assert.Equal(2, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Moonmelon));
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void BR_C1_moonmelon_cannot_be_planted_in_spring()
    {
        var game = TilledOn(new GameDate(2, Season.Spring, 20));

        Assert.Equal(Rejection.OutOfSeason, game.PlantRejection(Spot, Crop(CropTypeId.Moonmelon)));
    }

    public static TheoryData<CropTypeId, Season> EveryCropInEverySeason()
    {
        var data = new TheoryData<CropTypeId, Season>();
        foreach (var id in Enum.GetValues<CropTypeId>())
        {
            foreach (var season in Enum.GetValues<Season>())
            {
                data.Add(id, season);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryCropInEverySeason))]
    public void BR_T3_a_held_seed_on_empty_tilled_soil_plants_only_in_its_crop_types_season(CropTypeId id, Season season)
    {
        var game = TilledOn(new GameDate(1, season, 10));
        var crop = Crop(id);
        Rejection? expected = crop.Season == season ? null : Rejection.OutOfSeason;

        Assert.Equal(expected, game.PlantRejection(Spot, crop));
    }

    // ---- AC4: summer ----

    [Fact]
    public void AC4_in_summer_planting_emberroot_is_OutOfSeason_and_changes_nothing()
    {
        var game = TilledOn(SummerDay);
        var before = TestGames.FullSnapshot(game);

        var result = game.Plant(Spot, Crop(CropTypeId.Emberroot));

        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), result);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void AC4_in_summer_a_session_plants_sunbell_and_starcorn_using_one_seed_each()
    {
        var other = new GridPosition(5, 2);
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Sunbell, 3);
        inventory.Add(ItemKind.Seed, CropTypeId.Starcorn, 2);
        var session = new GameSession(
            TestGames.OnDate(SummerDay, inventory, TestGames.TilledWith(Spot, null), TestGames.TilledWith(other, null)),
            TestGames.Shipped);
        Assert.Equal(Season.Summer, Assert.Contains(CropTypeId.Sunbell, session.Data.CropTypes).Season);
        Assert.Equal(Season.Summer, Assert.Contains(CropTypeId.Starcorn, session.Data.CropTypes).Season);

        var sunbell = session.Plant(Spot, CropTypeId.Sunbell);
        var starcorn = session.Plant(other, CropTypeId.Starcorn);

        Assert.Equal(ActionResult.Ok, sunbell);
        Assert.Equal(ActionResult.Ok, starcorn);
        Assert.Equal(new Planting(CropTypeId.Sunbell, SummerDay, 0), session.Game.FarmPlot[Spot].Planting);
        Assert.Equal(new Planting(CropTypeId.Starcorn, SummerDay, 0), session.Game.FarmPlot[other].Planting);
        Assert.Equal(2, session.Game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Sunbell));
        Assert.Equal(1, session.Game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Starcorn));
    }

    [Fact]
    public void AC4_in_summer_the_session_refuses_emberroot_as_OutOfSeason()
    {
        var session = SessionOn(SummerDay, TestGames.StartingSeeds());
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), session.Plant(Spot, CropTypeId.Emberroot));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    // ---- AC5: fall and winter ----

    [Fact]
    public void AC5_in_fall_moonmelon_can_be_planted_and_sunbell_cannot()
    {
        var game = TilledOn(FallDay);

        Assert.Equal(Rejection.OutOfSeason, game.PlantRejection(Spot, Crop(CropTypeId.Sunbell)));
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), game.Plant(Spot, Crop(CropTypeId.Sunbell)));
        Assert.Null(game.PlantRejection(Spot, Crop(CropTypeId.Moonmelon)));
        Assert.Equal(ActionResult.Ok, game.Plant(Spot, Crop(CropTypeId.Moonmelon)));
        Assert.Equal(new Planting(CropTypeId.Moonmelon, FallDay, 0), game.FarmPlot[Spot].Planting);
        Assert.Equal(2, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Moonmelon));
        Assert.Equal(3, game.Player.Inventory.Quantity(ItemKind.Seed, CropTypeId.Sunbell));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Moonmelon)]
    [InlineData(CropTypeId.Sunbell)]
    [InlineData(CropTypeId.Starcorn)]
    public void AC5_in_winter_no_crop_can_be_planted(CropTypeId id)
    {
        var game = TilledOn(WinterDay);
        var before = TestGames.FullSnapshot(game);

        var result = game.Plant(Spot, Crop(id));

        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), result);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    // ---- AC6: check order ----

    [Fact]
    public void AC6_without_the_seed_an_out_of_season_plant_is_still_NoSeed()
    {
        var game = TilledOn(SpringDay, TestGames.StartingSeeds());

        Assert.Equal(Rejection.NoSeed, game.PlantRejection(Spot, Crop(CropTypeId.Moonmelon)));
        Assert.Equal(ActionResult.Rejected(Rejection.NoSeed), game.Plant(Spot, Crop(CropTypeId.Moonmelon)));
    }

    [Fact]
    public void AC6_an_out_of_season_plant_on_grass_is_NotTilled_and_on_a_planting_is_TileOccupied()
    {
        var game = TestGames.OnDate(
            SpringDay,
            EverySeed(),
            TestGames.TilledWith(Occupied, TestGames.PlantingOf(CropTypeId.Emberroot, 1)));

        Assert.Equal(Rejection.NotTilled, game.PlantRejection(Grass, Crop(CropTypeId.Moonmelon)));
        Assert.Equal(Rejection.TileOccupied, game.PlantRejection(Occupied, Crop(CropTypeId.Moonmelon)));
    }

    [Fact]
    public void AC6_in_winter_check_order_is_NotTilled_then_TileOccupied_then_NoSeed_then_OutOfSeason()
    {
        var game = TestGames.OnDate(
            WinterDay,
            Seeds(CropTypeId.Emberroot, 1),
            TestGames.TilledWith(Occupied, TestGames.PlantingOf(CropTypeId.Emberroot, 1)),
            TestGames.TilledWith(Spot, null));

        Assert.Equal(Rejection.NotTilled, game.PlantRejection(Grass, Crop(CropTypeId.Emberroot)));
        Assert.Equal(Rejection.TileOccupied, game.PlantRejection(Occupied, Crop(CropTypeId.Emberroot)));
        Assert.Equal(Rejection.NoSeed, game.PlantRejection(Spot, Crop(CropTypeId.Dusktuber)));
        Assert.Equal(Rejection.OutOfSeason, game.PlantRejection(Spot, Crop(CropTypeId.Emberroot)));
    }

    // ---- AC7 / BR-T5: growing across a season change ----

    [Fact]
    public void AC7_dusktuber_planted_on_spring_27_and_watered_before_every_night_is_ripe_on_summer_4_and_harvests()
    {
        var session = SessionOn(new GameDate(1, Season.Spring, 27), TestGames.StartingSeeds());
        Assert.Equal(ActionResult.Ok, session.Plant(Spot, CropTypeId.Dusktuber));

        for (var night = 0; night < 5; night++)
        {
            Assert.Equal(PlantingStatus.Growing, session.PlantingStatusAt(Spot));
            Assert.True(session.Water(Spot).IsOk);
            Assert.True(session.Sleep().IsOk);
        }

        Assert.Equal(new GameDate(1, Season.Summer, 4), session.Game.Date);
        Assert.Equal(PlantingStatus.Ripe, session.PlantingStatusAt(Spot));
        Assert.Equal(ActionResult.Ok, session.Harvest(Spot));
        Assert.Null(session.Game.FarmPlot[Spot].Planting);
        Assert.Equal(1, session.Game.Player.Inventory.Quantity(ItemKind.Crop, CropTypeId.Dusktuber));
    }

    [Fact]
    public void BR_T5_a_watered_planting_keeps_growing_through_the_night_the_season_changes()
    {
        var game = TestGames.OnDate(
            new GameDate(1, Season.Spring, 28),
            null,
            TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 3), watered: true));

        NightProcessor.Run(game, TestGames.Shipped, DayEndReason.Slept);

        Assert.Equal(new GameDate(1, Season.Summer, 1), game.Date);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 4), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void BR_T5_a_ripe_spring_crop_stays_and_can_be_harvested_after_the_season_ends()
    {
        var session = new GameSession(
            TestGames.OnDate(
                new GameDate(1, Season.Spring, 28),
                null,
                TestGames.TilledWith(Spot, TestGames.Ripe(CropTypeId.Emberroot))),
            TestGames.Shipped);

        Assert.True(session.Sleep().IsOk);

        Assert.Equal(Season.Summer, session.Game.Date.Season);
        Assert.Equal(TestGames.Ripe(CropTypeId.Emberroot), session.Game.FarmPlot[Spot].Planting);
        Assert.Equal(ActionResult.Ok, session.Harvest(Spot));
    }

    // ---- AC8: the Use preview ----

    [Fact]
    public void AC8_preview_with_a_held_out_of_season_seed_is_OutOfSeason_and_use_changes_nothing()
    {
        var session = SessionOn(SpringDay, Seeds(CropTypeId.Moonmelon, 2));
        var held = HeldItem.SeedOf(CropTypeId.Moonmelon);
        var before = TestGames.FullSnapshot(session.Game);
        var events = 0;
        session.TileChanged += _ => events++;
        session.InventoryChanged += () => events++;

        var preview = session.PreviewUse(Spot, held);
        var use = session.Use(Spot, held);
        var plant = session.Plant(Spot, CropTypeId.Moonmelon);

        Assert.Equal(new UsePreview(null, Rejection.OutOfSeason), preview);
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), use);
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), plant);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
        Assert.Equal(0, events);
    }

    [Fact]
    public void AC8_preview_with_an_in_season_seed_is_still_Plant()
    {
        var session = SessionOn(FallDay, Seeds(CropTypeId.Moonmelon, 2));

        Assert.Equal(
            new UsePreview(UseActionKind.Plant, null),
            session.PreviewUse(Spot, HeldItem.SeedOf(CropTypeId.Moonmelon)));
    }

    // ---- AC10: the summer crops ripen ----

    [Theory]
    [InlineData(CropTypeId.Sunbell, 4)]
    [InlineData(CropTypeId.Starcorn, 7)]
    public void AC10_a_summer_crop_planted_in_summer_and_watered_each_day_is_ripe_after_its_grow_days(
        CropTypeId id, int nights)
    {
        var session = SessionOn(SummerDay, Seeds(id, 1));
        Assert.Equal(ActionResult.Ok, session.Plant(Spot, id));

        for (var night = 0; night < nights; night++)
        {
            Assert.Equal(PlantingStatus.Growing, session.PlantingStatusAt(Spot));
            Assert.True(session.Water(Spot).IsOk);
            Assert.True(session.Sleep().IsOk);
        }

        Assert.Equal(PlantingStatus.Ripe, session.PlantingStatusAt(Spot));
        Assert.Equal(nights, session.Game.FarmPlot[Spot].Planting!.DaysGrown);
        Assert.Equal(ActionResult.Ok, session.Harvest(Spot));
        Assert.Equal(1, session.Game.Player.Inventory.Quantity(ItemKind.Crop, id));
    }

    // ---- Edge: a seed with no data row ----

    [Fact]
    public void Seed_with_no_crop_data_is_refused_out_of_season()
    {
        var data = TestGames.Shipped with
        {
            CropTypes = TestGames.Shipped.CropTypes
                .Where(pair => pair.Key != CropTypeId.Moonmelon)
                .ToDictionary(pair => pair.Key, pair => pair.Value),
        };
        var session = new GameSession(TilledOn(FallDay, Seeds(CropTypeId.Moonmelon, 2)), data);
        var held = HeldItem.SeedOf(CropTypeId.Moonmelon);
        var before = TestGames.FullSnapshot(session.Game);

        var preview = session.PreviewUse(Spot, held);
        var plant = session.Plant(Spot, CropTypeId.Moonmelon);
        var use = session.Use(Spot, held);

        Assert.Equal(new UsePreview(null, Rejection.OutOfSeason), preview);
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), plant);
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), use);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }
}
