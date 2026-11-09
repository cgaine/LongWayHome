using LongWayHome.Application.Games;
using LongWayHome.Domain.Buildings;
using LongWayHome.Domain.Catalog;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #163 AC1: the Game's Buildings, one per <see cref="BuildingId"/> in enum order, with the new-Game stages
/// (general_store 1, the others 0) and nothing delivered; a supplied list is validated like Residents.
/// </summary>
public class BuildingTests
{
    [Fact]
    public void AC1_A_new_game_has_the_store_at_stage_1_and_the_others_at_0_with_nothing_delivered()
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        Assert.Equal(BuildingGames.NewGameBuildings, BuildingGames.Describe(game.Buildings));
    }

    [Fact]
    public void AC1_A_game_built_without_a_buildings_list_gets_the_new_game_buildings()
    {
        var game = BuildingGames.WithBuildings(null);

        Assert.Equal(BuildingGames.NewGameBuildings, BuildingGames.Describe(game.Buildings));
    }

    [Fact]
    public void AC1_DefaultBuildings_is_one_per_building_id_in_enum_order_at_the_new_game_stages()
    {
        Assert.Equal(BuildingGames.NewGameBuildings, BuildingGames.Describe(GameState.DefaultBuildings()));
    }

    [Theory]
    [InlineData(BuildingId.GeneralStore, 1)]
    [InlineData(BuildingId.Carpenter, 0)]
    [InlineData(BuildingId.MirasHouse, 0)]
    [InlineData(BuildingId.Blacksmith, 0)]
    public void AC1_BuildingStage_of_a_new_game_reads_each_buildings_stage(BuildingId id, int stage)
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);

        Assert.Equal(stage, game.BuildingStage(id));
    }

    [Fact]
    public void A_supplied_buildings_list_is_kept()
    {
        var game = BuildingGames.WithBuildings(
            BuildingGames.All(storeStage: 2, storeGold: 40, (Domain.Players.MaterialId.Plank, 7)));

        Assert.Equal(
            "GeneralStore:2:Plank=7:40,Carpenter:0:-:0,MirasHouse:0:-:0,Blacksmith:0:-:0",
            BuildingGames.Describe(game.Buildings));
        Assert.Equal(2, game.BuildingStage(BuildingId.GeneralStore));
    }

    public static TheoryData<string> BadBuildingLists => new()
    {
        "empty",
        "missing blacksmith",
        "duplicate store",
        "out of enum order",
        "undefined id",
        "null entry",
    };

    [Theory]
    [MemberData(nameof(BadBuildingLists))]
    public void The_constructor_refuses_a_buildings_list_that_is_not_one_per_id_in_enum_order(string how)
    {
        var buildings = BuildingGames.All().Cast<Building?>().ToList();
        switch (how)
        {
            case "empty":
                buildings.Clear();
                break;
            case "missing blacksmith":
                buildings.RemoveAt(3);
                break;
            case "duplicate store":
                buildings.Add(new Building(BuildingId.GeneralStore, 1));
                break;
            case "out of enum order":
                (buildings[0], buildings[1]) = (buildings[1], buildings[0]);
                break;
            case "undefined id":
                buildings[3] = new Building((BuildingId)99, 0);
                break;
            case "null entry":
                buildings[2] = null;
                break;
        }

        Assert.ThrowsAny<ArgumentException>(() => BuildingGames.WithBuildings(buildings!));
    }

    [Fact]
    public void BuildingStage_of_a_value_outside_the_enum_throws()
    {
        var game = BuildingGames.WithBuildings(null);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.BuildingStage((BuildingId)99));
    }
}
