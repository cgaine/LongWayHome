using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Farming;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #164 AC10, the session side: Use from a held placeable on a free, untilled farm Tile places it; Use on a fence or an
/// empty chest with anything else held picks it up; a filled chest is ChestNotEmpty. The prompt reads "Place chest",
/// "Pick up fence", and so on. <see cref="GameSession.ChestAt"/>, <see cref="GameSession.StoreInChest"/> and
/// <see cref="GameSession.TakeFromChest"/> serve the Chest screen; every change raises InventoryChanged and
/// PlacedObjectsChanged (Place and PickUp TileChanged too).
/// </summary>
public class PlaceableSessionTests
{
    private const int HoePosition = 0;

    private const int CanPosition = 1;

    private const int WoodPosition = 2;

    private const int PlaceablePosition = 3;

    private const int EmptyPosition = 5;

    private static readonly GridPosition Target = new(10, 20);

    private static IReadOnlyDictionary<CropTypeId, CropType> Crops => TestGames.Shipped.CropTypes;

    /// <summary>The tools, 40 wood in Slot 2 and two of <paramref name="placeable"/> in Slot 3.</summary>
    private static Backpack Holding(PlaceableId placeable) => TestGames.RestoredBackpack(
        new BackpackSlot(WoodPosition, PlacedGames.Wood, 40, null),
        new BackpackSlot(PlaceablePosition, ItemRef.Placeable(placeable), 2, null));

    /// <summary>The tools and 40 wood in Slot 2: nothing placeable held.</summary>
    private static Backpack NoPlaceable() =>
        TestGames.RestoredBackpack(new BackpackSlot(WoodPosition, PlacedGames.Wood, 40, null));

    private static GameSession Session(GameState game) => new(game, TestGames.Shipped);

    private static GameSession SessionWith(Backpack backpack, params PlacedObject[] placed) =>
        Session(PlacedGames.With(backpack, placed));

    private static PlacedObject Standing(PlaceableId placeable) =>
        placeable == PlaceableId.Fence ? PlacedGames.FenceAt(Target) : PlacedGames.ChestAt(Target);

    /// <summary>Counts each change event the session raises.</summary>
    private sealed class Events
    {
        public Events(GameSession session)
        {
            session.InventoryChanged += () => Inventory++;
            session.PlacedObjectsChanged += () => Placed++;
            session.TileChanged += position => Tiles.Add(position);
        }

        public int Inventory { get; private set; }

        public int Placed { get; private set; }

        public List<GridPosition> Tiles { get; } = [];
    }

    // ---- HeldItem ----

    [Theory]
    [InlineData(PlaceableId.Fence, "Fence")]
    [InlineData(PlaceableId.Chest, "Chest")]
    public void AC10_a_held_placeable_is_labelled_fence_or_chest(PlaceableId placeable, string label) =>
        Assert.Equal(label, HeldItem.PlaceableOf(placeable).DisplayLabel);

    [Theory]
    [InlineData(PlaceableId.Fence)]
    [InlineData(PlaceableId.Chest)]
    public void AC10_a_placeable_hotbar_slot_is_held_as_that_placeable(PlaceableId placeable)
    {
        var session = TestGames.SessionWithBackpack(Holding(placeable));

        Assert.Equal(HeldItem.PlaceableOf(placeable), session.HeldItemAt(PlaceablePosition));
    }

    [Theory]
    [InlineData(PlaceableId.Fence, "Fence × 2")]
    [InlineData(PlaceableId.Chest, "Chest × 2")]
    public void AC10_a_placeable_slot_is_labelled_with_its_quantity(PlaceableId placeable, string label) =>
        Assert.Equal(label, BackpackText.Label(new BackpackSlot(3, ItemRef.Placeable(placeable), 2, null)));

    // ---- Place ----

    [Theory]
    [InlineData(PlaceableId.Fence)]
    [InlineData(PlaceableId.Chest)]
    public void AC10_holding_a_placeable_on_a_free_untilled_farm_tile_previews_Place(PlaceableId placeable)
    {
        var session = TestGames.SessionWithBackpack(Holding(placeable));

        Assert.Equal(new UsePreview(UseActionKind.Place, null), session.PreviewUse(Target, PlaceablePosition));
    }

    [Theory]
    [InlineData(PlaceableId.Fence, "Place fence")]
    [InlineData(PlaceableId.Chest, "Place chest")]
    public void AC10_the_use_prompt_reads_place_fence_or_place_chest(PlaceableId placeable, string prompt)
    {
        var session = TestGames.SessionWithBackpack(Holding(placeable));

        var text = UsePromptText.For(
            session.PreviewUse(Target, PlaceablePosition),
            session.HeldItemAt(PlaceablePosition),
            session.PlantingAt(Target),
            Crops);

        Assert.Equal((prompt, true), text);
    }

