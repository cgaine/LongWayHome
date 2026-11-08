using System.Diagnostics.CodeAnalysis;
using LongWayHome.Application.Games;
using LongWayHome.Application.Ports;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Shipping;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>The Night (<see cref="NightProcessor.Run"/>): growth (BR-T4, BR-T5), the date step (BR-G1), the weather
/// (BR-G4), watering reset (BR-T7, BR-T8), then the next morning's energy and clock (BR-P3).</summary>
public class NightTests
{
    private static readonly GridPosition Spot = new(3, 2);
    private static readonly GridPosition Other = new(4, 2);
    private static readonly GridPosition Grass = new(0, 0);

    private static void Night(GameState game) => NightProcessor.Run(game, TestGames.Shipped, DayEndReason.Slept);

    private static void WaterWithCan(GameState game, GridPosition position) =>
        Assert.True(game.Water(position, TestGames.Shipped.Tuning.ToolEnergyCost).IsOk);

    [Fact]
    public void AC4_a_growing_planting_on_a_watered_tile_gains_exactly_one_day()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 1), watered: true));

        Night(game);

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 2), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void BR_T4_a_growing_planting_on_a_watered_tile_grows_one_day_overnight()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Moonmelon, 4), watered: true);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Moonmelon));

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Moonmelon, 5), tile.Planting);
    }

    [Fact]
    public void BR_T4_planting_on_unwatered_tile_does_not_grow()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Moonmelon, 4), watered: false);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Moonmelon));

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Moonmelon, 4), tile.Planting);
    }

    [Fact]
    public void AC5_a_growing_planting_on_an_unwatered_tile_is_unchanged_after_the_night_and_still_there()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: false));

        Night(game);

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 1), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void BR_T5_no_planting_dies_or_disappears_in_the_night()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(new GridPosition(1, 1), TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: false),
            TestGames.TilledWith(new GridPosition(2, 1), TestGames.PlantingOf(CropTypeId.Dusktuber, 3), watered: true),
            TestGames.TilledWith(new GridPosition(3, 1), TestGames.Ripe(CropTypeId.Moonmelon), watered: true),
            TestGames.TilledWith(new GridPosition(4, 1), TestGames.Ripe(CropTypeId.Emberroot), watered: false));

        for (var night = 0; night < 5; night++)
        {
            Night(game);
        }

        Assert.Equal(4, game.FarmPlot.Tiles.Count(tile => tile.Planting is not null));
        Assert.All(
            new[] { new GridPosition(1, 1), new GridPosition(2, 1), new GridPosition(3, 1), new GridPosition(4, 1) },
            position => Assert.NotNull(game.FarmPlot[position].Planting));
    }

    [Fact]
    public void AC6_emberroot_watered_before_each_of_three_nights_is_ripe_after_the_third()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0)));

        for (var night = 0; night < 3; night++)
        {
            WaterWithCan(game, Spot);
            Night(game);
        }

        var planting = game.FarmPlot[Spot].Planting!;
        Assert.Equal(PlantingStatus.Ripe, planting.StatusFor(TestGames.Crop(CropTypeId.Emberroot)));
        Assert.Equal(3, planting.DaysGrown);
    }

    [Fact]
    public void AC6_emberroot_watered_before_only_two_of_four_nights_is_growing_with_two_days()
    {
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0)));
        var watered = new[] { true, false, true, false };

        foreach (var water in watered)
        {
            if (water)
            {
                WaterWithCan(game, Spot);
            }

            Night(game);
        }

        var planting = game.FarmPlot[Spot].Planting!;
        Assert.Equal(PlantingStatus.Growing, planting.StatusFor(TestGames.Crop(CropTypeId.Emberroot)));
        Assert.Equal(2, planting.DaysGrown);
    }

    [Fact]
    public void AC7_a_ripe_planting_on_a_watered_tile_is_unchanged_by_the_night()
    {
        var game = TestGames.WithTiles(
            150, null, TestGames.TilledWith(Spot, TestGames.Ripe(CropTypeId.Emberroot), watered: true));

        Night(game);

        Assert.Equal(TestGames.Ripe(CropTypeId.Emberroot), game.FarmPlot[Spot].Planting);
    }

    [Fact]
    public void AC7_grow_overnight_never_takes_a_ripe_planting_past_grow_days()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.Ripe(CropTypeId.Dusktuber), watered: true);

        tile.GrowOvernight(TestGames.Crop(CropTypeId.Dusktuber));

        Assert.Equal(TestGames.Crop(CropTypeId.Dusktuber).GrowDays, tile.Planting!.DaysGrown);
    }

    [Fact]
    public void AC8_after_the_night_no_tile_is_watered_tilled_stays_tilled_and_grass_stays_grass()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: true),
            TestGames.TilledWith(Other, null, watered: true),
            TestGames.TilledWith(new GridPosition(5, 2), null, watered: false));
        var tilledBefore = game.FarmPlot.Tiles.Where(tile => tile.State == TileState.Tilled).Select(tile => tile.Position).ToList();
        var grassBefore = game.FarmPlot.Tiles.Where(tile => tile.State == TileState.Grass).Select(tile => tile.Position).ToList();

        Night(game);

        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
        Assert.All(tilledBefore, position => Assert.Equal(TileState.Tilled, game.FarmPlot[position].State));
        Assert.All(grassBefore, position => Assert.Equal(TileState.Grass, game.FarmPlot[position].State));
    }

    [Fact]
    public void BR_T7_every_tile_is_dry_after_the_night()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: true),
            TestGames.TilledWith(Other, TestGames.Ripe(CropTypeId.Moonmelon), watered: true));

        game.DryAllTiles();

        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
    }

    [Fact]
    public void BR_T7_dry_clears_the_watered_flag_and_keeps_the_planting()
    {
        var tile = TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Emberroot, 1), watered: true);

        tile.Dry();

        Assert.False(tile.Watered);
        Assert.Equal(TileState.Tilled, tile.State);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 1), tile.Planting);
    }

    [Fact]
    public void BR_T8_a_tilled_tile_never_reverts_to_grass_over_the_night()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, null, watered: false),
            TestGames.TilledWith(Other, null, watered: true));

        Night(game);
        Night(game);

        Assert.Equal(TileState.Tilled, game.FarmPlot[Spot].State);
        Assert.Equal(TileState.Tilled, game.FarmPlot[Other].State);
        Assert.Equal(TileState.Grass, game.FarmPlot[Grass].State);
    }

    [Fact]
    public void AC9_growth_runs_before_the_watering_reset_then_energy_and_clock_are_set()
    {
        var game = TestGames.WithTiles(
            20, null, TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 2), watered: true));
        game.AdvanceClock(600, TestGames.Shipped.Tuning.DayEndMinutes);

        Night(game);

        // Grew although no Tile is watered afterwards: growth read the watered flag before the reset cleared it.
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 3), game.FarmPlot[Spot].Planting);
        Assert.All(game.FarmPlot.Tiles, tile => Assert.False(tile.Watered));
        Assert.Equal(TestGames.Shipped.Tuning.MaxEnergy, game.Player.Energy);
        Assert.Equal(TestGames.Shipped.Tuning.DayStartMinutes, game.ClockMinutes);
        Assert.Equal(DayEndReason.Slept, game.LastDayEnd);
        Assert.Equal(new GameDate(1, Season.Spring, 2), game.Date);
    }

    [Fact]
    public void BR_G1_the_date_step_runs_after_growth_and_before_the_weather_roll()
    {
        var game = TestGames.OnDate(
            new GameDate(1, Season.Spring, 28),
            null,
            TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 2), watered: true));
        var cropTypes = new DateRecordingCropTypes(game, TestGames.Shipped.CropTypes);
        var random = new DateRecordingRandom(game);

        NightProcessor.Run(game, TestGames.Shipped with { CropTypes = cropTypes }, DayEndReason.Slept, random);

        // Growth looked its Crop type up on the old date; the weather was rolled on the new one.
        Assert.Equal([new GameDate(1, Season.Spring, 28)], cropTypes.LookupDates);
        Assert.Equal(new GameDate(1, Season.Summer, 1), random.RolledOn);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 3), game.FarmPlot[Spot].Planting);
        Assert.Equal(new GameDate(1, Season.Summer, 1), game.Date);
    }

    /// <summary>Crop type rows that record the Game's date at every lookup.</summary>
    private sealed class DateRecordingCropTypes(GameState game, IReadOnlyDictionary<CropTypeId, CropType> inner)
        : IReadOnlyDictionary<CropTypeId, CropType>
    {
        public List<GameDate> LookupDates { get; } = [];

        public CropType this[CropTypeId key] => inner[key];

        public IEnumerable<CropTypeId> Keys => inner.Keys;

        public IEnumerable<CropType> Values => inner.Values;

        public int Count => inner.Count;

        public bool ContainsKey(CropTypeId key) => inner.ContainsKey(key);

        public bool TryGetValue(CropTypeId key, [MaybeNullWhen(false)] out CropType value)
        {
            LookupDates.Add(game.Date);
            return inner.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<CropTypeId, CropType>> GetEnumerator() => inner.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>A never-rain random source that records the Game's date when the weather is rolled.</summary>
    private sealed class DateRecordingRandom(GameState game) : IRandomSource
    {
        public GameDate? RolledOn { get; private set; }

        public int NextPercent()
        {
            RolledOn = game.Date;
            return 99;
        }
    }

    /// <summary>A session whose bin holds emberroot × 9 and dusktuber × 2 (worth 465), at the given energy.</summary>
    private static GameSession ShippedBasketSession(int energy = 150) =>
        new(
            TestGames.WithShippingBin(energy, null, (CropTypeId.Emberroot, 9), (CropTypeId.Dusktuber, 2)),
            TestGames.Shipped);

    [Fact]
    public void BR_P5_a_night_whose_payout_overflows_gold_throws_before_any_step_changes_the_game()
    {
        var game = TestGames.OnDateWithShippingBin(
            TestGames.FirstDay,
            150,
            null,
            [TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 1), watered: true)],
            (CropTypeId.Moonmelon, int.MaxValue / 190));

        Assert.Throws<OverflowException>(() => Night(game));

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 1), game.FarmPlot[Spot].Planting);
        Assert.Equal(TestGames.FirstDay, game.Date);
        Assert.Equal(new Gold(100), game.Player.Gold);
        Assert.Equal([new ShippedItem(CropTypeId.Moonmelon, int.MaxValue / 190)], game.ShippingBin.Items);
    }

    [Fact]
    public void AC5_sleeping_pays_465_for_emberroot_9_and_dusktuber_2_and_empties_the_bin()
    {
        var session = ShippedBasketSession();

        Assert.Equal(ActionResult.Ok, session.Sleep());

        Assert.Equal(new Gold(100 + 465), session.Game.Player.Gold);
        Assert.Empty(session.Game.ShippingBin.Items);
    }

    [Fact]
    public void AC5_crops_shipped_through_the_session_are_paid_after_sleeping()
    {
        var inventory = TestGames.ToolsOnly();
        inventory.Add(ItemRef.Crop(CropTypeId.Emberroot), 9);
        inventory.Add(ItemRef.Crop(CropTypeId.Dusktuber), 2);
        var session = new GameSession(TestGames.WithEnergy(150, inventory), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.ShipAll());

        session.Sleep();

        Assert.Equal(new Gold(565), session.Game.Player.Gold);
        Assert.Empty(session.Game.ShippingBin.Items);
    }

    [Fact]
    public void AC6_passing_out_pays_the_same_465_and_empties_the_bin()
    {
        var session = ShippedBasketSession();

        session.AdvanceClock(720);

        Assert.Equal(DayEndReason.PassedOut, session.Game.LastDayEnd);
        Assert.Equal(new Gold(100 + 465), session.Game.Player.Gold);
        Assert.Empty(session.Game.ShippingBin.Items);
    }

    [Fact]
    public void AC6_night_processor_pays_out_on_a_passed_out_night()
    {
        var game = TestGames.WithShippingBin(150, null, (CropTypeId.Emberroot, 9), (CropTypeId.Dusktuber, 2));

        NightProcessor.Run(game, TestGames.Shipped, DayEndReason.PassedOut);

        Assert.Equal(new Gold(565), game.Player.Gold);
        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void AC7_a_night_with_an_empty_bin_leaves_gold_unchanged()
    {
        var game = TestGames.WithEnergy(150);

        Night(game);

        Assert.Equal(new Gold(100), game.Player.Gold);
        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void AC7_a_bin_item_with_no_data_row_pays_0_and_is_removed_while_the_rest_pay()
    {
        var game = TestGames.WithShippingBin(150, null, (CropTypeId.Emberroot, 3), (CropTypeId.Dusktuber, 2));

        NightProcessor.Run(game, TestGames.ShippedWithout(CropTypeId.Dusktuber), DayEndReason.Slept);

        Assert.Equal(new Gold(100 + (3 * 35)), game.Player.Gold);
        Assert.Empty(game.ShippingBin.Items);
    }

    [Fact]
    public void AC8_the_payout_runs_after_growth_and_before_the_date_advances()
    {
        // Dusktuber is only planted (looked up by growth); emberroot is only in the bin (looked up by the payout).
        var lastDayOfSpring = new GameDate(1, Season.Spring, 28);
        var game = TestGames.OnDateWithShippingBin(
            lastDayOfSpring,
            150,
            null,
            [TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Dusktuber, 2), watered: true)],
            (CropTypeId.Emberroot, 4));
        var cropTypes = new LookupRecordingCropTypes(game, TestGames.Shipped.CropTypes);
        var random = new GoldRecordingRandom(game);

        NightProcessor.Run(game, TestGames.Shipped with { CropTypes = cropTypes }, DayEndReason.Slept, random);

        // Growth looked its Crop type up before any gold was paid.
        var growthLookups = cropTypes.Lookups.Where(lookup => lookup.Key == CropTypeId.Dusktuber).ToList();
        Assert.NotEmpty(growthLookups);
        Assert.All(growthLookups, lookup => Assert.Equal(new Gold(100), lookup.Gold));

        // The payout looked its Crop type up on the old date, before the date step.
        var payoutLookups = cropTypes.Lookups.Where(lookup => lookup.Key == CropTypeId.Emberroot).ToList();
        Assert.NotEmpty(payoutLookups);
        Assert.All(payoutLookups, lookup => Assert.Equal(lastDayOfSpring, lookup.Date));

        // By the weather roll (after the date step) the gold was already paid.
        Assert.Equal(new Gold(100 + (4 * 35)), random.GoldAtRoll);
        Assert.Equal(new Gold(240), game.Player.Gold);
        Assert.Empty(game.ShippingBin.Items);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Dusktuber, 3), game.FarmPlot[Spot].Planting);
        Assert.Equal(new GameDate(1, Season.Summer, 1), game.Date);
    }

    // ---- #130: dropped items are removed in the Night (BR-K4), after the payout and before the date ----

    private static readonly GridPosition Feet = new(2, 6);

    /// <summary>A Game with a full Backpack, emberroot × 4 in the bin and 5 dusktuber crops on the ground.</summary>
    private static GameState GameWithDroppedItemsAndBin()
    {
        var game = TestGames.OnDateWithShippingBin(
            TestGames.FirstDay, 150, TestGames.FullBackpack(), [], (CropTypeId.Emberroot, 4));
        game.Gain(ItemRef.Crop(CropTypeId.Dusktuber), 5, Feet);
        Assert.NotEmpty(game.DroppedItems);
        return game;
    }

    [Theory]
    [InlineData(DayEndReason.Slept)]
    [InlineData(DayEndReason.PassedOut)]
    public void AC8_after_sleep_or_pass_out_no_dropped_items_remain(DayEndReason reason)
    {
        var session = new GameSession(GameWithDroppedItemsAndBin(), TestGames.Shipped);

        if (reason == DayEndReason.Slept)
        {
            Assert.Equal(ActionResult.Ok, session.Sleep());
        }
        else
        {
            session.AdvanceClock(720);
        }

        Assert.Equal(reason, session.Game.LastDayEnd);
        Assert.Empty(session.Game.DroppedItems);
    }

    [Fact]
    public void AC9_dropped_items_cleared_after_payout_before_date_save_last()
    {
        var game = GameWithDroppedItemsAndBin();
        var cropTypes = new DroppedRecordingCropTypes(game, TestGames.Shipped.CropTypes);
        var random = new DroppedRecordingRandom(game);
        int? droppedAtSave = null;
        GameDate? dateAtSave = null;

        NightProcessor.Run(
            game,
            TestGames.Shipped with { CropTypes = cropTypes },
            DayEndReason.Slept,
            random,
            saved =>
            {
                droppedAtSave = saved.DroppedItems.Count;
                dateAtSave = saved.Date;
            });

        // The payout looked its Crop type up while the dropped items were still on the ground: not cleared before it.
        var payoutLookups = cropTypes.Lookups.Where(lookup => lookup.Key == CropTypeId.Emberroot).ToList();
        Assert.NotEmpty(payoutLookups);
        Assert.All(payoutLookups, lookup => Assert.Equal(1, lookup.Dropped));

        // By the weather roll (just after the date step) they were gone, and the gold was paid.
        Assert.Equal(0, random.DroppedAtRoll);
        Assert.Equal(new Gold(100 + (4 * 35)), game.Player.Gold);

        // The save is still the last step: it saw the next morning with nothing on the ground.
        Assert.Equal(0, droppedAtSave);
        Assert.Equal(new GameDate(1, Season.Spring, 2), dateAtSave);
        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void AC9_night_processor_clears_dropped_items_between_the_payout_and_the_date_step()
    {
        // Source order in NightProcessor.Run: no step can be observed between the clear and the date step, so the
        // order of the calls is checked directly.
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "LongWayHome.Application", "Games", "NightProcessor.cs"));

        var payout = source.IndexOf("game.PayOutShippingBin(", StringComparison.Ordinal);
        var clear = source.IndexOf("game.ClearDroppedItems()", StringComparison.Ordinal);
        var reset = source.IndexOf("game.ResetSwings()", StringComparison.Ordinal);
        var date = source.IndexOf("game.AdvanceDate()", StringComparison.Ordinal);
        var save = source.IndexOf("save(game)", StringComparison.Ordinal);

        Assert.True(clear >= 0, "NightProcessor never calls game.ClearDroppedItems()");
        Assert.True(payout < clear, "ClearDroppedItems must come after PayOutShippingBin");
        Assert.True(clear < date, "ClearDroppedItems must come before AdvanceDate");

        // #152 AC9, BR-N2: the swing reset sits in the node-regrowth slot (BR-N4), after the clear, before the date.
        Assert.True(reset >= 0, "NightProcessor never calls game.ResetSwings()");
        Assert.True(clear < reset, "ResetSwings must come after ClearDroppedItems");
        Assert.True(reset < date, "ResetSwings must come before AdvanceDate");
        var slotComment = source.LastIndexOf("BR-N4", reset, StringComparison.Ordinal);
        Assert.True(slotComment > clear, "the ResetSwings call has no BR-N4 comment marking the regrowth slot");

        Assert.True(date < save, "the save must stay the last step");
    }

    [Fact]
    public void AC9_a_tree_with_2_swings_taken_has_0_after_the_night()
    {
        var game = NodeGames.WithEnergyAndNodes(
            150,
            NodeGames.Node(10, 20, Domain.Nodes.NodeKind.Tree, swingsTaken: 2),
            NodeGames.Node(12, 20, Domain.Nodes.NodeKind.Rock, swingsTaken: 3));

        Night(game);

        Assert.Equal(
            ["farm-10-20:Tree:Farm:10:20:0", "farm-12-20:Rock:Farm:12:20:0"],
            NodeGames.Describe(game.ResourceNodes));
    }

    [Fact]
    public void BR_N2_the_swing_reset_has_happened_by_the_save()
    {
        var game = NodeGames.WithEnergyAndNodes(150, NodeGames.Node(10, 20, Domain.Nodes.NodeKind.Tree, swingsTaken: 2));
        int? swingsAtSave = null;

        NightProcessor.Run(
            game, TestGames.Shipped, DayEndReason.Slept, NeverRainSource.Instance,
            saved => swingsAtSave = saved.ResourceNodes.Single().SwingsTaken);

        Assert.Equal(0, swingsAtSave);
    }

    [Fact]
    public void BR_G5_save_runs_after_dropped_items_cleared()
    {
        var game = GameWithDroppedItemsAndBin();
        (int Dropped, int Clock, int Energy)? atSave = null;

        NightProcessor.Run(
            game,
            TestGames.Shipped,
            DayEndReason.PassedOut,
            NeverRainSource.Instance,
            saved => atSave = (saved.DroppedItems.Count, saved.ClockMinutes, saved.Player.Energy));

        Assert.Equal((0, TestGames.Shipped.Tuning.DayStartMinutes, TestGames.Shipped.Tuning.PassOutEnergy), atSave);
    }

    /// <summary>Crop type rows that record the looked-up key and how many DroppedItems lay on the ground.</summary>
    private sealed class DroppedRecordingCropTypes(GameState game, IReadOnlyDictionary<CropTypeId, CropType> inner)
        : IReadOnlyDictionary<CropTypeId, CropType>
    {
        public List<(CropTypeId Key, int Dropped)> Lookups { get; } = [];

        public CropType this[CropTypeId key]
        {
            get
            {
                Record(key);
                return inner[key];
            }
        }

        public IEnumerable<CropTypeId> Keys => inner.Keys;

        public IEnumerable<CropType> Values => inner.Values;

        public int Count => inner.Count;

        public bool ContainsKey(CropTypeId key)
        {
            Record(key);
            return inner.ContainsKey(key);
        }

        public bool TryGetValue(CropTypeId key, [MaybeNullWhen(false)] out CropType value)
        {
            Record(key);
            return inner.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<CropTypeId, CropType>> GetEnumerator() => inner.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private void Record(CropTypeId key) => Lookups.Add((key, game.DroppedItems.Count));
    }

    /// <summary>A never-rain random source that records how many DroppedItems lay on the ground at the roll.</summary>
    private sealed class DroppedRecordingRandom(GameState game) : IRandomSource
    {
        public int? DroppedAtRoll { get; private set; }

        public int NextPercent()
        {
            DroppedAtRoll = game.DroppedItems.Count;
            return 99;
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        return dir!.FullName;
    }

    [Fact]
    public void BR_P4_passing_out_and_sleeping_pay_the_same_gold_and_leave_the_same_bin()
    {
        var slept = ShippedBasketSession(energy: 60);
        var passedOut = ShippedBasketSession(energy: 60);

        slept.Sleep();
        passedOut.AdvanceClock(720);

        Assert.Equal(DayEndReason.PassedOut, passedOut.Game.LastDayEnd);
        Assert.Equal(slept.Game.Player.Gold, passedOut.Game.Player.Gold);
        Assert.Equal(slept.Game.ShippingBin.Items, passedOut.Game.ShippingBin.Items);
        Assert.Equal(new Gold(565), passedOut.Game.Player.Gold);
    }

    /// <summary>Crop type rows that record the looked-up key, the Game's date and the Player's gold at every lookup.</summary>
    private sealed class LookupRecordingCropTypes(GameState game, IReadOnlyDictionary<CropTypeId, CropType> inner)
        : IReadOnlyDictionary<CropTypeId, CropType>
    {
        public List<(CropTypeId Key, GameDate Date, Gold Gold)> Lookups { get; } = [];

        public CropType this[CropTypeId key]
        {
            get
            {
                Record(key);
                return inner[key];
            }
        }

        public IEnumerable<CropTypeId> Keys => inner.Keys;

        public IEnumerable<CropType> Values => inner.Values;

        public int Count => inner.Count;

        public bool ContainsKey(CropTypeId key)
        {
            Record(key);
            return inner.ContainsKey(key);
        }

        public bool TryGetValue(CropTypeId key, [MaybeNullWhen(false)] out CropType value)
        {
            Record(key);
            return inner.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<CropTypeId, CropType>> GetEnumerator() => inner.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        private void Record(CropTypeId key) => Lookups.Add((key, game.Date, game.Player.Gold));
    }

    /// <summary>A never-rain random source that records the Player's gold when the weather is rolled.</summary>
    private sealed class GoldRecordingRandom(GameState game) : IRandomSource
    {
        public Gold? GoldAtRoll { get; private set; }

        public int NextPercent()
        {
            GoldAtRoll = game.Player.Gold;
            return 99;
        }
    }

    [Fact]
    public void Planting_with_unknown_crop_type_is_unchanged()
    {
        // Sunbell with its data row removed.
        var sunbell = TestGames.PlantingOf(CropTypeId.Sunbell, 1);
        var game = TestGames.WithTiles(150, null, TestGames.TilledWith(Spot, sunbell, watered: true));

        NightProcessor.Run(game, TestGames.ShippedWithout(CropTypeId.Sunbell), DayEndReason.Slept);

        Assert.Equal(sunbell, game.FarmPlot[Spot].Planting);
        Assert.False(game.FarmPlot[Spot].Watered);
    }

    [Fact]
    public void Grow_plantings_leaves_a_planting_with_no_data_row_unchanged_and_grows_the_rest()
    {
        var game = TestGames.WithTiles(
            150,
            null,
            TestGames.TilledWith(Spot, TestGames.PlantingOf(CropTypeId.Starcorn, 0), watered: true),
            TestGames.TilledWith(Other, TestGames.PlantingOf(CropTypeId.Emberroot, 0), watered: true));

        game.GrowPlantings(TestGames.ShippedWithout(CropTypeId.Starcorn).CropTypes);

        Assert.Equal(TestGames.PlantingOf(CropTypeId.Starcorn, 0), game.FarmPlot[Spot].Planting);
        Assert.Equal(TestGames.PlantingOf(CropTypeId.Emberroot, 1), game.FarmPlot[Other].Planting);
    }
}
