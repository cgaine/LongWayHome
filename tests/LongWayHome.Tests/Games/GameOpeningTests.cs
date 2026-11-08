using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #139, domain: <c>Game.OpeningDone</c>, <c>CompleteOpening</c> and <c>GiveStarterSeeds</c> (BR-I1, BR-P6).
/// </summary>
public class GameOpeningTests
{
    private static readonly IReadOnlyList<StartingStack> Starter =
        [new StartingStack(CropTypeId.Emberroot, 10), new StartingStack(CropTypeId.Dusktuber, 5)];

    [Fact]
    public void BR_I1_A_game_built_without_openingDone_is_past_the_opening()
    {
        var game = TestGames.WithEnergy(150);

        Assert.True(game.OpeningDone);
    }

    [Fact]
    public void BR_I1_A_game_built_with_openingDone_false_has_not_finished_the_opening()
    {
        var game = OpeningGames.NotDone();

        Assert.False(game.OpeningDone);
    }

    [Fact]
    public void BR_I1_CompleteOpening_sets_OpeningDone()
    {
        var game = OpeningGames.NotDone();

        game.CompleteOpening();

        Assert.True(game.OpeningDone);
    }

    [Fact]
    public void BR_I1_CompleteOpening_again_changes_nothing()
    {
        var game = OpeningGames.NotDone();
        game.CompleteOpening();
        var before = TestGames.FullSnapshot(game);

        game.CompleteOpening();

        Assert.True(game.OpeningDone);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void AC2_BR_P6_GiveStarterSeeds_adds_each_stack_and_returns_ok()
    {
        var game = OpeningGames.NotDone();

        var result = game.GiveStarterSeeds(Starter);

        Assert.Equal(ActionResult.Ok, result);
        Assert.Equal(
            [
                "0:Tool:Hoe:1:Basic",
                "1:Tool:WateringCan:1:Basic",
                "2:Seed:Emberroot:10:-",
                "3:Seed:Dusktuber:5:-",
                .. Enumerable.Range(4, 8).Select(index => $"{index}:-:-:0:-"),
            ],
            game.Player.Backpack.Slots.Select(TestGames.DescribeSlot));
    }

    [Fact]
    public void AC2_GiveStarterSeeds_costs_no_energy()
    {
        var game = OpeningGames.NotDone(energy: 40);

        game.GiveStarterSeeds(Starter);

        Assert.Equal(40, game.Player.Energy);
    }

    [Fact]
    public void AC2_GiveStarterSeeds_tops_up_a_seed_stack_already_carried()
    {
        var game = OpeningGames.NotDone(TestGames.BackpackWith((ItemRef.Seed(CropTypeId.Emberroot), 3)));

        Assert.Equal(ActionResult.Ok, game.GiveStarterSeeds(Starter));

        Assert.Equal("2:Seed:Emberroot:13:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[2]));
        Assert.Equal("3:Seed:Dusktuber:5:-", TestGames.DescribeSlot(game.Player.Backpack.Slots[3]));
    }

    [Fact]
    public void AC3_GiveStarterSeeds_on_a_game_past_the_opening_is_rejected_OpeningOver_and_adds_nothing()
    {
        var game = TestGames.WithEnergy(150);
        var before = TestGames.FullSnapshot(game);

        var result = game.GiveStarterSeeds(Starter);

        Assert.Equal(ActionResult.Rejected(Rejection.OpeningOver), result);
        Assert.Equal(before, TestGames.FullSnapshot(game));
    }

    [Fact]
    public void AC3_GiveStarterSeeds_after_CompleteOpening_is_rejected_OpeningOver()
    {
        var game = OpeningGames.NotDone();
        game.CompleteOpening();

        Assert.Equal(ActionResult.Rejected(Rejection.OpeningOver), game.GiveStarterSeeds(Starter));
        Assert.Equal(0, OpeningGames.SeedCount(game, CropTypeId.Emberroot));
    }

    [Fact]
    public void AC3_GiveStarterSeeds_that_do_not_all_fit_is_rejected_BackpackFull_and_adds_nothing()
    {
        // One free Slot (11): the emberroot would fit, the dusktuber then would not, so nothing is added.
        var game = OpeningGames.NotDone(TestGames.BackpackWith((TestGames.Filler, 9 * Backpack.MaxStack)));
        Assert.Single(game.Player.Backpack.Slots, slot => slot.Item is null);
        var before = TestGames.FullSnapshot(game);

        var result = game.GiveStarterSeeds(Starter);

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), result);
        Assert.Equal(before, TestGames.FullSnapshot(game));
        Assert.False(game.OpeningDone);
    }

    [Fact]
    public void AC3_GiveStarterSeeds_into_a_full_backpack_drops_nothing()
    {
        var game = OpeningGames.NotDone(TestGames.FullBackpack());

        Assert.Equal(ActionResult.Rejected(Rejection.BackpackFull), game.GiveStarterSeeds(Starter));

        Assert.Empty(game.DroppedItems);
    }

    [Fact]
    public void OpeningOver_is_appended_after_the_existing_rejections()
    {
        var values = Enum.GetValues<Rejection>();

        Assert.Equal(Rejection.OpeningOver, values[^1]);
        Assert.Equal(Rejection.AreaClosed, values[^2]);
    }
}
