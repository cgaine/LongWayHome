using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

/// <summary>The Godot resource paths of the seed and crop icons under <c>game/art/icons/</c> (#95).</summary>
public static class ItemIcons
{
    /// <summary>"res://art/icons/seed_&lt;code&gt;.svg" for the crop's canonical code.</summary>
    public static string SeedIcon(CropTypeId crop) => throw new NotImplementedException();

    /// <summary>"res://art/icons/crop_&lt;code&gt;.svg" for the crop's canonical code.</summary>
    public static string CropIcon(CropTypeId crop) => throw new NotImplementedException();

    /// <summary>The seed icon for a held seed, or null for a tool.</summary>
    public static string? For(HeldItem item) => throw new NotImplementedException();
}
