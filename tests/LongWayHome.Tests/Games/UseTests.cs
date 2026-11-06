using LongWayHome.Application.Data;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>What Use does with the held item: <see cref="GameSession.PreviewUse"/>, <see cref="GameSession.Use"/>,
/// <see cref="GameSession.HeldItems"/> and the change events.</summary>
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

        var held = session.HeldItems();

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
    [InlineData(ToolId.WateringCan, null)]
    [InlineData(null, CropTypeId.Emberroot)]
    [InlineData(null, CropTypeId.Dusktuber)]
    public void Use_with_an_item_other_than_the_hoe_is_NothingToDo_and_changes_nothing(ToolId? tool, CropTypeId? seed)
    {
        var held = new HeldItem(tool, seed);
        var session = TestGames.NewSession();
        var events = 0;
        session.TileChanged += _ => events++;
        session.PlayerChanged += () => events++;
        var before = TestGames.Snapshot(session.Game);

        var preview = session.PreviewUse(Target, held);
        var result = session.Use(Target, held);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), preview);
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), result);
        Assert.Equal(before, TestGames.Snapshot(session.Game));
        Assert.Equal(0, events);
    }

    [Fact]
    public void Held_items_omit_a_seed_type_with_no_stack_and_never_include_crop_stacks()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Dusktuber, 3);
        inventory.Add(ItemKind.Crop, CropTypeId.Emberroot, 4);
        inventory.Add(ItemKind.Crop, CropTypeId.Moonmelon, 1);
        var session = new GameSession(TestGames.WithEnergy(150, inventory), TestGames.Shipped);

        var held = session.HeldItems();

        Assert.Equal(
            [HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan), HeldItem.SeedOf(CropTypeId.Dusktuber)],
            held);
    }

    [Fact]
    public void Held_items_list_seed_stacks_in_crop_type_order()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Starcorn, 1);
        inventory.Add(ItemKind.Seed, CropTypeId.Emberroot, 2);
        inventory.Add(ItemKind.Seed, CropTypeId.Moonmelon, 3);
        var session = new GameSession(TestGames.WithEnergy(150, inventory), TestGames.Shipped);

        var held = session.HeldItems();

        Assert.Equal(
            [
                HeldItem.Of(ToolId.Hoe),
                HeldItem.Of(ToolId.WateringCan),
                HeldItem.SeedOf(CropTypeId.Emberroot),
                HeldItem.SeedOf(CropTypeId.Moonmelon),
                HeldItem.SeedOf(CropTypeId.Starcorn),
            ],
            held);
    }

    [Fact]
    public void Held_items_with_no_seeds_are_just_the_hoe_and_the_can()
    {
        var session = new GameSession(TestGames.WithEnergy(150), TestGames.Shipped);

        Assert.Equal([HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan)], session.HeldItems());
    }

    private static GameData WithToolEnergyCost(int cost) =>
        TestGames.Shipped with { Tuning = TestGames.Shipped.Tuning with { ToolEnergyCost = cost } };
}
