namespace LongWayHome.Domain.Players;

/// <summary>The kind of an inventory item. Serialized as <c>seed</c>, <c>crop</c>, <c>tool</c>, <c>material</c>, <c>placeable</c>.</summary>
public enum ItemKind
{
    Seed,
    Crop,
    Tool,
    Material,
    Placeable,
}
