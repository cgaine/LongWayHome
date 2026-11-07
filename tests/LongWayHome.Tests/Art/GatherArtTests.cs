using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The Phase 2 gathering art drawn ahead of the game having it: the axe and pickaxe in the farmer's hand
/// (<see cref="HeldItemArt.PartsFor(GatherTool)"/>, ART_DIRECTION §6.4) and the tool and material icons
/// (<see cref="ItemIcons"/>), held to the same rules as the seed and crop icons (#95): 64 x 64, flat, palette colours
/// only. The files are read from the repository.
/// </summary>
public class GatherArtTests
{
    public static TheoryData<GatherTool> Tools() => new(Enum.GetValues<GatherTool>());

    public static TheoryData<string> IconPaths()
    {
        var data = new TheoryData<string>();
        foreach (var tool in Enum.GetValues<GatherTool>())
        {
            data.Add(ItemIcons.ToolIcon(tool));
        }

        foreach (var material in Enum.GetValues<MaterialKind>())
        {
            data.Add(ItemIcons.MaterialIcon(material));
        }

        return data;
    }

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    // ---- the tools in hand ----

    [Theory]
    [MemberData(nameof(Tools))]
    public void Every_tool_uses_palette_roles_with_unique_names(GatherTool tool)
    {
        var parts = HeldItemArt.PartsFor(tool);
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        Assert.Equal(parts.Count, parts.Select(part => part.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(parts, part => Assert.Equal(0f, part.Glow));
    }

    [Theory]
    [MemberData(nameof(Tools))]
    public void Every_tool_shares_the_hoes_short_timber_handle_and_has_a_rock_head_with_a_pale_edge(GatherTool tool)
    {
        var parts = HeldItemArt.PartsFor(tool);
        var hoe = HeldItemArt.PartsFor(HeldItem.Of(ToolId.Hoe));

        Assert.Equal(hoe.Single(part => part.Name == "Handle"), parts.Single(part => part.Name == "Handle"));
        Assert.Contains(parts, part => part.Role == "rock");
        Assert.Contains(parts, part => part.Role == "ship-metal");
        // Short and stocky: the head stays within about chest height of the grip, like the hoe's.
        Assert.InRange(Extent(parts).Max.Y, 0.5f, 0.62f);
    }

    [Fact]
    public void The_axe_is_a_single_wedge_forward_and_the_pickaxe_the_widest_silhouette()
    {
        var axe = Extent(HeldItemArt.PartsFor(GatherTool.Axe));
        var pickaxe = Extent(HeldItemArt.PartsFor(GatherTool.Pickaxe));
        var hoe = Extent(HeldItemArt.PartsFor(HeldItem.Of(ToolId.Hoe)));

        // The axe's blade juts forward (+Z), with only a short poll behind.
        Assert.True(axe.Max.Z > 0.17f, $"the axe's edge is only {axe.Max.Z:0.###} m forward");
        Assert.True(axe.Min.Z > -0.1f, $"the axe reaches {axe.Min.Z:0.###} m back");

        // The pickaxe's two points reach both ways and make it wider than the axe and the hoe.
        Assert.True(pickaxe.Max.Z > 0.2f && pickaxe.Min.Z < -0.2f, $"the pickaxe spans z {pickaxe.Min.Z:0.###}..{pickaxe.Max.Z:0.###}");
        var width = pickaxe.Max.Z - pickaxe.Min.Z;
        Assert.True(width > axe.Max.Z - axe.Min.Z && width > hoe.Max.Z - hoe.Min.Z, "the pickaxe is not the widest tool");
        Assert.InRange(width, 0.4f, 0.6f);
    }

    [Fact]
    public void An_undefined_tool_has_no_art() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => HeldItemArt.PartsFor((GatherTool)99));

    // ---- the icons ----

    [Theory]
    [InlineData(MaterialKind.Wood, "res://art/icons/material_wood.svg")]
    [InlineData(MaterialKind.Sap, "res://art/icons/material_sap.svg")]
    [InlineData(MaterialKind.Stone, "res://art/icons/material_stone.svg")]
    [InlineData(MaterialKind.RoskOre, "res://art/icons/material_rosk_ore.svg")]
    [InlineData(MaterialKind.GrothOre, "res://art/icons/material_groth_ore.svg")]
    [InlineData(MaterialKind.Glimmercap, "res://art/icons/material_glimmercap.svg")]
    [InlineData(MaterialKind.Plank, "res://art/icons/material_plank.svg")]
    [InlineData(MaterialKind.SealedPlank, "res://art/icons/material_sealed_plank.svg")]
    public void Material_icon_path(MaterialKind material, string expected) => Assert.Equal(expected, ItemIcons.MaterialIcon(material));

    [Theory]
    [InlineData(GatherTool.Axe, "res://art/icons/tool_axe.svg")]
    [InlineData(GatherTool.Pickaxe, "res://art/icons/tool_pickaxe.svg")]
    public void Tool_icon_path(GatherTool tool, string expected) => Assert.Equal(expected, ItemIcons.ToolIcon(tool));

    [Fact]
    public void Undefined_icons_throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemIcons.MaterialIcon((MaterialKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemIcons.ToolIcon((GatherTool)99));
    }

    [Theory]
    [MemberData(nameof(IconPaths))]
    public void Every_icon_exists_with_its_godot_import(string resPath)
    {
        var path = RepoPath(resPath);

        Assert.True(File.Exists(path), $"{resPath} is missing");
        Assert.True(File.Exists(path + ".import"), $"{resPath}.import is missing");
    }

    [Theory]
    [MemberData(nameof(IconPaths))]
    public void Every_icon_is_64_by_64_flat_and_in_palette_colours(string resPath)
    {
        var svg = XDocument.Load(RepoPath(resPath));
        var palette = ArtPalette.Swatches.Select(swatch => swatch.Hex).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal("svg", svg.Root!.Name.LocalName);
        Assert.Equal("0 0 64 64", (string?)svg.Root.Attribute("viewBox"));
        string[] forbidden = ["linearGradient", "radialGradient", "filter", "image", "style"];
        Assert.DoesNotContain(svg.Descendants(), element => forbidden.Contains(element.Name.LocalName));
        Assert.DoesNotContain(svg.Descendants(), element => element.Attribute("style") is not null);

        var paints = svg.Descendants()
            .SelectMany(element => new[] { element.Attribute("fill"), element.Attribute("stroke") })
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!.Value.Trim())
            .ToList();
        Assert.Contains(paints, value => value != "none");
        var bad = paints.Where(value => value != "none" && (!Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$") || !palette.Contains(value))).ToList();
        Assert.True(bad.Count == 0, $"{resPath} has non-palette colours: {string.Join(", ", bad)}");
        // The outline every icon shares: navy.
        Assert.Contains(paints, value => string.Equals(value, ArtPalette.Hex("navy"), StringComparison.OrdinalIgnoreCase));
    }

    // ---- the farmer and the sandbox ----

    [Fact]
    public void The_farmer_can_hold_a_gathering_tool_and_the_sandbox_shows_both_and_the_icons()
    {
        var farmer = File.ReadAllText(Path.Combine(RepoRoot(), "game", "art", "farmer", "FarmerModel.cs"));
        var sandbox = File.ReadAllText(Path.Combine(RepoRoot(), "game", "art", "sandbox", "ArtSandbox.cs"));

        Assert.Matches(@"public\s+void\s+ShowGatherTool\s*\(\s*GatherTool\s+\w+\s*\)", farmer);
        Assert.Contains("HeldItemArt.PartsFor(tool)", farmer, StringComparison.Ordinal);
        Assert.Contains("ShowGatherTool(", sandbox, StringComparison.Ordinal);
        Assert.Contains("ItemIcons.MaterialIcon(", sandbox, StringComparison.Ordinal);
        Assert.Contains("ItemIcons.ToolIcon(", sandbox, StringComparison.Ordinal);
    }

    // ---- helpers ----

    private static string RepoPath(string resPath)
    {
        Assert.StartsWith("res://", resPath);
        return Path.Combine(new[] { RepoRoot(), "game" }.Concat(resPath["res://".Length..].Split('/')).ToArray());
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