    [Theory]
    [InlineData(PlaceableId.Fence)]
    [InlineData(PlaceableId.Chest)]
    public void AC10_use_places_it_from_the_held_slot_and_raises_inventory_placed_objects_and_tile_changed(PlaceableId placeable)
    {
        var session = TestGames.SessionWithBackpack(Holding(placeable));
        var events = new Events(session);

        Assert.Equal(ActionResult.Ok, session.Use(Target, PlaceablePosition));

        Assert.Equal(placeable, session.Game.FarmPlot.PlacedAt(Target)!.Placeable);
        Assert.Equal(1, session.Game.Player.Backpack.Slots[PlaceablePosition].Quantity);
        Assert.Equal(1, events.Inventory);
        Assert.Equal(1, events.Placed);
        Assert.Equal([Target], events.Tiles);
    }

    [Fact]
    public void AC10_holding_a_placeable_on_a_tilled_tile_is_AlreadyTilled()
    {
        var session = TestGames.SessionWithBackpack(
            Holding(PlaceableId.Chest), new Tile(Target, TileState.Tilled, false, null));
        Assert.Equal(HeldItem.PlaceableOf(PlaceableId.Chest), session.HeldItemAt(PlaceablePosition));

        Assert.Equal(new UsePreview(null, Rejection.AlreadyTilled), session.PreviewUse(Target, PlaceablePosition));
    }

    [Fact]
    public void AC10_holding_a_placeable_aimed_at_a_placed_object_is_TileOccupied()
    {
        var session = SessionWith(Holding(PlaceableId.Chest), PlacedGames.FenceAt(Target));

        Assert.Equal(new UsePreview(null, Rejection.TileOccupied), session.PreviewUse(Target, PlaceablePosition));
    }

    [Fact]
    public void AC10_a_ripe_planting_is_still_harvested_while_holding_a_chest()
    {
        var session = TestGames.SessionWithBackpack(
            Holding(PlaceableId.Chest), TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));
        Assert.Equal(HeldItem.PlaceableOf(PlaceableId.Chest), session.HeldItemAt(PlaceablePosition));

