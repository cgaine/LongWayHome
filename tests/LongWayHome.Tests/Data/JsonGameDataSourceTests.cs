using System.Text.Json;
using System.Text.Json.Nodes;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data;

namespace LongWayHome.Tests.Data;

/// <summary>
/// One test per validation rule of <see cref="JsonGameDataSource"/>. Each test starts from a small valid
/// data set and breaks exactly one thing in it.
/// </summary>
public class JsonGameDataSourceTests
{
    private const string ValidCrops = """
        {
          "cropTypes": [
            { "id": "emberroot", "name": "Emberroot", "growDays": 3, "seedCost": 15, "sellPrice": 35, "season": "spring", "regrows": false },
            { "id": "moonmelon", "name": "Moonmelon", "growDays": 8, "seedCost": 60, "sellPrice": 190, "season": "fall", "regrows": true }
          ]
        }
        """;

    private const string ValidShop = """
        {
          "listings": [
            { "id": "seed_emberroot", "kind": "seed", "cropTypeId": "emberroot", "toolId": null, "price": 15, "vendor": "general_store", "requiresStage": 1, "oncePerGame": false },
            { "id": "seed_moonmelon", "kind": "seed", "cropTypeId": "moonmelon", "toolId": null, "price": 60, "vendor": "carpenter", "requiresStage": 0, "oncePerGame": false },
            { "id": "can_upgrade", "kind": "can_upgrade", "cropTypeId": null, "toolId": null, "price": 200, "vendor": "general_store", "requiresStage": 1, "oncePerGame": true },
            { "id": "tool_axe", "kind": "tool", "cropTypeId": null, "toolId": "axe", "price": 50, "vendor": "general_store", "requiresStage": 1, "oncePerGame": true }
          ]
        }
        """;

    private const string ValidTuning = """
        {
          "startingGold": 100,
          "startingSeeds": [ { "cropTypeId": "emberroot", "quantity": 10 } ],
          "maxEnergy": 150,
          "passOutEnergy": 75,
          "toolEnergyCost": 2,
          "canCapacity": 20,
          "canUpgradedCapacity": 40,
          "dayStartMinutes": 360,
          "dayEndMinutes": 1560,
          "dayLengthRealSeconds": 720,
          "rainChancePercent": 22,
          "seasonLengthDays": 28,
          "walkSpeedTilesPerSecond": 3.6,
          "runSpeedTilesPerSecond": 5.2
        }
        """;

    // ---- Valid data -------------------------------------------------------------------------------------

    [Fact]
    public void Valid_data_loads_and_maps_to_domain_records()
    {
        var data = Load(ValidCrops, ValidShop, ValidTuning);

        Assert.Equal(
            new CropType(CropTypeId.Moonmelon, "Moonmelon", 8, new Gold(60), new Gold(190), Season.Fall, true),
            data.CropTypes[CropTypeId.Moonmelon]);
        Assert.Equal(
            [
                // #145: every listing carries its vendor, the vendor's stage, its tool and the once-per-game flag.
                new ShopListing("seed_emberroot", ShopItemKind.Seed, CropTypeId.Emberroot, new Gold(15), BuildingId.GeneralStore, 1, null, false),
                new ShopListing("seed_moonmelon", ShopItemKind.Seed, CropTypeId.Moonmelon, new Gold(60), BuildingId.Carpenter, 0, null, false),
                new ShopListing("can_upgrade", ShopItemKind.CanUpgrade, null, new Gold(200), BuildingId.GeneralStore, 1, null, true),
                new ShopListing("tool_axe", ShopItemKind.Tool, null, new Gold(50), BuildingId.GeneralStore, 1, ToolId.Axe, true),
            ],
            data.ShopListings);
        Assert.Equal([new StartingStack(CropTypeId.Emberroot, 10)], data.Tuning.StartingSeeds);
        Assert.Equal(3.6, data.Tuning.WalkSpeedTilesPerSecond);
        Assert.Equal(5.2, data.Tuning.RunSpeedTilesPerSecond);
    }

