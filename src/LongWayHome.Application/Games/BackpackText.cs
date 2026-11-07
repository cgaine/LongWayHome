using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The Backpack screen's text. Holds no rules.</summary>
public static class BackpackText
{
    /// <summary>The panel's title.</summary>
    public const string Title = "Backpack";

    /// <summary>The label beside the hotbar row.</summary>
    public const string HotbarLabel = "Hotbar";

    /// <summary>"Hoe", "Watering can", "Emberroot seeds × 12", "Emberroot × 7", or "" for an empty Slot.</summary>
    public static string Label(BackpackSlot slot) => throw new NotImplementedException();

    /// <summary>"Choose where to put it" while holding a Slot; "&lt;ui_accept glyph&gt; Pick up" otherwise.</summary>
    public static string Hint(bool holding, InputDeviceKind device) => throw new NotImplementedException();
}
