using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LongWayHome.Application.Data;
using LongWayHome.Application.Ports;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Infrastructure.Data.Dtos;

namespace LongWayHome.Infrastructure.Data;

/// <summary>
/// Parses and validates <c>crops.json</c>, <c>shop.json</c>, <c>tuning.json</c>, <c>nodes.json</c>,
/// <c>materials.json</c> and <c>buildings.json</c>.
/// Takes the file contents as strings, not paths: in an exported build <c>res://</c> files live inside the
/// <c>.pck</c>, which <c>System.IO</c> can't open, so the Godot layer reads them with <c>Godot.FileAccess</c>.
/// </summary>
/// <remarks>
/// Every parse and validation error across the files is collected before a single
/// <see cref="GameDataException"/> is thrown. When <c>crops.json</c> doesn't parse, the checks in the other
/// files that refer to crop types are skipped, since there is nothing to check them against.
/// </remarks>
public sealed class JsonGameDataSource : IGameDataSource
{
    public const string CropsFileName = "crops.json";
    public const string ShopFileName = "shop.json";
    public const string TuningFileName = "tuning.json";
    public const string NodesFileName = "nodes.json";
    public const string MaterialsFileName = "materials.json";
    public const string BuildingsFileName = "buildings.json";

    private readonly string _cropsJson;
    private readonly string _shopJson;
    private readonly string _tuningJson;
    private readonly string _nodesJson;
    private readonly string _materialsJson;
    private readonly string _buildingsJson;

    /// <summary>The four files without <c>materials.json</c>: <see cref="GameData.MaterialTypes"/> loads empty.</summary>
    public JsonGameDataSource(string cropsJson, string shopJson, string tuningJson, string nodesJson)
        : this(cropsJson, shopJson, tuningJson, nodesJson, NoMaterials)
    {
    }

    /// <summary>
    /// The five files without <c>buildings.json</c> (#152): <see cref="GameData.BuildingStages"/> loads empty.
    /// </summary>
    public JsonGameDataSource(string cropsJson, string shopJson, string tuningJson, string nodesJson, string materialsJson)
        : this(cropsJson, shopJson, tuningJson, nodesJson, materialsJson, NoBuildings)
    {
    }

    /// <summary>All six data files, <c>buildings.json</c> included (#163).</summary>
    public JsonGameDataSource(
        string cropsJson, string shopJson, string tuningJson, string nodesJson, string materialsJson, string buildingsJson)
    {
        ArgumentNullException.ThrowIfNull(cropsJson);
        ArgumentNullException.ThrowIfNull(shopJson);
        ArgumentNullException.ThrowIfNull(tuningJson);
        ArgumentNullException.ThrowIfNull(nodesJson);
        ArgumentNullException.ThrowIfNull(materialsJson);
        ArgumentNullException.ThrowIfNull(buildingsJson);
        _cropsJson = cropsJson;
        _shopJson = shopJson;
        _tuningJson = tuningJson;
        _nodesJson = nodesJson;
        _materialsJson = materialsJson;
        _buildingsJson = buildingsJson;
    }

    private const string NoMaterials = """{ "materialTypes": [] }""";

    private const string NoBuildings = """{ "stages": [] }""";

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
        var nodesFile = Parse<NodesFileDto>(_nodesJson, NodesFileName, errors);
        var materialsFile = Parse<MaterialsFileDto>(_materialsJson, MaterialsFileName, errors);
        var buildingsFile = Parse<BuildingsFileDto>(_buildingsJson, BuildingsFileName, errors);

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

        if (nodesFile is not null)
        {
            ValidateNodes(nodesFile, errors);
        }

        if (materialsFile is not null)
        {
            ValidateMaterials(materialsFile, errors);
        }

        if (buildingsFile is not null)
        {
            ValidateBuildings(buildingsFile, errors);
        }

        if (errors.Count > 0)
        {
            throw new GameDataException(errors);
        }

