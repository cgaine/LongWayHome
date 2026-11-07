using System.Text.RegularExpressions;
using System.Xml.Linq;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The seed and crop icons (#95): <see cref="ItemIcons"/>'s paths, and the ten SVGs under <c>game/art/icons/</c>
/// (flat, 64 x 64, palette colours only). The files are read from the repository (found by walking up to
/// <c>LongWayHome.sln</c>), not from copies in <c>bin/</c>.
/// </summary>
public class ItemIconsTests
{
    private const string PacketColour = "#FFF6E0"; // target-highlight

    public static TheoryData<string, string> AllIcons()
    {
        var data = new TheoryData<string, string>();
        foreach (var kind in new[] { "seed", "crop" })
        {
            foreach (var code in Codes)
            {
                data.Add(kind, code);
            }
        }

        return data;
    }

    public static TheoryData<string> CropCodes() => new(Codes);

    private static readonly string[] Codes = ["emberroot", "dusktuber", "moonmelon", "sunbell", "starcorn"];

    // ---- AC1: the lookup ----

    [Theory]
    [InlineData(CropTypeId.Emberroot, "res://art/icons/seed_emberroot.svg")]
    [InlineData(CropTypeId.Dusktuber, "res://art/icons/seed_dusktuber.svg")]
    [InlineData(CropTypeId.Moonmelon, "res://art/icons/seed_moonmelon.svg")]
    [InlineData(CropTypeId.Sunbell, "res://art/icons/seed_sunbell.svg")]
    [InlineData(CropTypeId.Starcorn, "res://art/icons/seed_starcorn.svg")]
    public void AC1_seed_icon_is_res_art_icons_seed_code_svg(CropTypeId crop, string expected) =>
        Assert.Equal(expected, ItemIcons.SeedIcon(crop));

    [Theory]
    [InlineData(CropTypeId.Emberroot, "res://art/icons/crop_emberroot.svg")]
    [InlineData(CropTypeId.Dusktuber, "res://art/icons/crop_dusktuber.svg")]
    [InlineData(CropTypeId.Moonmelon, "res://art/icons/crop_moonmelon.svg")]
    [InlineData(CropTypeId.Sunbell, "res://art/icons/crop_sunbell.svg")]
    [InlineData(CropTypeId.Starcorn, "res://art/icons/crop_starcorn.svg")]
    public void AC1_crop_icon_is_res_art_icons_crop_code_svg(CropTypeId crop, string expected) =>
        Assert.Equal(expected, ItemIcons.CropIcon(crop));

    [Fact]
    public void AC1_seed_icon_of_an_undefined_crop_type_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemIcons.SeedIcon((CropTypeId)99));

    [Fact]
    public void AC1_crop_icon_of_an_undefined_crop_type_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemIcons.CropIcon((CropTypeId)99));

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Moonmelon)]
    [InlineData(CropTypeId.Sunbell)]
    [InlineData(CropTypeId.Starcorn)]
    public void AC1_held_seed_icon_is_its_seed_icon(CropTypeId crop) =>
        Assert.Equal(ItemIcons.SeedIcon(crop), ItemIcons.For(HeldItem.SeedOf(crop)));

    [Theory]
    [InlineData(ToolId.Hoe)]
    [InlineData(ToolId.WateringCan)]
    public void AC1_held_tool_has_no_icon(ToolId tool) =>
        Assert.Null(ItemIcons.For(HeldItem.Of(tool)));

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void AC1_icon_file_exists(string kind, string code)
    {
        var path = IconPath(kind, code);

        Assert.True(File.Exists(path), $"{path} is missing");
    }

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void AC1_icon_import_file_exists(string kind, string code)
    {
        var path = IconPath(kind, code) + ".import";

        Assert.True(File.Exists(path), $"{path} is missing");
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Moonmelon)]
    [InlineData(CropTypeId.Sunbell)]
    [InlineData(CropTypeId.Starcorn)]
    public void AC1_lookup_paths_name_files_in_the_game_folder(CropTypeId crop)
    {
        foreach (var resPath in new[] { ItemIcons.SeedIcon(crop), ItemIcons.CropIcon(crop) })
        {
            var path = ResToRepoPath(resPath);
            Assert.True(File.Exists(path), $"{resPath} ({path}) is missing");
        }
    }

    // ---- AC2: flat, 64 x 64, palette colours only ----

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void AC2_icon_viewbox_is_0_0_64_64(string kind, string code)
    {
        var svg = LoadSvg(kind, code);

        Assert.Equal("svg", svg.Root!.Name.LocalName);
        Assert.Equal("0 0 64 64", (string?)svg.Root.Attribute("viewBox"));
    }

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void AC2_icon_has_no_gradient_filter_or_image(string kind, string code)
    {
        var svg = LoadSvg(kind, code);

        string[] forbidden = ["linearGradient", "radialGradient", "filter", "image"];
        var found = svg.Descendants()
            .Select(e => e.Name.LocalName)
            .Where(name => forbidden.Contains(name))
            .Distinct()
            .ToList();

        Assert.True(found.Count == 0, $"{kind}_{code}.svg has {string.Join(", ", found)}");
    }

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void AC2_every_fill_and_stroke_is_a_palette_hex(string kind, string code)
    {
        var svg = LoadSvg(kind, code);
        var palette = PaletteHexes();

        var bad = PaintValues(svg)
            .Where(v => !string.Equals(v.Value, "none", StringComparison.OrdinalIgnoreCase))
            .Where(v => !IsSixDigitHex(v.Value) || !palette.Contains(v.Value))
            .Select(v => $"{v.Property}=\"{v.Value}\"")
            .ToList();

        Assert.True(bad.Count == 0, $"{kind}_{code}.svg has non-palette colours: {string.Join(", ", bad)}");
    }

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void AC2_icon_has_at_least_one_fill_colour(string kind, string code)
    {
        var svg = LoadSvg(kind, code);

        Assert.Contains(PaintValues(svg), v => v.Property == "fill" && IsSixDigitHex(v.Value));
    }

    // ---- AC3: the packet colour tells a seed from a crop ----

    [Theory]
    [InlineData("emberroot", "ember")]
    [InlineData("dusktuber", "dusktuber")]
    [InlineData("moonmelon", "moonmelon")]
    [InlineData("sunbell", "sunbell-gold")]
    [InlineData("starcorn", "starcorn")]
    public void AC3_seed_icon_has_the_packet_colour_and_its_crop_colour(string code, string cropRole)
    {
        var colours = Colours(LoadSvg("seed", code));

        Assert.Equal(PacketColour, ArtPalette.Hex("target-highlight"));
        Assert.Contains(PacketColour, colours);
        Assert.Contains(ArtPalette.Hex(cropRole), colours);
    }

    [Theory]
    [MemberData(nameof(CropCodes))]
    public void AC3_crop_icon_has_no_packet_colour(string code)
    {
        var colours = Colours(LoadSvg("crop", code));

        Assert.NotEmpty(colours);
        Assert.DoesNotContain(PacketColour, colours);
    }

    // ---- helpers ----

    private static bool IsSixDigitHex(string value) => Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$");

    private static HashSet<string> PaletteHexes() =>
        new(ArtPalette.Swatches.Select(s => s.Hex), StringComparer.OrdinalIgnoreCase);

    /// <summary>The fill and stroke hexes in the SVG, upper-cased.</summary>
    private static HashSet<string> Colours(XDocument svg) =>
        new(PaintValues(svg).Where(v => IsSixDigitHex(v.Value)).Select(v => v.Value.ToUpperInvariant()),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>Every fill and stroke value, from attributes and from <c>style="..."</c> declarations.</summary>
    private static List<(string Property, string Value)> PaintValues(XDocument svg)
    {
        var values = new List<(string, string)>();
        foreach (var element in svg.Descendants())
        {
            foreach (var property in new[] { "fill", "stroke" })
            {
                var attribute = element.Attribute(property);
                if (attribute is not null)
                {
                    values.Add((property, attribute.Value.Trim()));
                }
            }

            var style = (string?)element.Attribute("style");
            if (style is null)
            {
                continue;
            }

            foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = declaration.Split(':', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var property = parts[0].Trim().ToLowerInvariant();
                if (property is "fill" or "stroke")
                {
                    values.Add((property, parts[1].Trim()));
                }
            }
        }

        return values;
    }

    private static XDocument LoadSvg(string kind, string code)
    {
        var path = IconPath(kind, code);
        Assert.True(File.Exists(path), $"{path} is missing");
        return XDocument.Load(path);
    }

    private static string IconPath(string kind, string code) =>
        Path.Combine(RepoRoot(), "game", "art", "icons", $"{kind}_{code}.svg");

    // ---- #130, AC12: a dropped seed or crop shows its icon ----

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Starcorn)]
    public void AC12_dropped_seed_and_crop_use_their_icons(CropTypeId crop)
    {
        Assert.Equal(ItemIcons.SeedIcon(crop), ItemIcons.For(ItemRef.Seed(crop)));
        Assert.Equal(ItemIcons.CropIcon(crop), ItemIcons.For(ItemRef.Crop(crop)));
    }

    [Fact]
    public void AC12_a_tool_item_has_no_dropped_icon()
    {
        Assert.Null(ItemIcons.For(ItemRef.Tool(ToolId.Hoe)));
    }

    private static string ResToRepoPath(string resPath)
    {
        Assert.StartsWith("res://", resPath);
        var relative = resPath["res://".Length..].Split('/');
        return Path.Combine(new[] { RepoRoot(), "game" }.Concat(relative).ToArray());
    }

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
}
