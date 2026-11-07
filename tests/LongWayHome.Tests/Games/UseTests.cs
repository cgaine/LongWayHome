using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>What Use does with the held item: <see cref="GameSession.PreviewUse"/>, <see cref="GameSession.Use"/>,
/// <see cref="GameSession.HeldItemAt"/> and the change events.</summary>
public class UseTests
{
    private static readonly GridPosition Target = new(5, 2);

    private static readonly HeldItem Hoe = HeldItem.Of(ToolId.Hoe);

    public static TheoryData<string> Scenarios => ["grass", "tilled", "no_target", "too_tired"];

    public static TheoryData<string> RejectedScenarios => ["tilled", "no_target", "too_tired"];

    [Fact]
    public void AC5_preview_with_the_hoe_on_grass_is_Till_and_changes_nothing()
    {
        var session = TestGames.NewSession();
        var before = TestGames.Snapshot(session.Game);

        var preview = session.PreviewUse(Target, Hoe);

        Assert.Equal(new UsePreview(UseActionKind.Till, null), preview);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
    }

    [Fact]
    public void AC5_preview_with_the_hoe_on_tilled_soil_is_AlreadyTilled()
    {
        var session = TestGames.NewSession();
        session.Game.FarmPlot[Target].Till();
        var before = TestGames.Snapshot(session.Game);

        var preview = session.PreviewUse(Target, Hoe);

        Assert.Equal(new UsePreview(null, Rejection.AlreadyTilled), preview);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
    }

