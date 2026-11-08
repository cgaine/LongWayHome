using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The Backpack screen's text. Holds no rules.</summary>
public static class BackpackText
{
    /// <summary>The panel's title.</summary>
    public const string Title = "Backpack";

    /// <summary>The label beside the hotbar row.</summary>
    public const string HotbarLabel = "Hotbar";

    /// <summary>
    /// "Hoe", "Watering can", "Emberroot seeds × 12", "Emberroot × 7", "Wood × 5", or "" for an empty Slot. The HUD hotbar shows the
    /// same labels.
    /// </summary>
    public static string Label(BackpackSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return slot.Item switch
        {
            { Kind: ItemKind.Tool, ToolId: { } tool } => HeldItem.Of(tool).DisplayLabel,
            { Kind: ItemKind.Seed, CropTypeId: { } seed } => $"{HeldItem.SeedOf(seed).DisplayLabel} × {slot.Quantity}",
            { Kind: ItemKind.Crop, CropTypeId: { } crop } => $"{CropLabels.For(crop)} × {slot.Quantity}",
            { Kind: ItemKind.Material, MaterialId: { } material } => $"{MaterialLabels.For(material)} × {slot.Quantity}",
            _ => "",
        };
    }

    /// <summary>"Choose where to put it" while holding a Slot; "&lt;ui_accept glyph&gt; Pick up" otherwise.</summary>
    public static string Hint(bool holding, InputDeviceKind device) =>
        holding ? "Choose where to put it" : $"{InputGlyphs.For("ui_accept", device)} Pick up";
}
