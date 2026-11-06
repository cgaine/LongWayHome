namespace LongWayHome.Application.Saves;

/// <summary>The save slots a save store offers.</summary>
public static class SaveSlots
{
    /// <summary>How many save slots there are, numbered 1 to Count.</summary>
    public const int Count = 3;

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when the slot is not between 1 and <see cref="Count"/>.</summary>
    public static void ThrowIfOutOfRange(int slot)
    {
        if (slot is < 1 or > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, $"A save slot is between 1 and {Count}.");
        }
    }
}