        return new GameData(
            cropsFile!.CropTypes.ToDictionary(dto => dto.Id, ToCropType),
            shopFile!.Listings.Select(ToShopListing).ToList(),
            ToGameTuning(tuningFile!))
        {
            NodeSpawns = nodesFile!.Spawns.Select(ToNodeSpawn).ToList().AsReadOnly(),
            MaterialTypes = materialsFile!.MaterialTypes
                .ToDictionary(dto => dto.Id, dto => new MaterialType(dto.Id, dto.Name, dto.MaxStack))
                .AsReadOnly(),
            BuildingStages = buildingsFile!.Stages.Select(dto => ToBuildingStage(dto!)).ToList().AsReadOnly(),
        };
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
        ValidateSwingTuning(tuning, errors);
        if (tuning.NodeRegrowthNights is { } regrowthNights)
        {
            Check(regrowthNights >= 1, "nodeRegrowthNights", "at least 1 (BR-N4)", regrowthNights);
        }

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

    private static void ValidateMaterials(MaterialsFileDto file, List<string> errors)
    {
        if (file.MaterialTypes is null)
        {
            errors.Add($"{MaterialsFileName}: materialTypes must be a list, was null");
            return;
        }

        var ids = new HashSet<MaterialId>();
        for (var index = 0; index < file.MaterialTypes.Count; index++)
        {
            var material = file.MaterialTypes[index];
            if (material is null)
            {
                errors.Add($"{MaterialsFileName}: materialTypes[{index}] must be an object, was null");
                continue;
            }

            var id = Code(material.Id);
            if (!ids.Add(material.Id))
            {
                errors.Add($"{MaterialsFileName}: {id}.id is used by more than one material type");
            }

            if (string.IsNullOrWhiteSpace(material.Name))
            {
                errors.Add($"{MaterialsFileName}: {id}.name must not be empty");
            }

            // BR-K2: a Slot holds 1 to 99 of one item, so every material stacks to exactly 99.
            if (material.MaxStack != Backpack.MaxStack)
            {
                errors.Add($"{MaterialsFileName}: {id}.maxStack must be {Backpack.MaxStack} (BR-K2), was {material.MaxStack}");
            }
        }
    }

    // BR-B1: each Building's rows run one stage at a time from the stage after its new-Game stage, with known ids,
    // quantities of at least 1 and gold of at least 0.
    private static void ValidateBuildings(BuildingsFileDto file, List<string> errors)
    {
        if (file.Stages is null)
        {
            errors.Add($"{BuildingsFileName}: stages must be a list, was null");
            return;
        }

        var stagesByBuilding = new Dictionary<BuildingId, SortedSet<int>>();
        for (var index = 0; index < file.Stages.Count; index++)
        {
            var row = file.Stages[index];
            if (row is null)
            {
                errors.Add($"{BuildingsFileName}: stages[{index}] must be an object, was null");
                continue;
            }

            var label = $"stages[{index}] ({Code(row.BuildingId)} {row.Stage})";
            if (!Enum.IsDefined(row.BuildingId))
            {
                errors.Add($"{BuildingsFileName}: {label}.buildingId is not a known Building");
                continue;
            }

            if (row.Stage < 1)
            {
                errors.Add($"{BuildingsFileName}: {label}.stage must be at least 1, was {row.Stage}");
            }
            else if (!(stagesByBuilding.TryAdd(row.BuildingId, [row.Stage]) || stagesByBuilding[row.BuildingId].Add(row.Stage)))
            {
                errors.Add($"{BuildingsFileName}: {Code(row.BuildingId)} stage {row.Stage} has more than one row");
            }

            if (row.CostGold < 0)
            {
                errors.Add($"{BuildingsFileName}: {label}.costGold must be at least 0, was {row.CostGold}");
            }

            if (row.Unlocks is null)
            {
                errors.Add($"{BuildingsFileName}: {label}.unlocks must be text, was null");
            }

            ValidateCostItems(row, label, errors);
        }

        // BR-B1: the rows reach start + 1, start + 2, ... with no gap, the start being the new-Game stage.
        var defaults = Game.DefaultBuildings();
        foreach (var (building, stages) in stagesByBuilding)
        {
            var start = defaults[(int)building].Stage;
            var expected = Enumerable.Range(start + 1, stages.Count);
            if (!stages.SequenceEqual(expected))
            {
                errors.Add(
                    $"{BuildingsFileName}: {Code(building)} stages must run one at a time from {start + 1} (BR-B1), " +
                    $"were {string.Join(", ", stages)}");
            }
        }
    }

