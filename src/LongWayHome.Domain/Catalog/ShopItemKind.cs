namespace LongWayHome.Domain.Catalog;

/// <summary>What a Shop listing sells. Serialized as <c>seed</c>, <c>can_upgrade</c>, <c>tool</c>, <c>backpack_upgrade</c> and <c>placeable</c>.</summary>
public enum ShopItemKind
{
    Seed,
    CanUpgrade,
    Tool,
    BackpackUpgrade,
    Placeable,
}
