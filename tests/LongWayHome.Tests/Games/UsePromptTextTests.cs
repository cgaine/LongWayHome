using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The words of the Use prompt: <see cref="UsePromptText.For"/>, read from the real
/// <see cref="GameSession.PreviewUse"/>.</summary>
public class UsePromptTextTests
{
    private static readonly GridPosition Target = new(5, 2);

    private static readonly HeldItem Hoe = HeldItem.Of(ToolId.Hoe);

    private static readonly HeldItem Can = HeldItem.Of(ToolId.WateringCan);

    public static TheoryData<Rejection, string> RejectionTexts => new()
    {
        { Rejection.AlreadyTilled, "Already tilled" },
        { Rejection.TooTired, "Too tired" },
        { Rejection.NotTilled, "Till the soil first" },
        { Rejection.TileOccupied, "Something is growing here" },
        { Rejection.NoSeed, "No seeds left" },
        { Rejection.NotRipe, "Not ready yet" },
        { Rejection.AlreadyWatered, "Already watered" },
        { Rejection.CanEmpty, "Can is empty — refill at the pond" },
    };

    public static TheoryData<Rejection> SilentRejections =>
        [Rejection.NoTarget, Rejection.NothingToDo, Rejection.NoPlanting];

    [Fact]
    public void AC3_hoe_on_grass_prompts_Till()
    {
        var session = TestGames.NewSession();

        var prompt = PromptFor(session, Hoe);

        AssertPrompt("Till", true, prompt);
    }

    [Fact]
    public void AC3_can_on_dry_tilled_soil_prompts_Water()
    {
        var session = TestGames.SessionWithCan(150, TestGames.CanWith(5), TestGames.TilledWith(Target, null));

        var prompt = PromptFor(session, Can);

        AssertPrompt("Water", true, prompt);
    }

