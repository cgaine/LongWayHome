using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Players;

public class PlayerTests
{
    [Fact]
    public void AC7_BR_P1_player_energy_below_0_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePlayer(energy: -1));
    }

    [Fact]
    public void AC7_BR_P1_player_energy_above_150_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePlayer(energy: 151));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(150)]
    public void BR_P1_player_energy_of_0_and_150_is_accepted(int energy)
    {
        var player = CreatePlayer(energy);

        Assert.Equal(energy, player.Energy);
    }

    [Fact]
    public void BR_P1_player_max_energy_is_150()
    {
        Assert.Equal(150, Player.MaxEnergy);
    }

    [Fact]
    public void BR_P1_spending_more_energy_than_held_fails_and_changes_nothing()
    {
        var player = CreatePlayer(energy: 1);

        var spent = player.TrySpendEnergy(2);

        Assert.False(spent);
        Assert.Equal(1, player.Energy);
    }

    [Fact]
    public void BR_P1_spending_exactly_the_energy_held_leaves_0()
    {
        var player = CreatePlayer(energy: 2);

        var spent = player.TrySpendEnergy(2);

        Assert.True(spent);
        Assert.Equal(0, player.Energy);
    }

    [Fact]
    public void BR_P1_spending_energy_with_none_left_fails()
    {
        var player = CreatePlayer(energy: 0);

        Assert.False(player.TrySpendEnergy(2));
        Assert.Equal(0, player.Energy);
    }

    [Fact]
    public void Spending_energy_subtracts_the_amount()
    {
        var player = CreatePlayer(energy: 150);

        Assert.True(player.TrySpendEnergy(5));
        Assert.Equal(145, player.Energy);
    }

    [Fact]
    public void BR_P1_spending_a_negative_amount_is_refused_so_energy_cannot_rise_above_150()
    {
        var player = CreatePlayer(energy: 150);

        Assert.Throws<ArgumentOutOfRangeException>(() => player.TrySpendEnergy(-1));
        Assert.Equal(150, player.Energy);
    }

    [Fact]
    public void Player_rejects_null_tools_can_or_inventory()
    {
        var tools = new HashSet<ToolId> { ToolId.Hoe };

        Assert.Throws<ArgumentNullException>(() => new Player(new Gold(0), 0, null!, new WateringCan(20), new Inventory()));
        Assert.Throws<ArgumentNullException>(() => new Player(new Gold(0), 0, tools, null!, new Inventory()));
        Assert.Throws<ArgumentNullException>(() => new Player(new Gold(0), 0, tools, new WateringCan(20), null!));
    }

    [Fact]
    public void Player_tools_are_a_copy_of_the_set_passed_in()
    {
        var tools = new HashSet<ToolId> { ToolId.Hoe };
        var player = new Player(new Gold(0), 0, tools, new WateringCan(20), new Inventory());

        tools.Add(ToolId.WateringCan);

        Assert.True(player.Tools.SetEquals([ToolId.Hoe]));
    }

    [Fact]
    public void Player_exposes_the_values_it_was_created_with()
    {
        var can = new WateringCan(20);
        var inventory = new Inventory();

        var player = new Player(new Gold(42), 10, new HashSet<ToolId>(), can, inventory);

        Assert.Equal(new Gold(42), player.Gold);
        Assert.Same(can, player.WateringCan);
        Assert.Same(inventory, player.Inventory);
        Assert.Empty(player.Tools);
    }

    [Fact]
    public void BR_P5_earning_negative_gold_throws_and_leaves_gold_unchanged()
    {
        var player = CreatePlayer(energy: 150);

        Assert.Throws<ArgumentOutOfRangeException>(() => player.EarnGold(-1));
        Assert.Equal(new Gold(100), player.Gold);
    }

    [Fact]
    public void BR_P5_earning_gold_past_int_max_throws_overflow_and_leaves_gold_unchanged()
    {
        var player = CreatePlayer(energy: 150);

        Assert.Throws<OverflowException>(() => player.EarnGold(int.MaxValue));
        Assert.Equal(new Gold(100), player.Gold);
    }

    [Fact]
    public void Earning_0_gold_leaves_gold_unchanged()
    {
        var player = CreatePlayer(energy: 150);

        player.EarnGold(0);

        Assert.Equal(new Gold(100), player.Gold);
    }

    [Fact]
    public void Earning_50_gold_adds_50()
    {
        var player = CreatePlayer(energy: 150);

        player.EarnGold(50);

        Assert.Equal(new Gold(150), player.Gold);
    }

    [Fact]
    public void BR_P5_spending_negative_gold_throws_and_leaves_gold_unchanged()
    {
        var player = CreatePlayer(energy: 150);

        Assert.Throws<ArgumentOutOfRangeException>(() => player.TrySpendGold(-1));
        Assert.Equal(new Gold(100), player.Gold);
    }

    [Fact]
    public void BR_L2_spending_more_gold_than_held_fails_and_changes_nothing()
    {
        var player = CreatePlayer(energy: 150);

        var spent = player.TrySpendGold(101);

        Assert.False(spent);
        Assert.Equal(new Gold(100), player.Gold);
    }

    [Fact]
    public void BR_L2_spending_gold_held_subtracts_the_amount()
    {
        var player = CreatePlayer(energy: 150);

        var spent = player.TrySpendGold(15);

        Assert.True(spent);
        Assert.Equal(new Gold(85), player.Gold);
    }

    [Fact]
    public void BR_P5_spending_exactly_the_gold_held_leaves_0()
    {
        var player = CreatePlayer(energy: 150);

        var spent = player.TrySpendGold(100);

        Assert.True(spent);
        Assert.Equal(new Gold(0), player.Gold);
    }

    private static Player CreatePlayer(int energy) =>
        new(
            new Gold(100),
            energy,
            new HashSet<ToolId> { ToolId.Hoe, ToolId.WateringCan },
            new WateringCan(20),
            new Inventory());
}
