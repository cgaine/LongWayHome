using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LongWayHome.Application.Data;
using LongWayHome.Application.Ports;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data.Dtos;

namespace LongWayHome.Infrastructure.Data;

/// <summary>
/// Parses and validates <c>crops.json</c>, <c>shop.json</c> and <c>tuning.json</c>.
/// Takes the file contents as strings, not paths: in an exported build <c>res://</c> files live inside the
/// <c>.pck</c>, which <c>System.IO</c> can't open, so the Godot layer reads them with <c>Godot.FileAccess</c>.
/// </summary>
/// <remarks>
/// Every parse and validation error across the three files is collected before a single
/// <see cref="GameDataException"/> is thrown. When <c>crops.json</c> doesn't parse, the checks in the other
/// files that refer to crop types are skipped, since there is nothing to check them against.
/// </remarks>
public sealed class JsonGameDataSource : IGameDataSource
{
    public const string CropsFileName = "crops.json";
    public const string ShopFileName = "shop.json";
    public const string TuningFileName = "tuning.json";

    private readonly string _cropsJson;
    private readonly string _shopJson;
    private readonly string _tuningJson;

    public JsonGameDataSource(string cropsJson, string shopJson, string tuningJson)
    {
        ArgumentNullException.ThrowIfNull(cropsJson);
        ArgumentNullException.ThrowIfNull(shopJson);
        ArgumentNullException.ThrowIfNull(tuningJson);
        _cropsJson = cropsJson;
        _shopJson = shopJson;
        _tuningJson = tuningJson;
    }

    /// <summary>
    /// The serializer options for the data files: camelCase properties, enums as snake_case codes
    /// (<c>can_upgrade</c>) with integers refused, and unknown fields rejected. Read-only.
    /// </summary>
    public static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

