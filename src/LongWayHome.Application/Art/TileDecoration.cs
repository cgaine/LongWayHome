using LongWayHome.Domain.Farming;

namespace LongWayHome.Application.Art;

/// <summary>The native plant a grass Tile may carry (#78).</summary>
public enum NativePlantKind
{
    None,
    Frond,
    Puffball,
}

/// <summary>
/// A grass Tile's deterministic decoration (#78): its native plant, if any, and where its three tufts sit, all from a
/// hash of the Tile's grid position. Pure C#, no Godot.
/// </summary>
public sealed class TileDecoration
{
    private TileDecoration(NativePlantKind nativePlant, IReadOnlyList<(float X, float Z)> tuftOffsets)
    {
        NativePlant = nativePlant;
        TuftOffsets = tuftOffsets;
    }

    public NativePlantKind NativePlant { get; }

    /// <summary>Exactly three tuft offsets from the Tile's centre, each within ±0.35 m on both axes.</summary>
    public IReadOnlyList<(float X, float Z)> TuftOffsets { get; }

    /// <summary>The position hash: <c>(X * 73856093) ^ (Y * 19349663)</c>.</summary>
    public static int Hash(GridPosition position) => throw new NotImplementedException();

    /// <summary>The decoration for a grid position; the same position always gives the same decoration.</summary>
    public static TileDecoration For(GridPosition position) => throw new NotImplementedException();
}
