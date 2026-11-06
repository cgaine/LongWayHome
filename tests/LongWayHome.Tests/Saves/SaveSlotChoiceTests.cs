using LongWayHome.Application.Saves;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>Which slot Continue loads, which slots can be loaded and which need an overwrite confirmation (#53).</summary>
public class SaveSlotChoiceTests
{
    private static readonly DateTimeOffset Earlier = new(2026, 11, 1, 22, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = new(2026, 11, 4, 23, 30, 0, TimeSpan.Zero);

    private static SaveSlotInfo Saved(int slot, DateTimeOffset savedAt) =>
        new(slot, SaveSlotState.Saved, new GameDate(1, Season.Spring, 3), 100, savedAt);

    private static SaveSlotInfo Empty(int slot) => new(slot, SaveSlotState.Empty, null, null, null);

    private static SaveSlotInfo Unreadable(int slot) => new(slot, SaveSlotState.Unreadable, null, null, null);

    private static SaveSlotInfo InState(int slot, SaveSlotState state) => state switch
    {
        SaveSlotState.Saved => Saved(slot, Later),
        SaveSlotState.Unreadable => Unreadable(slot),
        _ => Empty(slot),
    };

    [Fact]
    public void AC3_continue_picks_the_saved_slot_with_the_latest_saved_at()
    {
        // Slot 1 is the latest, so the choice is not simply the highest-numbered Saved slot.
        IReadOnlyList<SaveSlotInfo> slots = [Saved(1, Later), Saved(2, Earlier), Saved(3, Earlier.AddHours(1))];

        Assert.Equal(1, SaveSlotChoice.ContinueSlot(slots));
    }

    [Fact]
    public void AC3_continue_picks_a_later_slot_when_it_is_the_latest()
    {
        IReadOnlyList<SaveSlotInfo> slots = [Saved(1, Earlier), Empty(2), Saved(3, Later)];

        Assert.Equal(3, SaveSlotChoice.ContinueSlot(slots));
    }

    [Fact]
    public void AC3_continue_ignores_empty_and_unreadable_slots()
    {
        IReadOnlyList<SaveSlotInfo> slots = [Unreadable(1), Saved(2, Earlier), Empty(3)];

        Assert.Equal(2, SaveSlotChoice.ContinueSlot(slots));
    }

    [Fact]
    public void AC3_no_continue_slot_when_nothing_is_saved()
    {
        IReadOnlyList<SaveSlotInfo> slots = [Empty(1), Unreadable(2), Empty(3)];

        Assert.Null(SaveSlotChoice.ContinueSlot(slots));
    }

    [Fact]
    public void AC3_no_continue_slot_when_all_slots_are_empty()
    {
        IReadOnlyList<SaveSlotInfo> slots = [Empty(1), Empty(2), Empty(3)];

        Assert.Null(SaveSlotChoice.ContinueSlot(slots));
    }

    [Fact]
    public void AC3_a_saved_at_tie_goes_to_the_lower_slot()
    {
        IReadOnlyList<SaveSlotInfo> slots = [Empty(1), Saved(2, Later), Saved(3, Later)];

        Assert.Equal(2, SaveSlotChoice.ContinueSlot(slots));
    }

    [Theory]
    [InlineData(SaveSlotState.Saved, true)]
    [InlineData(SaveSlotState.Unreadable, true)]
    [InlineData(SaveSlotState.Empty, false)]
    public void AC5_new_game_asks_to_overwrite_a_saved_or_unreadable_slot(SaveSlotState state, bool expected)
    {
        Assert.Equal(expected, SaveSlotChoice.NeedsOverwriteConfirmation(InState(2, state)));
    }

    [Theory]
    [InlineData(SaveSlotState.Saved, true)]
    [InlineData(SaveSlotState.Unreadable, false)]
    [InlineData(SaveSlotState.Empty, false)]
    public void AC8_only_saved_slots_can_be_loaded(SaveSlotState state, bool expected)
    {
        Assert.Equal(expected, SaveSlotChoice.CanLoad(InState(1, state)));
    }

    [Fact]
    public void AC3_a_saved_at_tie_goes_to_the_lower_slot_whatever_the_list_order()
    {
        IReadOnlyList<SaveSlotInfo> slots = [Saved(3, Later), Saved(2, Later), Empty(1)];

        Assert.Equal(2, SaveSlotChoice.ContinueSlot(slots));
    }

    [Fact]
    public void Choices_refuse_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => SaveSlotChoice.ContinueSlot(null!));
        Assert.Throws<ArgumentNullException>(() => SaveSlotChoice.CanLoad(null!));
        Assert.Throws<ArgumentNullException>(() => SaveSlotChoice.NeedsOverwriteConfirmation(null!));
    }
}
