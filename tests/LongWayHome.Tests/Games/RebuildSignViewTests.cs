using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #163 AC10: the Rebuild sign's pips (<c>RebuildSignArt.Parts(wood, stone, gold)</c>) and the Rebuild menu's text
/// (<see cref="RebuildText"/>).
/// </summary>
public class RebuildSignViewTests
{
    private static RebuildLine Item(MaterialId material, int delivered, int needed, int held = 0) =>
        new(material, MaterialLabels.For(material), delivered, needed, held);

    private static RebuildLine Gold(int delivered, int needed, int held = 0) => new(null, "Gold", delivered, needed, held);

    private static RebuildSignView View(params RebuildLine[] lines) =>
        new(BuildingId.GeneralStore, 2, BuildingGames.Stage2Unlocks, lines);

    private static (bool Wood, bool Stone, bool Gold) Pips(RebuildSignView view) => (view.WoodPip, view.StonePip, view.GoldPip);

    // ---- Pips ----

    [Fact]
    public void AC10_No_line_complete_leaves_every_pip_empty()
    {
        var view = View(Item(MaterialId.Wood, 12, 50), Item(MaterialId.Stone, 0, 25), Gold(0, 300));

        Assert.Equal((false, false, false), Pips(view));
    }

    [Fact]
    public void AC10_The_wood_pip_fills_when_the_wood_line_is_complete()
    {
        var view = View(Item(MaterialId.Wood, 50, 50), Item(MaterialId.Stone, 0, 25), Gold(0, 300));

        Assert.Equal((true, false, false), Pips(view));
    }

    [Fact]
    public void AC10_The_stone_pip_fills_when_the_stone_line_is_complete()
    {
        var view = View(Item(MaterialId.Wood, 0, 50), Item(MaterialId.Stone, 25, 25), Gold(0, 300));

        Assert.Equal((false, true, false), Pips(view));
    }

    [Fact]
    public void AC10_The_gold_pip_fills_when_the_gold_line_is_complete()
    {
        var view = View(Item(MaterialId.Wood, 0, 50), Item(MaterialId.Stone, 0, 25), Gold(300, 300));

        Assert.Equal((false, false, true), Pips(view));
    }

    [Fact]
    public void AC10_Plank_and_sealed_plank_lines_count_toward_the_wood_pip()
    {
        var plankShort = View(Item(MaterialId.Wood, 10, 10), Item(MaterialId.Plank, 39, 40), Gold(0, 1));
        var sealedShort = View(Item(MaterialId.Plank, 40, 40), Item(MaterialId.SealedPlank, 0, 5), Gold(0, 1));
        var allWood = View(Item(MaterialId.Plank, 40, 40), Item(MaterialId.SealedPlank, 5, 5), Gold(0, 1));

        Assert.False(plankShort.WoodPip);
        Assert.False(sealedShort.WoodPip);
        Assert.True(allWood.WoodPip);
    }

    [Fact]
    public void AC10_Ore_lines_count_toward_the_stone_pip()
    {
        var oreShort = View(Item(MaterialId.Stone, 120, 120), Item(MaterialId.RoskOre, 19, 20), Gold(0, 1));
        var grothShort = View(Item(MaterialId.Stone, 1, 1), Item(MaterialId.GrothOre, 0, 3), Gold(0, 1));
        var allStone = View(Item(MaterialId.Stone, 120, 120), Item(MaterialId.RoskOre, 20, 20), Gold(0, 1));

        Assert.False(oreShort.StonePip);
        Assert.False(grothShort.StonePip);
        Assert.True(allStone.StonePip);
    }

    [Fact]
    public void AC10_A_pip_with_no_line_of_its_kind_counts_as_filled()
    {
        var goldOnly = View(Gold(0, 300));
        var woodOnly = View(Item(MaterialId.Wood, 0, 50));

        Assert.Equal((true, true, false), Pips(goldOnly));
        Assert.Equal((false, true, true), Pips(woodOnly));
    }

    [Fact]
    public void AC10_AllComplete_is_true_only_when_every_line_is_complete()
    {
        Assert.False(View(Item(MaterialId.Wood, 50, 50), Gold(299, 300)).AllComplete);
        Assert.True(View(Item(MaterialId.Wood, 50, 50), Gold(300, 300)).AllComplete);
    }

    [Theory]
    [InlineData(0, 50, false)]
    [InlineData(49, 50, false)]
    [InlineData(50, 50, true)]
    public void AC10_A_line_is_complete_when_delivered_reaches_needed(int delivered, int needed, bool complete)
    {
        Assert.Equal(complete, Item(MaterialId.Wood, delivered, needed).Complete);
    }

    // ---- RebuildText ----

    [Fact]
    public void AC10_The_menu_title_names_the_store_and_the_stage_it_builds_to()
    {
        Assert.Equal("Rebuild the general store — stage 2", RebuildText.Title(BuildingId.GeneralStore, 2));
    }

    [Fact]
    public void AC10_An_item_row_shows_delivered_needed_and_held()
    {
        Assert.Equal("Wood 12 / 50 (you have 8)", RebuildText.Row(Item(MaterialId.Wood, 12, 50, held: 8)));
    }

    [Fact]
    public void AC10_The_gold_row_shows_delivered_needed_and_held()
    {
        Assert.Equal("Gold 0 / 300 (you have 120)", RebuildText.Row(Gold(0, 300, held: 120)));
    }

    [Fact]
    public void AC10_A_complete_row_shows_the_check_mark()
    {
        var row = RebuildText.Row(Item(MaterialId.Stone, 25, 25, held: 3));

        Assert.Contains(RebuildText.CompleteMark, row, StringComparison.Ordinal);
        Assert.StartsWith("Stone 25 / 25", row, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_After_a_delivery_the_result_names_the_amount_and_the_item()
    {
        var text = RebuildText.AfterDeliver(Item(MaterialId.Wood, 30, 50), 30);

        Assert.Contains("30", text, StringComparison.Ordinal);
        Assert.Contains("Wood", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_Each_refusal_has_its_own_line()
    {
        var lines = new[] { Rejection.NotOffered, Rejection.AlreadyDelivered, Rejection.NothingToDeliver }
            .Select(RebuildText.Rejection)
            .ToList();

        Assert.All(lines, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        Assert.Equal(lines.Count, lines.Distinct().Count());
    }
}
