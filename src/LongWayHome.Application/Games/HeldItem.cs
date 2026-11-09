using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>What the Player holds: a tool, a seed type or a material (#152; a held material does nothing on Use).</summary>
public sealed record HeldItem(ToolId? Tool, CropTypeId? Seed, MaterialId? Material = null, PlaceableId? Placeable = null)
{
    public static HeldItem Of(ToolId tool) => new(tool, null);

    public static HeldItem SeedOf(CropTypeId seed) => new(null, seed);

    public static HeldItem MaterialOf(MaterialId material) => new(null, null, material);

    public static HeldItem PlaceableOf(PlaceableId placeable) => new(null, null, null, placeable);

    /// <summary>
    /// The display label from canonical-values.md: "Hoe", "Watering can", "Axe", "Pickaxe", "&lt;Crop type&gt; seeds",
    /// or a material's label ("Wood", "Rosk ore", …).
    /// </summary>
    public string DisplayLabel => Placeable is not null ? throw new NotImplementedException() : (Tool, Seed, Material) switch
    {
        (ToolId.Hoe, null, null) => "Hoe",
        (ToolId.WateringCan, null, null) => "Watering can",
        (ToolId.Axe, null, null) => "Axe",
        (ToolId.Pickaxe, null, null) => "Pickaxe",
        (null, { } seed, null) => $"{CropLabels.For(seed)} seeds",
        (null, null, { } material) => MaterialLabels.For(material),
        _ => throw new InvalidOperationException("A held item is exactly one tool, one seed type or one material."),
    };
}
