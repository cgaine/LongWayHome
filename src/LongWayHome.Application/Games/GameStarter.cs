using LongWayHome.Application.Data;
using LongWayHome.Application.Ports;

namespace LongWayHome.Application.Games;

/// <summary>Starts a new Game in a save slot or loads one, returning a session that saves to that slot (BR-G5).</summary>
public static class GameStarter
{
    /// <summary>Creates a new Game, saves it to the slot at once and returns a session saving to that slot.</summary>
    public static GameSession StartNew(GameData data, ISaveStore store, int slot, IRandomSource random, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(time);
        var game = NewGameFactory.Create(data, time.GetUtcNow());
        store.Save(slot, game);
        return new GameSession(game, data, random, store, slot, time);
    }

    /// <summary>Loads the slot's Game and returns a session saving to that slot. Store exceptions propagate.</summary>
    public static GameSession Load(GameData data, ISaveStore store, int slot, IRandomSource random, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(store);
        var game = store.Load(slot);
        return new GameSession(game, data, random, store, slot, time);
    }
}
