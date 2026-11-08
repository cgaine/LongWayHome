using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The Backpack screen's text (#131): <see cref="BackpackText"/>.</summary>
public class BackpackTextTests
{
    [Fact]
    public void AC3_A_hoe_slot_is_labelled_Hoe()
    {
        var slot = new BackpackSlot(0, ItemRef.Tool(ToolId.Hoe), 1, ToolTier.Rosk);

        Assert.Equal("Hoe", BackpackText.Label(slot));
    }

    [Fact]
    public void AC3_A_watering_can_slot_is_labelled_Watering_can()
    {
        var slot = new BackpackSlot(1, ItemRef.Tool(ToolId.WateringCan), 1, ToolTier.Basic);

        Assert.Equal("Watering can", BackpackText.Label(slot));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, 12, "Emberroot seeds × 12")]
    [InlineData(CropTypeId.Dusktuber, 99, "Dusktuber seeds × 99")]
    [InlineData(CropTypeId.Starcorn, 1, "Starcorn seeds × 1")]
    public void AC3_A_seed_slot_is_labelled_crop_seeds_times_quantity(CropTypeId crop, int quantity, string expected)
    {
        var slot = new BackpackSlot(4, ItemRef.Seed(crop), quantity, null);

        Assert.Equal(expected, BackpackText.Label(slot));
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, 7, "Emberroot × 7")]
    [InlineData(CropTypeId.Moonmelon, 99, "Moonmelon × 99")]
    public void AC3_A_crop_slot_is_labelled_crop_times_quantity(CropTypeId crop, int quantity, string expected)
    {
        var slot = new BackpackSlot(9, ItemRef.Crop(crop), quantity, null);

        Assert.Equal(expected, BackpackText.Label(slot));
    }

    [Fact]
    public void AC3_An_empty_slot_is_labelled_empty()
    {
        Assert.Equal("", BackpackText.Label(new BackpackSlot(5, null, 0, null)));
    }

    [Fact]
    public void AC3_Labels_match_the_hud_hotbar_labels_for_the_same_slots()
    {
        var session = TestGames.SessionWithBackpack(TestGames.BackpackWith(
            (ItemRef.Seed(CropTypeId.Emberroot), 10),
            (ItemRef.Crop(CropTypeId.Sunbell), 3)));
        var row = session.Game.Player.Backpack.HotbarSlots;

        Assert.Equal(session.Hotbar(0).Select(slot => slot.Label), row.Select(BackpackText.Label));
    }

    [Fact]
    public void REQ_13_The_hint_on_a_controller_with_nothing_held_reads_A_Pick_up()
    {
        Assert.Equal("(A) Pick up", BackpackText.Hint(holding: false, InputDeviceKind.Controller));
    }

    [Fact]
    public void REQ_13_The_hint_on_keyboard_and_mouse_with_nothing_held_reads_Space_Pick_up()
    {
        Assert.Equal("[Space] Pick up", BackpackText.Hint(holding: false, InputDeviceKind.KeyboardMouse));
    }

    [Theory]
    [InlineData(InputDeviceKind.Controller)]
    [InlineData(InputDeviceKind.KeyboardMouse)]
    public void REQ_13_The_hint_while_holding_reads_Choose_where_to_put_it(InputDeviceKind device)
    {
        Assert.Equal("Choose where to put it", BackpackText.Hint(holding: true, device));
    }
}
