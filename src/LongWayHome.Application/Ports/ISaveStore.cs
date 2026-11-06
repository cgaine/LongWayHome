using LongWayHome.Application.Saves;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Ports;

/// <summary>
/// Stores whole Games in numbered save slots 1 to <see cref="SaveSlots.Count"/> (BR-G5). A slot outside that range
/// throws <see cref="ArgumentOutOfRangeException"/> from every member that takes one.
/// </summary>
public interface ISaveStore
{
    /// <summary>One entry per slot 1 to <see cref="SaveSlots.Count"/>, in slot order.</summary>
    IReadOnlyList<SaveSlotInfo> List();

    /// <summary>The saved Game; throws <see cref="SaveStoreException"/> when the slot is empty or unreadable.</summary>
    Game Load(int slot);

    void Save(int slot, Game game);
}
