using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Art;

/// <summary>
/// #164 AC10, the art: <see cref="PlaceableArt"/>'s pure parts (a 1 x 1 m fence section of two posts and two rails;
/// a 0.8 x 0.55 x 0.55 m chest of box, lid and trim band), checked on the parts' rotated corners
/// (<see cref="PartBoxes"/>), and the two icons through <see cref="ItemIcons"/>, mirroring the material icons (#152).
/// </summary>
public class PlaceableArtTests
{
    public static TheoryData<PlaceableId> EveryPlaceable() => new(Enum.GetValues<PlaceableId>());

    public static TheoryData<string> IconPaths() =>
        new("res://art/icons/placeable_fence.svg", "res://art/icons/placeable_chest.svg");

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    // ---- the parts ----

    [Theory]
    [MemberData(nameof(EveryPlaceable))]
    public void AC10_every_placeable_uses_palette_roles_with_unique_names_and_real_sizes(PlaceableId placeable)
    {
        var parts = PlaceableArt.For(placeable);
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        Assert.Equal(parts.Count, parts.Select(part => part.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(parts, part => Assert.True(part.SizeX > 0f && part.SizeY > 0f && part.SizeZ > 0f, $"{part.Name} has a zero size"));
    }

    [Theory]
    [MemberData(nameof(EveryPlaceable))]
    public void AC10_every_placeable_stands_on_the_ground_round_its_tile_centre(PlaceableId placeable)
    {
        var (min, max) = Extent(PlaceableArt.For(placeable));

        Assert.InRange(min.Y, -0.08f, 0.02f);
        Assert.InRange((min.X + max.X) / 2f, -0.1f, 0.1f);
        Assert.InRange((min.Z + max.Z) / 2f, -0.1f, 0.1f);
    }

    [Fact]
    public void AC10_a_fence_section_has_two_posts_and_two_rails_in_timber()
    {
        var parts = PlaceableArt.For(PlaceableId.Fence);

        Assert.Equal(2, parts.Count(part => Named(part, "Post")));
        Assert.Equal(2, parts.Count(part => Named(part, "Rail")));
        Assert.All(
            parts.Where(part => Named(part, "Post") || Named(part, "Rail")),
            part => Assert.StartsWith("timber", part.Role, StringComparison.Ordinal));
    }

    [Fact]
    public void AC10_a_fence_section_is_about_1_m_wide_and_1_m_tall()
    {
        var (min, max) = Extent(PlaceableArt.For(PlaceableId.Fence));

        Assert.InRange(max.X - min.X, 0.85f, 1.1f);
        Assert.InRange(max.Y, 0.85f, 1.15f);
        Assert.InRange(max.Z - min.Z, 0.05f, 0.4f);
    }

    [Fact]
    public void AC10_a_fences_rails_run_between_its_posts()
    {
        var parts = PlaceableArt.For(PlaceableId.Fence);
        var posts = parts.Where(part => Named(part, "Post")).Select(part => part.X).Order().ToList();

        Assert.True(posts[1] - posts[0] > 0.6f, $"the posts stand only {posts[1] - posts[0]:0.##} m apart");
        foreach (var rail in parts.Where(part => Named(part, "Rail")))
        {
            var (min, max) = Extent([rail]);
            Assert.True(min.X <= posts[0] + 0.05f && max.X >= posts[1] - 0.05f, $"{rail.Name} doesn't span the posts");
        }
    }

    [Fact]
    public void AC10_a_chest_is_0_8_by_0_55_by_0_55_m()
    {
        var (min, max) = Extent(PlaceableArt.For(PlaceableId.Chest));

        Assert.InRange(max.X - min.X, 0.74f, 0.86f);
        Assert.InRange(max.Y, 0.5f, 0.6f);
        Assert.InRange(max.Z - min.Z, 0.5f, 0.6f);
    }

    [Fact]
    public void AC10_a_chest_has_a_box_a_lid_and_a_trim_band()
    {
        var parts = PlaceableArt.For(PlaceableId.Chest);

        Assert.Contains(parts, part => Named(part, "Box"));
        Assert.Contains(parts, part => Named(part, "Lid"));
        Assert.Contains(parts, part => Named(part, "Trim"));
        var box = Extent(parts.Where(part => Named(part, "Box")));
        var lid = Extent(parts.Where(part => Named(part, "Lid")));
        Assert.True(lid.Max.Y > box.Max.Y, "the lid doesn't sit on top of the box");
    }

    [Fact]
    public void AC10_nothing_glows_on_a_fence_or_a_chest()
    {
        Assert.All(Enum.GetValues<PlaceableId>().SelectMany(PlaceableArt.For), part => Assert.Equal(0f, part.Glow));
    }

    // ---- the icons ----

    [Theory]
    [InlineData(PlaceableId.Fence, "res://art/icons/placeable_fence.svg")]
    [InlineData(PlaceableId.Chest, "res://art/icons/placeable_chest.svg")]
    public void AC10_placeable_icon_path(PlaceableId placeable, string expected) =>
        Assert.Equal(expected, ItemIcons.PlaceableIcon(placeable));

    [Theory]
    [InlineData(PlaceableId.Fence, "res://art/icons/placeable_fence.svg")]
    [InlineData(PlaceableId.Chest, "res://art/icons/placeable_chest.svg")]
    public void AC10_a_placeable_item_and_a_held_placeable_show_its_icon(PlaceableId placeable, string expected)
    {
        Assert.Equal(expected, ItemIcons.For(ItemRef.Placeable(placeable)));
        Assert.Equal(expected, ItemIcons.For(HeldItem.PlaceableOf(placeable)));
    }

    [Fact]
    public void AC10_an_undefined_placeable_has_no_icon() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemIcons.PlaceableIcon((PlaceableId)99));

    [Theory]
    [MemberData(nameof(IconPaths))]
    public void AC10_every_placeable_icon_exists_with_its_godot_import(string resPath)
    {
        var path = RepoPath(resPath);

        Assert.True(File.Exists(path), $"{resPath} is missing");
        Assert.True(File.Exists(path + ".import"), $"{resPath}.import is missing");
    }

    [Theory]
    [MemberData(nameof(IconPaths))]
    public void AC10_every_placeable_icon_is_64_by_64_flat_and_in_palette_colours(string resPath)
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
        Assert.Contains(paints, value => string.Equals(value, ArtPalette.Hex("navy"), StringComparison.OrdinalIgnoreCase));
    }

    // ---- helpers ----

    private static string RepoPath(string resPath)
    {
        Assert.StartsWith("res://", resPath);
        return Path.Combine(new[] { LongWayHome.Tests.Game.SceneText.RepoRoot(), "game" }.Concat(resPath["res://".Length..].Split('/')).ToArray());
    }
}