    [Fact]
    public void AC5_preview_with_no_target_is_NoTarget()
    {
        var session = TestGames.NewSession();
        var before = TestGames.Snapshot(session.Game);

        var preview = session.PreviewUse(null, Hoe);

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), preview);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
    }

    [Fact]
    public void AC5_preview_with_the_hoe_below_2_energy_is_TooTired()
    {
        var session = TestGames.SessionWithEnergy(1);
        var before = TestGames.Snapshot(session.Game);

        var preview = session.PreviewUse(Target, Hoe);

        Assert.Equal(new UsePreview(null, Rejection.TooTired), preview);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
    }

    [Fact]
    public void AC5_preview_with_the_hoe_on_tilled_soil_and_no_energy_is_AlreadyTilled()
    {
        var session = TestGames.SessionWithEnergy(0);
        session.Game.FarmPlot[Target].Till();

        var preview = session.PreviewUse(Target, Hoe);

        Assert.Equal(new UsePreview(null, Rejection.AlreadyTilled), preview);
    }

    [Fact]
    public void AC5_use_with_the_hoe_on_grass_tills_it_and_spends_2_energy()
    {
        var session = TestGames.NewSession();

        var result = session.Use(Target, Hoe);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Target].State);
        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void AC5_use_gives_the_preview_outcome_and_tills_only_when_the_preview_was_Till(string scenario)
    {
        var session = scenario == "too_tired" ? TestGames.SessionWithEnergy(1) : TestGames.NewSession();
        if (scenario == "tilled")
        {
            session.Game.FarmPlot[Target].Till();
        }

        GridPosition? target = scenario == "no_target" ? null : Target;
        var preview = session.PreviewUse(target, Hoe);
        var before = TestGames.Snapshot(session.Game);

        var result = session.Use(target, Hoe);

        if (preview.Action == UseActionKind.Till)
        {
            Assert.True(result.IsOk);
            Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Target].State);
            Assert.Equal(before.Energy - 2, session.Game.Player.Energy);
        }
        else
        {
            Assert.NotNull(preview.Rejection);
            Assert.Equal(ActionResult.Rejected(preview.Rejection!.Value), result);
            Assert.Equal(before, TestGames.Snapshot(session.Game));
        }
    }

    [Fact]
    public void AC6_held_items_on_a_new_game_are_hoe_can_emberroot_and_dusktuber_seeds()
    {
        var session = TestGames.NewSession();

        var held = TestGames.HeldItemsInHotbar(session);

        Assert.Equal(
            [
                HeldItem.Of(ToolId.Hoe),
                HeldItem.Of(ToolId.WateringCan),
                HeldItem.SeedOf(CropTypeId.Emberroot),
                HeldItem.SeedOf(CropTypeId.Dusktuber),
            ],
            held);
    }

    [Fact]
    public void AC7_a_successful_till_raises_TileChanged_once_with_its_position_and_PlayerChanged_once()
    {
        var session = TestGames.NewSession();
        var tiles = new List<GridPosition>();
        var players = 0;
        session.TileChanged += tiles.Add;
        session.PlayerChanged += () => players++;

        session.Use(Target, Hoe);

        Assert.Equal([Target], tiles);
        Assert.Equal(1, players);
    }

    [Fact]
    public void AC7_a_successful_till_command_raises_TileChanged_and_PlayerChanged_once()
    {
        var session = TestGames.NewSession();
        var tiles = new List<GridPosition>();
        var players = 0;
        session.TileChanged += tiles.Add;
        session.PlayerChanged += () => players++;

        session.Till(Target);

        Assert.Equal([Target], tiles);
        Assert.Equal(1, players);
    }

    [Theory]
    [MemberData(nameof(RejectedScenarios))]
    public void AC7_a_rejected_use_raises_neither_event(string scenario)
    {
        var session = scenario == "too_tired" ? TestGames.SessionWithEnergy(1) : TestGames.NewSession();
        if (scenario == "tilled")
        {
            session.Game.FarmPlot[Target].Till();
        }

        var events = 0;
        session.TileChanged += _ => events++;
        session.PlayerChanged += () => events++;
        GridPosition? target = scenario == "no_target" ? null : Target;

        var result = session.Use(target, Hoe);

        Assert.False(result.IsOk);
        Assert.Equal(0, events);
    }

    [Fact]
    public void AC7_a_rejected_till_command_raises_neither_event()
    {
        var session = TestGames.NewSession();
        session.Till(Target);
        var events = 0;
        session.TileChanged += _ => events++;
        session.PlayerChanged += () => events++;

        var result = session.Till(Target);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), result);
        Assert.Equal(0, events);
    }

    [Fact]
    public void BR_P2_use_charges_the_tuning_tool_energy_cost()
    {
        var data = WithToolEnergyCost(5);
        var session = TestGames.SessionWithEnergy(150, data);

        var result = session.Use(Target, Hoe);

        Assert.True(result.IsOk);
        Assert.Equal(145, session.Game.Player.Energy);
    }

    [Fact]
    public void BR_P2_use_is_refused_below_the_tuning_tool_energy_cost()
    {
        var data = WithToolEnergyCost(5);
        var session = TestGames.SessionWithEnergy(4, data);

        var preview = session.PreviewUse(Target, Hoe);
        var result = session.Use(Target, Hoe);

        Assert.Equal(new UsePreview(null, Rejection.TooTired), preview);
        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), result);
        Assert.Equal(4, session.Game.Player.Energy);
        Assert.Equal(TileState.Grass, session.Game.FarmPlot[Target].State);
    }

    [Fact]
    public void BR_P2_shipped_till_costs_2_energy()
    {
        var session = TestGames.NewSession();

        session.Till(Target);

        Assert.Equal(TestGames.Shipped.Tuning.ToolEnergyCost, 150 - session.Game.Player.Energy);
        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Theory]
    [InlineData(ToolId.WateringCan, null, Rejection.NotTilled)]
    [InlineData(null, CropTypeId.Emberroot, Rejection.NotTilled)]
    [InlineData(null, CropTypeId.Dusktuber, Rejection.NotTilled)]
    public void Use_on_grass_with_an_item_other_than_the_hoe_is_rejected_and_changes_nothing(
        ToolId? tool, CropTypeId? seed, Rejection expected)
    {
        var held = new HeldItem(tool, seed);
        var session = TestGames.NewSession();
        var events = 0;
        session.TileChanged += _ => events++;
        session.PlayerChanged += () => events++;
        var before = TestGames.Snapshot(session.Game);

        var preview = session.PreviewUse(Target, held);
        var result = session.Use(Target, held);

        Assert.Equal(new UsePreview(null, expected), preview);
        Assert.Equal(ActionResult.Rejected(expected), result);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
        Assert.Equal(0, events);
    }

    [Fact]
    public void Held_items_omit_a_seed_type_with_no_stack_and_never_include_crop_stacks()
    {
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Seed(CropTypeId.Dusktuber), 3);
        inventory.Add(ItemRef.Crop(CropTypeId.Emberroot), 4);
        inventory.Add(ItemRef.Crop(CropTypeId.Moonmelon), 1);
        var session = new GameSession(TestGames.WithEnergy(150, inventory), TestGames.Shipped);

        var held = TestGames.HeldItemsInHotbar(session);

        Assert.Equal(
            [HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan), HeldItem.SeedOf(CropTypeId.Dusktuber)],
            held);
    }

    [Fact]
    public void Held_items_list_seed_stacks_in_slot_order()
    {
        // #129: the hotbar is the Backpack row, so seeds come in Slot order, no longer in CropTypeId order.
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Seed(CropTypeId.Starcorn), 1);
        inventory.Add(ItemRef.Seed(CropTypeId.Emberroot), 2);
        inventory.Add(ItemRef.Seed(CropTypeId.Moonmelon), 3);
        var session = new GameSession(TestGames.WithEnergy(150, inventory), TestGames.Shipped);

        var held = TestGames.HeldItemsInHotbar(session);

        Assert.Equal(
            [
                HeldItem.Of(ToolId.Hoe),
                HeldItem.Of(ToolId.WateringCan),
                HeldItem.SeedOf(CropTypeId.Starcorn),
                HeldItem.SeedOf(CropTypeId.Emberroot),
                HeldItem.SeedOf(CropTypeId.Moonmelon),
            ],
            held);
    }

    [Fact]
    public void Held_items_with_no_seeds_are_just_the_hoe_and_the_can()
    {
        var session = new GameSession(TestGames.WithEnergy(150), TestGames.Shipped);

        Assert.Equal([HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan)], TestGames.HeldItemsInHotbar(session));
    }

    // ---- #24: planting and harvesting ----

    private static readonly HeldItem Can = HeldItem.Of(ToolId.WateringCan);

    private static readonly HeldItem EmberrootSeeds = HeldItem.SeedOf(CropTypeId.Emberroot);

    public static TheoryData<ToolId?, CropTypeId?> EveryHeldItem => new()
    {
        { ToolId.Hoe, null },
        { ToolId.WateringCan, null },
        { null, CropTypeId.Emberroot },
        { null, CropTypeId.Dusktuber },
        { null, CropTypeId.Moonmelon },
    };

    public static TheoryData<string> RejectedPlantOrHarvest =>
        ["plant_grass", "plant_occupied", "plant_no_seed", "harvest_growing", "harvest_empty"];

    [Theory]
    [MemberData(nameof(EveryHeldItem))]
    public void AC9_preview_on_a_ripe_planting_is_Harvest_whatever_is_held_and_changes_nothing(
        ToolId? tool, CropTypeId? seed)
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));
        var before = TestGames.FullSnapshot(session.Game);

        var preview = session.PreviewUse(Target, new HeldItem(tool, seed));

        Assert.Equal(new UsePreview(UseActionKind.Harvest, null), preview);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void AC9_preview_with_seeds_on_tilled_empty_soil_is_Plant_and_changes_nothing()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));
        var before = TestGames.FullSnapshot(session.Game);

        var preview = session.PreviewUse(Target, EmberrootSeeds);

        Assert.Equal(new UsePreview(UseActionKind.Plant, null), preview);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void AC9_preview_with_seeds_on_grass_is_NotTilled()
    {
        var session = TestGames.NewSession();

        var preview = session.PreviewUse(Target, EmberrootSeeds);

        Assert.Equal(new UsePreview(null, Rejection.NotTilled), preview);
    }

    [Fact]
    public void AC9_preview_with_seeds_on_a_growing_planting_is_TileOccupied()
    {
        var session = TestGames.SessionWithTiles(
            TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 2)));

        var preview = session.PreviewUse(Target, HeldItem.SeedOf(CropTypeId.Dusktuber));

        Assert.Equal(new UsePreview(null, Rejection.TileOccupied), preview);
    }

    [Fact]
    public void AC9_preview_with_seeds_the_player_has_none_of_on_tilled_empty_soil_is_NoSeed()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));

        var preview = session.PreviewUse(Target, HeldItem.SeedOf(CropTypeId.Moonmelon));

        Assert.Equal(new UsePreview(null, Rejection.NoSeed), preview);
    }

    [Fact]
    public void AC9_preview_with_the_hoe_on_a_growing_planting_is_still_AlreadyTilled()
    {
        var session = TestGames.SessionWithTiles(
            TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 0)));

        var preview = session.PreviewUse(Target, Hoe);

        Assert.Equal(new UsePreview(null, Rejection.AlreadyTilled), preview);
    }

    [Fact]
    public void AC8_preview_with_the_can_on_a_dry_growing_planting_is_Water()
    {
        var session = TestGames.SessionWithTiles(
            TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 2)));

        var preview = session.PreviewUse(Target, Can);

        Assert.Equal(new UsePreview(UseActionKind.Water, null), preview);
    }

    [Fact]
    public void AC10_held_label_for_emberroot_seeds_on_a_new_game_shows_10()
    {
        var session = TestGames.NewSession();

        Assert.Equal("Emberroot seeds × 10", session.HeldLabel(EmberrootSeeds));
        Assert.Equal("Dusktuber seeds × 5", session.HeldLabel(HeldItem.SeedOf(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void AC10_held_label_drops_to_9_after_use_plants_an_emberroot()
    {
        var session = TestGames.NewSession();
        session.Use(Target, Hoe);

        var result = session.Use(Target, EmberrootSeeds);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal("Emberroot seeds × 9", session.HeldLabel(EmberrootSeeds));
        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Fact]
    public void Held_label_for_the_tools_is_their_name()
    {
        var session = TestGames.NewSession();

        Assert.Equal("Hoe", session.HeldLabel(Hoe));
        Assert.Equal("Watering can", session.HeldLabel(Can));
    }

    [Fact]
    public void Use_with_seeds_on_tilled_empty_soil_plants_one_seed_and_spends_no_energy()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));

        var result = session.Use(Target, EmberrootSeeds);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(
            new Planting(CropTypeId.Emberroot, TestGames.FirstDay, 0),
            session.Game.FarmPlot[Target].Planting);
        Assert.Equal(9, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Theory]
    [MemberData(nameof(EveryHeldItem))]
    public void Use_on_a_ripe_planting_harvests_it_whatever_is_held(ToolId? tool, CropTypeId? seed)
    {
        var session = TestGames.SessionWithTiles(
            TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Dusktuber), watered: true));

        var result = session.Use(Target, new HeldItem(tool, seed));

        Assert.Equal(ActionResult.Ok, result);
        var tile = session.Game.FarmPlot[Target];
        Assert.Null(tile.Planting);
        Assert.Equal(TileState.Tilled, tile.State);
        Assert.True(tile.Watered);
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Dusktuber)));
        Assert.Equal(5, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
        Assert.Equal(150, session.Game.Player.Energy);
    }

    [Fact]
    public void Plant_command_raises_InventoryChanged_once_and_TileChanged_once_with_its_position()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));
        var tiles = new List<GridPosition>();
        var inventories = 0;
        session.TileChanged += tiles.Add;
        session.InventoryChanged += () => inventories++;

        var result = session.Plant(Target, CropTypeId.Emberroot);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal([Target], tiles);
        Assert.Equal(1, inventories);
    }

    [Fact]
    public void Harvest_command_raises_InventoryChanged_once_and_TileChanged_once_with_its_position()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Moonmelon)));
        var tiles = new List<GridPosition>();
        var inventories = 0;
        session.TileChanged += tiles.Add;
        session.InventoryChanged += () => inventories++;

        var result = session.Harvest(Target);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Null(session.Game.FarmPlot[Target].Planting);
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Moonmelon)));
        Assert.Equal([Target], tiles);
        Assert.Equal(1, inventories);
    }

    [Fact]
    public void Use_that_plants_raises_InventoryChanged_and_TileChanged()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));
        var tiles = new List<GridPosition>();
        var inventories = 0;
        session.TileChanged += tiles.Add;
        session.InventoryChanged += () => inventories++;

        session.Use(Target, EmberrootSeeds);

        Assert.Equal([Target], tiles);
        Assert.Equal(1, inventories);
    }

    [Theory]
    [MemberData(nameof(RejectedPlantOrHarvest))]
    public void A_rejected_plant_or_harvest_raises_no_event_and_changes_nothing(string scenario)
    {
        var growingAt = new GridPosition(0, 0);
        var emptyAt = new GridPosition(1, 0);
        var session = TestGames.SessionWithTiles(
            TestGames.TilledWith(growingAt, TestGames.PlantingOf(CropTypeId.Emberroot, 1)),
            TestGames.TilledWith(emptyAt, null));
        var events = 0;
        session.TileChanged += _ => events++;
        session.InventoryChanged += () => events++;
        session.PlayerChanged += () => events++;
        var before = TestGames.FullSnapshot(session.Game);

        var result = scenario switch
        {
            "plant_grass" => session.Plant(Target, CropTypeId.Emberroot),
            "plant_occupied" => session.Plant(growingAt, CropTypeId.Emberroot),
            "plant_no_seed" => session.Plant(emptyAt, CropTypeId.Moonmelon),
            "harvest_growing" => session.Harvest(growingAt),
            _ => session.Harvest(emptyAt),
        };

        Assert.False(result.IsOk);
        Assert.Equal(0, events);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void Held_items_drop_emberroot_seeds_after_all_10_are_planted()
    {
        var session = TestGames.NewSession();
        var positions = Enumerable.Range(0, 10).Select(x => new GridPosition(x, 1)).ToList();
        positions.ForEach(position => session.Game.FarmPlot[position].Till());

        var results = positions.Select(position => session.Use(position, EmberrootSeeds)).ToList();

        Assert.All(results, result => Assert.Equal(ActionResult.Ok, result));
        Assert.Equal(
            [HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan), HeldItem.SeedOf(CropTypeId.Dusktuber)],
            TestGames.HeldItemsInHotbar(session));
    }

    [Fact]
    public void BR_T5_a_growing_planting_stays_through_tilling_planting_and_every_use()
    {
        var planting = TestGames.PlantingOf(CropTypeId.Emberroot, 2);
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, planting));
        HeldItem[] held = [Hoe, Can, EmberrootSeeds, HeldItem.SeedOf(CropTypeId.Dusktuber)];

        for (var round = 0; round < 3; round++)
        {
            foreach (var item in held)
            {
                session.PreviewUse(Target, item);
                session.Use(Target, item);
            }

            session.Till(Target);
            session.Plant(Target, CropTypeId.Dusktuber);
            session.Harvest(Target);
        }

        Assert.Equal(planting, session.Game.FarmPlot[Target].Planting);
        Assert.Equal(10, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Emberroot)));
        Assert.Equal(5, session.Game.Player.Backpack.Quantity(ItemRef.Seed(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void BR_T5_only_harvesting_removes_a_ripe_planting()
    {
        var ripe = TestGames.Ripe(CropTypeId.Emberroot);
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, ripe));

        var till = session.Till(Target);
        var plant = session.Plant(Target, CropTypeId.Dusktuber);

        Assert.Equal(ActionResult.Rejected(Rejection.AlreadyTilled), till);
        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), plant);
        Assert.Equal(ripe, session.Game.FarmPlot[Target].Planting);

        var harvest = session.Harvest(Target);

        Assert.Equal(ActionResult.Ok, harvest);
        Assert.Null(session.Game.FarmPlot[Target].Planting);
    }

    [Fact]
    public void Harvest_command_on_a_tile_with_no_planting_is_NoPlanting_and_raises_nothing()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));
        var events = 0;
        session.TileChanged += _ => events++;
        session.InventoryChanged += () => events++;

        Assert.Equal(ActionResult.Rejected(Rejection.NoPlanting), session.Harvest(Target));
        Assert.Equal(ActionResult.Rejected(Rejection.NoPlanting), session.Harvest(new GridPosition(0, 0)));
        Assert.Equal(0, events);
    }

    [Fact]
    public void A_planting_whose_crop_type_has_no_data_is_never_ripe_for_preview_use_or_harvest()
    {
        var ripe = TestGames.Ripe(CropTypeId.Emberroot);
        var data = TestGames.Shipped with
        {
            CropTypes = TestGames.Shipped.CropTypes
                .Where(pair => pair.Key != CropTypeId.Emberroot)
                .ToDictionary(pair => pair.Key, pair => pair.Value),
        };
        var session = new GameSession(TestGames.WithTiles(150, null, TestGames.TilledWith(Target, ripe)), data);
        var events = 0;
        session.TileChanged += _ => events++;
        session.InventoryChanged += () => events++;
        session.PlayerChanged += () => events++;
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.AlreadyTilled), session.PreviewUse(Target, Hoe));
        Assert.Equal(new UsePreview(UseActionKind.Water, null), session.PreviewUse(Target, Can));
        Assert.Equal(new UsePreview(null, Rejection.TileOccupied), session.PreviewUse(Target, EmberrootSeeds));
        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), session.Use(Target, EmberrootSeeds));
        Assert.Equal(ActionResult.Rejected(Rejection.NotRipe), session.Harvest(Target));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
        Assert.Equal(0, events);
    }

    [Fact]
    public void Planting_status_at_a_tile_is_null_Growing_or_Ripe_and_Growing_when_its_crop_has_no_data()
    {
        var growingAt = new GridPosition(0, 0);
        var ripeAt = new GridPosition(1, 0);
        var emptyAt = new GridPosition(2, 0);
        var noDataAt = new GridPosition(3, 0);
        var data = TestGames.Shipped with
        {
            CropTypes = TestGames.Shipped.CropTypes
                .Where(pair => pair.Key != CropTypeId.Moonmelon)
                .ToDictionary(pair => pair.Key, pair => pair.Value),
        };
        var session = new GameSession(
            TestGames.WithTiles(
                150,
                null,
                TestGames.TilledWith(growingAt, TestGames.PlantingOf(CropTypeId.Emberroot, 2)),
                TestGames.TilledWith(ripeAt, TestGames.Ripe(CropTypeId.Dusktuber)),
                TestGames.TilledWith(emptyAt, null),
                TestGames.TilledWith(noDataAt, TestGames.PlantingOf(CropTypeId.Moonmelon, 8))),
            data);

        Assert.Equal(PlantingStatus.Growing, session.PlantingStatusAt(growingAt));
        Assert.Equal(PlantingStatus.Ripe, session.PlantingStatusAt(ripeAt));
        Assert.Null(session.PlantingStatusAt(emptyAt));
        Assert.Null(session.PlantingStatusAt(Target));
        Assert.Equal(PlantingStatus.Growing, session.PlantingStatusAt(noDataAt));
    }

    [Fact]
    public void Preview_with_no_target_is_NoTarget_even_with_seeds_held()
    {
        var session = TestGames.NewSession();

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(null, EmberrootSeeds));
        Assert.Equal(ActionResult.Rejected(Rejection.NoTarget), session.Use(null, EmberrootSeeds));
    }

    [Fact]
    public void Held_label_for_a_seed_type_with_no_stack_shows_0()
    {
        var session = TestGames.NewSession();

        Assert.Equal("Moonmelon seeds × 0", session.HeldLabel(HeldItem.SeedOf(CropTypeId.Moonmelon)));
    }

    [Fact]
    public void Held_label_counts_seeds_only_not_crops_of_the_same_type()
    {
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Seed(CropTypeId.Dusktuber), 3);
        inventory.Add(ItemRef.Crop(CropTypeId.Dusktuber), 7);
        var session = new GameSession(TestGames.WithEnergy(150, inventory), TestGames.Shipped);

        Assert.Equal("Dusktuber seeds × 3", session.HeldLabel(HeldItem.SeedOf(CropTypeId.Dusktuber)));
    }

    [Fact]
    public void Held_label_rejects_a_null_item()
    {
        Assert.Throws<ArgumentNullException>(() => TestGames.NewSession().HeldLabel(null!));
    }

    [Fact]
    public void Harvest_raises_no_PlayerChanged_because_it_costs_no_energy()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));
        var playerChanges = 0;
        session.PlayerChanged += () => playerChanges++;

        session.Use(Target, Hoe);
        session.Use(new GridPosition(0, 0), Hoe);
        session.Use(new GridPosition(0, 0), EmberrootSeeds);

        Assert.Equal(1, playerChanges);
        Assert.Equal(148, session.Game.Player.Energy);
    }

    // ---- #25: watering and refilling the can ----

    public static TheoryData<string, Rejection> CanRejections => new()
    {
        { "grass", Rejection.NotTilled },
        { "watered", Rejection.AlreadyWatered },
        { "empty", Rejection.CanEmpty },
        { "too_tired", Rejection.TooTired },
    };

    [Fact]
    public void AC8_preview_with_the_can_on_dry_tilled_soil_is_Water_and_changes_nothing()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));
        var before = TestGames.WaterSnapshot(session.Game);

        var preview = session.PreviewUse(Target, Can);

        Assert.Equal(new UsePreview(UseActionKind.Water, null), preview);
        Assert.Equal(before, TestGames.WaterSnapshot(session.Game));
    }

    [Theory]
    [MemberData(nameof(CanRejections))]
    public void AC8_preview_with_the_can_gives_the_rejection_and_changes_nothing(string scenario, Rejection expected)
    {
        var session = CanScenario(scenario);
        var before = TestGames.WaterSnapshot(session.Game);

        var preview = session.PreviewUse(Target, Can);

        Assert.Equal(new UsePreview(null, expected), preview);
        Assert.Equal(before, TestGames.WaterSnapshot(session.Game));
    }

    [Fact]
    public void AC8_preview_with_the_can_checks_AlreadyWatered_before_CanEmpty_and_TooTired()
    {
        var session = TestGames.SessionWithCan(0, TestGames.CanWith(0), TestGames.TilledWith(Target, null, watered: true));

        Assert.Equal(new UsePreview(null, Rejection.AlreadyWatered), session.PreviewUse(Target, Can));
    }

    [Fact]
    public void AC8_preview_with_the_can_checks_CanEmpty_before_TooTired()
    {
        var session = TestGames.SessionWithCan(0, TestGames.CanWith(0), TestGames.TilledWith(Target, null));

        Assert.Equal(new UsePreview(null, Rejection.CanEmpty), session.PreviewUse(Target, Can));
    }

    [Fact]
    public void AC8_preview_with_the_can_on_a_ripe_planting_is_still_Harvest()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        Assert.Equal(new UsePreview(UseActionKind.Harvest, null), session.PreviewUse(Target, Can));
    }

    [Fact]
    public void AC8_preview_with_the_can_and_no_target_is_NoTarget()
    {
        var session = TestGames.NewSession();

        Assert.Equal(new UsePreview(null, Rejection.NoTarget), session.PreviewUse(null, Can));
    }

    [Fact]
    public void AC8_use_with_the_can_on_dry_tilled_soil_waters_it_using_one_charge_and_2_energy()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));

        var result = session.Use(Target, Can);

        Assert.Equal(ActionResult.Ok, result);
        Assert.True(session.Game.FarmPlot[Target].Watered);
        Assert.Equal(19, session.Game.Player.WateringCan.Charges);
        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Theory]
    [MemberData(nameof(CanRejections))]
    public void AC8_a_rejected_use_with_the_can_returns_the_rejection_raises_nothing_and_changes_nothing(
        string scenario, Rejection expected)
    {
        var session = CanScenario(scenario);
        var events = 0;
        session.TileChanged += _ => events++;
        session.PlayerChanged += () => events++;
        session.InventoryChanged += () => events++;
        var before = TestGames.WaterSnapshot(session.Game);

        var result = session.Use(Target, Can);

        Assert.Equal(ActionResult.Rejected(expected), result);
        Assert.Equal(before, TestGames.WaterSnapshot(session.Game));
        Assert.Equal(0, events);
    }

    [Fact]
    public void AC8_a_successful_water_command_raises_TileChanged_once_with_its_position_and_PlayerChanged_once()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));
        var tiles = new List<GridPosition>();
        var players = 0;
        var inventories = 0;
        session.TileChanged += tiles.Add;
        session.PlayerChanged += () => players++;
        session.InventoryChanged += () => inventories++;

        var result = session.Water(Target);

        Assert.Equal(ActionResult.Ok, result);
        Assert.True(session.Game.FarmPlot[Target].Watered);
        Assert.Equal([Target], tiles);
        Assert.Equal(1, players);
        Assert.Equal(0, inventories);
    }

    [Fact]
    public void AC8_a_use_that_waters_raises_TileChanged_and_PlayerChanged()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));
        var tiles = new List<GridPosition>();
        var players = 0;
        session.TileChanged += tiles.Add;
        session.PlayerChanged += () => players++;

        session.Use(Target, Can);

        Assert.Equal([Target], tiles);
        Assert.Equal(1, players);
    }

    [Theory]
    [MemberData(nameof(CanRejections))]
    public void AC8_a_rejected_water_command_raises_nothing_and_changes_nothing(string scenario, Rejection expected)
    {
        var session = CanScenario(scenario);
        var events = 0;
        session.TileChanged += _ => events++;
        session.PlayerChanged += () => events++;
        session.InventoryChanged += () => events++;
        var before = TestGames.WaterSnapshot(session.Game);

        var result = session.Water(Target);

        Assert.Equal(ActionResult.Rejected(expected), result);
        Assert.Equal(before, TestGames.WaterSnapshot(session.Game));
        Assert.Equal(0, events);
    }

    [Fact]
    public void AC8_refill_can_command_fills_the_can_returns_Ok_and_raises_PlayerChanged_once()
    {
        var session = TestGames.SessionWithCan(150, TestGames.CanWith(0));
        var tiles = 0;
        var players = 0;
        session.TileChanged += _ => tiles++;
        session.PlayerChanged += () => players++;

        var result = session.RefillCan();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(session.Game.Player.WateringCan.Capacity, session.Game.Player.WateringCan.Charges);
        Assert.Equal(150, session.Game.Player.Energy);
        Assert.Equal(1, players);
        Assert.Equal(0, tiles);
    }

    [Fact]
    public void AC8_refill_can_command_on_a_full_can_returns_Ok_and_raises_PlayerChanged()
    {
        var session = TestGames.NewSession();
        var players = 0;
        session.PlayerChanged += () => players++;
        var before = TestGames.WaterSnapshot(session.Game);

        var result = session.RefillCan();

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(before, TestGames.WaterSnapshot(session.Game));
        Assert.Equal(1, players);
    }

    [Fact]
    public void BR_P2_water_command_charges_the_tuning_tool_energy_cost()
    {
        var session = new GameSession(
            TestGames.WithTiles(150, null, TestGames.TilledWith(Target, null)), WithToolEnergyCost(5));

        var result = session.Water(Target);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(145, session.Game.Player.Energy);
    }

    [Fact]
    public void BR_P2_preview_with_the_can_is_TooTired_below_the_tuning_tool_energy_cost()
    {
        var session = new GameSession(
            TestGames.WithTiles(4, null, TestGames.TilledWith(Target, null)), WithToolEnergyCost(5));

        Assert.Equal(new UsePreview(null, Rejection.TooTired), session.PreviewUse(Target, Can));
    }

    private static GameSession CanScenario(string scenario) => scenario switch
    {
        "grass" => TestGames.NewSession(),
        "watered" => TestGames.SessionWithTiles(TestGames.TilledWith(Target, null, watered: true)),
        "empty" => TestGames.SessionWithCan(150, TestGames.CanWith(0), TestGames.TilledWith(Target, null)),
        "too_tired" => TestGames.SessionWithCan(1, TestGames.CanWith(20), TestGames.TilledWith(Target, null)),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
    };

    private static GameData WithToolEnergyCost(int cost) =>
        TestGames.Shipped with { Tuning = TestGames.Shipped.Tuning with { ToolEnergyCost = cost } };

    // ---- #129: Use acts from the held hotbar position ----

    private static readonly ItemRef EmberrootSeed = ItemRef.Seed(CropTypeId.Emberroot);

    /// <summary>Emberroot seeds in Slots 2 and 3, 5 each; the domain's Remove takes from the highest index first.</summary>
    private static GameSession TwoEmberrootSlots(params Tile[] tiles) =>
        TestGames.SessionWithBackpack(
            TestGames.RestoredBackpack(
                new BackpackSlot(2, EmberrootSeed, 5, null),
                new BackpackSlot(3, EmberrootSeed, 5, null)),
            tiles);

    [Fact]
    public void AC5_Using_a_held_seed_slot_on_tilled_empty_soil_in_season_plants_that_seed()
    {
        var session = TwoEmberrootSlots(TestGames.TilledWith(Target, null));

        var result = session.Use(Target, 2);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(CropTypeId.Emberroot, session.Game.FarmPlot[Target].Planting?.CropTypeId);
    }

    [Fact]
    public void AC5_Planting_takes_the_seed_from_the_held_slot_not_another_slot_of_the_same_seed()
    {
        var session = TwoEmberrootSlots(TestGames.TilledWith(Target, null));

        session.Use(Target, 2);

        var slots = session.Game.Player.Backpack.Slots;
        Assert.Equal((EmberrootSeed, 4), (slots[2].Item!.Value, slots[2].Quantity));
        Assert.Equal((EmberrootSeed, 5), (slots[3].Item!.Value, slots[3].Quantity));
    }

    [Fact]
    public void AC5_Preview_from_a_seed_slot_on_tilled_empty_soil_is_Plant_and_changes_nothing()
    {
        var session = TwoEmberrootSlots(TestGames.TilledWith(Target, null));
        var before = TestGames.FullSnapshot(session.Game);

        var preview = session.PreviewUse(Target, 2);

        Assert.Equal(new UsePreview(UseActionKind.Plant, null), preview);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void AC5_Planting_from_a_seed_slot_costs_no_energy_and_raises_InventoryChanged()
    {
        var session = TwoEmberrootSlots(TestGames.TilledWith(Target, null));
        var inventory = 0;
        session.InventoryChanged += () => inventory++;

        session.Use(Target, 3);

        Assert.Equal(150, session.Game.Player.Energy);
        Assert.Equal(1, inventory);
    }

    [Fact]
    public void BR_T3_A_seed_slot_on_untilled_grass_is_NotTilled_as_before_and_changes_nothing()
    {
        var session = TwoEmberrootSlots();
        var before = TestGames.FullSnapshot(session.Game);
        var asBefore = session.PreviewUse(Target, HeldItem.SeedOf(CropTypeId.Emberroot));

        var preview = session.PreviewUse(Target, 2);
        var result = session.Use(Target, 2);

        Assert.Equal(new UsePreview(null, Rejection.NotTilled), asBefore);
        Assert.Equal(asBefore, preview);
        Assert.Equal(ActionResult.Rejected(Rejection.NotTilled), result);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void BR_T3_A_seed_slot_out_of_season_is_OutOfSeason_as_before_and_changes_nothing()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, ItemRef.Seed(CropTypeId.Moonmelon), 5, null)),
            TestGames.TilledWith(Target, null));
        var before = TestGames.FullSnapshot(session.Game);
        var asBefore = session.PreviewUse(Target, HeldItem.SeedOf(CropTypeId.Moonmelon));

        var preview = session.PreviewUse(Target, 2);
        var result = session.Use(Target, 2);

        Assert.Equal(new UsePreview(null, Rejection.OutOfSeason), asBefore);
        Assert.Equal(asBefore, preview);
        Assert.Equal(ActionResult.Rejected(Rejection.OutOfSeason), result);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void BR_T3_A_seed_slot_on_an_occupied_tile_is_TileOccupied()
    {
        var session = TwoEmberrootSlots(TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Dusktuber, 1)));
        var before = TestGames.FullSnapshot(session.Game);

        var result = session.Use(Target, 2);

        Assert.Equal(ActionResult.Rejected(Rejection.TileOccupied), result);
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void Planting_the_last_seed_in_the_held_slot_empties_it_and_HeldItemAt_is_then_null()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, EmberrootSeed, 1, null)),
            TestGames.TilledWith(Target, null));

        Assert.Equal(ActionResult.Ok, session.Use(Target, 2));

        Assert.Null(session.Game.Player.Backpack.Slots[2].Item);
        Assert.Null(session.HeldItemAt(2));
        var held = session.Hotbar(2)[2];
        Assert.Equal(("", true), (held.Label, held.Selected));
    }

    [Fact]
    public void AC6_Holding_the_hoe_slot_tills_grass()
    {
        var session = TestGames.NewSession();

        var result = session.Use(Target, 0);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(TileState.Tilled, session.Game.FarmPlot[Target].State);
    }

    [Fact]
    public void AC6_Holding_the_can_slot_waters_tilled_soil()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));

        var result = session.Use(Target, 1);

        Assert.Equal(ActionResult.Ok, result);
        Assert.True(session.Game.FarmPlot[Target].Watered);
        Assert.Equal(19, session.Game.Player.WateringCan.Charges);
    }

    [Fact]
    public void BR_P2_Tilling_from_the_hoe_slot_costs_2_energy()
    {
        var session = TestGames.NewSession();

        session.Use(Target, 0);

        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Fact]
    public void BR_P2_Watering_from_the_can_slot_costs_2_energy()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));

        session.Use(Target, 1);

        Assert.Equal(148, session.Game.Player.Energy);
    }

    [Fact]
    public void BR_P2_The_hoe_slot_below_2_energy_is_TooTired_and_changes_nothing()
    {
        var session = TestGames.SessionWithEnergy(1);
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.TooTired), session.PreviewUse(Target, 0));
        Assert.Equal(ActionResult.Rejected(Rejection.TooTired), session.Use(Target, 0));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void AC7_An_empty_slot_on_a_growing_planting_is_NothingToDo_and_changes_nothing()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Emberroot, 1)));
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, 4));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(Target, 4));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void AC7_An_empty_slot_on_grass_is_NothingToDo()
    {
        var session = TestGames.NewSession();

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(Target, 5));
        Assert.Equal(TileState.Grass, session.Game.FarmPlot[Target].State);
    }

    [Fact]
    public void AC7_A_crop_slot_on_tilled_empty_soil_is_NothingToDo_and_changes_nothing()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, ItemRef.Crop(CropTypeId.Emberroot), 3, null)),
            TestGames.TilledWith(Target, null));
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(Target, 2));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(Target, 2));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Fact]
    public void AC7_A_crop_slot_on_a_ripe_planting_harvests_it()
    {
        var session = TestGames.SessionWithBackpack(
            TestGames.RestoredBackpack(new BackpackSlot(2, ItemRef.Crop(CropTypeId.Dusktuber), 3, null)),
            TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        Assert.Equal(new UsePreview(UseActionKind.Harvest, null), session.PreviewUse(Target, 2));
        Assert.Equal(ActionResult.Ok, session.Use(Target, 2));
        Assert.Null(session.Game.FarmPlot[Target].Planting);
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }

    [Fact]
    public void BR_T6_An_empty_slot_on_a_ripe_planting_harvests_it_and_leaves_the_tile_tilled()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot), watered: true));

        var result = session.Use(Target, 4);

        Assert.Equal(ActionResult.Ok, result);
        var tile = session.Game.FarmPlot[Target];
        Assert.Equal((TileState.Tilled, true, (Planting?)null), (tile.State, tile.Watered, tile.Planting));
        Assert.Equal(1, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }

    [Fact]
    public void BR_T6_A_seed_slot_on_a_ripe_planting_harvests_rather_than_planting()
    {
        var session = TwoEmberrootSlots(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Emberroot)));

        Assert.Equal(new UsePreview(UseActionKind.Harvest, null), session.PreviewUse(Target, 2));
        Assert.Equal(ActionResult.Ok, session.Use(Target, 2));
        Assert.Equal(10, session.Game.Player.Backpack.Quantity(EmberrootSeed));
    }
}
