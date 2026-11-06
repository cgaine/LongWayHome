using LongWayHome.Application.Games;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The title screen's save slot texts (#53).</summary>
public class SaveSlotTextTests
{
    private static readonly DateTimeOffset SavedAt = new(2026, 11, 5, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AC2_empty_slot_row()
    {
        Assert.Equal("Slot 1 — Empty", SaveSlotText.Row(new SaveSlotInfo(1, SaveSlotState.Empty, null, null, null)));
    }

    [Fact]
    public void AC2_saved_slot_row_shows_the_date_and_gold()
    {
        var slot = new SaveSlotInfo(2, SaveSlotState.Saved, new GameDate(1, Season.Spring, 5), 340, SavedAt);

        Assert.Equal("Slot 2 — Spring 5, Year 1 · Gold 340", SaveSlotText.Row(slot));
    }

    [Fact]
    public void AC2_unreadable_slot_row()
    {
        Assert.Equal("Slot 3 — Unreadable save", SaveSlotText.Row(new SaveSlotInfo(3, SaveSlotState.Unreadable, null, null, null)));
    }

    [Fact]
    public void AC2_row_uses_the_slot_number()
    {
        Assert.Equal("Slot 3 — Empty", SaveSlotText.Row(new SaveSlotInfo(3, SaveSlotState.Empty, null, null, null)));
    }

    [Fact]
    public void AC2_saved_row_uses_the_saved_season_year_and_gold()
    {
        var slot = new SaveSlotInfo(1, SaveSlotState.Saved, new GameDate(2, Season.Fall, 28), 12345, SavedAt);

        Assert.Equal("Slot 1 — Fall 28, Year 2 · Gold 12345", SaveSlotText.Row(slot));
    }

    [Fact]
    public void AC5_overwrite_question_names_the_slot()
    {
        Assert.Equal("Overwrite slot 2? Its save will be lost.", SaveSlotText.OverwriteQuestion(2));
    }

    [Fact]
    public void AC9_load_failed_message()
    {
        Assert.Equal("That save could not be loaded.", SaveSlotText.LoadFailed);
    }

    [Fact]
    public void Save_failed_message()
    {
        Assert.Equal("The game could not be saved.", SaveSlotText.SaveFailed);
    }
}