    public GameData Load()
    {
        var errors = new List<string>();

        var cropsFile = Parse<CropsFileDto>(_cropsJson, CropsFileName, errors);
        var shopFile = Parse<ShopFileDto>(_shopJson, ShopFileName, errors);
        var tuningFile = Parse<TuningDto>(_tuningJson, TuningFileName, errors);

        // Null when crops.json didn't parse: cross-file reference checks are then skipped.
        var cropsById = cropsFile is null ? null : ValidateCrops(cropsFile, errors);
        if (shopFile is not null)
        {
            ValidateShop(shopFile, cropsById, errors);
        }

        if (tuningFile is not null)
        {
            ValidateTuning(tuningFile, cropsById, errors);
        }

        if (errors.Count > 0)
        {
            throw new GameDataException(errors);
        }

        return new GameData(
            cropsFile!.CropTypes.ToDictionary(dto => dto.Id, ToCropType),
            shopFile!.Listings.Select(ToShopListing).ToList(),
            ToGameTuning(tuningFile!));
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false) },
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    private static T? Parse<T>(string json, string fileName, List<string> errors)
        where T : class
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(json, SerializerOptions);
            if (result is null)
            {
                errors.Add($"{fileName}: the file contains null instead of an object");
            }

            return result;
        }
        catch (JsonException exception)
        {
            errors.Add($"{fileName}: invalid JSON at {exception.Path ?? "$"}: {exception.Message}");
            return null;
        }
    }

    private static Dictionary<CropTypeId, CropTypeDto> ValidateCrops(CropsFileDto file, List<string> errors)
    {
        var cropsById = new Dictionary<CropTypeId, CropTypeDto>();
        if (file.CropTypes is null)
        {
            errors.Add($"{CropsFileName}: cropTypes must be a list, was null");
            return cropsById;
        }

        for (var index = 0; index < file.CropTypes.Count; index++)
        {
            var crop = file.CropTypes[index];
            if (crop is null)
            {
                errors.Add($"{CropsFileName}: cropTypes[{index}] must be an object, was null");
                continue;
            }

            var id = Code(crop.Id);
            if (!cropsById.TryAdd(crop.Id, crop))
            {
                errors.Add($"{CropsFileName}: {id}.id is used by more than one crop type");
            }

            if (string.IsNullOrWhiteSpace(crop.Name))
            {
                errors.Add($"{CropsFileName}: {id}.name must not be empty");
            }

            if (crop.GrowDays < 1)
            {
                errors.Add($"{CropsFileName}: {id}.growDays must be at least 1 (BR-C2), was {crop.GrowDays}");
            }

            if (crop.SeedCost <= 0)
            {
                errors.Add($"{CropsFileName}: {id}.seedCost must be above 0 (BR-C2), was {crop.SeedCost}");
            }

            if (crop.SellPrice <= 0)
            {
                errors.Add($"{CropsFileName}: {id}.sellPrice must be above 0 (BR-C2), was {crop.SellPrice}");
            }
        }

        return cropsById;
    }

    private static void ValidateShop(
        ShopFileDto file,
        Dictionary<CropTypeId, CropTypeDto>? cropsById,
        List<string> errors)
    {
        if (file.Listings is null)
        {
            errors.Add($"{ShopFileName}: listings must be a list, was null");
            return;
        }

        var listingIds = new HashSet<string>(StringComparer.Ordinal);
        var seededCropTypes = new HashSet<CropTypeId>();
        var listedTools = new HashSet<ToolId>();
        var canUpgradeCount = 0;

        for (var index = 0; index < file.Listings.Count; index++)
        {
            var listing = file.Listings[index];
            if (listing is null)
            {
                errors.Add($"{ShopFileName}: listings[{index}] must be an object, was null");
                continue;
            }

            string label;
            if (string.IsNullOrWhiteSpace(listing.Id))
            {
                label = $"listings[{index}]";
                errors.Add($"{ShopFileName}: {label}.id must not be empty");
            }
            else
            {
                label = listing.Id;
                if (!listingIds.Add(listing.Id))
                {
                    errors.Add($"{ShopFileName}: {label}.id is used by more than one listing");
                }
            }

            if (listing.Price <= 0)
            {
                errors.Add($"{ShopFileName}: {label}.price must be above 0, was {listing.Price}");
            }

            // BR-L5: only the general store and the carpenter sell; no listing needs a stage below 0.
            if (listing.Vendor is not (BuildingId.GeneralStore or BuildingId.Carpenter))
            {
                errors.Add($"{ShopFileName}: {label}.vendor must be general_store or carpenter, was {Code(listing.Vendor)}");
            }

            if (listing.RequiresStage < 0)
            {
                errors.Add($"{ShopFileName}: {label}.requiresStage must be 0 or more, was {listing.RequiresStage}");
            }

            if (listing.Kind != ShopItemKind.Tool && listing.ToolId is { } toolId)
            {
                errors.Add($"{ShopFileName}: {label}.toolId must be null for a {Code(listing.Kind)} listing, was {Code(toolId)}");
            }

            switch (listing.Kind)
            {
                case ShopItemKind.Seed:
                    ValidateSeedListing(listing, label, cropsById, seededCropTypes, errors);
                    break;

                case ShopItemKind.Tool:
                    ValidateToolListing(listing, label, listedTools, errors);
                    break;

                case ShopItemKind.CanUpgrade:
                    if (listing.CropTypeId is { } cropTypeId)
                    {
                        errors.Add($"{ShopFileName}: {label}.cropTypeId must be null for a can_upgrade listing, was {Code(cropTypeId)}");
                    }

                    // BR-L5, BR-W4: the Can upgrade is bought once, and the menu drops its row by this flag.
                    if (!listing.OncePerGame)
                    {
                        errors.Add($"{ShopFileName}: {label}.oncePerGame must be true for a can_upgrade listing (BR-W4)");
                    }

                    if (++canUpgradeCount == 2)
                    {
                        errors.Add($"{ShopFileName}: {label}.kind can_upgrade is listed more than once");
                    }

                    break;
            }
        }
    }

    private static void ValidateToolListing(
        ShopListingDto listing,
        string label,
        HashSet<ToolId> listedTools,
        List<string> errors)
    {
        // BR-L4: the shop sells the axe and the pickaxe, each once per Game and listed once.
        if (listing.ToolId is not ({ } toolId and (ToolId.Axe or ToolId.Pickaxe)))
        {
            var was = listing.ToolId is { } other ? Code(other) : "null";
            errors.Add($"{ShopFileName}: {label}.toolId must be axe or pickaxe for a tool listing, was {was}");
        }
        else if (!listedTools.Add(toolId))
        {
            errors.Add($"{ShopFileName}: {label}.toolId {Code(toolId)} already has a tool listing");
        }

        if (listing.CropTypeId is { } cropTypeId)
        {
            errors.Add($"{ShopFileName}: {label}.cropTypeId must be null for a tool listing, was {Code(cropTypeId)}");
        }

        if (!listing.OncePerGame)
        {
            errors.Add($"{ShopFileName}: {label}.oncePerGame must be true for a tool listing (BR-L4)");
        }
    }

    private static void ValidateSeedListing(
        ShopListingDto listing,
        string label,
        Dictionary<CropTypeId, CropTypeDto>? cropsById,
        HashSet<CropTypeId> seededCropTypes,
        List<string> errors)
    {
        // BR-L1: seeds are sold every day of their Season, never once per Game.
        if (listing.OncePerGame)
        {
            errors.Add($"{ShopFileName}: {label}.oncePerGame must be false for a seed listing (BR-L1)");
        }

        if (listing.CropTypeId is not { } cropTypeId)
        {
            errors.Add($"{ShopFileName}: {label}.cropTypeId is required for a seed listing");
            return;
        }

        if (!seededCropTypes.Add(cropTypeId))
        {
            errors.Add($"{ShopFileName}: {label}.cropTypeId {Code(cropTypeId)} already has a seed listing");
        }

        if (cropsById is null)
        {
            return;
        }

        if (!cropsById.TryGetValue(cropTypeId, out var crop))
        {
            errors.Add($"{ShopFileName}: {label}.cropTypeId {Code(cropTypeId)} is not a crop type in {CropsFileName}");
        }
        else if (listing.Price != crop.SeedCost)
        {
            errors.Add($"{ShopFileName}: {label}.price must equal the seedCost of {Code(cropTypeId)} ({crop.SeedCost}), was {listing.Price}");
        }
    }

    private static void ValidateTuning(
        TuningDto tuning,
        Dictionary<CropTypeId, CropTypeDto>? cropsById,
        List<string> errors)
    {
        void Check(bool valid, string field, string rule, object value)
        {
            if (!valid)
            {
                errors.Add(string.Create(CultureInfo.InvariantCulture, $"{TuningFileName}: {field} must be {rule}, was {value}"));
            }
        }

        Check(tuning.StartingGold >= 0, "startingGold", "at least 0 (BR-P5)", tuning.StartingGold);
        Check(tuning.MaxEnergy > 0, "maxEnergy", "above 0", tuning.MaxEnergy);
        Check(tuning.MaxEnergy <= Player.MaxEnergy, "maxEnergy", $"at most {Player.MaxEnergy} (BR-P1)", tuning.MaxEnergy);
        Check(tuning.PassOutEnergy > 0, "passOutEnergy", "above 0", tuning.PassOutEnergy);
        Check(tuning.PassOutEnergy <= tuning.MaxEnergy, "passOutEnergy", $"at most maxEnergy ({tuning.MaxEnergy})", tuning.PassOutEnergy);
        Check(tuning.ToolEnergyCost > 0, "toolEnergyCost", "above 0", tuning.ToolEnergyCost);
        Check(tuning.CanCapacity > 0, "canCapacity", "above 0", tuning.CanCapacity);
        Check(tuning.CanUpgradedCapacity > tuning.CanCapacity, "canUpgradedCapacity", $"above canCapacity ({tuning.CanCapacity})", tuning.CanUpgradedCapacity);
        Check(tuning.DayStartMinutes >= 0, "dayStartMinutes", "at least 0", tuning.DayStartMinutes);
        Check(tuning.DayEndMinutes > tuning.DayStartMinutes, "dayEndMinutes", $"above dayStartMinutes ({tuning.DayStartMinutes})", tuning.DayEndMinutes);
        Check(tuning.DayLengthRealSeconds > 0, "dayLengthRealSeconds", "above 0", tuning.DayLengthRealSeconds);
        Check(tuning.RainChancePercent is >= 0 and <= 100, "rainChancePercent", "between 0 and 100", tuning.RainChancePercent);
        Check(tuning.SeasonLengthDays >= 1, "seasonLengthDays", "at least 1", tuning.SeasonLengthDays);
        Check(tuning.WalkSpeedTilesPerSecond > 0, "walkSpeedTilesPerSecond", "above 0", tuning.WalkSpeedTilesPerSecond);
        Check(tuning.RunSpeedTilesPerSecond >= tuning.WalkSpeedTilesPerSecond, "runSpeedTilesPerSecond", $"at least walkSpeedTilesPerSecond ({tuning.WalkSpeedTilesPerSecond.ToString(CultureInfo.InvariantCulture)})", tuning.RunSpeedTilesPerSecond);

        if (tuning.StartingSeeds is null)
        {
            errors.Add($"{TuningFileName}: startingSeeds must be a list, was null");
            return;
        }

        var stackedCropTypes = new HashSet<CropTypeId>();
        for (var index = 0; index < tuning.StartingSeeds.Count; index++)
        {
            var stack = tuning.StartingSeeds[index];
            var field = $"startingSeeds[{index}]";
            if (stack is null)
            {
                errors.Add($"{TuningFileName}: {field} must be an object, was null");
                continue;
            }

            var cropTypeId = Code(stack.CropTypeId);
            Check(stack.Quantity >= 1, $"{field}.quantity", "at least 1 (BR-P7)", stack.Quantity);

            if (!stackedCropTypes.Add(stack.CropTypeId))
            {
                errors.Add($"{TuningFileName}: {field}.cropTypeId {cropTypeId} already has a starting stack (BR-P7)");
            }

            if (cropsById is not null && !cropsById.ContainsKey(stack.CropTypeId))
            {
                errors.Add($"{TuningFileName}: {field}.cropTypeId {cropTypeId} is not a crop type in {CropsFileName}");
            }
        }
    }

    private static CropType ToCropType(CropTypeDto dto) => new(
        dto.Id,
        dto.Name,
        dto.GrowDays,
        new Gold(dto.SeedCost),
        new Gold(dto.SellPrice),
        dto.Season,
        dto.Regrows);

    private static ShopListing ToShopListing(ShopListingDto dto) =>
        new(dto.Id, dto.Kind, dto.CropTypeId, new Gold(dto.Price), dto.Vendor, dto.RequiresStage, dto.ToolId, dto.OncePerGame);

    private static GameTuning ToGameTuning(TuningDto dto) => new()
    {
        StartingGold = dto.StartingGold,
        StartingSeeds = dto.StartingSeeds.Select(stack => new StartingStack(stack.CropTypeId, stack.Quantity)).ToList(),
        MaxEnergy = dto.MaxEnergy,
        PassOutEnergy = dto.PassOutEnergy,
        ToolEnergyCost = dto.ToolEnergyCost,
        CanCapacity = dto.CanCapacity,
        CanUpgradedCapacity = dto.CanUpgradedCapacity,
        DayStartMinutes = dto.DayStartMinutes,
        DayEndMinutes = dto.DayEndMinutes,
        DayLengthRealSeconds = dto.DayLengthRealSeconds,
        RainChancePercent = dto.RainChancePercent,
        SeasonLengthDays = dto.SeasonLengthDays,
        WalkSpeedTilesPerSecond = dto.WalkSpeedTilesPerSecond,
        RunSpeedTilesPerSecond = dto.RunSpeedTilesPerSecond,
    };

    /// <summary>The canonical snake_case code of an enum value, as it appears in the data files.</summary>
    private static string Code<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
}
