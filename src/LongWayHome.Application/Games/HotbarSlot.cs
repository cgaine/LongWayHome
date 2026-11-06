namespace LongWayHome.Application.Games;

/// <summary>One slot of the HUD hotbar: the held item it holds, its label, and whether it is the selected one.</summary>
public sealed record HotbarSlot(HeldItem Item, string Label, bool Selected);