    [Fact]
    public void Can_upgrade_listing_without_a_cropTypeId_field_is_accepted()
    {
        var shop = Edit(ValidShop, root => root["listings"]![2]!.AsObject().Remove("cropTypeId"));

        var data = Load(ValidCrops, shop, ValidTuning);

        Assert.Null(data.ShopListings[2].CropTypeId);
    }

    // ---- AC3: BR-C2 ------------------------------------------------------------------------------------

    [Fact]
    public void BR_C2_crop_with_zero_grow_days_is_rejected()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![0]!["growDays"] = 0);

        var error = Assert.Single(LoadErrors(crops, ValidShop, ValidTuning));

        Assert.Equal("crops.json: emberroot.growDays must be at least 1 (BR-C2), was 0", error);
    }

    [Fact]
    public void BR_C2_crop_with_zero_seed_cost_is_rejected()
    {
        // The seed listing's price is set to match, so only the BR-C2 error is reported.
        var crops = Edit(ValidCrops, root => root["cropTypes"]![0]!["seedCost"] = 0);
        var shop = Edit(ValidShop, root => root["listings"]![0]!["price"] = 0);

        var errors = LoadErrors(crops, shop, ValidTuning);

        Assert.Contains("crops.json: emberroot.seedCost must be above 0 (BR-C2), was 0", errors);
    }

    [Fact]
    public void BR_C2_crop_with_zero_sell_price_is_rejected()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![1]!["sellPrice"] = 0);

        var error = Assert.Single(LoadErrors(crops, ValidShop, ValidTuning));

        Assert.Equal("crops.json: moonmelon.sellPrice must be above 0 (BR-C2), was 0", error);
    }

    [Fact]
    public void Crop_with_an_empty_name_is_rejected()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![0]!["name"] = " ");

        var error = Assert.Single(LoadErrors(crops, ValidShop, ValidTuning));

        Assert.Equal("crops.json: emberroot.name must not be empty", error);
    }

    // ---- AC4: shop references and prices ---------------------------------------------------------------

    [Fact]
    public void Seed_listing_for_a_crop_type_absent_from_crops_json_is_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]![0]!["cropTypeId"] = "dusktuber");

        var errors = LoadErrors(ValidCrops, shop, ValidTuning);

        var error = Assert.Single(errors);
        Assert.Equal("shop.json: seed_emberroot.cropTypeId dusktuber is not a crop type in crops.json", error);
    }

    [Fact]
    public void Seed_listing_with_no_cropTypeId_is_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]![0]!["cropTypeId"] = null);

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: seed_emberroot.cropTypeId is required for a seed listing", error);
    }

    [Fact]
    public void Can_upgrade_listing_with_a_cropTypeId_is_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]![2]!["cropTypeId"] = "emberroot");

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: can_upgrade.cropTypeId must be null for a can_upgrade listing, was emberroot", error);
    }

    [Fact]
    public void Seed_listing_whose_price_differs_from_the_crop_seed_cost_is_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]![1]!["price"] = 61);

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: seed_moonmelon.price must equal the seedCost of moonmelon (60), was 61", error);
    }

    [Fact]
    public void Listing_with_a_zero_price_is_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]![2]!["price"] = 0);

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: can_upgrade.price must be above 0, was 0", error);
    }

    [Fact]
    public void Listing_with_an_empty_id_is_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]![2]!["id"] = "");

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: listings[2].id must not be empty", error);
    }

    // ---- AC5: uniqueness ------------------------------------------------------------------------------

    [Fact]
    public void Duplicate_crop_ids_are_rejected()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![1]!["id"] = "emberroot");
        var shop = Edit(ValidShop, root => root["listings"]!.AsArray().RemoveAt(1));

        var error = Assert.Single(LoadErrors(crops, shop, ValidTuning));

        Assert.Equal("crops.json: emberroot.id is used by more than one crop type", error);
    }

    [Fact]
    public void Duplicate_listing_ids_are_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]![1]!["id"] = "seed_emberroot");

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: seed_emberroot.id is used by more than one listing", error);
    }

    [Fact]
    public void Two_seed_listings_for_one_crop_type_are_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]!.AsArray().Add(
            JsonNode.Parse("""{ "id": "seed_emberroot_2", "kind": "seed", "cropTypeId": "emberroot", "toolId": null, "price": 15, "vendor": "general_store", "requiresStage": 1, "oncePerGame": false }""")));

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: seed_emberroot_2.cropTypeId emberroot already has a seed listing", error);
    }

    [Fact]
    public void Two_can_upgrade_listings_are_rejected()
    {
        var shop = Edit(ValidShop, root => root["listings"]!.AsArray().Add(
            JsonNode.Parse("""{ "id": "can_upgrade_2", "kind": "can_upgrade", "cropTypeId": null, "toolId": null, "price": 300, "vendor": "general_store", "requiresStage": 1, "oncePerGame": true }""")));

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.Equal("shop.json: can_upgrade_2.kind can_upgrade is listed more than once", error);
    }

    // ---- AC6: parse failures --------------------------------------------------------------------------

    [Fact]
    public void Malformed_json_is_rejected_naming_the_file()
    {
        var error = Assert.Single(LoadErrors(ValidCrops, "{ \"listings\": [ ", ValidTuning));

        Assert.StartsWith("shop.json: invalid JSON at ", error);
    }

    [Fact]
    public void Empty_file_is_rejected_naming_the_file()
    {
        var error = Assert.Single(LoadErrors(ValidCrops, ValidShop, ""));

        Assert.StartsWith("tuning.json: invalid JSON at ", error);
    }

    [Fact]
    public void Unknown_enum_value_is_rejected_naming_the_file_and_path()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![0]!["season"] = "autumn");

        var error = Assert.Single(LoadErrors(crops, ValidShop, ValidTuning));

        Assert.StartsWith("crops.json: invalid JSON at $.cropTypes[0].season: ", error);
    }

    [Fact]
    public void Integer_enum_value_is_rejected()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![0]!["season"] = 2);

        var error = Assert.Single(LoadErrors(crops, ValidShop, ValidTuning));

        Assert.StartsWith("crops.json: invalid JSON at $.cropTypes[0].season: ", error);
    }

    [Fact]
    public void Unknown_field_is_rejected_naming_the_file()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![0]!["growdays"] = 3);

        var error = Assert.Single(LoadErrors(crops, ValidShop, ValidTuning));

        Assert.StartsWith("crops.json: invalid JSON at ", error);
        Assert.Contains("growdays", error);
    }

    [Fact]
    public void Missing_required_field_is_rejected_naming_the_file_and_field()
    {
        var tuning = Edit(ValidTuning, root => root.AsObject().Remove("maxEnergy"));

        var error = Assert.Single(LoadErrors(ValidCrops, ValidShop, tuning));

        Assert.StartsWith("tuning.json: invalid JSON at ", error);
        Assert.Contains("maxEnergy", error);
    }

    [Fact]
    public void Null_file_content_is_rejected_naming_the_file()
    {
        var error = Assert.Single(LoadErrors(ValidCrops, "null", ValidTuning));

        Assert.Equal("shop.json: the file contains null instead of an object", error);
    }

    [Fact]
    public void Null_list_is_rejected_naming_the_file_and_field()
    {
        var error = Assert.Single(LoadErrors("""{ "cropTypes": null }""", "{ \"listings\": [] }", """{ "startingGold": 0, "startingSeeds": [], "maxEnergy": 1, "passOutEnergy": 1, "toolEnergyCost": 1, "canCapacity": 1, "canUpgradedCapacity": 2, "dayStartMinutes": 0, "dayEndMinutes": 1, "dayLengthRealSeconds": 1, "rainChancePercent": 0, "seasonLengthDays": 1, "walkSpeedTilesPerSecond": 1, "runSpeedTilesPerSecond": 1 }"""));

        Assert.Equal("crops.json: cropTypes must be a list, was null", error);
    }

    [Fact]
    public void Parse_failures_never_escape_as_a_raw_JsonException()
    {
        var exception = Record.Exception(() => Load("not json", "[]", "{ \"x\": 1 }"));

        var gameDataException = Assert.IsType<GameDataException>(exception);
        Assert.Equal(3, gameDataException.Errors.Count);
    }

    // ---- AC7: tuning ---------------------------------------------------------------------------------

    public static TheoryData<string, double, string> TuningViolations => new()
    {
        { "startingGold", -1, "tuning.json: startingGold must be at least 0 (BR-P5), was -1" },
        { "maxEnergy", 0, "tuning.json: maxEnergy must be above 0, was 0" },
        { "maxEnergy", 151, "tuning.json: maxEnergy must be at most 150 (BR-P1), was 151" },
        { "passOutEnergy", 0, "tuning.json: passOutEnergy must be above 0, was 0" },
        { "passOutEnergy", 151, "tuning.json: passOutEnergy must be at most maxEnergy (150), was 151" },
        { "toolEnergyCost", 0, "tuning.json: toolEnergyCost must be above 0, was 0" },
        { "canCapacity", 0, "tuning.json: canCapacity must be above 0, was 0" },
        { "canUpgradedCapacity", 20, "tuning.json: canUpgradedCapacity must be above canCapacity (20), was 20" },
        { "dayStartMinutes", -1, "tuning.json: dayStartMinutes must be at least 0, was -1" },
        { "dayEndMinutes", 360, "tuning.json: dayEndMinutes must be above dayStartMinutes (360), was 360" },
        { "dayLengthRealSeconds", 0, "tuning.json: dayLengthRealSeconds must be above 0, was 0" },
        { "rainChancePercent", -1, "tuning.json: rainChancePercent must be between 0 and 100, was -1" },
        { "rainChancePercent", 101, "tuning.json: rainChancePercent must be between 0 and 100, was 101" },
        { "seasonLengthDays", 0, "tuning.json: seasonLengthDays must be at least 1, was 0" },
        { "walkSpeedTilesPerSecond", 0, "tuning.json: walkSpeedTilesPerSecond must be above 0, was 0" },
        { "runSpeedTilesPerSecond", 3.5, "tuning.json: runSpeedTilesPerSecond must be at least walkSpeedTilesPerSecond (3.6), was 3.5" },
    };

    [Theory]
    [MemberData(nameof(TuningViolations))]
    public void Tuning_value_that_breaks_its_rule_is_rejected(string field, double value, string expectedError)
    {
        var tuning = Edit(ValidTuning, root => root[field] = field.EndsWith("TilesPerSecond", StringComparison.Ordinal)
            ? JsonValue.Create(value)
            : JsonValue.Create((int)value));

        // Contains, not Single: breaking one bound can break a related one too (maxEnergy 0 is also below passOutEnergy).
        Assert.Contains(expectedError, LoadErrors(ValidCrops, ValidShop, tuning));
    }

    [Fact]
    public void Tuning_boundary_values_are_accepted()
    {
        var tuning = Edit(ValidTuning, root =>
        {
            root["startingGold"] = 0;
            root["passOutEnergy"] = 150;
            root["dayStartMinutes"] = 0;
            root["rainChancePercent"] = 100;
            root["seasonLengthDays"] = 1;
            root["runSpeedTilesPerSecond"] = 3.6;
        });

        var data = Load(ValidCrops, ValidShop, tuning);

        Assert.Equal(150, data.Tuning.PassOutEnergy);
    }

    [Fact]
    public void BR_P7_starting_stack_with_zero_quantity_is_rejected()
    {
        var tuning = Edit(ValidTuning, root => root["startingSeeds"]![0]!["quantity"] = 0);

        var error = Assert.Single(LoadErrors(ValidCrops, ValidShop, tuning));

        Assert.Equal("tuning.json: startingSeeds[0].quantity must be at least 1 (BR-P7), was 0", error);
    }

    [Fact]
    public void BR_P7_starting_stacks_repeating_a_crop_type_are_rejected()
    {
        var tuning = Edit(ValidTuning, root => root["startingSeeds"]!.AsArray().Add(
            JsonNode.Parse("""{ "cropTypeId": "emberroot", "quantity": 1 }""")));

        var error = Assert.Single(LoadErrors(ValidCrops, ValidShop, tuning));

        Assert.Equal("tuning.json: startingSeeds[1].cropTypeId emberroot already has a starting stack (BR-P7)", error);
    }

    [Fact]
    public void Starting_stack_for_a_crop_type_absent_from_crops_json_is_rejected()
    {
        var tuning = Edit(ValidTuning, root => root["startingSeeds"]![0]!["cropTypeId"] = "dusktuber");

        var error = Assert.Single(LoadErrors(ValidCrops, ValidShop, tuning));

        Assert.Equal("tuning.json: startingSeeds[0].cropTypeId dusktuber is not a crop type in crops.json", error);
    }

    // ---- AC8: all errors at once -----------------------------------------------------------------------

    [Fact]
    public void Errors_in_several_files_are_all_listed_in_one_exception()
    {
        var crops = Edit(ValidCrops, root =>
        {
            root["cropTypes"]![0]!["growDays"] = 0;
            root["cropTypes"]![1]!["sellPrice"] = 0;
        });
        var shop = Edit(ValidShop, root => root["listings"]![2]!["price"] = 0);
        var tuning = Edit(ValidTuning, root => root["rainChancePercent"] = 101);

        var exception = Assert.Throws<GameDataException>(() => Load(crops, shop, tuning));

        string[] expected =
        [
            "crops.json: emberroot.growDays must be at least 1 (BR-C2), was 0",
            "crops.json: moonmelon.sellPrice must be above 0 (BR-C2), was 0",
            "shop.json: can_upgrade.price must be above 0, was 0",
            "tuning.json: rainChancePercent must be between 0 and 100, was 101",
        ];
        Assert.Equal(expected, exception.Errors);
        Assert.Equal(string.Join(Environment.NewLine, expected), exception.Message);
    }

    [Fact]
    public void A_parse_error_in_one_file_does_not_hide_validation_errors_in_another()
    {
        var crops = Edit(ValidCrops, root => root["cropTypes"]![0]!["growDays"] = 0);

        var errors = LoadErrors(crops, "{", ValidTuning);

        // Parse errors are listed first, then validation errors.
        Assert.Equal(2, errors.Count);
        Assert.StartsWith("shop.json: invalid JSON at ", errors[0]);
        Assert.Equal("crops.json: emberroot.growDays must be at least 1 (BR-C2), was 0", errors[1]);
    }

    // ---- AC9 and AC10 --------------------------------------------------------------------------------

    [Fact]
    public void BR_P5_negative_gold_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Gold(-1));
        Assert.Equal(0, new Gold(0).Value);
    }

    [Fact]
    public void Enum_values_round_trip_as_canonical_snake_case_codes()
    {
        var options = JsonGameDataSource.SerializerOptions;

        Assert.Equal("\"can_upgrade\"", JsonSerializer.Serialize(ShopItemKind.CanUpgrade, options));
        Assert.Equal("\"seed\"", JsonSerializer.Serialize(ShopItemKind.Seed, options));
        Assert.Equal("\"emberroot\"", JsonSerializer.Serialize(CropTypeId.Emberroot, options));
        Assert.Equal(Season.Fall, JsonSerializer.Deserialize<Season>("\"fall\"", options));
        Assert.Equal(ShopItemKind.CanUpgrade, JsonSerializer.Deserialize<ShopItemKind>("\"can_upgrade\"", options));
    }

    // ---- #145 AC7: vendor, requiresStage, toolId and oncePerGame ---------------------------------------

    private const int ToolAxe = 3;

    /// <summary>The single load error, checked to name the listing and the offending field.</summary>
    private static void AssertSingleErrorNaming(IReadOnlyList<string> errors, string listingId, string field)
    {
        var error = Assert.Single(errors);
        Assert.StartsWith($"shop.json: {listingId}.", error);
        Assert.Contains(field, error);
    }

    [Fact]
    public void AC7_A_tool_listing_loads_its_tool_vendor_stage_and_once_per_game_flag()
    {
        var data = Load(ValidCrops, ValidShop, ValidTuning);

        Assert.Equal(
            new ShopListing("tool_axe", ShopItemKind.Tool, null, new Gold(50), BuildingId.GeneralStore, 1, ToolId.Axe, true),
            data.ShopListings[ToolAxe]);
    }

    [Fact]
    public void AC7_A_carpenter_listing_at_stage_0_loads_its_vendor_and_stage()
    {
        var data = Load(ValidCrops, ValidShop, ValidTuning);

        Assert.Equal((BuildingId.Carpenter, 0, false), (data.ShopListings[1].Vendor, data.ShopListings[1].RequiresStage, data.ShopListings[1].OncePerGame));
    }

    [Fact]
    public void AC7_A_tool_listing_with_no_toolId_is_rejected_naming_the_listing()
    {
        var shop = Edit(ValidShop, root => root["listings"]![ToolAxe]!["toolId"] = null);

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "tool_axe", "toolId");
    }

    [Fact]
    public void AC7_A_tool_listing_without_a_toolId_field_is_rejected_naming_the_listing()
    {
        var shop = Edit(ValidShop, root => root["listings"]![ToolAxe]!.AsObject().Remove("toolId"));

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "tool_axe", "toolId");
    }

    [Fact]
    public void AC7_A_tool_listing_for_the_hoe_is_rejected_naming_the_listing()
    {
        var shop = Edit(ValidShop, root => root["listings"]![ToolAxe]!["toolId"] = "hoe");

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "tool_axe", "toolId");
    }

    [Fact]
    public void AC7_A_second_listing_for_the_same_tool_is_rejected_naming_it()
    {
        var shop = Edit(ValidShop, root => root["listings"]!.AsArray().Add(JsonNode.Parse(
            """{ "id": "tool_axe_2", "kind": "tool", "cropTypeId": null, "toolId": "axe", "price": 60, "vendor": "general_store", "requiresStage": 1, "oncePerGame": true }""")));

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "tool_axe_2", "axe");
    }

    [Fact]
    public void AC7_A_listing_for_the_other_tool_is_accepted()
    {
        var shop = Edit(ValidShop, root => root["listings"]!.AsArray().Add(JsonNode.Parse(
            """{ "id": "tool_pickaxe", "kind": "tool", "cropTypeId": null, "toolId": "pickaxe", "price": 75, "vendor": "general_store", "requiresStage": 1, "oncePerGame": true }""")));

        var data = Load(ValidCrops, shop, ValidTuning);

        Assert.Equal(
            new ShopListing("tool_pickaxe", ShopItemKind.Tool, null, new Gold(75), BuildingId.GeneralStore, 1, ToolId.Pickaxe, true),
            data.ShopListings[^1]);
    }

    [Fact]
    public void AC7_A_tool_listing_with_a_cropTypeId_is_rejected_naming_the_listing()
    {
        var shop = Edit(ValidShop, root => root["listings"]![ToolAxe]!["cropTypeId"] = "emberroot");

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "tool_axe", "cropTypeId");
    }

    [Fact]
    public void AC7_A_tool_listing_that_is_not_once_per_game_is_rejected_naming_the_listing()
    {
        var shop = Edit(ValidShop, root => root["listings"]![ToolAxe]!["oncePerGame"] = false);

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "tool_axe", "oncePerGame");
    }

    [Theory]
    [InlineData(0, "seed_emberroot", true)]
    [InlineData(2, "can_upgrade", false)]
    public void AC7_A_seed_listing_once_per_game_or_a_can_upgrade_not_once_per_game_is_rejected_naming_it(
        int index, string listingId, bool oncePerGame)
    {
        var shop = Edit(ValidShop, root => root["listings"]![index]!["oncePerGame"] = oncePerGame);

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), listingId, "oncePerGame");
    }

    [Theory]
    [InlineData(0, "seed_emberroot")]
    [InlineData(2, "can_upgrade")]
    public void AC7_A_seed_or_can_upgrade_listing_with_a_toolId_is_rejected_naming_the_listing(int index, string listingId)
    {
        var shop = Edit(ValidShop, root => root["listings"]![index]!["toolId"] = "axe");

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), listingId, "toolId");
    }

    [Theory]
    [InlineData("miras_house")]
    [InlineData("blacksmith")]
    public void AC7_A_vendor_other_than_the_general_store_or_carpenter_is_rejected_naming_the_listing(string vendor)
    {
        var shop = Edit(ValidShop, root => root["listings"]![0]!["vendor"] = vendor);

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "seed_emberroot", "vendor");
    }

    [Fact]
    public void AC7_An_unknown_vendor_code_is_a_load_error_at_the_listings_vendor()
    {
        // Characterization: an unknown BuildingId code already fails to parse once the DTO has a vendor field.
        var shop = Edit(ValidShop, root => root["listings"]![0]!["vendor"] = "bakery");

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.StartsWith("shop.json: invalid JSON at $.listings[0].vendor", error);
    }

    [Fact]
    public void AC7_A_negative_requiresStage_is_rejected_naming_the_listing()
    {
        var shop = Edit(ValidShop, root => root["listings"]![ToolAxe]!["requiresStage"] = -1);

        AssertSingleErrorNaming(LoadErrors(ValidCrops, shop, ValidTuning), "tool_axe", "requiresStage");
    }

    [Theory]
    [InlineData("vendor")]
    [InlineData("requiresStage")]
    [InlineData("oncePerGame")]
    public void AC7_A_listing_missing_a_required_field_is_rejected_naming_the_field(string field)
    {
        var shop = Edit(ValidShop, root => root["listings"]![0]!.AsObject().Remove(field));

        var error = Assert.Single(LoadErrors(ValidCrops, shop, ValidTuning));

        Assert.StartsWith("shop.json: invalid JSON at ", error);
        Assert.Contains(field, error);
    }

    [Fact]
    public void AC7_Tool_and_building_codes_round_trip_as_canonical_snake_case()
    {
        var options = JsonGameDataSource.SerializerOptions;

        Assert.Equal("\"tool\"", JsonSerializer.Serialize(ShopItemKind.Tool, options));
        Assert.Equal("\"axe\"", JsonSerializer.Serialize(ToolId.Axe, options));
        Assert.Equal("\"pickaxe\"", JsonSerializer.Serialize(ToolId.Pickaxe, options));
        Assert.Equal("\"general_store\"", JsonSerializer.Serialize(BuildingId.GeneralStore, options));
        Assert.Equal("\"carpenter\"", JsonSerializer.Serialize(BuildingId.Carpenter, options));
        Assert.Equal("\"miras_house\"", JsonSerializer.Serialize(BuildingId.MirasHouse, options));
        Assert.Equal("\"blacksmith\"", JsonSerializer.Serialize(BuildingId.Blacksmith, options));
    }

    // ---- Helpers -------------------------------------------------------------------------------------

    private static Application.Data.GameData Load(string crops, string shop, string tuning) =>
        new JsonGameDataSource(crops, shop, tuning).Load();

    private static IReadOnlyList<string> LoadErrors(string crops, string shop, string tuning) =>
        Assert.Throws<GameDataException>(() => Load(crops, shop, tuning)).Errors;

    private static string Edit(string json, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(json)!;
        edit(root);
        return root.ToJsonString();
    }
}
