using LongWayHome.Application.Ports;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>
/// An <see cref="ISaveStore"/> that saves nowhere, for the sessions built without a save store: every slot is always
/// empty.
/// </summary>
internal sealed class NoSaveStore : ISaveStore
{
    public static readonly NoSaveStore Instance = new();

    private NoSaveStore()
    {
    }

    public IReadOnlyList<SaveSlotInfo> List() =>
        Enumerable.Range(1, SaveSlots.Count)
            .Select(slot => new SaveSlotInfo(slot, SaveSlotState.Empty, null, null, null))
            .ToList()
            .AsReadOnly();

    public Game Load(int slot)
    {
        SaveSlots.ThrowIfOutOfRange(slot);
        throw new SaveStoreException(slot, "the slot is empty");
    }

    public void Save(int slot, Game game)
    {
        SaveSlots.ThrowIfOutOfRange(slot);
        ArgumentNullException.ThrowIfNull(game);
    }
}
