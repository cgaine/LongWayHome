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
/// hash of the Tile's grid position, so the same Tile looks the same after a save and load. Pure C#, no Godot.
/// <para>
/// About one Tile in six carries a native plant (<c>hash % 6 == 0</c>). Its kind and the tuft offsets come from a
/// small integer mixer seeded with the hash rather than <see cref="System.Random"/>, whose algorithm .NET does not
/// promise to keep across versions.
/// </para>
/// </summary>
public sealed class TileDecoration
{
    /// <summary>How far a tuft may sit from the Tile's centre, on each axis.</summary>
    public const float MaxTuftOffset = 0.35f;

    private const int TuftCount = 3;

    private TileDecoration(NativePlantKind nativePlant, IReadOnlyList<(float X, float Z)> tuftOffsets)
    {
        NativePlant = nativePlant;
        TuftOffsets = tuftOffsets;
    }

    public NativePlantKind NativePlant { get; }

    /// <summary>Exactly three tuft offsets from the Tile's centre, each within ±0.35 m on both axes.</summary>
    public IReadOnlyList<(float X, float Z)> TuftOffsets { get; }

    /// <summary>The position hash: <c>(X * 73856093) ^ (Y * 19349663)</c>.</summary>
    public static int Hash(GridPosition position) =>
        unchecked((position.X * 73856093) ^ (position.Y * 19349663));

    /// <summary>The decoration for a grid position; the same position always gives the same decoration.</summary>
    public static TileDecoration For(GridPosition position)
    {
        var hash = unchecked((uint)Hash(position));
        var state = hash;

        var kind = NativePlantKind.None;
        if (hash % 6 == 0)
        {
            kind = (Next(ref state) & 1) == 0 ? NativePlantKind.Frond : NativePlantKind.Puffball;
        }

        var offsets = new (float X, float Z)[TuftCount];
        for (var i = 0; i < TuftCount; i++)
        {
            offsets[i] = (Offset(ref state), Offset(ref state));
        }

        return new TileDecoration(kind, offsets);
    }

    /// <summary>A value in −0.35..0.35 from the next 24 random bits.</summary>
    private static float Offset(ref uint state)
    {
        var unit = (Next(ref state) >> 8) / (float)(1 << 24);
        return Math.Clamp(-MaxTuftOffset + (2f * MaxTuftOffset * unit), -MaxTuftOffset, MaxTuftOffset);
    }

    /// <summary>The next value of a Weyl sequence through the murmur3 finaliser: fixed arithmetic, stable everywhere.</summary>
    private static uint Next(ref uint state)
    {
        unchecked
        {
            state += 0x9E3779B9u;
            var z = state;
            z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
            z = (z ^ (z >> 13)) * 0xC2B2AE35u;
            return z ^ (z >> 16);
        }
    }
}
