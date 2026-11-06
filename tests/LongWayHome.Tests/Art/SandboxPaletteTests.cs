using LongWayHome.Game.Art;

namespace LongWayHome.Tests.Art;

/// <summary>The art sandbox's palette row (#75): <see cref="SandboxPalette"/>.</summary>
public class SandboxPaletteTests
{
    [Fact]
    public void AC3_palette_has_the_23_issue_colours_in_order()
    {
        string[] expected =
        [
            "#7FA65A", "#3F7A6C", "#3E8F82", "#7A68A8", "#8A5A3C", "#3E2A20", "#9C7E8C", "#6B5A78",
            "#8F8A96", "#2D6A86", "#5FB3AA", "#E9773A", "#FFB04A", "#6FF2E0", "#B48CFF", "#C9D6E8",
            "#8CB862", "#9A95A2", "#B8946C", "#4A3226", "#FFF6E0", "#A8A458", "#E4ECF0",
        ];

        Assert.Equal(expected, SandboxPalette.Swatches.Select(s => s.Hex));
    }

    [Fact]
    public void AC3_every_swatch_has_a_role()
    {
        var roles = SandboxPalette.Swatches.Select(s => s.Role).ToList();

        Assert.NotEmpty(roles);
        Assert.All(roles, role => Assert.False(string.IsNullOrWhiteSpace(role)));
        Assert.Equal(roles.Count, roles.Distinct().Count());
    }

    [Fact]
    public void AC3_hexes_are_distinct()
    {
        var hexes = SandboxPalette.Swatches.Select(s => s.Hex).ToList();

        Assert.NotEmpty(hexes);
        Assert.Equal(hexes.Count, hexes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