    [Fact]
    public void AC3_emberroot_seeds_on_empty_tilled_soil_prompt_Plant_Emberroot()
    {
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, null));

        var prompt = PromptFor(session, HeldItem.SeedOf(CropTypeId.Emberroot));

        AssertPrompt("Plant Emberroot", true, prompt);
    }

    [Theory]
    [InlineData("hoe")]
    [InlineData("can")]
    [InlineData("emberroot_seeds")]
    public void AC3_ripe_dusktuber_targeted_prompts_Harvest_Dusktuber_whatever_is_held(string heldName)
    {
        var held = heldName switch
        {
            "hoe" => Hoe,
            "can" => Can,
            _ => HeldItem.SeedOf(CropTypeId.Emberroot),
        };
        var session = TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Dusktuber)));

        var prompt = PromptFor(session, held);

        AssertPrompt("Harvest Dusktuber", true, prompt);
    }

    [Theory]
    [MemberData(nameof(RejectionTexts))]
    public void AC4_rejection_prompts_its_reason_as_a_non_action(Rejection rejection, string expected)
    {
        var prompt = UsePromptText.For(new UsePreview(null, rejection), Hoe, null, TestGames.Shipped.CropTypes);

        AssertPrompt(expected, false, prompt);
    }

    [Theory]
    [MemberData(nameof(SilentRejections))]
    public void AC4_rejection_with_no_row_shows_no_prompt(Rejection rejection)
    {
        var prompt = UsePromptText.For(new UsePreview(null, rejection), Hoe, null, TestGames.Shipped.CropTypes);

        Assert.Null(prompt);
    }

    [Fact]
    public void AC8_BackpackFull_prompts_Backpack_full_as_a_non_action()
    {
        var prompt = UsePromptText.For(
            new UsePreview(null, Rejection.BackpackFull), Hoe, null, TestGames.Shipped.CropTypes);

        AssertPrompt("Backpack full", false, prompt);
    }

    [Theory]
    [InlineData("hoe")]
    [InlineData("can")]
    [InlineData("emberroot_seeds")]
    public void AC8_Ripe_tile_with_no_room_previews_BackpackFull_and_prompts_Backpack_full(string heldName)
    {
        var held = heldName switch
        {
            "hoe" => Hoe,
            "can" => Can,
            _ => HeldItem.SeedOf(CropTypeId.Emberroot),
        };
        var session = TestGames.SessionWithBackpack(
            TestGames.FullBackpack((ItemRef.Seed(CropTypeId.Emberroot), 10)),
            TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Dusktuber)));

        var preview = session.PreviewUse(Target, held);
        var prompt = UsePromptText.For(preview, held, session.Game.FarmPlot[Target].Planting, session.Data.CropTypes);

        Assert.Equal(new UsePreview(null, Rejection.BackpackFull), preview);
        AssertPrompt("Backpack full", false, prompt);
    }

    [Fact]
    public void AC8_Use_on_a_ripe_tile_with_no_room_is_BackpackFull_and_keeps_the_planting()
    {
        var ripe = TestGames.Ripe(CropTypeId.Dusktuber);
        var session = TestGames.SessionWithBackpack(TestGames.FullBackpack(), TestGames.TilledWith(Target, ripe));
        var events = 0;
        session.TileChanged += _ => events++;
        session.InventoryChanged += () => events++;

        var result = session.Use(Target, Hoe);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(ripe, session.Game.FarmPlot[Target].Planting);
        Assert.Equal(0, events);
    }

    [Fact]
    public void BR_T6_growing_dusktuber_targeted_with_the_hoe_never_prompts_Harvest()
    {
        var session = TestGames.SessionWithTiles(
            TestGames.TilledWith(Target, TestGames.PlantingOf(CropTypeId.Dusktuber, 2)));

        var preview = session.PreviewUse(Target, Hoe);
        var prompt = UsePromptText.For(preview, Hoe, session.Game.FarmPlot[Target].Planting, session.Data.CropTypes);

        Assert.NotEqual(UseActionKind.Harvest, preview.Action);
        Assert.NotNull(prompt);
        Assert.False(prompt.Value.Text.StartsWith("Harvest", StringComparison.Ordinal));
        Assert.False(prompt.Value.IsAction);
    }

    [Fact]
    public void BR_P2_hoe_on_grass_below_2_energy_prompts_Too_tired()
    {
        var session = TestGames.SessionWithEnergy(1);

        var prompt = PromptFor(session, Hoe);

        AssertPrompt("Too tired", false, prompt);
    }

    [Fact]
    public void BR_T9_can_on_watered_tilled_soil_prompts_Already_watered()
    {
        var session = TestGames.SessionWithCan(
            150,
            TestGames.CanWith(5),
            TestGames.TilledWith(Target, null, watered: true));

        var prompt = PromptFor(session, Can);

        AssertPrompt("Already watered", false, prompt);
    }

    [Theory]
    [InlineData("till")]
    [InlineData("harvest")]
    public void Preview_for_prompt_changes_nothing_and_raises_no_events(string scenario)
    {
        var session = scenario == "till"
            ? TestGames.NewSession()
            : TestGames.SessionWithTiles(TestGames.TilledWith(Target, TestGames.Ripe(CropTypeId.Dusktuber)));
        var before = TestGames.WaterSnapshot(session.Game);
        var events = 0;
        session.TileChanged += _ => events++;
        session.PlayerChanged += () => events++;
        session.InventoryChanged += () => events++;

        for (var i = 0; i < 10; i++)
        {
            var prompt = PromptFor(session, Hoe);
            Assert.NotNull(prompt);
        }

        Assert.Equal(before, TestGames.WaterSnapshot(session.Game));
        Assert.Equal(0, events);
    }

    [Fact]
    public void AC8_moonmelon_seeds_held_in_spring_over_empty_tilled_soil_prompt_Out_of_season_plant_in_Fall()
    {
        var inventory = new Inventory();
        inventory.Add(ItemKind.Seed, CropTypeId.Moonmelon, 2);
        var session = new GameSession(
            TestGames.OnDate(new GameDate(1, Season.Spring, 5), inventory, TestGames.TilledWith(Target, null)),
            TestGames.Shipped);
        var held = HeldItem.SeedOf(CropTypeId.Moonmelon);

        var preview = session.PreviewUse(Target, held);
        var prompt = UsePromptText.For(preview, held, null, session.Data.CropTypes);

        Assert.Equal(new UsePreview(null, Rejection.OutOfSeason), preview);
        AssertPrompt("Out of season — plant in Fall", false, prompt);
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot, "Out of season — plant in Spring")]
    [InlineData(CropTypeId.Sunbell, "Out of season — plant in Summer")]
    public void AC8_out_of_season_prompt_names_the_held_seeds_season(CropTypeId seed, string expected)
    {
        var prompt = UsePromptText.For(
            new UsePreview(null, Rejection.OutOfSeason),
            HeldItem.SeedOf(seed),
            null,
            TestGames.CanonicalCrops);

        AssertPrompt(expected, false, prompt);
    }

    [Fact]
    public void Out_of_season_prompt_for_a_seed_with_no_data_row_names_no_season()
    {
        var prompt = UsePromptText.For(
            new UsePreview(null, Rejection.OutOfSeason),
            HeldItem.SeedOf(CropTypeId.Moonmelon),
            null,
            TestGames.ShippedWithout(CropTypeId.Moonmelon).CropTypes);

        AssertPrompt("Out of season", false, prompt);
    }

    [Fact]
    public void Unknown_rejection_shows_no_prompt()
    {
        var prompt = UsePromptText.For(
            new UsePreview(null, (Rejection)999),
            Hoe,
            null,
            TestGames.Shipped.CropTypes);

        Assert.Null(prompt);
    }

    [Fact]
    public void Plant_with_a_crop_that_has_no_data_row_uses_its_display_label()
    {
        var prompt = UsePromptText.For(
            new UsePreview(UseActionKind.Plant, null),
            HeldItem.SeedOf(CropTypeId.Sunbell),
            null,
            TestGames.ShippedWithout(CropTypeId.Sunbell).CropTypes);

        AssertPrompt("Plant Sunbell", true, prompt);
    }

    [Fact]
    public void Harvest_of_a_crop_with_no_data_row_uses_its_display_label()
    {
        var prompt = UsePromptText.For(
            new UsePreview(UseActionKind.Harvest, null),
            Hoe,
            TestGames.PlantingOf(CropTypeId.Starcorn, 4),
            TestGames.ShippedWithout(CropTypeId.Starcorn).CropTypes);

        AssertPrompt("Harvest Starcorn", true, prompt);
    }

    private static (string Text, bool IsAction)? PromptFor(GameSession session, HeldItem held)
    {
        var preview = session.PreviewUse(Target, held);
        return UsePromptText.For(preview, held, session.Game.FarmPlot[Target].Planting, session.Data.CropTypes);
    }

    private static void AssertPrompt(string text, bool isAction, (string Text, bool IsAction)? prompt)
    {
        Assert.NotNull(prompt);
        Assert.Equal(text, prompt.Value.Text);
        Assert.Equal(isAction, prompt.Value.IsAction);
    }
}