    private static void ValidateCostItems(BuildingStageDto row, string label, List<string> errors)
    {
        if (row.CostItems is null)
        {
            errors.Add($"{BuildingsFileName}: {label}.costItems must be a list, was null");
            return;
        }

        var materials = new HashSet<MaterialId>();
        for (var index = 0; index < row.CostItems.Count; index++)
        {
            var item = row.CostItems[index];
            if (item is null)
            {
                errors.Add($"{BuildingsFileName}: {label}.costItems[{index}] must be an object, was null");
                continue;
            }

            if (!Enum.IsDefined(item.MaterialId))
            {
                errors.Add($"{BuildingsFileName}: {label}.costItems[{index}].materialId is not a known material");
            }
            else if (!materials.Add(item.MaterialId))
            {
                errors.Add($"{BuildingsFileName}: {label}.costItems lists {Code(item.MaterialId)} more than once");
            }

            if (item.Quantity < 1)
            {
                errors.Add(
                    $"{BuildingsFileName}: {label}.costItems[{index}].quantity must be at least 1, was {item.Quantity}");
            }
        }
    }

    // BR-N2, BR-N3: optional in the file; when present every tier has a count of at least 1 and every yield stack at
    // least 1 of a material, keyed by canonical codes.
    private static void ValidateSwingTuning(TuningDto tuning, List<string> errors)
    {
        if (tuning.SwingsToClear is { } swingsToClear)
        {
            foreach (var (key, count) in swingsToClear)
            {
                if (!TryParseCode<ToolTier>(key, out _))
                {
                    errors.Add($"{TuningFileName}: swingsToClear.{key} is not a tool tier");
                }
                else if (count < 1)
                {
                    errors.Add($"{TuningFileName}: swingsToClear.{key} must be at least 1 (BR-N2), was {count}");
                }
            }

            foreach (var tier in Enum.GetValues<ToolTier>().Where(tier => !swingsToClear.ContainsKey(Code(tier))))
            {
                errors.Add($"{TuningFileName}: swingsToClear.{Code(tier)} is missing (BR-N2)");
            }
        }

        if (tuning.NodeYields is { } nodeYields)
        {
            foreach (var (key, stacks) in nodeYields)
            {
                if (!TryParseCode<NodeKind>(key, out _))
                {
                    errors.Add($"{TuningFileName}: nodeYields.{key} is not a node kind");
                    continue;
                }

                if (stacks is null)
                {
                    errors.Add($"{TuningFileName}: nodeYields.{key} must be a list, was null");
                    continue;
                }

                for (var index = 0; index < stacks.Count; index++)
                {
                    if (stacks[index] is not { } stack)
                    {
                        errors.Add($"{TuningFileName}: nodeYields.{key}[{index}] must be an object, was null");
                    }
                    else if (stack.Quantity < 1)
                    {
                        errors.Add($"{TuningFileName}: nodeYields.{key}[{index}].quantity must be at least 1 (BR-N3), was {stack.Quantity}");
                    }
                }
            }
        }
    }

