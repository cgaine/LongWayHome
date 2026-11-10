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
    public void AC9_shipped_crops_are_the_five_canonical_crop_types()
    {
        CropType[] expected =
        [
            new(CropTypeId.Emberroot, "Emberroot", 3, new Gold(15), new Gold(35), Season.Spring, false),
            new(CropTypeId.Dusktuber, "Dusktuber", 5, new Gold(30), new Gold(75), Season.Spring, false),
            new(CropTypeId.Moonmelon, "Moonmelon", 8, new Gold(60), new Gold(190), Season.Fall, false),
            new(CropTypeId.Sunbell, "Sunbell", 4, new Gold(20), new Gold(50), Season.Summer, false),
            new(CropTypeId.Starcorn, "Starcorn", 7, new Gold(45), new Gold(130), Season.Summer, false),
        ];

        Assert.Equal(expected, Data.CropTypes.Values.OrderBy(crop => crop.Id));
        Assert.All(Data.CropTypes, pair => Assert.Equal(pair.Key, pair.Value.Id));
    }

    [Fact]
    public void AC9_every_crop_type_id_has_a_shipped_data_row()
    {
        Assert.Equal(Enum.GetValues<CropTypeId>(), Data.CropTypes.Keys.Order());
    }

    [Fact]
    public void AC9_shipped_shop_has_five_seed_listings_and_the_can_upgrade()
    {
        // #145: each listing is sold by the general store from stage 1; only the Can upgrade is once per game.
        ShopListing[] expected =
        [
            new("seed_emberroot", ShopItemKind.Seed, CropTypeId.Emberroot, new Gold(15), BuildingId.GeneralStore, 1, null, false),
            new("seed_dusktuber", ShopItemKind.Seed, CropTypeId.Dusktuber, new Gold(30), BuildingId.GeneralStore, 1, null, false),
            new("seed_moonmelon", ShopItemKind.Seed, CropTypeId.Moonmelon, new Gold(60), BuildingId.GeneralStore, 1, null, false),
            new("seed_sunbell", ShopItemKind.Seed, CropTypeId.Sunbell, new Gold(20), BuildingId.GeneralStore, 1, null, false),
            new("seed_starcorn", ShopItemKind.Seed, CropTypeId.Starcorn, new Gold(45), BuildingId.GeneralStore, 1, null, false),
            new("can_upgrade", ShopItemKind.CanUpgrade, null, new Gold(200), BuildingId.GeneralStore, 1, null, true),
        ];

        // #166: the backpack upgrades and the chest are pinned by AC5_shipped_shop_lists_the_stage_2_and_3_unlocks.
        Assert.Equal(expected, Data.ShopListings.Where(listing => listing.Kind is ShopItemKind.Seed or ShopItemKind.CanUpgrade));
        Assert.All(
            Data.ShopListings.Where(listing => listing.Kind == ShopItemKind.Seed),
            listing => Assert.Equal(Data.CropTypes[listing.CropTypeId!.Value].SeedCost, listing.Price));
    }

    [Fact]
    public void AC7_shipped_shop_sells_the_axe_for_50_and_the_pickaxe_for_75_at_the_general_store_once_each()
    {
        ShopListing[] expected =
        [
            new("tool_axe", ShopItemKind.Tool, null, new Gold(50), BuildingId.GeneralStore, 1, ToolId.Axe, true),
            new("tool_pickaxe", ShopItemKind.Tool, null, new Gold(75), BuildingId.GeneralStore, 1, ToolId.Pickaxe, true),
        ];

        Assert.Equal(expected, Data.ShopListings.Where(listing => listing.Kind == ShopItemKind.Tool));
    }

    [Fact]
    public void AC5_shipped_shop_lists_the_stage_2_and_3_unlocks()
    {
        // #166: draft prices; every row carries placeableId and backpackCapacity, null where unused.
        ShopListing[] expected =
        [
            new("backpack_24", ShopItemKind.BackpackUpgrade, null, new Gold(400), BuildingId.GeneralStore, 2, null, true, null, 24),
            new("backpack_36", ShopItemKind.BackpackUpgrade, null, new Gold(1000), BuildingId.GeneralStore, 3, null, true, null, 36),
            new("chest_store", ShopItemKind.Placeable, null, new Gold(100), BuildingId.GeneralStore, 3, null, false, PlaceableId.Chest, null),
        ];

        Assert.Equal(
            expected,
            Data.ShopListings.Where(listing => listing.Kind is ShopItemKind.BackpackUpgrade or ShopItemKind.Placeable));
    }

    [Fact]
    public void AC5_every_shipped_shop_row_carries_placeableId_and_backpackCapacity()
    {
        var shop = System.Text.Json.Nodes.JsonNode.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", JsonGameDataSource.ShopFileName)))!;

        Assert.All(
            shop["listings"]!.AsArray(),
            row =>
            {
                Assert.True(row!.AsObject().ContainsKey("placeableId"), $"{row["id"]} has no placeableId field");
                Assert.True(row.AsObject().ContainsKey("backpackCapacity"), $"{row["id"]} has no backpackCapacity field");
            });
    }

    [Fact]
    public void BR_L4_shipped_shop_lists_each_tool_once()
    {
        var tools = Data.ShopListings.Where(listing => listing.Kind == ShopItemKind.Tool).Select(listing => listing.ToolId);

        Assert.Equal([ToolId.Axe, ToolId.Pickaxe], tools.Order());
    }

    [Fact]
    public void BR_C2_every_shipped_crop_grows_at_least_one_day_and_has_a_seed_cost_and_sell_price_above_0()
    {
        Assert.Equal(5, Data.CropTypes.Count);
        Assert.All(Data.CropTypes.Values, crop =>
        {
            Assert.True(crop.GrowDays >= 1, $"{crop.Id} growDays {crop.GrowDays}");
            Assert.True(crop.SeedCost.Value > 0, $"{crop.Id} seedCost {crop.SeedCost}");
            Assert.True(crop.SellPrice.Value > 0, $"{crop.Id} sellPrice {crop.SellPrice}");
        });
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
            Read(JsonGameDataSource.TuningFileName),
            Read(JsonGameDataSource.NodesFileName)).Load();
    }
}
