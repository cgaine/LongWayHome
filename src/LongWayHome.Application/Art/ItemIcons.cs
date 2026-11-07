using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Art;

/// <summary>
/// The Phase 2 materials that have backpack icons (design doc, Gathering and Rebuilding; the concept art's Backpack
/// page), drawn ahead of the game having them: nothing in play carries them yet.
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

    /// <summary>The seed icon for a held seed, or null for a tool.</summary>
    public static string? For(HeldItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Seed is { } seed ? SeedIcon(seed) : null;
    }

    /// <summary>The seed icon for a Shop seed listing, or null for the Watering can upgrade (which has no icon).</summary>
    public static string? For(ShopListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);
        return listing is { Kind: ShopItemKind.Seed, CropTypeId: { } crop } ? SeedIcon(crop) : null;
    }

    /// <summary>The seed or crop icon for an item, or null for a tool.</summary>
    public static string? For(ItemRef item) => item switch
    {
        { Kind: ItemKind.Seed, CropTypeId: { } seed } => SeedIcon(seed),
        { Kind: ItemKind.Crop, CropTypeId: { } crop } => CropIcon(crop),
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
