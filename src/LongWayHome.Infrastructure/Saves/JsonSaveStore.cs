using System.Text.Json;
using System.Text.Json.Nodes;
using LongWayHome.Application.Games;
using LongWayHome.Application.Ports;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Residents;
using LongWayHome.Domain.Shipping;
using LongWayHome.Infrastructure.Data;
using LongWayHome.Infrastructure.Saves.Dtos;
using LongWayHome.Infrastructure.Saves.Migrations;

namespace LongWayHome.Infrastructure.Saves;

/// <summary>
/// Saves each slot as versioned JSON in <c>&lt;folder&gt;/slot-&lt;n&gt;.json</c>, written through a <c>.tmp</c> file and
/// keeping the previous file as <c>.bak</c>, so a crash mid-save never corrupts the slot (blueprint §3, §9).
/// </summary>
/// <remarks>
/// Uses <see cref="JsonGameDataSource.SerializerOptions"/>, so every enum is written as its canonical code and an
/// integer enum is refused. Loading rebuilds the Game through the domain constructors, so their checks (BR-T1, BR-K1,
/// BR-K2, BR-K6, BR-W1, BR-G1) make a file that breaks a rule unreadable rather than loading a broken Game.
/// </remarks>
public sealed class JsonSaveStore : ISaveStore
{
    private const string BackupSuffix = ".bak";
    private const string TempSuffix = ".tmp";

    /// <param name="folder">Where the slot files live.</param>
    /// <param name="nodeSpawns">
    /// The spawn points from <c>nodes.json</c>: a saved node is placed on its spawn point's Tile, a node whose spawn
    /// point is gone is dropped, and a file from before #151 gets the atStart nodes outside the old plot's block.
    /// </param>
    public JsonSaveStore(string folder, IReadOnlyList<NodeSpawn>? nodeSpawns = null)
    {
        ArgumentNullException.ThrowIfNull(folder);
        Folder = folder;
        _spawnsById = (nodeSpawns ?? []).ToDictionary(spawn => spawn.Id, StringComparer.Ordinal);
        _legacyNodeSpawns = (nodeSpawns ?? [])
            .Where(spawn => spawn.AtStart && !(spawn.Area == AreaId.Farm && FarmPlot.Phase1Block.Contains(spawn.Position)))
            .ToList()
            .AsReadOnly();
    }

    public string Folder { get; }

    private readonly Dictionary<string, NodeSpawn> _spawnsById;

    /// <summary>The spawn points a file without <c>resourceNodes</c> gets a node at (AC8).</summary>
    private readonly IReadOnlyList<NodeSpawn> _legacyNodeSpawns;

    public IReadOnlyList<SaveSlotInfo> List()
    {
        var slots = new List<SaveSlotInfo>(SaveSlots.Count);
        for (var slot = 1; slot <= SaveSlots.Count; slot++)
        {
            slots.Add(Describe(slot));
        }

        return slots.AsReadOnly();
    }

    /// <summary>
    /// Loads the slot's file, or its <c>.bak</c> when the file is missing or unreadable. A stray <c>.tmp</c> is
    /// ignored. Throws <see cref="SaveStoreException"/> when the slot is empty or neither file is readable.
    /// </summary>
    public Game Load(int slot)
    {
        SaveSlots.ThrowIfOutOfRange(slot);

        var path = SlotPath(slot);
        var backup = path + BackupSuffix;
        if (!File.Exists(path) && !File.Exists(backup))
        {
            throw new SaveStoreException(slot, "the slot is empty");
        }

        if (TryRead(path, out var game, out var failure))
        {
            return game;
        }

        if (TryRead(backup, out game, out var backupFailure))
        {
            return game;
        }

        throw new SaveStoreException(
            slot, $"{Path.GetFileName(path)}: {failure}; {Path.GetFileName(backup)}: {backupFailure}");
    }

    /// <summary>
    /// Writes the Game to <c>slot-&lt;n&gt;.json.tmp</c>, flushes it to disk, then replaces <c>slot-&lt;n&gt;.json</c> with it,
    /// keeping the previous file as <c>.bak</c>. Creates the folder when it is missing.
    /// </summary>
    public void Save(int slot, Game game)
    {
        SaveSlots.ThrowIfOutOfRange(slot);
        ArgumentNullException.ThrowIfNull(game);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(ToDto(game), JsonGameDataSource.SerializerOptions);

        Directory.CreateDirectory(Folder);
        var path = SlotPath(slot);
        var temp = path + TempSuffix;
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);

