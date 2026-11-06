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

    private static Player CreatePlayer(int energy) =>
        new(
            new Gold(100),
            energy,
            new HashSet<ToolId> { ToolId.Hoe, ToolId.WateringCan },
            new WateringCan(20),
            new Inventory());
}
