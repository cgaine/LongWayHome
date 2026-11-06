using LongWayHome.Application.Ports;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>An <see cref="ISaveStore"/> that saves nowhere, for the sessions built without a save store.</summary>
internal sealed class NoSaveStore : ISaveStore
{
    public static readonly NoSaveStore Instance = new();

    private NoSaveStore()
    {
    }

    public IReadOnlyList<SaveSlotInfo> List() => throw new NotImplementedException();

    public Game Load(int slot) => throw new NotImplementedException();

    public void Save(int slot, Game game) => throw new NotImplementedException();
}
