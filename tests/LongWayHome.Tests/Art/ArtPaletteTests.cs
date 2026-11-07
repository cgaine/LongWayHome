using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.RegularExpressions;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The shared palette material (#76): <see cref="ArtPalette"/>'s swatch list, layout, UVs and PNG, the committed
/// <c>game/art/palette/palette.png</c>, and the shader, material, project and sandbox files that use it. The files
/// are read from the repository (found by walking up to <c>LongWayHome.sln</c>), not from copies in <c>bin/</c>.
/// </summary>
public class ArtPaletteTests
{
    private const string RegenerateHint =
        "run `UPDATE_PALETTE=1 dotnet test --filter AC4_committed_palette_png_is_current` to regenerate";

    /// <summary>The 43 swatches #76 shipped, indices 0–42. The list is append-only, so these never move or change.</summary>
    private static readonly (string Role, string Hex)[] BaselineSwatches =
    [
        ("grass-lit", "#7FA65A"),
        ("grass-shade", "#3F7A6C"),
        ("grass-mown", "#8CB862"),
        ("grass-fall", "#A8A458"),
        ("snow", "#E4ECF0"),
        ("foliage", "#3E8F82"),
        ("leaf-underside", "#7A68A8"),
        ("soil-tilled", "#8A5A3C"),
        ("soil-watered", "#4A3226"),
        ("timber", "#9C7E8C"),
        ("trunk", "#6B5A78"),
        ("stone", "#8F8A96"),
        ("rock", "#9A95A2"),
        ("dirt-path", "#B8946C"),
        ("lake-deep", "#2D6A86"),
        ("lake-shallow", "#5FB3AA"),
        ("ember", "#E9773A"),
        ("lantern", "#FFB04A"),
        ("glow-cool", "#6FF2E0"),
        ("glow-rare", "#B48CFF"),
        ("ship-metal", "#C9D6E8"),
        ("target-highlight", "#FFF6E0"),
        ("dusktuber", "#4A2A66"),
        ("moonmelon", "#C9DCF5"),
        ("sunbell-gold", "#F7C531"),
        ("sunbell-orange", "#E9821A"),
        ("starcorn", "#E8B830"),
        ("coverall-shade", "#A3B3CB"),
        ("navy", "#34405E"),
        ("boots", "#3B3F4A"),
        ("earth-blue", "#3D7FC4"),
        ("earth-green", "#5BAF6A"),
        ("wrist-screen", "#9FD3FF"),
        ("timber-side", "#7E6474"),
        ("timber-grain", "#6E5664"),
        ("timber-trim", "#5A4458"),
        ("roof-wilted", "#8A7A4A"),
        ("tips-dry", "#9A7E44"),
        ("trunk-grey", "#6E6670"),
        ("wall-grey", "#8A7C80"),
        ("roof-rows", "#2F7266"),
        ("roof-tips", "#4FA294"),
        ("roof-far", "#2F6B60"),
    ];

    /// <summary>
    /// The whole list: the baseline, the Season margins appended by #79, starcorn's sparkle (#91), then the farmer's
    /// skin, skin shade, hair and seed pouch (#102).
    /// </summary>
    private static readonly (string Role, string Hex)[] ExpectedSwatches =
    [
        .. BaselineSwatches,
        ("grass-spring-mown", "#A6D07C"),
        ("grass-fall-mown", "#C0BC72"),
        ("starcorn-sparkle", "#FFFFFF"),
        ("skin", "#D9A07A"),
        ("skin-shade", "#B57E5F"),
        ("hair", "#5A3E2E"),
        ("seed-pouch", "#D9C39A"),
    ];

    /// <summary>The swatches the encoder is given, for the PNG layout tests, which hold for any list length.</summary>
    private static (string Role, string Hex)[] PaletteSwatches =>
        ArtPalette.Swatches.Select(s => (s.Role, s.Hex)).ToArray();

    // ---- AC1: the swatch list ----

    [Fact]
    public void AC3_palette_lists_the_50_swatches_in_order()
    {
        var actual = ArtPalette.Swatches.Select(s => (s.Role, s.Hex)).ToArray();

        Assert.Equal(50, actual.Length);
        Assert.Equal(ExpectedSwatches, actual);
    }

    // ---- #79 AC3: the two Season margin swatches, appended ----

    [Fact]
    public void AC3_swatch_43_is_grass_spring_mown_A6D07C()
    {
        Assert.True(ArtPalette.Swatches.Count > 43, $"the palette has {ArtPalette.Swatches.Count} swatches, no index 43");
        Assert.Equal(new ArtSwatch("grass-spring-mown", "#A6D07C"), ArtPalette.Swatches[43]);
    }

    [Fact]
    public void AC3_swatch_44_is_grass_fall_mown_C0BC72()
    {
        Assert.True(ArtPalette.Swatches.Count > 44, $"the palette has {ArtPalette.Swatches.Count} swatches, no index 44");
        Assert.Equal(new ArtSwatch("grass-fall-mown", "#C0BC72"), ArtPalette.Swatches[44]);
    }

    // ---- #91 AC1: starcorn's sparkle swatch, appended ----

    [Fact]
    public void AC1_swatch_45_is_starcorn_sparkle_FFFFFF()
    {
        Assert.True(ArtPalette.Swatches.Count > 45, $"the palette has {ArtPalette.Swatches.Count} swatches, no index 45");
        Assert.Equal(new ArtSwatch("starcorn-sparkle", "#FFFFFF"), ArtPalette.Swatches[45]);
    }

    [Fact]
    public void AC3_indices_0_to_42_are_unchanged()
    {
        Assert.Equal(43, BaselineSwatches.Length);
        Assert.True(ArtPalette.Swatches.Count >= 43, $"the palette has only {ArtPalette.Swatches.Count} swatches");

        var first43 = ArtPalette.Swatches.Take(43).Select(s => (s.Role, s.Hex)).ToArray();
        Assert.Equal(BaselineSwatches, first43);
    }

    [Fact]
    public void AC1_roles_are_unique()
    {
        var roles = ArtPalette.Swatches.Select(s => s.Role).ToList();

        Assert.NotEmpty(roles);
        Assert.Equal(roles.Count, roles.Distinct(StringComparer.Ordinal).Count());
    }

    // ---- AC2: UV centres ----

    [Theory]
    [InlineData("grass-lit", 4f / 64, 4f / 64)]
    [InlineData("soil-tilled", 60f / 64, 4f / 64)]
    [InlineData("dusktuber", 52f / 64, 20f / 64)]
    public void AC2_uv_centres_of_grass_lit_soil_tilled_dusktuber(string role, float u, float v)
    {
        var (actualU, actualV) = ArtPalette.UvCentre(role);

        Assert.Equal(u, actualU, 1e-6f);
        Assert.Equal(v, actualV, 1e-6f);
    }

    [Fact]
    public void AC2_unknown_role_throws()
    {
        Assert.Throws<ArgumentException>(() => ArtPalette.UvCentre("not-a-role"));
    }

    // ---- AC3: the PNG ----

    [Fact]
    public void AC3_png_is_64x64_rgba()
    {
        var png = DecodePng(ArtPalette.EncodePng());

        Assert.Equal(64, png.Width);
        Assert.Equal(64, png.Height);
        Assert.Equal(8, png.BitDepth);
        Assert.Equal(6, png.ColourType);
        Assert.Equal(64, png.RowFilters.Length);
        Assert.All(png.RowFilters, filter => Assert.Equal(0, filter));
    }

    [Fact]
    public void AC3_swatch_centres_and_unused_pixels()
    {
        var png = DecodePng(ArtPalette.EncodePng());

        for (var i = 0; i < PaletteSwatches.Length; i++)
        {
            var (role, hex) = PaletteSwatches[i];
            var x = (i % 8) * 8 + 4;
            var y = (i / 8) * 8 + 4;
            Assert.True(
                HexToRgba(hex) == png.Pixel(x, y),
                $"swatch {i} ({role}) centre ({x},{y}) is {png.Pixel(x, y)}, expected {HexToRgba(hex)}");
        }

        for (var i = PaletteSwatches.Length; i < 64; i++)
        {
            for (var dy = 0; dy < 8; dy++)
            {
                for (var dx = 0; dx < 8; dx++)
                {
                    var x = (i % 8) * 8 + dx;
                    var y = (i / 8) * 8 + dy;
                    Assert.True(
                        png.Pixel(x, y) == (0, 0, 0, 0),
                        $"unused swatch {i} pixel ({x},{y}) is {png.Pixel(x, y)}, expected (0, 0, 0, 0)");
                }
            }
        }
    }

    [Fact]
    public void AC3_each_swatch_fills_its_whole_8x8_block()
    {
        var png = DecodePng(ArtPalette.EncodePng());

        for (var i = 0; i < PaletteSwatches.Length; i++)
        {
            var expected = HexToRgba(PaletteSwatches[i].Hex);
            for (var dy = 0; dy < 8; dy++)
            {
                for (var dx = 0; dx < 8; dx++)
                {
                    var x = (i % 8) * 8 + dx;
                    var y = (i / 8) * 8 + dy;
                    Assert.True(
                        png.Pixel(x, y) == expected,
                        $"swatch {i} pixel ({x},{y}) is {png.Pixel(x, y)}, expected {expected}");
                }
            }
        }
    }

    // ---- AC4: the committed file ----

    [Fact]
    public void AC4_committed_palette_png_is_current()
    {
        var path = RepoPath("game", "art", "palette", "palette.png");

        if (Environment.GetEnvironmentVariable("UPDATE_PALETTE") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, ArtPalette.EncodePng());
            return;
        }

        Assert.True(File.Exists(path), $"{path} is missing; {RegenerateHint}");

        // Compare decoded pixels, not file bytes: the zlib stream ZLibStream writes differs between .NET runtimes
        // (zlib vs zlib-ng), so the same pixels encode to different bytes on different machines.
        var committed = DecodePng(File.ReadAllBytes(path));
        var expected = DecodePng(ArtPalette.EncodePng());
        Assert.True(
            committed.Width == expected.Width && committed.Height == expected.Height,
            $"{path} is {committed.Width}x{committed.Height}, expected {expected.Width}x{expected.Height}; {RegenerateHint}");
        Assert.True(
            committed.Rgba.AsSpan().SequenceEqual(expected.Rgba),
            $"{path} is stale (its pixels differ from ArtPalette.EncodePng()); {RegenerateHint}");
    }

    // ---- AC5: import, sampler, project global ----

    [Fact]
    public void AC5_import_disables_mipmaps()
    {
        var path = RepoPath("game", "art", "palette", "palette.png.import");
        Assert.True(File.Exists(path), $"{path} is missing");

        var text = File.ReadAllText(path);
        Assert.Contains("mipmaps/generate=false", text);
        Assert.Contains("detect_3d/compress_to=0", text);
    }

    [Fact]
    public void AC5_shader_samples_nearest()
    {
        var shader = ReadRepoFile("game", "art", "shaders", "low_poly.gdshader");

        var match = Regex.Match(shader, @"\buniform\s+sampler2D\s+palette\s*:\s*([^;=]*)");
        Assert.True(match.Success, "low_poly.gdshader does not declare `uniform sampler2D palette : ...`");
        var hints = match.Groups[1].Value;
        Assert.Contains("filter_nearest", hints);
        Assert.Contains("source_color", hints);
    }

    [Fact]
    public void AC5_project_registers_art_stepped_shading()
    {
        var project = ReadRepoFile("game", "project.godot");

        var section = Regex.Match(project, @"^\[shader_globals\]\s*$(.*?)(?=^\[|\z)", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(section.Success, "project.godot has no [shader_globals] section");

        var entry = Regex.Match(section.Groups[1].Value, @"^art_stepped_shading\s*=\s*\{(.*?)\}", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(entry.Success, "[shader_globals] has no art_stepped_shading entry");
        Assert.Matches(@"""type""\s*:\s*""bool""", entry.Groups[1].Value);
        Assert.Matches(@"""value""\s*:\s*false\b", entry.Groups[1].Value);
    }

    // ---- AC6: the shared material and the sandbox ----

    [Fact]
    public void AC6_sandbox_colours_come_from_art_materials()
    {
        var sandbox = ReadRepoFile("game", "art", "sandbox", "ArtSandbox.cs");

        Assert.Contains("ArtMaterials.Apply(", sandbox);
        Assert.Contains("ArtPalette.Swatches", sandbox);
        Assert.DoesNotContain("SandboxPalette", sandbox);
        Assert.DoesNotContain("new ShaderMaterial", sandbox);
    }

    [Fact]
    public void AC6_art_materials_applies_the_shared_material_and_swatch()
    {
        var path = RepoPath("game", "art", "ArtMaterials.cs");
        Assert.True(File.Exists(path), $"{path} is missing");

        var text = File.ReadAllText(path);
        Assert.Matches(@"namespace\s+LongWayHome\.Game\.Art\b", text);
        Assert.Matches(@"\bstatic\s+class\s+ArtMaterials\b", text);
        Assert.Contains("\"res://art/materials/palette.tres\"", text);
        Assert.Contains("SetInstanceShaderParameter", text);
        Assert.Contains("\"swatch\"", text);
        Assert.Contains("ArtPalette.UvCentre(", text);
    }

    [Fact]
    public void AC6_shared_material_uses_the_low_poly_shader_and_palette()
    {
        var path = RepoPath("game", "art", "materials", "palette.tres");
        Assert.True(File.Exists(path), $"{path} is missing");

        var text = File.ReadAllText(path);
        Assert.Matches(@"type=""ShaderMaterial""", text);
        Assert.Contains("res://art/shaders/low_poly.gdshader", text);
        Assert.Contains("res://art/palette/palette.png", text);
    }

    // ---- AC7: the global shading style ----

    [Fact]
    public void AC7_shader_reads_the_global_style()
    {
        var shader = ReadRepoFile("game", "art", "shaders", "low_poly.gdshader");

        Assert.Matches(@"\bglobal\s+uniform\s+bool\s+art_stepped_shading\b", shader);
        Assert.Matches(@"\binstance\s+uniform\s+vec2\s+swatch\b", shader);
        Assert.Matches(@"\buniform\s+vec4\s+tint\b", shader);
        Assert.DoesNotMatch(@"\buniform\s+bool\s+stepped\b", shader);
    }

    // ---- AC8: ART_DIRECTION watered soil ----

    [Fact]
    public void AC8_art_direction_section2_watered_soil()
    {
        var doc = ReadRepoFile("docs", "ART_DIRECTION.md");

        var section2 = Regex.Match(doc, @"^## 2\. .*?(?=^## 3\.)", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(section2.Success, "ART_DIRECTION.md has no §2");

        var row = Regex.Match(section2.Value, @"^\|\s*Watered soil\s*\|.*$", RegexOptions.Multiline);
        Assert.True(row.Success, "§2 colour table has no Watered soil row");
        Assert.Contains("`#4A3226`", row.Value);
        Assert.DoesNotContain("#3E2A20", doc);
    }

    // ---- AC9: layering ----

    [Fact]
    public void AC9_art_palette_lives_in_application()
    {
        Assert.Equal("LongWayHome.Application", typeof(ArtPalette).Assembly.GetName().Name);
    }

    // ---- helpers ----

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LongWayHome.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"no folder holding LongWayHome.sln above {AppContext.BaseDirectory}");
        return dir!.FullName;
    }

    private static string RepoPath(params string[] parts) =>
        Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());

    private static string ReadRepoFile(params string[] parts)
    {
        var path = RepoPath(parts);
        Assert.True(File.Exists(path), $"{path} is missing");
        return File.ReadAllText(path);
    }

    private static (int R, int G, int B, int A) HexToRgba(string hex) =>
    (
        Convert.ToInt32(hex.Substring(1, 2), 16),
        Convert.ToInt32(hex.Substring(3, 2), 16),
        Convert.ToInt32(hex.Substring(5, 2), 16),
        255
    );

    private sealed record DecodedPng(int Width, int Height, int BitDepth, int ColourType, byte[] RowFilters, byte[] Rgba)
    {
        public (int R, int G, int B, int A) Pixel(int x, int y)
        {
            var i = (y * Width + x) * 4;
            return (Rgba[i], Rgba[i + 1], Rgba[i + 2], Rgba[i + 3]);
        }
    }

    /// <summary>
    /// A minimal PNG reader for RGBA8, non-interlaced images: checks the signature and each chunk's CRC, reads IHDR,
    /// inflates the IDAT data and undoes the row filters (None, Sub, Up, Average, Paeth).
    /// </summary>
    private static DecodedPng DecodePng(byte[] data)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.True(data.Length > 8 && data.AsSpan(0, 8).SequenceEqual(signature), "not a PNG: bad signature");

        int width = 0, height = 0, bitDepth = 0, colourType = 0, interlace = 0;
        var idat = new MemoryStream();
        var sawIhdr = false;
        var sawIend = false;
        var pos = 8;
        while (pos < data.Length)
        {
            Assert.True(pos + 12 <= data.Length, "truncated PNG chunk");
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos, 4));
            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            Assert.True(pos + 12 + length <= data.Length, $"truncated {type} chunk");
            var body = data.AsSpan(pos + 8, length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos + 8 + length, 4));
            Assert.True(crc == Crc32(data.AsSpan(pos + 4, 4 + length)), $"{type} chunk CRC mismatch");

            switch (type)
            {
                case "IHDR":
                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(body[..4]);
                    height = (int)BinaryPrimitives.ReadUInt32BigEndian(body.Slice(4, 4));
                    bitDepth = body[8];
                    colourType = body[9];
                    interlace = body[12];
                    sawIhdr = true;
                    break;
                case "IDAT":
                    idat.Write(body);
                    break;
                case "IEND":
                    sawIend = true;
                    break;
            }

            pos += 12 + length;
            if (sawIend)
            {
                break;
            }
        }

        Assert.True(sawIhdr, "PNG has no IHDR");
        Assert.True(sawIend, "PNG has no IEND");
        Assert.True(bitDepth == 8 && colourType == 6, $"PNG is not RGBA8 (bit depth {bitDepth}, colour type {colourType})");
        Assert.True(interlace == 0, "PNG is interlaced");

        idat.Position = 0;
        using var inflated = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress))
        {
            zlib.CopyTo(inflated);
        }

        var raw = inflated.ToArray();
        var stride = width * 4;
        Assert.True(raw.Length == height * (stride + 1), $"inflated data is {raw.Length} bytes, expected {height * (stride + 1)}");

        var rgba = new byte[height * stride];
        var filters = new byte[height];
        for (var y = 0; y < height; y++)
        {
            var filter = raw[y * (stride + 1)];
            filters[y] = filter;
            for (var x = 0; x < stride; x++)
            {
                int value = raw[y * (stride + 1) + 1 + x];
                int left = x >= 4 ? rgba[y * stride + x - 4] : 0;
                int up = y > 0 ? rgba[(y - 1) * stride + x] : 0;
                int upLeft = x >= 4 && y > 0 ? rgba[(y - 1) * stride + x - 4] : 0;
                rgba[y * stride + x] = filter switch
                {
                    0 => (byte)value,
                    1 => (byte)(value + left),
                    2 => (byte)(value + up),
                    3 => (byte)(value + (left + up) / 2),
                    4 => (byte)(value + Paeth(left, up, upLeft)),
                    _ => throw new InvalidDataException($"row {y} has unknown filter type {filter}"),
                };
            }
        }

        return new DecodedPng(width, height, bitDepth, colourType, filters, rgba);
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

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
