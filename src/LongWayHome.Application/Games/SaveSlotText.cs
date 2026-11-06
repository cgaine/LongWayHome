using LongWayHome.Application.Saves;

namespace LongWayHome.Application.Games;

/// <summary>The title screen's save slot texts: slot rows, the overwrite question and the failure messages.</summary>
public static class SaveSlotText
{
    /// <summary>"Slot 1 — Empty", "Slot 2 — Spring 5, Year 1 · Gold 340" or "Slot 3 — Unreadable save".</summary>
    public static string Row(SaveSlotInfo slot) => throw new NotImplementedException();

    /// <summary>"Overwrite slot 2? Its save will be lost."</summary>
    public static string OverwriteQuestion(int slot) => throw new NotImplementedException();

    /// <summary>"That save could not be loaded."</summary>
    public static string LoadFailed => throw new NotImplementedException();

    /// <summary>"The game could not be saved."</summary>
    public static string SaveFailed => throw new NotImplementedException();
}
