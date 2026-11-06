namespace LongWayHome.Application.Saves;

/// <summary>Which save slots the title screen offers: the Continue slot, loadable slots and overwrite confirmation.</summary>
public static class SaveSlotChoice
{
    /// <summary>The Saved slot with the latest SavedAt (ties go to the lower slot), or null when no slot is Saved.</summary>
    public static int? ContinueSlot(IReadOnlyList<SaveSlotInfo> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        SaveSlotInfo? best = null;
        foreach (var slot in slots)
        {
            if (!CanLoad(slot))
            {
                continue;
            }

            if (best is null
                || slot.SavedAt > best.SavedAt
                || (slot.SavedAt == best.SavedAt && slot.Slot < best.Slot))
            {
                best = slot;
            }
        }

        return best?.Slot;
    }

    /// <summary>Only a Saved slot can be loaded.</summary>
    public static bool CanLoad(SaveSlotInfo slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return slot.State == SaveSlotState.Saved;
    }

    /// <summary>Starting a new game over a Saved or Unreadable slot asks first; an Empty slot does not.</summary>
    public static bool NeedsOverwriteConfirmation(SaveSlotInfo slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return slot.State != SaveSlotState.Empty;
    }
}
