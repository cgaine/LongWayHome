using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #129: planting from one Backpack Slot (the domain's PlantFromSlot and its guards), Use from a held position on a row
/// other than row 0, and the Use prompt with nothing held.
/// </summary>
public class PlantFromSlotTests
{
    private static readonly GridPosition Target = new(5, 2);

    private static readonly ItemRef EmberrootSeed = ItemRef.Seed(CropTypeId.Emberroot);

    private static CropType Emberroot => TestGames.Shipped.CropTypes[CropTypeId.Emberroot];

    [Fact]
    public void BR_T3_PlantFromSlot_plants_and_takes_one_seed_from_that_slot_only()
    {
        var game = TestGames.WithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, EmberrootSeed, 5, null), new BackpackSlot(3, EmberrootSeed, 5, null)),
            tiles: [TestGames.TilledWith(Target, null)]);

        Assert.Equal(ActionResult.Ok, game.PlantFromSlot(Target, Emberroot, 3));

        Assert.Equal(CropTypeId.Emberroot, game.FarmPlot[Target].Planting?.CropTypeId);
        Assert.Equal(5, game.Player.Backpack.Slots[2].Quantity);
        Assert.Equal(4, game.Player.Backpack.Slots[3].Quantity);
    }

    [Fact]
    public void BR_T3_A_slot_without_that_seed_is_NoSeed_even_when_another_slot_holds_it()
    {
        var game = TestGames.WithBackpack(
            TestGames.RestoredBackpack(
                new BackpackSlot(2, EmberrootSeed, 5, null),
                new BackpackSlot(3, ItemRef.Seed(CropTypeId.Dusktuber), 5, null),
                new BackpackSlot(4, ItemRef.Crop(CropTypeId.Emberroot), 5, null)),
            tiles: [TestGames.TilledWith(Target, null)]);
        var before = TestGames.FullSnapshot(game);

        Assert.Equal(Rejection.NoSeed, game.PlantFromSlotRejection(Target, Emberroot, 3));
        Assert.Equal(Rejection.NoSeed, game.PlantFromSlotRejection(Target, Emberroot, 4));
        Assert.Equal(Rejection.NoSeed, game.PlantFromSlotRejection(Target, Emberroot, 5));
        Assert.Equal(Rejection.NoSeed, game.PlantFromSlotRejection(Target, Emberroot, 0));
        Assert.Equal(ActionResult.Rejected(Rejection.NoSeed), game.PlantFromSlot(Target, Emberroot, 3));
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void BR_T3_The_tile_checks_come_before_the_slot_check()
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack());

        Assert.Equal(Rejection.NotTilled, game.PlantFromSlotRejection(Target, Emberroot, 5));
    }

    [Fact]
    public void BR_T3_A_seed_with_no_data_row_is_OutOfSeason_from_its_slot()
    {
        var game = TestGames.WithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, EmberrootSeed, 5, null)),
            tiles: [TestGames.TilledWith(Target, null)]);

        Assert.Equal(Rejection.OutOfSeason, game.PlantFromSlotRejection(Target, CropTypeId.Emberroot, season: null, 2));
        Assert.Equal(Rejection.NoSeed, game.PlantFromSlotRejection(Target, CropTypeId.Emberroot, season: null, 3));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(12)]
    public void A_slot_index_outside_the_backpack_throws(int slotIndex)
    {
        var game = TestGames.WithBackpack(TestGames.RestoredBackpack(), tiles: [TestGames.TilledWith(Target, null)]);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.PlantFromSlotRejection(Target, Emberroot, slotIndex));
        Assert.Throws<ArgumentOutOfRangeException>(() => game.PlantFromSlot(Target, Emberroot, slotIndex));
    }

    [Fact]
    public void Use_from_a_seed_slot_whose_crop_type_has_no_data_is_OutOfSeason_and_changes_nothing()
    {
        var data = TestGames.Shipped with
        {
            CropTypes = TestGames.Shipped.CropTypes
                .Where(pair => pair.Key != CropTypeId.Emberroot)
                .ToDictionary(pair => pair.Key, pair => pair.Value),
        };
        var session = new GameSession(
            TestGames.WithBackpack(
                TestGames.RestoredBackpack(new BackpackSlot(2, EmberrootSeed, 5, null)),
                tiles: [TestGames.TilledWith(Target, null)]),
            data);
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.OutOfSeason), session.PreviewUse(Target, 2));
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), session.Use(Target, 2));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void Use_on_row_1_plants_from_the_slot_at_the_same_position_of_that_row()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, EmberrootSeed, 5, null), new BackpackSlot(8, EmberrootSeed, 5, null)),
            TestGames.TilledWith(Target, null));
        session.CycleHotbar();

        Assert.Equal(ActionResult.Ok, session.Use(Target, 2));

        Assert.Equal(5, session.Game.Player.Backpack.Slots[2].Quantity);
        Assert.Equal(4, session.Game.Player.Backpack.Slots[8].Quantity);
    }

    [Fact]
    public void RemoveFrom_a_slot_index_outside_the_backpack_or_a_quantity_below_1_throws()
    {
        var backpack = TestGames.RestoredBackpack(new BackpackSlot(2, EmberrootSeed, 5, null));

        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.RemoveFrom(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.RemoveFrom(12, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => backpack.RemoveFrom(2, 0));
    }

    [Fact]
    public void The_use_prompt_with_nothing_held_on_a_ripe_planting_names_the_harvest()
    {
        var prompt = UsePromptText.For(
            new UsePreview(UseActionKind.Harvest, null),
            null,
            TestGames.Ripe(CropTypeId.Emberroot),
            TestGames.Shipped.CropTypes);

        Assert.NotNull(prompt);
        Assert.True(prompt.Value.IsAction);
        Assert.StartsWith("Harvest ", prompt.Value.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void The_use_prompt_for_nothing_to_do_with_nothing_held_is_hidden() =>
        Assert.Null(UsePromptText.For(new UsePreview(null, Rejection.NothingToDo), null, null, TestGames.Shipped.CropTypes));
}
