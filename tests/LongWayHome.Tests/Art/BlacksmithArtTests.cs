using System.Numerics;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The blacksmith at each rebuild stage (design doc, Blacksmith; ART_DIRECTION §7.3): a round drum with a stone lower
/// storey, a forge arch that glows when lit, an anvil, a quench trough, a hammer sign and a big stone chimney.
/// Building-local metres: origin at the drum's centre on the ground, the forge facing +Z.
/// </summary>
public class BlacksmithArtTests
{
    public static TheoryData<BlacksmithStage> Stages() => new(Enum.GetValues<BlacksmithStage>());

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_uses_palette_roles_with_unique_names_and_positive_sizes(BlacksmithStage stage)
    {
        var parts = BlacksmithArt.For(stage).Concat(BlacksmithArt.NightLights(stage)).ToList();
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        var duplicates = parts.GroupBy(part => part.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Assert.True(duplicates.Count == 0, $"duplicate names: {string.Join(", ", duplicates)}");
        Assert.All(parts, part => Assert.True(part.SizeX > 0f && part.SizeY > 0f, $"{part.Name} has a zero size"));
    }

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_stands_on_the_ground_fits_its_plot_and_the_chimney_tops_it(BlacksmithStage stage)
    {
        var parts = BlacksmithArt.For(stage);
        var (min, max) = Extent(parts);

        Assert.True(min.Y >= -0.05f, $"{stage} reaches y {min.Y:0.###}");
        Assert.True(min.X >= -5.2f && max.X <= 5.2f && min.Z >= -5.2f && max.Z <= 5.2f,
            $"{stage} spans x {min.X:0.##}..{max.X:0.##}, z {min.Z:0.##}..{max.Z:0.##}");
        var chimney = parts.Single(part => part.Name == "ChimneyStack");
        Assert.True(Extent([chimney]).Max.Y >= BlacksmithArt.ChimneyTop - 0.01f && chimney.Z < 0f, "the chimney isn't tall at the back");
        Assert.True(Extent(parts.Where(part => Named(part, "Leaf"))).Max.Y < BlacksmithArt.ChimneyTop, "the chimney doesn't rise above the roof");
    }

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_has_a_stone_lower_storey_with_staves_above_and_a_forge_anvil_and_trough(BlacksmithStage stage)
    {
        var parts = BlacksmithArt.For(stage);
        var stone = parts.Where(part => Named(part, "Stone")).ToList();
        var staves = parts.Where(part => Named(part, "Stave")).ToList();

        Assert.True(stone.Count >= 50, $"only {stone.Count} stone blocks");
        Assert.All(stone, part => Assert.Contains(part.Role, new[] { "stone", "rock" }));
        Assert.True(Extent(stone).Max.Y <= BlacksmithArt.StoneTo + 0.05f, "the stone goes above the stone storey");
        Assert.True(Extent(staves).Min.Y >= BlacksmithArt.StoneTo - 0.05f, "staves reach down into the stone storey");
        Assert.DoesNotContain(staves, part => part.Name.EndsWith("Lower", StringComparison.Ordinal));

        var surround = parts.Single(part => part.Name == "ForgeSurround");
        Assert.True(surround.Z > BlacksmithArt.WallRadius - 0.1f && MathF.Abs(surround.X) < 0.01f, "the forge doesn't face the front");
        Assert.Equal(7, parts.Count(part => Named(part, "Coal")));
        Assert.Contains(parts, part => part.Name == "AnvilFace");
        Assert.Contains(parts, part => part.Name == "Trough");
    }

    [Fact]
    public void Empty_is_a_cold_dark_forge()
    {
        var parts = BlacksmithArt.For(BlacksmithStage.Empty);

        Assert.All(parts, part => Assert.Equal(0f, part.Glow));
        Assert.All(parts.Where(part => Named(part, "Coal")), coal => Assert.Equal("stone", coal.Role));
        Assert.DoesNotContain(parts, part => Named(part, "Flame") || Named(part, "Smoke") || Named(part, "Sign") || part.Name == "ForgeGlow");
        Assert.Equal("timber-trim", parts.Single(part => part.Name == "TroughInside").Role);
        Assert.Contains(parts, part => Named(part, "WindowBoard"));
        Assert.Contains(parts, part => Named(part, "Leaf") && part.Role == "roof-wilted");
        Assert.Empty(BlacksmithArt.NightLights(BlacksmithStage.Empty));
    }

    [Fact]
    public void Reopened_lights_the_forge_with_smoke_water_and_the_hammer_sign()
    {
        var parts = BlacksmithArt.For(BlacksmithStage.Reopened);

        Assert.All(parts.Where(part => Named(part, "Coal") || Named(part, "Flame")), part => Assert.True(part.Glow > 0f, $"{part.Name} doesn't glow"));
        Assert.Contains(parts, part => part.Name == "ForgeGlow" && part.Glow > 0f);
        Assert.Equal(3, parts.Count(part => Named(part, "Smoke")));
        Assert.Equal("lake-deep", parts.Single(part => part.Name == "TroughInside").Role);
        Assert.Contains(parts, part => Named(part, "SignHammer"));
        Assert.DoesNotContain(parts, part => Named(part, "Furnace") || Named(part, "Vane") || Named(part, "Rack"));
        Assert.DoesNotContain(parts, part => part.Role is "wall-grey" or "roof-wilted" or "trunk-grey");
        Assert.NotEmpty(BlacksmithArt.NightLights(BlacksmithStage.Reopened));
    }

    [Fact]
    public void The_forge_stage_adds_a_glowing_smelting_furnace_and_more_smoke()
    {
        var parts = BlacksmithArt.For(BlacksmithStage.Forge);
        var furnace = parts.Where(part => Named(part, "Furnace")).ToList();

        Assert.True(Extent(furnace).Min.X > 1.5f, "the furnace isn't on the right");
        Assert.Contains(furnace, part => part.Name == "FurnaceMouth" && part.Glow > 0f);
        Assert.True(parts.Count(part => Named(part, "Smoke") || Named(part, "FurnaceSmoke")) > BlacksmithArt.For(BlacksmithStage.Reopened).Count(part => Named(part, "Smoke")));
        Assert.DoesNotContain(parts, part => Named(part, "Vane") || Named(part, "Rack"));
    }

    [Fact]
    public void The_master_smith_adds_a_teal_weathervane_on_the_peak_and_finished_metal_parts()
    {
        var parts = BlacksmithArt.For(BlacksmithStage.MasterSmith);

        var vane = parts.Where(part => Named(part, "Vane")).ToList();
        Assert.Contains(vane, part => part.Role == "foliage");
        Assert.True(Extent(vane).Min.Y > 6.5f, "the weathervane isn't on the peak");
        var rack = parts.Where(part => Named(part, "Rack")).ToList();
        Assert.True(Extent(rack).Max.X < -1.5f, "the rack isn't on the left");
        Assert.Contains(rack, part => part.Role == "rosk");
        Assert.Contains(rack, part => part.Role == "groth");
        Assert.Contains(rack, part => part.Role == "ship-metal");
        Assert.Contains(parts, part => Named(part, "Furnace"));
    }
}
