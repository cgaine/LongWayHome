using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace LongWayHome.Application.Art;

/// <summary>One palette swatch: an ART_DIRECTION colour role and its uppercase hex, e.g. "#7FA65A".</summary>
public readonly record struct ArtSwatch(string Role, string Hex);

/// <summary>
/// The shared art palette (#76): every ART_DIRECTION model colour as a named swatch, laid out in the 64 x 64
/// <c>game/art/palette/palette.png</c> that the shared material samples. Swatch <c>i</c> is the 8 x 8 pixel block at
/// column <c>i % 8</c>, row <c>i / 8</c>; unused blocks are transparent black.
/// <para>
/// The list is <b>append-only</b>: a new swatch goes at the end and existing ones never move or change role, so UVs
/// already mapped onto the texture (imported models, <see cref="UvCentre"/> callers) stay valid. After changing it,
/// regenerate the committed PNG (<c>UPDATE_PALETTE=1 dotnet test --filter AC4_committed_palette_png_is_current</c>).
/// </para>
/// Pure C#, no Godot.
/// </summary>
public static class ArtPalette
{
    public const int Columns = 8;
    public const int SwatchSize = 8;
    public const int ImageSize = 64;

    /// <summary>How many swatches the texture holds: one 8 x 8 block per cell of the 8 x 8 grid.</summary>
    public const int Capacity = (ImageSize / SwatchSize) * Columns;

    /// <summary>Every swatch, in texture order. Append-only.</summary>
    public static IReadOnlyList<ArtSwatch> Swatches { get; } =
    [
        new("grass-lit", "#7FA65A"),
        new("grass-shade", "#3F7A6C"),
        new("grass-mown", "#8CB862"),
        new("grass-fall", "#A8A458"),
        new("snow", "#E4ECF0"),
        new("foliage", "#3E8F82"),
        new("leaf-underside", "#7A68A8"),
        new("soil-tilled", "#8A5A3C"),
        new("soil-watered", "#4A3226"),
        new("timber", "#9C7E8C"),
        new("trunk", "#6B5A78"),
        new("stone", "#8F8A96"),
        new("rock", "#9A95A2"),
        new("dirt-path", "#B8946C"),
        new("lake-deep", "#2D6A86"),
        new("lake-shallow", "#5FB3AA"),
        new("ember", "#E9773A"),
        new("lantern", "#FFB04A"),
        new("glow-cool", "#6FF2E0"),
        new("glow-rare", "#B48CFF"),
        new("ship-metal", "#C9D6E8"),
        new("target-highlight", "#FFF6E0"),
        new("dusktuber", "#4A2A66"),
        new("moonmelon", "#C9DCF5"),
        new("sunbell-gold", "#F7C531"),
        new("sunbell-orange", "#E9821A"),
        new("starcorn", "#E8B830"),
        new("coverall-shade", "#A3B3CB"),
        new("navy", "#34405E"),
        new("boots", "#3B3F4A"),
        new("earth-blue", "#3D7FC4"),
        new("earth-green", "#5BAF6A"),
        new("wrist-screen", "#9FD3FF"),
        new("timber-side", "#7E6474"),
        new("timber-grain", "#6E5664"),
        new("timber-trim", "#5A4458"),
        new("roof-wilted", "#8A7A4A"),
        new("tips-dry", "#9A7E44"),
        new("trunk-grey", "#6E6670"),
        new("wall-grey", "#8A7C80"),
        new("roof-rows", "#2F7266"),
        new("roof-tips", "#4FA294"),
        new("roof-far", "#2F6B60"),
        new("grass-spring-mown", "#A6D07C"),
        new("grass-fall-mown", "#C0BC72"),
        new("starcorn-sparkle", "#FFFFFF"),
        new("skin", "#D9A07A"),
        new("skin-shade", "#B57E5F"),
        new("hair", "#5A3E2E"),
        new("seed-pouch", "#D9C39A"),
        new("door-faded", "#B88464"),
        new("glass-dull", "#8C97A3"),
        new("canopy-spring", "#5FAE8E"),
        new("canopy-fall", "#A0507A"),
        new("underside-fall", "#5E3A6E"),
        new("blossom", "#F2B8DE"),
        new("wood-cut", "#C9A88E"),
        new("rosk", "#C77A52"),
        new("groth", "#59627A"),
        new("canopy-spring-light", "#8CC8A8"),
        new("canopy-fall-light", "#C27098"),
        new("canopy-fall-shade", "#7E3E62"),
        new("groth-light", "#A3AEC8"),
    ];

    /// <summary>The centre of <paramref name="role"/>'s swatch in 0–1 texture space (U right, V down).</summary>
    /// <exception cref="ArgumentException">No swatch has that role.</exception>
    public static (float U, float V) UvCentre(string role)
    {
        var index = IndexOf(role);
        var column = index % Columns;
        var row = index / Columns;
        const float half = SwatchSize / 2f;
        return ((column * SwatchSize + half) / ImageSize, (row * SwatchSize + half) / ImageSize);
    }

    /// <summary>The <c>#RRGGBB</c> hex of <paramref name="role"/>'s swatch, e.g. <c>"#E9773A"</c> for <c>ember</c>.</summary>
    /// <exception cref="ArgumentException">No swatch has that role.</exception>
    public static string Hex(string role) => Swatches[IndexOf(role)].Hex;

    /// <summary>
    /// The palette image as a PNG: 64 x 64 RGBA8, filter type 0 on every row, one zlib-compressed IDAT chunk.
    /// The same bytes every call.
    /// </summary>
    public static byte[] EncodePng()
    {
        if (Swatches.Count > Capacity)
        {
            throw new InvalidOperationException(
                $"The palette has {Swatches.Count} swatches but the {ImageSize} x {ImageSize} texture holds {Capacity}.");
        }

        const int stride = ImageSize * 4;
        var raw = new byte[ImageSize * (stride + 1)];
        for (var i = 0; i < Swatches.Count; i++)
        {
            var (r, g, b) = ParseHex(Swatches[i].Hex);
            var left = i % Columns * SwatchSize;
            var top = i / Columns * SwatchSize;
            for (var y = top; y < top + SwatchSize; y++)
            {
                for (var x = left; x < left + SwatchSize; x++)
                {
                    // Each row starts with its filter byte (0, None), already zero.
                    var p = y * (stride + 1) + 1 + x * 4;
                    raw[p] = r;
                    raw[p + 1] = g;
                    raw[p + 2] = b;
                    raw[p + 3] = 255;
                }
            }
        }

        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(raw);
            }

            compressed = buffer.ToArray();
        }

        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), ImageSize);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), ImageSize);
        header[8] = 8; // bit depth
        header[9] = 6; // colour type: RGBA
        // compression 0, filter method 0, interlace 0: already zero.

        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", compressed);
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static int IndexOf(string role)
    {
        for (var i = 0; i < Swatches.Count; i++)
        {
            if (string.Equals(Swatches[i].Role, role, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new ArgumentException($"No palette swatch has the role '{role}'.", nameof(role));
    }

    private static (byte R, byte G, byte B) ParseHex(string hex) =>
    (
        byte.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        byte.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
    );

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(word, (uint)data.Length);
        stream.Write(word);

        var typeAndData = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type, typeAndData.AsSpan(0, 4));
        data.CopyTo(typeAndData.AsSpan(4));
        stream.Write(typeAndData);

        BinaryPrimitives.WriteUInt32BigEndian(word, Crc32(typeAndData));
        stream.Write(word);
    }

    /// <summary>The PNG chunk CRC (ISO 3309 / ITU-T V.42, polynomial 0xEDB88320), over type and data.</summary>
    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
