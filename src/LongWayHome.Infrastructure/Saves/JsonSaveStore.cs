using LongWayHome.Application.Ports;
using LongWayHome.Application.Saves;
using LongWayHome.Domain.Games;

namespace LongWayHome.Infrastructure.Saves;

/// <summary>
/// Saves each slot as versioned JSON in <c>&lt;folder&gt;/slot-&lt;n&gt;.json</c>, written through a <c>.tmp</c> file and
/// keeping the previous file as <c>.bak</c>, so a crash mid-save never corrupts the slot (blueprint §3, §9).
/// </summary>
public sealed class JsonSaveStore : ISaveStore
{
    public JsonSaveStore(string folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        Folder = folder;
    }

    public string Folder { get; }

    public IReadOnlyList<SaveSlotInfo> List() => throw new NotImplementedException();

    public Game Load(int slot) => throw new NotImplementedException();

    public void Save(int slot, Game game) => throw new NotImplementedException();
}