        Assert.Equal(new UsePreview(UseActionKind.Harvest, null), session.PreviewUse(Target, PlaceablePosition));
    }

    [Fact]
    public void AC10_off_the_farm_holding_a_chest_has_no_target()
    {
        var session = TestGames.SessionWithBackpack(Holding(PlaceableId.Chest));
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        Assert.Equal(HeldItem.PlaceableOf(PlaceableId.Chest), session.HeldItemAt(PlaceablePosition));

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(Target, PlaceablePosition));
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.Use(Target, PlaceablePosition));
        Assert.Equal(2, session.Game.Player.Backpack.Slots[PlaceablePosition].Quantity);
    }

    // ---- PickUp ----

    public static TheoryData<PlaceableId, int> PlacedAndHeld()
    {
        var data = new TheoryData<PlaceableId, int>();
        foreach (var placeable in Enum.GetValues<PlaceableId>())
        {
            foreach (var held in new[] { HoePosition, CanPosition, WoodPosition, EmptyPosition })
            {
                data.Add(placeable, held);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PlacedAndHeld))]
    public void AC10_targeting_a_fence_or_an_empty_chest_with_anything_but_a_placeable_previews_PickUp(
        PlaceableId placeable, int heldPosition)
    {
        var session = SessionWith(NoPlaceable(), Standing(placeable));

        Assert.Equal(new UsePreview(UseActionKind.PickUp, null), session.PreviewUse(Target, heldPosition));
    }

    [Theory]
    [InlineData(PlaceableId.Fence, "Pick up fence")]
    [InlineData(PlaceableId.Chest, "Pick up chest")]
    public void AC10_the_pick_up_prompt_names_the_targeted_object(PlaceableId placeable, string prompt)
    {
        var text = UsePromptText.For(
            new UsePreview(UseActionKind.PickUp, null), HeldItem.Of(ToolId.Hoe), null, Crops, placeable);

        Assert.Equal((prompt, true), text);
    }

    [Theory]
    [InlineData(PlaceableId.Fence)]
    [InlineData(PlaceableId.Chest)]
    public void AC10_use_picks_it_up_and_raises_inventory_placed_objects_and_tile_changed(PlaceableId placeable)
    {
        var session = SessionWith(NoPlaceable(), Standing(placeable));
        var events = new Events(session);

        Assert.Equal(ActionResult.Ok, session.Use(Target, HoePosition));

        Assert.Null(session.Game.FarmPlot.PlacedAt(Target));
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Placeable(placeable)));
        Assert.Equal(1, events.Inventory);
        Assert.Equal(1, events.Placed);
        Assert.Equal([Target], events.Tiles);
        Assert.Equal(TileState.Grass, session.Game.FarmPlot[Target].State); // the hoe picked it up, it didn't till
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Fact]
    public void AC10_targeting_a_filled_chest_is_ChestNotEmpty_and_use_changes_nothing()
    {
        var session = SessionWith(NoPlaceable(), PlacedGames.ChestAt(Target, new BackpackSlot(0, PlacedGames.Stone, 12, null)));
        var before = PlacedGames.Snapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.ChestNotEmpty), session.PreviewUse(Target, WoodPosition));
        Assert.Equal(ActionResult.Rejected(Rejection.ChestNotEmpty), session.Use(Target, WoodPosition));

        Assert.Equal(before, PlacedGames.Snapshot(session.Game));
    }

    [Fact]
    public void AC10_tilling_a_placed_objects_tile_through_the_session_is_TileOccupied()
    {
        var session = SessionWith(NoPlaceable(), PlacedGames.FenceAt(Target));

        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), session.Till(Target));
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Fact]
    public void AC10_tools_on_tiles_without_a_placed_object_work_as_before()
    {
        var session = SessionWith(NoPlaceable(), PlacedGames.FenceAt(new GridPosition(11, 20)));

        Assert.Equal(new UsePreview(UseActionKind.Till, null), session.PreviewUse(Target, HoePosition));
    }

    // ---- the Chest screen ----

    [Fact]
    public void AC10_chest_at_shows_the_chests_36_slots_and_null_for_a_fence_or_an_empty_tile()
    {
        var fenceTile = new GridPosition(11, 20);
        var session = SessionWith(
            NoPlaceable(),
            PlacedGames.ChestAt(Target, new BackpackSlot(4, PlacedGames.Wood, 40, null)),
            PlacedGames.FenceAt(fenceTile));

        var view = session.ChestAt(Target);

        Assert.NotNull(view);
        Assert.Equal(Target, view.Position);
        Assert.Equal(Enumerable.Range(0, 36), view.Slots.Select(slot => slot.Index));
        Assert.Equal(PlacedGames.Wood, view.Slots[4].Item);
        Assert.Equal(40, view.Slots[4].Quantity);
        Assert.Equal("Wood × 40", view.Slots[4].Label);
        Assert.Equal("", view.Slots[0].Label);
        Assert.Null(session.ChestAt(fenceTile));
        Assert.Null(session.ChestAt(new GridPosition(12, 20)));
    }

    [Fact]
    public void AC10_store_in_chest_moves_the_stack_and_raises_inventory_and_placed_objects_changed()
    {
        var session = SessionWith(NoPlaceable(), PlacedGames.ChestAt(Target));
        var events = new Events(session);

        Assert.Equal(ActionResult.Ok, session.StoreInChest(Target, WoodPosition));

        Assert.Equal(40, session.ChestAt(Target)!.Slots[0].Quantity);
        Assert.Null(session.Game.Player.Backpack.Slots[WoodPosition].Item);
        Assert.Equal(1, events.Inventory);
        Assert.Equal(1, events.Placed);
    }

    [Fact]
    public void AC10_take_from_chest_moves_the_stack_back_and_raises_inventory_and_placed_objects_changed()
    {
        var session = SessionWith(TestGames.ToolsOnly(), PlacedGames.ChestAt(Target, new BackpackSlot(7, PlacedGames.Wood, 40, null)));
        var events = new Events(session);

        Assert.Equal(ActionResult.Ok, session.TakeFromChest(Target, 7));

        Assert.Equal(40, session.Game.Player.Backpack.Quantity(PlacedGames.Wood));
        Assert.Null(session.ChestAt(Target)!.Slots[7].Item);
        Assert.Equal(1, events.Inventory);
        Assert.Equal(1, events.Placed);
    }

    [Fact]
    public void AC10_a_refused_store_raises_no_events()
    {
        var session = SessionWith(NoPlaceable(), PlacedGames.ChestAt(Target));
        var events = new Events(session);

        Assert.Equal(ActionResult.Rejected(Rejection.NotOffered), session.StoreInChest(Target, HoePosition));

        Assert.Equal(0, events.Inventory);
        Assert.Equal(0, events.Placed);
        Assert.All(session.ChestAt(Target)!.Slots, slot => Assert.Null(slot.Item));
    }
}