            // Flushed through to the disk before the rename, so the renamed file is never partial.
            stream.Flush(flushToDisk: true);
        }

        if (File.Exists(path))
        {
            File.Replace(temp, path, path + BackupSuffix);
        }
        else
        {
            File.Move(temp, path);
        }
    }

    private SaveSlotInfo Describe(int slot)
    {
        var path = SlotPath(slot);
        if (!File.Exists(path) && !File.Exists(path + BackupSuffix))
        {
            return new SaveSlotInfo(slot, SaveSlotState.Empty, null, null, null);
        }

        try
        {
            var game = Load(slot);
            return new SaveSlotInfo(slot, SaveSlotState.Saved, game.Date, game.Player.Gold.Value, game.SavedAt);
        }
        catch (SaveStoreException)
        {
            return new SaveSlotInfo(slot, SaveSlotState.Unreadable, null, null, null);
        }
    }

    /// <summary>The slot's file path; the slot must already be checked to be in range.</summary>
    private string SlotPath(int slot) => Path.Combine(Folder, $"slot-{slot}.json");

    /// <summary>Reads, migrates and rebuilds one file; false with a one-line reason when any step fails.</summary>
    private bool TryRead(string path, out Game game, out string failure)
    {
        game = null!;
        try
        {
            if (!File.Exists(path))
            {
                failure = "missing";
                return false;
            }

            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject document)
            {
                failure = "not a JSON object";
                return false;
            }

            // #150: a version 2 file written on the 12 x 9 plot places its Tiles in the grown grid, like a v1 file.
            var migrated = Phase1PlotPlacement.Apply(SaveMigrator.Default.Migrate(document));
            var dto = migrated.Deserialize<SaveFileDto>(JsonGameDataSource.SerializerOptions)
                ?? throw new InvalidDataException("the file contains null");
            game = ToGame(dto);
            failure = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException
                                              or FormatException or OverflowException or IOException
                                              or UnauthorizedAccessException or InvalidOperationException)
        {
            failure = OneLine(exception.Message);
            return false;
        }
    }

    private static string OneLine(string message) =>
        message.ReplaceLineEndings(" ").Trim();

    private static SaveFileDto ToDto(Game game) => new()
    {
        SaveVersion = SaveMigrator.CurrentVersion,
        Id = game.Id,
        Date = ToDto(game.Date),
        Clock = game.ClockMinutes,
        Weather = game.Weather,
        LastDayEnd = game.LastDayEnd,
        CreatedAt = game.CreatedAt,
        SavedAt = game.SavedAt,
        Player = new PlayerDto
        {
            Gold = game.Player.Gold.Value,
            Energy = game.Player.Energy,
            WateringCan = new WateringCanDto
            {
                Capacity = game.Player.WateringCan.Capacity,
                Charges = game.Player.WateringCan.Charges,
                Upgraded = game.Player.WateringCan.Upgraded,
            },
            Backpack = ToDto(game.Player.Backpack),
        },
        FarmPlot = new FarmPlotDto
        {
            Width = FarmPlot.Width,
            Height = FarmPlot.Height,
            Tiles = game.FarmPlot.Tiles
                .Select(tile => new TileDto
                {
                    Position = new GridPositionDto { X = tile.Position.X, Y = tile.Position.Y },
                    State = tile.State,
                    Watered = tile.Watered,
                    Planting = tile.Planting is not { } planting
                        ? null
                        : new PlantingDto
                        {
                            CropTypeId = planting.CropTypeId,
                            PlantedOn = ToDto(planting.PlantedOn),
                            DaysGrown = planting.DaysGrown,
                        },
                })
                .ToList(),
        },
        ShippingBin = new ShippingBinDto
        {
            Items = game.ShippingBin.Items
                .Select(item => new ShippedItemDto { CropTypeId = item.CropTypeId, Quantity = item.Quantity })
                .ToList(),
        },
        Residents = game.Residents
            .Select(resident => new ResidentDto { Id = ResidentCodes.Code(resident.Id), Present = resident.Present })
            .ToList(),
        OpeningDone = game.OpeningDone,
        PurchasedListings = game.PurchasedListings.Order(StringComparer.Ordinal).ToList(),
        ResourceNodes = game.ResourceNodes
            .Select(node => new ResourceNodeDto { SpawnId = node.SpawnId, Kind = node.Kind, SwingsTaken = node.SwingsTaken })
            .ToList(),
        NodeRegrowths = game.NodeRegrowths
            .Select(regrowth => new NodeRegrowthDto { SpawnId = regrowth.SpawnId, ClearedOn = ToDto(regrowth.ClearedOn) })
            .ToList(),
    };

    /// <summary>Every Slot is written, an empty one with a null item, quantity 0 and a null tier.</summary>
    private static BackpackDto ToDto(Backpack backpack) => new()
    {
        Capacity = backpack.Capacity,
        HotbarRow = backpack.HotbarRow,
        Slots = backpack.Slots
            .Select(slot => new BackpackSlotDto
            {
                Index = slot.Index,
                Item = slot.Item is not { } item
                    ? null
                    : ToItemRefDto(item),
                Quantity = slot.Quantity,
                ToolTier = slot.ToolTier,
            })
            .ToList(),
    };

    private static GameDateDto ToDto(GameDate date) => new() { Year = date.Year, Season = date.Season, Day = date.Day };

    /// <summary>
    /// Rebuilds the Game through the domain constructors, whose checks throw <see cref="ArgumentException"/> on a
    /// value that breaks a rule. JSON nulls in place of required objects and undefined enum values are refused here.
    /// </summary>
    private Game ToGame(SaveFileDto dto)
    {
        if (dto.Clock < 0)
        {
            throw new InvalidDataException($"clock must be at least 0, was {dto.Clock}.");
        }

        var player = Required(dto.Player, "player");
        var farmPlot = Required(dto.FarmPlot, "farmPlot");
        var shippingBin = Required(dto.ShippingBin, "shippingBin");
        var plot = ToFarmPlot(farmPlot);

        return new Game(
            dto.Id,
            ToGameDate(Required(dto.Date, "date"), "date"),
            dto.Clock,
            Defined(dto.Weather, "weather"),
            dto.LastDayEnd is { } lastDayEnd ? Defined(lastDayEnd, "lastDayEnd") : null,
            dto.CreatedAt,
            dto.SavedAt,
            ToPlayer(player),
            plot,
            ToShippingBin(shippingBin),
            ToResidents(dto.Residents),
            openingDone: dto.OpeningDone ?? true, // BR-I1: a save from before #139 is past the Opening
            purchasedListings: ToPurchasedListings(dto.PurchasedListings),
            resourceNodes: ToResourceNodes(dto.ResourceNodes, plot),
            nodeRegrowths: ToNodeRegrowths(dto.NodeRegrowths));
    }

    /// <summary>
    /// No <c>nodeRegrowths</c> (a version 2 file from before #153) is none waiting. An entry whose spawn point isn't in
    /// <c>nodes.json</c> is dropped, as it can never grow back. A null entry or spawn id, or a null or invalid
    /// <c>clearedOn</c>, is refused; the Game constructor refuses a repeat.
    /// </summary>
    private List<NodeRegrowth> ToNodeRegrowths(IReadOnlyList<NodeRegrowthDto?>? regrowths)
    {
        var restored = new List<NodeRegrowth>();
        foreach (var entry in regrowths ?? [])
        {
            var regrowth = Required(entry, "nodeRegrowths[]");
            var spawnId = Required(regrowth.SpawnId, "nodeRegrowths[].spawnId");
            var clearedOn = ToGameDate(
                Required(regrowth.ClearedOn, "nodeRegrowths[].clearedOn"), "nodeRegrowths[].clearedOn");
            if (_spawnsById.ContainsKey(spawnId))
            {
                restored.Add(new NodeRegrowth(spawnId, clearedOn));
            }
        }

        return restored;
    }

    /// <summary>
    /// No <c>resourceNodes</c> (a version 1 file, or a version 2 file from before #151) is a node at every atStart
    /// spawn point outside the old plot's block, so nothing lands on the Phase 1 crops (AC8), and none on a farm Tile
    /// that isn't grass, since a version 2 file from #150 may hold soil tilled anywhere on the meadow. Otherwise each entry
    /// is placed on its spawn point's Tile; one whose spawn point isn't in <c>nodes.json</c> is dropped (AC7). A null
    /// entry or spawn id, an undefined kind or negative swings are refused; the Game constructor refuses a repeat.
    /// </summary>
    private IReadOnlyList<ResourceNode> ToResourceNodes(IReadOnlyList<ResourceNodeDto?>? nodes, FarmPlot farmPlot)
    {
        if (nodes is null)
        {
            return _legacyNodeSpawns
                .Where(spawn => spawn.Area != AreaId.Farm || farmPlot[spawn.Position].State == TileState.Grass)
                .Select(ResourceNode.FromSpawn)
                .ToList();
        }

        var restored = new List<ResourceNode>(nodes.Count);
        foreach (var entry in nodes)
        {
            var node = Required(entry, "resourceNodes[]");
            var spawnId = Required(node.SpawnId, "resourceNodes[].spawnId");
            var kind = Defined(node.Kind, "resourceNodes[].kind");
            if (node.SwingsTaken < 0)
            {
                throw new InvalidDataException(
                    $"resourceNodes[].swingsTaken must be at least 0, was {node.SwingsTaken}.");
            }

            if (_spawnsById.TryGetValue(spawnId, out var spawn))
            {
                restored.Add(new ResourceNode(spawnId, kind, spawn.Area, spawn.Position, node.SwingsTaken, spawn.Respawns));
            }
        }

        return restored;
    }

    /// <summary>
    /// No <c>purchasedListings</c> (a version 2 file from before #145) is none bought (BR-L5). A null entry is refused;
    /// the Game constructor refuses a blank one.
    /// </summary>
    private static HashSet<string> ToPurchasedListings(IReadOnlyList<string?>? purchased) =>
        purchased is null
            ? []
            : purchased.Select(id => Required(id, "purchasedListings[]")).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// No <c>residents</c> (a version 2 file from before #138) is the new-Game default (BR-R1). Otherwise each entry
    /// must name a known Resident by its exact code; the Game constructor refuses a missing, repeated or out-of-order
    /// one.
    /// </summary>
    private static IReadOnlyList<Resident>? ToResidents(IReadOnlyList<ResidentDto?>? residents)
    {
        if (residents is null)
        {
            return null;
        }

        var restored = new List<Resident>(residents.Count);
        foreach (var entry in residents)
        {
            var resident = Required(entry, "residents[]");
            if (!ResidentCodes.TryParse(resident.Id, out var id))
            {
                throw new InvalidDataException($"residents[].id '{resident.Id}' is not a known ResidentId code.");
            }

            restored.Add(new Resident(id, resident.Present));
        }

        return restored;
    }

    private static Player ToPlayer(PlayerDto dto)
    {
        var can = Required(dto.WateringCan, "player.wateringCan");

        return new Player(
            new Gold(dto.Gold),
            dto.Energy,
            new WateringCan(can.Capacity, can.Charges, can.Upgraded),
            ToBackpack(Required(dto.Backpack, "player.backpack")));
    }

    /// <summary>
    /// Rebuilds the Backpack through its restore constructor, which refuses a capacity other than 12/24/36 (BR-K1), a
    /// Slot count or index set that doesn't match it, a Slot that breaks BR-K2, a hotbar row out of range, and a
    /// missing or repeated hoe or can (BR-K6). The message names <c>player.backpack</c>.
    /// </summary>
    private static Backpack ToBackpack(BackpackDto dto)
    {
        var slots = Required(dto.Slots, "player.backpack.slots")
            .Select(slot => ToSlot(Required(slot, "player.backpack.slots[]")))
            .ToList();
        try
        {
            return new Backpack(dto.Capacity, dto.HotbarRow, slots);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"player.backpack is invalid: {exception.Message}", exception);
        }
    }

    private static BackpackSlot ToSlot(BackpackSlotDto dto)
    {
        var field = $"player.backpack.slots[{dto.Index}]";
        var tier = dto.ToolTier is { } toolTier ? Defined(toolTier, field + ".toolTier") : (ToolTier?)null;
        if (dto.Item is not { } item)
        {
            return new BackpackSlot(dto.Index, null, dto.Quantity, tier);
        }

        var kind = Defined(item.Kind, field + ".item.kind");
        return new BackpackSlot(dto.Index, ToItemRef(item, kind, field + ".item"), dto.Quantity, tier);
    }

    // A material carries its code in materialId and no id; every other kind carries its code in id and no materialId.
    private static ItemRef ToItemRef(ItemRefDto item, ItemKind kind, string field)
    {
        if (kind == ItemKind.Material)
        {
            if (item.Id is not null)
            {
                throw new InvalidDataException($"{field}.id must be absent for a material, was '{item.Id}'.");
            }

            return ItemRef.Material(
                FromCode<MaterialId>(Required(item.MaterialId, field + ".materialId"), field + ".materialId"));
        }

        if (item.MaterialId is not null)
        {
            throw new InvalidDataException($"{field}.materialId must be absent for a {Code(kind)}, was '{item.MaterialId}'.");
        }

        var id = Required(item.Id, field + ".id");
        return kind == ItemKind.Tool
            ? ItemRef.Tool(FromCode<ToolId>(id, field + ".id"))
            : new ItemRef(kind, FromCode<CropTypeId>(id, field + ".id"), null);
    }

    private static ItemRefDto ToItemRefDto(ItemRef item) => item.Kind switch
    {
        ItemKind.Material => new ItemRefDto { Kind = item.Kind, MaterialId = Code(item.MaterialId!.Value) },
        ItemKind.Tool => new ItemRefDto { Kind = item.Kind, Id = Code(item.ToolId!.Value) },
        _ => new ItemRefDto { Kind = item.Kind, Id = Code(item.CropTypeId!.Value) },
    };

    private static FarmPlot ToFarmPlot(FarmPlotDto dto)
    {
        if (dto.Width != FarmPlot.Width || dto.Height != FarmPlot.Height)
        {
            throw new InvalidDataException(
                $"farmPlot must be {FarmPlot.Width} x {FarmPlot.Height} (BR-T1), was {dto.Width} x {dto.Height}.");
        }

        // BR-T1: the FarmPlot constructor refuses a missing or duplicated grid position.
        return new FarmPlot(Required(dto.Tiles, "farmPlot.tiles").Select(ToTile));
    }

    private static Tile ToTile(TileDto? dto)
    {
        var tile = Required(dto, "farmPlot.tiles[]");
        var position = Required(tile.Position, "farmPlot.tiles[].position");
        var planting = tile.Planting is { } saved
            ? new Planting(
                Defined(saved.CropTypeId, "planting.cropTypeId"),
                ToGameDate(Required(saved.PlantedOn, "planting.plantedOn"), "planting.plantedOn"),
                saved.DaysGrown)
            : null;

        return new Tile(
            new GridPosition(position.X, position.Y),
            Defined(tile.State, "farmPlot.tiles[].state"),
            tile.Watered,
            planting);
    }

    private static ShippingBin ToShippingBin(ShippingBinDto dto)
    {
        var bin = new ShippingBin();
        foreach (var entry in Required(dto.Items, "shippingBin.items"))
        {
            var item = Required(entry, "shippingBin.items[]");
            var cropTypeId = Defined(item.CropTypeId, "shippingBin.items[].cropTypeId");
            if (bin.Items.Any(held => held.CropTypeId == cropTypeId))
            {
                throw new InvalidDataException($"shippingBin.items has more than one item of {cropTypeId}.");
            }

            bin.Add(cropTypeId, item.Quantity);
        }

        return bin;
    }

    private static GameDate ToGameDate(GameDateDto dto, string field) =>
        new(dto.Year, Defined(dto.Season, field + ".season"), dto.Day);

    /// <summary>The canonical snake_case code of an enum value, as the serializer writes it.</summary>
    private static string Code<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    /// <summary>The enum value whose canonical code is <paramref name="code"/>; any other code is unreadable.</summary>
    private static TEnum FromCode<TEnum>(string code, string field)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Cast<TEnum?>().FirstOrDefault(value => Code(value!.Value) == code)
            ?? throw new InvalidDataException($"{field} '{code}' is not a known {typeof(TEnum).Name} code.");

    /// <summary>A JSON null where an object or list is required is unreadable.</summary>
    private static T Required<T>(T? value, string field)
        where T : class =>
        value ?? throw new InvalidDataException($"{field} must not be null.");

    /// <summary>Refuses an enum value that is not one of the type's named values.</summary>
    private static TEnum Defined<TEnum>(TEnum value, string field)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new InvalidDataException($"{field} is not a known value.");
}