    private static void ValidateNodes(NodesFileDto file, List<string> errors)
    {
        if (file.Spawns is null)
        {
            errors.Add($"{NodesFileName}: spawns must be a list, was null");
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var idsByTile = new Dictionary<(AreaId, int?, int, int), string>();

        for (var index = 0; index < file.Spawns.Count; index++)
        {
            var spawn = file.Spawns[index];
            if (spawn is null)
            {
                errors.Add($"{NodesFileName}: spawns[{index}] must be an object, was null");
                continue;
            }

            string label;
            if (string.IsNullOrWhiteSpace(spawn.Id))
            {
                label = $"spawns[{index}]";
                errors.Add($"{NodesFileName}: {label}.id must not be empty");
            }
            else
            {
                label = spawn.Id;
                if (!ids.Add(spawn.Id))
                {
                    errors.Add($"{NodesFileName}: {label}.id is used by more than one spawn point");
                }
            }

            var onGrid = true;
            void CheckCoordinate(string field, int value, int size)
            {
                if (value < 0 || value >= size)
                {
                    onGrid = false;
                    errors.Add($"{NodesFileName}: {label}.{field} must be between 0 and {size - 1}, was {value}");
                }
            }

            // A spawn point lies on its Area's grid (#154): the farm's 72 x 36 (BR-T1), the forest path's 14 x 40.
            var (width, height) = AreaGrid.Size(spawn.Area);
            CheckCoordinate("x", spawn.X, width);
            CheckCoordinate("y", spawn.Y, height);

            if (onGrid && !idsByTile.TryAdd((spawn.Area, spawn.MineLevel, spawn.X, spawn.Y), label))
            {
                var other = idsByTile[(spawn.Area, spawn.MineLevel, spawn.X, spawn.Y)];
                errors.Add($"{NodesFileName}: {label} is on the same Tile ({spawn.X}, {spawn.Y}) as {other}");
            }

            // BR-N5: stumps and debris never grow back.
            if (spawn.Kind is NodeKind.Stump or NodeKind.Debris && spawn.Respawns)
            {
                var kindName = spawn.Kind == NodeKind.Stump ? "a stump" : "debris";
                errors.Add($"{NodesFileName}: {label}.respawns must be false for {kindName} (BR-N5)");
            }
        }
    }

    private static NodeSpawn ToNodeSpawn(NodeSpawnDto dto) =>
        new(dto.Id, dto.Area, dto.MineLevel, AreaGrid.Position(dto.Area, dto.X, dto.Y), dto.Kind, dto.AtStart, dto.Respawns);

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
        SwingsToClear = (dto.SwingsToClear ?? new Dictionary<string, int>())
            .ToDictionary(pair => ParseCode<ToolTier>(pair.Key), pair => pair.Value)
            .AsReadOnly(),
        NodeYields = (dto.NodeYields ?? new Dictionary<string, IReadOnlyList<MaterialStackDto>>())
            .ToDictionary(
                pair => ParseCode<NodeKind>(pair.Key),
                pair => (IReadOnlyList<MaterialStack>)pair.Value
                    .Select(stack => new MaterialStack(stack.Material, stack.Quantity))
                    .ToList()
                    .AsReadOnly())
            .AsReadOnly(),
        NodeRegrowthNights = dto.NodeRegrowthNights ?? GameTuning.DefaultNodeRegrowthNights,
    };

    private static BuildingStage ToBuildingStage(BuildingStageDto dto) => new(
        dto.BuildingId,
        dto.Stage,
        dto.CostItems.Select(item => new MaterialStack(item!.MaterialId, item.Quantity)).ToList().AsReadOnly(),
        new Gold(dto.CostGold),
        dto.Unlocks);

    // The enum value whose canonical code is <paramref name="code"/>; validation has already refused unknown codes.

    private static TEnum ParseCode<TEnum>(string code)
        where TEnum : struct, Enum =>
        TryParseCode<TEnum>(code, out var value) ? value : throw new InvalidOperationException($"Unknown code '{code}'.");

    private static bool TryParseCode<TEnum>(string code, out TEnum value)
        where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (string.Equals(Code(candidate), code, StringComparison.Ordinal))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>The canonical snake_case code of an enum value, as it appears in the data files.</summary>
    private static string Code<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
}
