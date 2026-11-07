using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>
/// One slot of the HUD hotbar: the held item it holds (null for an empty slot or an item that is neither a tool nor a
/// seed), the Backpack Slot's item and quantity (for the icon), its label, and whether it is the held position.
/// </summary>
public sealed record HotbarSlot(HeldItem? Item, ItemRef? SlotItem, int Quantity, string Label, bool Selected);
