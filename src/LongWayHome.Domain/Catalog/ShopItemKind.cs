namespace LongWayHome.Domain.Catalog;

/// <summary>What a Shop listing sells. Serialized as <c>seed</c> and <c>can_upgrade</c>.</summary>
public enum ShopItemKind
{
    Seed,
    CanUpgrade,
    Tool,
    BackpackUpgrade,
    Placeable,
}
