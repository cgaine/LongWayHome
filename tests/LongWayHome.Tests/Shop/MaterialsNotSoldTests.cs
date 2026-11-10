using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Shop;

/// <summary>
/// #152 AC10: materials can't be sold or shipped (canonical-values.md ItemKind). They never appear in Maggie's sell
/// list, Sell all leaves them, and Ship all leaves them in the Backpack.
/// </summary>
public class MaterialsNotSoldTests
{
    private static ItemRef Wood => ItemRef.Material(MaterialId.Wood);

    private static ItemRef Stone => ItemRef.Material(MaterialId.Stone);

    private static Backpack WithMaterialsAndACrop() => TestGames.RestoredBackpack(
        new BackpackSlot(2, Wood, 40, null),
        new BackpackSlot(3, ItemRef.Crop(CropTypeId.Emberroot), 2, null),
        new BackpackSlot(4, Stone, 7, null));

    private static GameSession SessionInTown(Backpack backpack)
    {
        var session = new GameSession(TestGames.WithBackpack(backpack), TestGames.Shipped);
        Assert.Equal(ActionResult.Ok, session.EnterArea(AreaId.Town));
        return session;
    }

    [Fact]
    public void AC10_materials_never_appear_in_maggies_sell_list()
    {
        var session = SessionInTown(WithMaterialsAndACrop());

        var rows = session.SellableCrops();

        Assert.Equal([CropTypeId.Emberroot], rows.Select(row => row.CropTypeId));
    }

    [Fact]
    public void AC10_sell_all_to_maggie_leaves_the_materials_in_the_backpack()
    {
        var session = SessionInTown(WithMaterialsAndACrop());

        Assert.Equal(ActionResult.Ok, session.SellAllToMaggie());

        Assert.Equal(40, session.Game.Player.Backpack.Quantity(Wood));
        Assert.Equal(7, session.Game.Player.Backpack.Quantity(Stone));
        Assert.Equal(0, session.Game.Player.Backpack.Quantity(ItemRef.Crop(CropTypeId.Emberroot)));
    }

    [Fact]
    public void AC10_with_only_materials_carried_there_is_nothing_to_sell()
    {
        var session = SessionInTown(TestGames.RestoredBackpack(new BackpackSlot(2, Wood, 40, null)));

        Assert.Empty(session.SellableCrops());
        Assert.Equal(ActionResult.Rejected(Rejection.NothingToSell), session.SellAllToMaggie());
        Assert.Equal(40, session.Game.Player.Backpack.Quantity(Wood));
    }

    [Fact]
    public void AC10_ship_all_ships_the_crops_and_leaves_the_materials_in_the_backpack()
    {
        var game = TestGames.WithStoreAt(TestGames.WithBackpack(WithMaterialsAndACrop()));

        Assert.Equal(ActionResult.Ok, game.ShipAll());

        Assert.Equal(40, game.Player.Backpack.Quantity(Wood));
        Assert.Equal(7, game.Player.Backpack.Quantity(Stone));
        Assert.Equal([CropTypeId.Emberroot], game.ShippingBin.Items.Select(item => item.CropTypeId));
    }

    [Fact]
    public void AC10_with_only_materials_carried_ship_all_is_nothing_to_ship()
    {
        var game = TestGames.WithStoreAt(TestGames.WithBackpack(TestGames.RestoredBackpack(new BackpackSlot(2, Stone, 3, null))));

        Assert.Equal(ActionResult.Rejected(Rejection.NothingToShip), game.ShipAll());

        Assert.Equal(3, game.Player.Backpack.Quantity(Stone));
        Assert.Empty(game.ShippingBin.Items);
    }
}
