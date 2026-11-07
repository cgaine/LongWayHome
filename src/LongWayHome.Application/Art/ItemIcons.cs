using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

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
