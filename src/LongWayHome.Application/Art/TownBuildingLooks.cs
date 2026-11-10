namespace LongWayHome.Application.Art;

/// <summary>
/// Which look a town Building is drawn at for its stage (#165), and the general store's storeroom drum collision.
/// Pure C#, no Godot.
/// </summary>
public static class TownBuildingLooks
{
    /// <summary>The storeroom drum's centre, building-local x (metres).</summary>
    public const float StoreroomOffsetX = 4.35f;

    /// <summary>The storeroom drum's centre, building-local z (metres).</summary>
    public const float StoreroomOffsetZ = -0.6f;

    /// <summary>The storeroom drum's wall radius (metres).</summary>
    public const float StoreroomRadius = 1.7f;

    /// <summary>The storeroom collision's height (metres).</summary>
    public const float StoreroomHeight = 2.3f;

    /// <summary>The store's look at <paramref name="stage"/>: 1–4; anything else throws ArgumentOutOfRangeException.</summary>
    public static StoreStage Store(int stage) => throw new NotImplementedException();

    /// <summary>Whether the store's look has the storeroom drum (Storeroom, ExpandedShop).</summary>
    public static bool HasStoreroom(StoreStage stage) => throw new NotImplementedException();
}
