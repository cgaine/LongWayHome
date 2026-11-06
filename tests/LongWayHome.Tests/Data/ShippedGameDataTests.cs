using LongWayHome.Application.Data;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// Loads the real <c>game/data/*.json</c> files, linked into the test output by the test project.
/// The tuning values pin settled rules: changing one is a rules change and must fail here loudly.
/// </summary>
public class ShippedGameDataTests
{
    private static readonly GameData Data = LoadShippedData();

    [Fact]
    public void AC1_shipped_crops_are_the_three_canonical_crop_types()
    {
        CropType[] expected =
        [
            new(CropTypeId.Emberroot, "Emberroot", 3, new Gold(15), new Gold(35), Season.Spring, false),
            new(CropTypeId.Dusktuber, "Dusktuber", 5, new Gold(30), new Gold(75), Season.Spring, false),
            new(CropTypeId.Moonmelon, "Moonmelon", 8, new Gold(60), new Gold(190), Season.Fall, false),
        ];

        Assert.Equal(expected, Data.CropTypes.Values.OrderBy(crop => crop.Id));
        Assert.All(Data.CropTypes, pair => Assert.Equal(pair.Key, pair.Value.Id));
    }

    [Fact]
    public void AC2_shipped_shop_has_three_seed_listings_and_the_can_upgrade()
    {
        ShopListing[] expected =
        [
            new("seed_emberroot", ShopItemKind.Seed, CropTypeId.Emberroot, new Gold(15)),
            new("seed_dusktuber", ShopItemKind.Seed, CropTypeId.Dusktuber, new Gold(30)),
            new("seed_moonmelon", ShopItemKind.Seed, CropTypeId.Moonmelon, new Gold(60)),
            new("can_upgrade", ShopItemKind.CanUpgrade, null, new Gold(200)),
        ];

        Assert.Equal(expected, Data.ShopListings);
        Assert.All(
            Data.ShopListings.Where(listing => listing.Kind == ShopItemKind.Seed),
            listing => Assert.Equal(Data.CropTypes[listing.CropTypeId!.Value].SeedCost, listing.Price));
    }

    [Fact]
    public void AC11_BR_P6_new_game_starts_with_100_gold_10_emberroot_and_5_dusktuber_seeds()
    {
        Assert.Equal(100, Data.Tuning.StartingGold);
        Assert.Equal(
            [new StartingStack(CropTypeId.Emberroot, 10), new StartingStack(CropTypeId.Dusktuber, 5)],
            Data.Tuning.StartingSeeds);
    }

    [Fact]
    public void AC11_BR_P1_BR_P3_energy_is_150_max_and_75_after_passing_out()
    {
        Assert.Equal(150, Data.Tuning.MaxEnergy);
        Assert.Equal(75, Data.Tuning.PassOutEnergy);
    }

    [Fact]
    public void AC11_BR_P2_tool_actions_cost_2_energy()
    {
        Assert.Equal(2, Data.Tuning.ToolEnergyCost);
    }

    [Fact]
    public void AC11_BR_W1_can_holds_20_charges_or_40_upgraded()
    {
        Assert.Equal(20, Data.Tuning.CanCapacity);
        Assert.Equal(40, Data.Tuning.CanUpgradedCapacity);
    }

    [Fact]
    public void AC11_BR_G2_day_runs_06_00_to_02_00_over_12_real_minutes()
    {
        Assert.Equal(360, Data.Tuning.DayStartMinutes);
        Assert.Equal(1560, Data.Tuning.DayEndMinutes);
        Assert.Equal(720, Data.Tuning.DayLengthRealSeconds);
    }

    [Fact]
    public void AC11_BR_G4_rain_chance_is_22_percent()
    {
        Assert.Equal(22, Data.Tuning.RainChancePercent);
    }

    [Fact]
    public void AC11_BR_G1_season_lasts_28_days()
    {
        Assert.Equal(28, Data.Tuning.SeasonLengthDays);
    }

    [Fact]
    public void Shipped_movement_speeds_are_the_draft_values()
    {
        Assert.Equal(3.6, Data.Tuning.WalkSpeedTilesPerSecond);
        Assert.Equal(5.2, Data.Tuning.RunSpeedTilesPerSecond);
    }

    private static GameData LoadShippedData()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "data");
        string Read(string fileName) => File.ReadAllText(Path.Combine(folder, fileName));

        return new JsonGameDataSource(
            Read(JsonGameDataSource.CropsFileName),
            Read(JsonGameDataSource.ShopFileName),
            Read(JsonGameDataSource.TuningFileName)).Load();
    }
}
