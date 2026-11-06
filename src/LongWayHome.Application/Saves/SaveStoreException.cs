namespace LongWayHome.Application.Saves;

/// <summary>A save slot could not be loaded: it is empty or unreadable. Carries the slot and a one-line reason.</summary>
public sealed class SaveStoreException : Exception
{
    public SaveStoreException(int slot, string reason)
        : base($"Save slot {slot}: {reason}")
    {
        Slot = slot;
        Reason = reason;
    }

    public int Slot { get; }

    public string Reason { get; }
}
