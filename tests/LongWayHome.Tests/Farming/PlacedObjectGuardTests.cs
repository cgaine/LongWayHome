using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using static LongWayHome.Tests.Farming.PlacedGames;

namespace LongWayHome.Tests.Farming;

/// <summary>
/// #164: the guards and side paths around placed objects that the AC tests don't reach — what the PlacedObject,
/// FarmPlot and Game constructors refuse (BR-F1, BR-F2, BR-K2, BR-K6), chest moves with nothing to move, the watering
/// can on a placed object, the session's off-farm answers, and the Chest screen's and prompt's words.
/// </summary>
public class PlacedObjectGuardTests
{
    private static readonly GridPosition Target = new(10, 20);

    private static GridPosition OffFarm()
    {
        Assert.True(AreaGrid.TryCreate(AreaId.ForestPath, 5, 39, out var offFarm));
        return offFarm;
    }

    // ---- PlacedObject: BR-F2, BR-K2, BR-K6 ----

    [Fact]
    public void BR_F2_a_fence_with_slots_is_refused() =>
        Assert.Throws<ArgumentException>(() => new PlacedObject(Target, PlaceableId.Fence, ChestSlots()));

    [Fact]
    public void BR_F2_a_fence_given_an_empty_slot_list_has_none() =>
        Assert.Empty(new PlacedObject(Target, PlaceableId.Fence, []).Slots);

    [Fact]
    public void BR_K6_a_chest_slot_holding_a_tool_is_refused() =>
        Assert.Throws<ArgumentException>(() => ChestAt(Target, new BackpackSlot(3, ItemRef.Tool(ToolId.Axe), 1, ToolTier.Basic)));

    [Fact]
    public void BR_K2_a_chest_slot_over_99_is_refused() =>
        Assert.Throws<ArgumentException>(() => ChestAt(Target, new BackpackSlot(0, Stone, 100, null)));

    [Fact]
    public void BR_F2_a_chest_with_a_repeated_slot_index_is_refused()
    {
        var slots = ChestSlots().Append(new BackpackSlot(0, null, 0, null));
        Assert.Throws<ArgumentException>(() => new PlacedObject(Target, PlaceableId.Chest, slots));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(36)]
    public void BR_F2_a_chest_slot_index_outside_0_to_35_is_refused(int index)
    {
        var slots = ChestSlots().Skip(1).Append(new BackpackSlot(index, null, 0, null));
        Assert.Throws<ArgumentException>(() => new PlacedObject(Target, PlaceableId.Chest, slots));
    }

    [Fact]
    public void BR_F2_a_chest_missing_a_slot_is_refused() =>
        Assert.Throws<ArgumentException>(() => new PlacedObject(Target, PlaceableId.Chest, ChestSlots().Skip(1)));

    [Fact]
    public void BR_F2_a_chest_given_a_null_slot_is_refused() =>
        Assert.Throws<ArgumentNullException>(() => new PlacedObject(Target, PlaceableId.Chest, [null!]));

