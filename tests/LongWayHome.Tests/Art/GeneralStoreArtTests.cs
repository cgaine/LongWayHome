using System.Numerics;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The general store at each rebuild stage (design doc, First building: General store; ART_DIRECTION §7.3) and the
/// rebuild sign (§7.5). Store parts are in building-local metres: origin at the drum's centre on the ground, the door
/// facing +Z. Every check works on the parts' rotated corners (<see cref="PartBoxes"/>).
/// </summary>
public class GeneralStoreArtTests
{
    public static TheoryData<StoreStage> Stages() => new(Enum.GetValues<StoreStage>());

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    private static float Angle(CropPart part) => MathF.Atan2(part.X, part.Z) * 180f / MathF.PI;

    private static HashSet<string> Roles() => ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

    // ---- every stage ----

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_uses_palette_roles_with_unique_names_and_positive_sizes(StoreStage stage)
    {
        var parts = GeneralStoreArt.For(stage).Concat(GeneralStoreArt.NightLights(stage)).ToList();
        var roles = Roles();

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        var duplicates = parts.GroupBy(part => part.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Assert.True(duplicates.Count == 0, $"duplicate names: {string.Join(", ", duplicates)}");
        Assert.All(parts, part => Assert.True(part.SizeX > 0f && part.SizeY > 0f, $"{part.Name} has a zero size"));
    }

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_stands_on_the_ground_and_fits_its_plot(StoreStage stage)
    {
        var parts = GeneralStoreArt.For(stage);
        var (min, max) = Extent(parts);

        Assert.True(min.Y >= -0.05f, $"{stage} reaches y {min.Y:0.###}");
        Assert.InRange(max.Y, GeneralStoreArt.ApexY, GeneralStoreArt.ApexY + 0.8f);
        // The drum and its eaves, plus the storeroom grown onto the east side from stage 3.
        Assert.True(min.X >= -4.6f && max.X <= 7f && min.Z >= -4.6f && max.Z <= 4.6f,
            $"{stage} spans x {min.X:0.##}..{max.X:0.##}, z {min.Z:0.##}..{max.Z:0.##}");
    }

    [Theory]
    [MemberData(nameof(Stages))]
    public void Every_stage_is_a_wide_drum_with_the_ember_door_a_round_topped_shop_window_and_the_sprout_sign(StoreStage stage)
    {
        var parts = GeneralStoreArt.For(stage);

        var staves = parts.Where(part => Named(part, "Stave")).ToList();
        Assert.True(staves.Max(part => MathF.Sqrt(part.X * part.X + part.Z * part.Z)) is > 3.0f and < 3.3f, "the drum isn't 6.4 m across");
        var door = parts.Single(part => part.Name == "Door");
        Assert.Equal("ember", door.Role);
        Assert.True(door.Z > GeneralStoreArt.WallRadius && MathF.Abs(door.X) < 0.01f, "the door doesn't face the front");
        var shopArch = parts.Single(part => part.Name == "ShopWindowGlassArch");
        Assert.True(shopArch.Sides >= 16, "the shop window's top isn't round");
        Assert.True(shopArch.X < 0f, "the shop window isn't left of the door");
        Assert.True(shopArch.SizeX > parts.Single(part => part.Name == "SideWindowGlassArch").SizeX, "the shop window isn't the big one");
        Assert.Contains(parts, part => part.Name == "SignFace" && part.Role == "wood-cut");
        Assert.Contains(parts, part => Named(part, "SignSprout"));
        Assert.Contains(parts, part => Named(part, "Crate"));
        Assert.Contains(parts, part => Named(part, "Awning"));
    }

    [Theory]
    [MemberData(nameof(Stages))]
    public void The_shop_windows_light_sits_in_front_of_its_glass_and_glows(StoreStage stage)
    {
        var lights = GeneralStoreArt.NightLights(stage);
        var glass = GeneralStoreArt.For(stage).Single(part => part.Name == "ShopWindowGlass");
        var light = lights.Single(part => part.Name == "NightShopWindow");

        Assert.All(lights, part => Assert.True(part.Role == "lantern" && part.Glow == 1f, $"{part.Name} isn't a glowing lantern pane"));
        Assert.True(light.X * light.X + light.Z * light.Z > glass.X * glass.X + glass.Z * glass.Z, "the light isn't in front of the glass");
    }

    // ---- the stages ----

    [Fact]
    public void Maggies_corner_is_alive_only_round_the_shop_window()
    {
        var parts = GeneralStoreArt.For(StoreStage.MaggiesCorner);
        var staves = parts.Where(part => Named(part, "Stave")).ToList();

        Assert.Contains(staves, part => part.Role == "timber");
        Assert.All(staves.Where(part => part.Role == "timber"), part => Assert.InRange(Angle(part), -66f, -4f));
        Assert.Contains(staves, part => part.Role == "wall-grey");
        Assert.Contains(parts, part => Named(part, "Leaf") && part.Role == "roof-wilted");
        // Fresh leaves only over the corner: the roof comes back to life above it first.
        Assert.All(parts.Where(part => Named(part, "Leaf") && part.Role is "foliage" or "roof-rows"), part => Assert.InRange(Angle(part), -66f, -4f));
        Assert.All(parts.Where(part => Named(part, "AwningLeaf")), part => Assert.InRange(Angle(part), -66f, -4f));
        Assert.Contains(parts, part => Named(part, "SideWindowBoard"));
        Assert.Contains(parts, part => Named(part, "Weed"));
        Assert.Single(GeneralStoreArt.NightLights(StoreStage.MaggiesCorner), part => part.Name.EndsWith("Window", StringComparison.Ordinal));
    }

    [Fact]
    public void Reopened_is_alive_all_round_with_the_awning_ringing_the_front()
    {
        var parts = GeneralStoreArt.For(StoreStage.Reopened);

        Assert.DoesNotContain(parts, part => part.Role is "wall-grey" or "roof-wilted" or "tips-dry" or "trunk-grey");
        Assert.DoesNotContain(parts, part => Named(part, "Weed") || Named(part, "SideWindowBoard") || Named(part, "Storeroom"));
        var awning = parts.Where(part => Named(part, "AwningLeaf") || Named(part, "AwningEastLeaf")).Select(Angle).ToList();
        Assert.True(awning.Min() < -70f && awning.Max() > 70f, "the awning doesn't ring the front");
        Assert.Equal(4, parts.Count(part => Named(part, "Crate") && !part.Name.Contains("Slat", StringComparison.Ordinal) && !part.Name.Contains("Produce", StringComparison.Ordinal)));
        Assert.Equal(2, GeneralStoreArt.NightLights(StoreStage.Reopened).Count(part => !part.Name.EndsWith("Arch", StringComparison.Ordinal)));
    }

    [Fact]
    public void The_storeroom_is_a_smaller_living_drum_on_the_east_side_with_sacks()
    {
        var parts = GeneralStoreArt.For(StoreStage.Storeroom);
        var storeroom = parts.Where(part => Named(part, "Storeroom")).ToList();

        Assert.NotEmpty(storeroom);
        var (min, max) = Extent(storeroom);
        Assert.True(min.X > 1.5f, "the storeroom isn't on the east side");
        Assert.True(max.Y < GeneralStoreArt.ApexY - 1.0f, "the storeroom isn't smaller");
        Assert.True(storeroom.Count(part => Named(part, "StoreroomSack") && part.Role == "seed-pouch") >= 3);
        Assert.Contains(storeroom, part => part.Name == "StoreroomDoor");
    }

    [Fact]
    public void The_expanded_shop_adds_a_second_awning_more_crates_and_glowing_string_lights()
    {
        var reopened = GeneralStoreArt.For(StoreStage.Storeroom);
        var expanded = GeneralStoreArt.For(StoreStage.ExpandedShop);

        Assert.Contains(expanded, part => Named(part, "AwningWest"));
        Assert.True(expanded.Count(part => Named(part, "Crate")) > reopened.Count(part => Named(part, "Crate")));
        var bulbs = expanded.Where(part => Named(part, "Light") && !Named(part, "LightWire")).ToList();
        Assert.True(bulbs.Count >= 12, $"only {bulbs.Count} bulbs");
        Assert.All(bulbs, bulb => Assert.True(bulb.Role == "lantern" && bulb.Glow > 0f));
        Assert.DoesNotContain(reopened, part => Named(part, "Light"));
        Assert.Contains(expanded, part => Named(part, "Storeroom"));
    }

    // ---- the rebuild sign ----

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void The_rebuild_sign_fills_a_pip_per_material_delivered(bool wood, bool stone, bool gold)
    {
        var parts = RebuildSignArt.Parts(wood, stone, gold);
        var roles = Roles();

        Assert.All(parts, part => Assert.Contains(part.Role, roles));
        Assert.Equal(parts.Count, parts.Select(part => part.Name).Distinct().Count());
        Assert.Equal(wood ? "soil-tilled" : "target-highlight", parts.Single(part => part.Name == "PipWood").Role);
        Assert.Equal(stone ? "stone" : "target-highlight", parts.Single(part => part.Name == "PipStone").Role);
        Assert.Equal(gold ? "sunbell-gold" : "target-highlight", parts.Single(part => part.Name == "PipGold").Role);
        Assert.Equal(2, parts.Count(part => Named(part, "Post") && !part.Name.EndsWith("Bud", StringComparison.Ordinal)));
        Assert.Contains(parts, part => Named(part, "Trim"));
        Assert.All(parts, part => Assert.Equal(0f, part.Glow));
        var (min, max) = Extent(parts);
        Assert.True(min.Y >= -0.01f && max.Y is > 1.3f and < 1.8f, $"the sign spans y {min.Y:0.##}..{max.Y:0.##}");
    }

    [Fact]
    public void The_rebuild_signs_night_outline_glows_amber_round_the_board()
    {
        var outline = RebuildSignArt.NightOutline;
        var board = RebuildSignArt.Parts(false, false, false).Single(part => part.Name == "Board");

        Assert.Equal(4, outline.Count);
        Assert.All(outline, part => Assert.True(part.Role == "lantern" && part.Glow == 1f));
        var (min, max) = Extent(outline);
        Assert.True(min.X < board.X - board.SizeX / 2f && max.X > board.X + board.SizeX / 2f, "the outline doesn't go round the board");
    }
}
