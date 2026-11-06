using LongWayHome.Application.Saves;

namespace LongWayHome.Application.Games;

/// <summary>The title screen's save slot texts: slot rows, the overwrite question and the failure messages.</summary>
public static class SaveSlotText
{
    /// <summary>"Slot 1 — Empty", "Slot 2 — Spring 5, Year 1 · Gold 340" or "Slot 3 — Unreadable save".</summary>
    public static string Row(SaveSlotInfo slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var detail = slot.State switch
        {
            SaveSlotState.Saved when slot.Date is { } date && slot.Gold is { } gold =>
                $"{DateText.For(date)} · {GoldText.For(gold)}",
            SaveSlotState.Empty => "Empty",
            _ => "Unreadable save",
        };
        return $"Slot {slot.Slot} — {detail}";
    }

    /// <summary>"Overwrite slot 2? Its save will be lost."</summary>
    public static string OverwriteQuestion(int slot) => $"Overwrite slot {slot}? Its save will be lost.";

    /// <summary>"That save could not be loaded."</summary>
    public static string LoadFailed => "That save could not be loaded.";

    /// <summary>"The game could not be saved."</summary>
    public static string SaveFailed => "The game could not be saved.";
}
