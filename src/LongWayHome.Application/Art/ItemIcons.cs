using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Art;

/// <summary>
/// The Phase 2 materials that have backpack icons (design doc, Gathering and Rebuilding; the concept art's Backpack
/// page). <see cref="ItemIcons.For(ItemRef)"/> maps a held <see cref="MaterialId"/> onto one (#152); the Rosk pump has
/// no icon yet.
/// </summary>
public enum MaterialKind
{
    Wood,
    Sap,
    Stone,
    RoskOre,
    GrothOre,
    Glimmercap,
    Plank,
    SealedPlank,
}

/// <summary>
/// The Godot resource paths of the seed and crop icons under <c>game/art/icons/</c> (#95). Icons key on
/// <see cref="CropTypeId"/>, not on the data file, so a seed type with no data row still has one. Pure C#, no Godot.
/// </summary>
public static class ItemIcons
{
    private const string Folder = "res://art/icons/";

    /// <summary>"res://art/icons/seed_&lt;code&gt;.svg" for the crop's canonical code.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined Crop type.</exception>
    public static string SeedIcon(CropTypeId crop) => $"{Folder}seed_{CodeOf(crop)}.svg";

    /// <summary>"res://art/icons/crop_&lt;code&gt;.svg" for the crop's canonical code.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined Crop type.</exception>
    public static string CropIcon(CropTypeId crop) => $"{Folder}crop_{CodeOf(crop)}.svg";

    /// <summary>"res://art/icons/material_&lt;code&gt;.svg": wood, sap, stone, rosk_ore, groth_ore, glimmercap, plank, sealed_plank.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined material.</exception>
    public static string MaterialIcon(MaterialKind material) => $"{Folder}material_{CodeOf(material)}.svg";

    /// <summary>The material icon of a held material, or null for one with no icon yet (the Rosk pump).</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined material.</exception>
    public static string? MaterialIcon(MaterialId material) => material switch
    {
        MaterialId.Wood => MaterialIcon(MaterialKind.Wood),
        MaterialId.Sap => MaterialIcon(MaterialKind.Sap),
        MaterialId.Stone => MaterialIcon(MaterialKind.Stone),
        MaterialId.RoskOre => MaterialIcon(MaterialKind.RoskOre),
        MaterialId.GrothOre => MaterialIcon(MaterialKind.GrothOre),
        MaterialId.Glimmercap => MaterialIcon(MaterialKind.Glimmercap),
        MaterialId.Plank => MaterialIcon(MaterialKind.Plank),
        MaterialId.SealedPlank => MaterialIcon(MaterialKind.SealedPlank),
        MaterialId.RoskPump => null,
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown material."),
    };

    /// <summary>"res://art/icons/placeable_&lt;code&gt;.svg": fence, chest.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined placeable.</exception>
    public static string PlaceableIcon(PlaceableId placeable) => throw new NotImplementedException();

    /// <summary>"res://art/icons/tool_&lt;code&gt;.svg": axe, pickaxe.</summary>
    /// <exception cref="ArgumentOutOfRangeException">An undefined tool.</exception>
    public static string ToolIcon(GatherTool tool) => tool switch
    {
        GatherTool.Axe => $"{Folder}tool_axe.svg",
        GatherTool.Pickaxe => $"{Folder}tool_pickaxe.svg",
        _ => throw new ArgumentOutOfRangeException(nameof(tool), tool, "Unknown gathering tool."),
    };

    /// <summary>The material's code in its icon's file name.</summary>
    private static string CodeOf(MaterialKind material) => material switch
    {
        MaterialKind.Wood => "wood",
        MaterialKind.Sap => "sap",
        MaterialKind.Stone => "stone",
        MaterialKind.RoskOre => "rosk_ore",
        MaterialKind.GrothOre => "groth_ore",
        MaterialKind.Glimmercap => "glimmercap",
        MaterialKind.Plank => "plank",
        MaterialKind.SealedPlank => "sealed_plank",
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown material."),
    };

    /// <summary>
    /// The seed icon for a held seed, the material icon for a held material, the tool icon for the axe or pickaxe, or
    /// null for the hoe and can.
    /// </summary>
    public static string? For(HeldItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item switch
        {
            { Seed: { } seed } => SeedIcon(seed),
            { Material: { } material } => MaterialIcon(material),
            { Tool: { } tool } => GatherIcon(tool),
            _ => null,
        };
    }

    /// <summary>
    /// The seed icon for a Shop seed listing, the tool icon for the axe or pickaxe listing, or null for the Watering
    /// can upgrade (which has no icon).
    /// </summary>
    public static string? For(ShopListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);
        return listing switch
        {
            { Kind: ShopItemKind.Seed, CropTypeId: { } crop } => SeedIcon(crop),
            { Kind: ShopItemKind.Tool, ToolId: { } tool } => GatherIcon(tool),
            _ => null,
        };
    }

    /// <summary>
    /// The seed, crop or material icon for an item, the tool icon for the axe or pickaxe, or null for the hoe and can.
    /// </summary>
    public static string? For(ItemRef item) => item switch
    {
        { Kind: ItemKind.Seed, CropTypeId: { } seed } => SeedIcon(seed),
        { Kind: ItemKind.Crop, CropTypeId: { } crop } => CropIcon(crop),
        { Kind: ItemKind.Tool, ToolId: { } tool } => GatherIcon(tool),
        { Kind: ItemKind.Material, MaterialId: { } material } => MaterialIcon(material),
        _ => null,
    };

    /// <summary>The axe's or pickaxe's icon; null for the hoe and the can, which have none.</summary>
    private static string? GatherIcon(ToolId tool) => tool switch
    {
        ToolId.Axe => ToolIcon(GatherTool.Axe),
        ToolId.Pickaxe => ToolIcon(GatherTool.Pickaxe),
        _ => null,
    };

    /// <summary>The canonical code value (canonical-values.md): "emberroot", "dusktuber", and so on.</summary>
    private static string CodeOf(CropTypeId crop) => crop switch
    {
        CropTypeId.Emberroot => "emberroot",
        CropTypeId.Dusktuber => "dusktuber",
        CropTypeId.Moonmelon => "moonmelon",
        CropTypeId.Sunbell => "sunbell",
        CropTypeId.Starcorn => "starcorn",
        _ => throw new ArgumentOutOfRangeException(nameof(crop), crop, "Unknown Crop type."),
    };
}
