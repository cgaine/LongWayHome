using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>The HUD label of each held item, from canonical-values.md.</summary>
public class HeldItemTests
{
    [Theory]
    [InlineData(ToolId.Hoe, "Hoe")]
    [InlineData(ToolId.WateringCan, "Watering can")]
    [InlineData(ToolId.Axe, "Axe")]
    [InlineData(ToolId.Pickaxe, "Pickaxe")]
    public void Tool_label_is_its_canonical_display_label(ToolId tool, string label) =>
        Assert.Equal(label, HeldItem.Of(tool).DisplayLabel);

    [Theory]
    [InlineData(CropTypeId.Emberroot, "Emberroot seeds")]
    [InlineData(CropTypeId.Dusktuber, "Dusktuber seeds")]
    [InlineData(CropTypeId.Moonmelon, "Moonmelon seeds")]
    [InlineData(CropTypeId.Sunbell, "Sunbell seeds")]
    [InlineData(CropTypeId.Starcorn, "Starcorn seeds")]
    public void Seed_label_is_the_crop_type_label_plus_seeds(CropTypeId seed, string label) =>
        Assert.Equal(label, HeldItem.SeedOf(seed).DisplayLabel);

    [Fact]
    public void Every_crop_type_has_a_seed_label() =>
        Assert.All(Enum.GetValues<CropTypeId>(), seed => Assert.EndsWith(" seeds", HeldItem.SeedOf(seed).DisplayLabel));

    [Fact]
    public void AC9_the_held_axe_is_labelled_Axe() =>
        Assert.Equal("Axe", HeldItem.Of(ToolId.Axe).DisplayLabel);

    [Fact]
    public void AC9_the_held_pickaxe_is_labelled_Pickaxe() =>
        Assert.Equal("Pickaxe", HeldItem.Of(ToolId.Pickaxe).DisplayLabel);

    [Theory]
    [InlineData(ToolId.Axe)]
    [InlineData(ToolId.Pickaxe)]
    public void BR_P2_Using_a_held_axe_or_pickaxe_on_grass_is_NothingToDo_and_costs_no_energy(ToolId tool)
    {
        // Swinging comes with epic #123; until then a held gathering tool does nothing anywhere.
        var target = new GridPosition(5, 2);
        var backpack = TestGames.RestoredBackpack(new BackpackSlot(2, ItemRef.Tool(tool), 1, ToolTier.Basic));
        var session = TestGames.SessionWithBackpack(backpack);
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(target, 2));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(target, 2));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Theory]
    [InlineData(ToolId.Axe)]
    [InlineData(ToolId.Pickaxe)]
    public void Using_a_held_axe_or_pickaxe_on_tilled_soil_is_NothingToDo(ToolId tool)
    {
        var target = new GridPosition(5, 2);
        var backpack = TestGames.RestoredBackpack(new BackpackSlot(2, ItemRef.Tool(tool), 1, ToolTier.Basic));
        var session = TestGames.SessionWithBackpack(backpack, TestGames.TilledWith(target, null));
        var before = TestGames.FullSnapshot(session.Game);

        Assert.Equal(new UsePreview(null, Rejection.NothingToDo), session.PreviewUse(target, 2));
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToDo), session.Use(target, 2));
        Assert.Equal(before, TestGames.FullSnapshot(session.Game));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(ToolId.Hoe, CropTypeId.Emberroot)]
    public void An_item_that_is_not_exactly_one_tool_or_seed_has_no_label(ToolId? tool, CropTypeId? seed) =>
        Assert.Throws<InvalidOperationException>(() => new HeldItem(tool, seed).DisplayLabel);
}
