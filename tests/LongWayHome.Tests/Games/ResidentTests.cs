using System.Reflection;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Residents;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Games;

/// <summary>
/// #138: the Game's Residents (BR-R1): exactly one per <see cref="ResidentId"/> in enum order, Maggie and Clyde present
/// by default, a supplied list validated by the constructor, and no way to set a Resident's <c>Present</c> back to false.
/// </summary>
public class ResidentTests
{
    /// <summary>The Game <see cref="TestGames.NewSession"/> starts, rebuilt with the given residents list.</summary>
    internal static GameState WithResidents(IReadOnlyList<Resident>? residents)
    {
        var game = NewGameFactory.Create(TestGames.Shipped, TestGames.Now);
        return new GameState(
            game.Id,
            game.Date,
            game.ClockMinutes,
            game.Weather,
            game.LastDayEnd,
            game.CreatedAt,
            game.SavedAt,
            game.Player,
            game.FarmPlot,
            game.ShippingBin,
            residents);
    }

    /// <summary>All six Residents in enum order, the given ones present.</summary>
    internal static List<Resident> AllSix(params ResidentId[] present) =>
        Enum.GetValues<ResidentId>().Select(id => new Resident(id, present.Contains(id))).ToList();

    private static string Describe(GameState game) =>
        string.Join(",", game.Residents.Select(resident => $"{resident.Id}:{resident.Present}"));

    [Fact]
    public void BR_R1_A_game_built_without_a_residents_list_has_maggie_and_clyde_present_and_the_others_not()
    {
        var game = WithResidents(null);

        Assert.Equal(
            "Maggie:True,Clyde:True,Tom:False,June:False,Jesse:False,Mira:False",
            Describe(game));
        Assert.True(game.IsPresent(ResidentId.Maggie));
        Assert.True(game.IsPresent(ResidentId.Clyde));
        Assert.False(game.IsPresent(ResidentId.Tom));
        Assert.False(game.IsPresent(ResidentId.June));
        Assert.False(game.IsPresent(ResidentId.Jesse));
        Assert.False(game.IsPresent(ResidentId.Mira));
    }

    [Fact]
    public void BR_R1_A_supplied_residents_list_is_kept_and_is_present_reads_it()
    {
        var game = WithResidents(AllSix(ResidentId.Maggie, ResidentId.Clyde, ResidentId.Tom, ResidentId.Mira));

        Assert.Equal(
            "Maggie:True,Clyde:True,Tom:True,June:False,Jesse:False,Mira:True",
            Describe(game));
        Assert.True(game.IsPresent(ResidentId.Tom));
        Assert.True(game.IsPresent(ResidentId.Mira));
        Assert.False(game.IsPresent(ResidentId.June));
    }

    [Fact]
    public void BR_R1_No_public_api_sets_a_residents_present_back_to_false()
    {
        var present = typeof(Resident).GetProperty(nameof(Resident.Present))!;

        Assert.Equal(typeof(bool), present.PropertyType);
        Assert.True(
            present.SetMethod is null || !present.SetMethod.IsPublic,
            "Resident.Present has a public setter; BR-R1 says a Resident who arrives never leaves");
        var methods = typeof(Resident)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToList();
        Assert.True(methods.Count == 0, $"Resident declares public methods ({string.Join(", ", methods)}); #138 adds none");
    }

    public static TheoryData<string> BadResidentLists => new()
    {
        "empty",
        "missing mira",
        "duplicate maggie",
        "out of enum order",
        "undefined id",
        "null entry",
    };

    [Theory]
    [MemberData(nameof(BadResidentLists))]
    public void BR_R1_The_constructor_refuses_a_residents_list_that_is_not_one_per_id_in_enum_order(string how)
    {
        var residents = AllSix(ResidentId.Maggie, ResidentId.Clyde).Cast<Resident?>().ToList();
        switch (how)
        {
            case "empty":
                residents.Clear();
                break;
            case "missing mira":
                residents.RemoveAt(5);
                break;
            case "duplicate maggie":
                residents.Add(new Resident(ResidentId.Maggie, true));
                break;
            case "out of enum order":
                (residents[0], residents[1]) = (residents[1], residents[0]);
                break;
            case "undefined id":
                residents[5] = new Resident((ResidentId)99, false);
                break;
            case "null entry":
                residents[2] = null;
                break;
        }

        Assert.ThrowsAny<ArgumentException>(() => WithResidents(residents!));
    }

    [Fact]
    public void Is_present_of_a_value_outside_the_enum_throws()
    {
        var game = WithResidents(null);

        Assert.Throws<ArgumentOutOfRangeException>(() => game.IsPresent((ResidentId)99));
    }
}