    [Fact]
    public void An_undefined_placeable_is_refused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlacedObject(Target, (PlaceableId)99));

    [Fact]
    public void BR_F2_a_chest_is_empty_until_it_holds_something()
    {
        Assert.True(ChestAt(Target).IsEmpty);
        Assert.False(ChestAt(Target, new BackpackSlot(5, Wood, 1, null)).IsEmpty);
        Assert.True(FenceAt(Target).IsEmpty);
    }

    // ---- FarmPlot and Game constructors: BR-F1 ----

    private static List<Tile> GrassTiles(Tile? replace = null)
    {
        var tiles = new List<Tile>();
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                var position = new GridPosition(x, y);
                tiles.Add(replace is not null && replace.Position == position ? replace : new Tile(position));
            }
        }

        return tiles;
    }

    [Fact]
    public void BR_F1_a_plot_refuses_a_placed_object_on_a_tilled_tile() =>
        Assert.Throws<ArgumentException>(() =>
            new FarmPlot(GrassTiles(new Tile(Target, TileState.Tilled, false, null)), [FenceAt(Target)]));

    [Fact]
    public void BR_F1_a_plot_refuses_two_placed_objects_on_one_tile() =>
        Assert.Throws<ArgumentException>(() => new FarmPlot(GrassTiles(), [FenceAt(Target), ChestAt(Target)]));

    [Fact]
    public void BR_F1_a_plot_refuses_a_placed_object_off_the_farm_grid() =>
        Assert.Throws<ArgumentException>(() => new FarmPlot(GrassTiles(), [FenceAt(OffFarm())]));

    [Fact]
    public void BR_F1_a_plot_refuses_a_null_placed_object() =>
        Assert.Throws<ArgumentNullException>(() => new FarmPlot(GrassTiles(), [null!]));

    [Fact]
    public void BR_F1_a_game_refuses_a_placed_object_on_a_farm_node() =>
        Assert.Throws<ArgumentException>(() =>
            With(TestGames.ToolsOnly(), [FenceAt(Target)], nodes: [NodeGames.Node(Target.X, Target.Y, NodeKind.Rock)]));

    // ---- Game actions: guards ----

    [Fact]
    public void Watering_a_placed_objects_tile_is_TileOccupied() =>
        Assert.Equal(Rejection.TileOccupied, With(TestGames.ToolsOnly(), [FenceAt(Target)]).WaterRejection(Target, 2));

    [Theory]
    [InlineData(-1)]
    [InlineData(12)]
    public void Placing_from_a_slot_outside_the_backpack_throws(int slotIndex) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Plain(TestGames.ToolsOnly()).Place(Target, slotIndex));

    [Fact]
    public void BR_F2_picking_up_off_the_farm_grid_is_NothingToDo() =>
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), Plain(TestGames.ToolsOnly()).PickUpPlaced(OffFarm()));

    [Fact]
    public void Storing_where_there_is_no_chest_is_NothingToDo()
    {
        var game = With(TestGames.RestoredBackpack(new BackpackSlot(2, Wood, 5, null)), [FenceAt(Target)]);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), game.StoreInChest(Target, 2));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), game.StoreInChest(new GridPosition(11, 20), 2));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), game.StoreInChest(OffFarm(), 2));
        Assert.Equal(5, game.Player.Backpack.Quantity(Wood));
    }

    [Fact]
    public void Storing_an_empty_backpack_slot_is_NothingToDo() =>
        Assert.Equal(
            ActionResult.Rejected(Rejection.NothingToDo),
            With(TestGames.ToolsOnly(), [ChestAt(Target)]).StoreInChest(Target, 5));

    [Fact]
    public void Taking_from_an_empty_chest_slot_or_no_chest_is_NothingToDo()
    {
        var game = With(TestGames.ToolsOnly(), [ChestAt(Target)]);

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), game.TakeFromChest(Target, 4));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), game.TakeFromChest(new GridPosition(11, 20), 4));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(36)]
    public void A_chest_slot_index_outside_0_to_35_throws(int chestSlot) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => With(TestGames.ToolsOnly(), [ChestAt(Target)]).TakeFromChest(Target, chestSlot));

    [Theory]
    [InlineData(-1)]
    [InlineData(12)]
    public void A_backpack_slot_index_outside_the_backpack_throws_when_storing(int slotIndex) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => With(TestGames.ToolsOnly(), [ChestAt(Target)]).StoreInChest(Target, slotIndex));

    // ---- GameSession ----

    private static GameSession SessionWith(Backpack backpack, params PlacedObject[] placed) =>
        new(With(backpack, placed), TestGames.Shipped);

    [Fact]
    public void Off_the_farm_the_session_sees_no_placed_object_and_moves_no_stack()
    {
        var session = SessionWith(TestGames.RestoredBackpack(new BackpackSlot(2, Wood, 5, null)), ChestAt(Target));
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));

        Assert.Null(session.ChestAt(Target));
        Assert.Null(session.PlacedAt(Target));
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.StoreInChest(Target, 2));
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.PickUpPlaced(Target));
        Assert.Equal(5, session.Game.Player.Backpack.Quantity(Wood));
    }

    [Fact]
    public void On_the_farm_the_session_names_the_placed_object_or_none()
    {
        var session = SessionWith(TestGames.ToolsOnly(), FenceAt(Target));

        Assert.Equal(PlaceableId.Fence, session.PlacedAt(Target));
        Assert.Null(session.PlacedAt(new GridPosition(11, 20)));
        Assert.Null(session.PlacedAt(null));
        Assert.Null(session.PlacedAt(OffFarm()));
    }

    [Fact]
    public void A_held_placeable_without_a_slot_holding_it_has_nothing_to_place()
    {
        var session = SessionWith(TestGames.ToolsOnly());

        Assert.Equal(
            new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, HeldItem.PlaceableOf(PlaceableId.Chest)));
    }

    [Fact]
    public void Use_with_a_held_placeable_places_it_from_the_lowest_slot_holding_it()
    {
        var session = SessionWith(TestGames.RestoredBackpack(
            new BackpackSlot(4, Fence, 2, null), new BackpackSlot(7, Fence, 3, null)));

        Assert.Equal(ActionResult.Ok, session.Use(Target, HeldItem.PlaceableOf(PlaceableId.Fence)));

        Assert.Equal(PlaceableId.Fence, session.Game.FarmPlot.PlacedAt(Target)!.Placeable);
        Assert.Equal(1, session.Game.Player.Backpack.Slots[4].Quantity);
        Assert.Equal(3, session.Game.Player.Backpack.Slots[7].Quantity);
    }

    [Fact]
    public void Use_on_a_fence_holding_a_seed_picks_it_up()
    {
        var session = SessionWith(
            TestGames.RestoredBackpack(new BackpackSlot(2, ItemRef.Seed(CropTypeId.Emberroot), 3, null)), FenceAt(Target));

        Assert.Equal(new UsePreview(UseActionKind.PickUp, null), session.PreviewUse(Target, 2));
        Assert.Equal(ActionResult.Ok, session.Use(Target, 2));
        Assert.Null(session.Game.FarmPlot.PlacedAt(Target));
    }

    // ---- words ----

    private static IReadOnlyDictionary<CropTypeId, CropType> Crops => TestGames.Shipped.CropTypes;

    [Fact]
    public void The_prompt_on_a_placed_object_that_cant_take_another_reads_somethings_in_the_way() =>
        Assert.Equal(
            ("Something's in the way", false),
            UsePromptText.For(
                new UsePreview(null, Rejection.TileOccupied), HeldItem.PlaceableOf(PlaceableId.Chest), null, Crops, PlaceableId.Fence));

    [Fact]
    public void The_prompt_on_a_filled_chest_reads_empty_the_chest_first() =>
        Assert.Equal(
            ("Empty the chest first", false),
            UsePromptText.For(new UsePreview(null, Rejection.ChestNotEmpty), HeldItem.Of(ToolId.Hoe), null, Crops, PlaceableId.Chest));

    [Fact]
    public void A_place_or_pick_up_preview_without_what_it_names_shows_no_prompt()
    {
        Assert.Null(UsePromptText.For(new UsePreview(UseActionKind.Place, null), HeldItem.Of(ToolId.Hoe), null, Crops));
        Assert.Null(UsePromptText.For(new UsePreview(UseActionKind.PickUp, null), HeldItem.Of(ToolId.Hoe), null, Crops));
    }

    [Fact]
    public void The_chest_screen_hint_names_the_accept_glyph() =>
        Assert.Equal($"{InputGlyphs.For("ui_accept", InputDeviceKind.KeyboardMouse)} Move stack", ChestText.Hint(InputDeviceKind.KeyboardMouse));

    [Fact]
    public void The_chest_screen_says_why_a_stack_did_not_move()
    {
        Assert.Null(ChestText.Reason(ActionResult.Ok));
        Assert.Equal("Tools stay in the Backpack", ChestText.Reason(ActionResult.Rejected(Rejection.NotOffered)));
        Assert.Equal("No room for it", ChestText.Reason(ActionResult.Rejected(Rejection.BackpackFull)));
        Assert.Equal("Nothing to move", ChestText.Reason(ActionResult.Rejected(Rejection.NothingToDo)));
    }

    [Fact]
    public void An_undefined_placeable_has_no_label_and_no_art()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HeldItem.PlaceableLabel((PlaceableId)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlaceableArt.For((PlaceableId)99));
    }
}
