using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Domain.Nodes;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using GameState = LongWayHome.Domain.Games.Game;

namespace LongWayHome.Tests.Farming;

/// <summary>Placed objects, and Games holding them, for #164's fence and chest tests.</summary>
internal static class PlacedGames
{
    /// <summary>The slots a chest has (BR-F2).</summary>
    public const int ChestSlotCount = 36;

    public static ItemRef Fence => ItemRef.Placeable(PlaceableId.Fence);

    public static ItemRef Chest => ItemRef.Placeable(PlaceableId.Chest);

    public static ItemRef Wood => ItemRef.Material(MaterialId.Wood);

    public static ItemRef Stone => ItemRef.Material(MaterialId.Stone);

    /// <summary>A fence on <paramref name="position"/>.</summary>
    public static PlacedObject FenceAt(GridPosition position) => new(position, PlaceableId.Fence);

    /// <summary>A chest on <paramref name="position"/>: the given slots, and an empty slot at every other index 0–35.</summary>
    public static PlacedObject ChestAt(GridPosition position, params BackpackSlot[] filled) =>
        new(position, PlaceableId.Chest, ChestSlots(filled));

    /// <summary>36 chest slots: the given ones, the rest empty.</summary>
    public static IReadOnlyList<BackpackSlot> ChestSlots(params BackpackSlot[] filled)
    {
        var given = filled.ToDictionary(slot => slot.Index);
        return Enumerable.Range(0, ChestSlotCount)
            .Select(index => given.TryGetValue(index, out var slot) ? slot : new BackpackSlot(index, null, 0, null))
            .ToList();
    }

    /// <summary>
    /// A Game on <paramref name="date"/> (Spring 1) at <paramref name="energy"/> (150) carrying <paramref name="backpack"/>,
    /// grass except for <paramref name="tiles"/>, with the given Placed objects on its plot and the given nodes and
    /// NodeRegrowths, passed through the constructors.
    /// </summary>
    public static GameState With(
        Backpack backpack,
        PlacedObject[] placed,
        Tile[]? tiles = null,
        ResourceNode[]? nodes = null,
        NodeRegrowth[]? regrowths = null,
        GameDate? date = null,
        int energy = 150)
    {
        var overrides = (tiles ?? []).ToDictionary(tile => tile.Position);
        var all = new List<Tile>();
        for (var y = 0; y < FarmPlot.Height; y++)
        {
            for (var x = 0; x < FarmPlot.Width; x++)
            {
                var position = new GridPosition(x, y);
                all.Add(overrides.TryGetValue(position, out var tile) ? tile : new Tile(position));
            }
        }

        return new GameState(
            Guid.NewGuid(),
            date ?? TestGames.FirstDay,
            360,
            Weather.Clear,
            null,
            TestGames.Now,
            TestGames.Now,
            new Player(new Gold(100), energy, new WateringCan(20), backpack),
            new FarmPlot(all, placed),
            resourceNodes: nodes,
            nodeRegrowths: regrowths);
    }

    /// <summary>A Game on a plain plot (no Placed objects passed in), for actions that place them through the Game.</summary>
    public static GameState Plain(Backpack backpack, Tile[]? tiles = null, ResourceNode[]? nodes = null, int energy = 150)
    {
        var game = TestGames.WithBackpack(backpack, energy: energy, tiles: tiles ?? []);
        return nodes is null ? game : NodeGames.WithNodes(game, nodes);
    }

    /// <summary>One Slot as "index:kind:id:quantity:tier", naming the material or placeable too.</summary>
    public static string DescribeSlot(BackpackSlot slot) =>
        $"{slot.Index}:{slot.Item?.Kind.ToString() ?? "-"}:{IdOf(slot.Item)}:{slot.Quantity}:{slot.ToolTier?.ToString() ?? "-"}";

    /// <summary>Every Slot, as <see cref="DescribeSlot"/> writes it.</summary>
    public static string Describe(IEnumerable<BackpackSlot> slots) => string.Join(",", slots.Select(DescribeSlot));

    /// <summary>Every Placed object as "x:y:placeable:[slots]", sorted, to compare plots or prove nothing changed.</summary>
    public static IReadOnlyList<string> Describe(FarmPlot plot) =>
        plot.PlacedObjects
            .Select(placed => $"{placed.Position.X}:{placed.Position.Y}:{placed.Placeable}:[{Describe(placed.Slots)}]")
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Energy, every Tile, every Backpack Slot and every Placed object, to prove an action changed nothing.</summary>
    public static (int Energy, string Tiles, string Backpack, string Placed) Snapshot(GameState game) =>
        (game.Player.Energy,
            TestGames.FullSnapshot(game).Tiles,
            Describe(game.Player.Backpack.Slots),
            string.Join("|", Describe(game.FarmPlot)));

    /// <summary>The total of an item across a chest's slots.</summary>
    public static int QuantityIn(PlacedObject chest, ItemRef item) =>
        chest.Slots.Where(slot => slot.Item == item).Sum(slot => slot.Quantity);

    private static string IdOf(ItemRef? item) =>
        item is not { } value
            ? "-"
            : value.CropTypeId?.ToString()
              ?? value.ToolId?.ToString()
              ?? value.MaterialId?.ToString()
              ?? value.PlaceableId?.ToString()
              ?? "-";
}
