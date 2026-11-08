using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The items in the farmer's right hand (#102): parts in the hand's space, y up from the grip (origin at the
/// <c>RightHand</c> bone), drawn from the shared palette.
/// </summary>
public class HeldItemArtTests
{
    public static TheoryData<CropTypeId, string> SeedColours => new()
    {
        { CropTypeId.Emberroot, "ember" },
        { CropTypeId.Dusktuber, "dusktuber" },
        { CropTypeId.Moonmelon, "moonmelon" },
        { CropTypeId.Sunbell, "sunbell-gold" },
        { CropTypeId.Starcorn, "starcorn" },
    };

    public static IEnumerable<object[]> EveryHeldItem() =>
        new[] { HeldItem.Of(ToolId.Hoe), HeldItem.Of(ToolId.WateringCan) }
            .Concat(Enum.GetValues<CropTypeId>().Select(HeldItem.SeedOf))
            .Select(held => new object[] { held });

    // ---- #145: the farmer holds the bought axe and pickaxe ----

    [Fact]
    public void AC9_the_held_axe_uses_the_axe_parts() =>
        Assert.Equal(HeldItemArt.PartsFor(GatherTool.Axe), HeldItemArt.PartsFor(HeldItem.Of(ToolId.Axe)));

    [Fact]
    public void AC9_the_held_pickaxe_uses_the_pickaxe_parts() =>
        Assert.Equal(HeldItemArt.PartsFor(GatherTool.Pickaxe), HeldItemArt.PartsFor(HeldItem.Of(ToolId.Pickaxe)));

    [Fact]
    public void AC9_every_tool_has_held_item_art() =>
        Assert.All(Enum.GetValues<ToolId>(), tool => Assert.NotEmpty(HeldItemArt.PartsFor(HeldItem.Of(tool))));

    // ---- AC2: every held-item role is a palette role ----

    [Theory]
    [MemberData(nameof(EveryHeldItem))]
    public void AC2_every_held_item_part_role_is_a_palette_role(HeldItem held)
    {
        var roles = ArtPalette.Swatches.Select(s => s.Role).ToHashSet(StringComparer.Ordinal);
        var parts = HeldItemArt.PartsFor(held);

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.True(roles.Contains(part.Role), $"{part.Name} has unknown role '{part.Role}'"));
    }

    // ---- AC5: the hoe, the can and a pouch per crop ----

    [Theory]
    [MemberData(nameof(EveryHeldItem))]
    public void AC5_parts_for_returns_parts_for_every_held_item(HeldItem held)
    {
        Assert.NotEmpty(HeldItemArt.PartsFor(held));
    }

    [Theory]
    [MemberData(nameof(SeedColours))]
    public void AC5_seed_pouch_shows_the_crop_colour(CropTypeId crop, string colour)
    {
        var parts = HeldItemArt.PartsFor(HeldItem.SeedOf(crop));

        Assert.Contains(parts, part => part.Role == colour);
    }

    [Theory]
    [MemberData(nameof(SeedColours))]
    public void AC5_seed_pouch_is_made_of_seed_pouch_cloth(CropTypeId crop, string colour)
    {
        _ = colour;
        var parts = HeldItemArt.PartsFor(HeldItem.SeedOf(crop));

        Assert.Contains(parts, part => part.Role == "seed-pouch");
    }

    [Fact]
    public void AC5_hoe_head_stands_at_least_0_5_m_above_the_grip()
    {
        var top = Top(HeldItemArt.PartsFor(HeldItem.Of(ToolId.Hoe)));

        Assert.True(top >= 0.5f, $"the hoe's highest part tops out at {top:F3} m above the grip");
    }

    [Fact]
    public void AC5_watering_can_hangs_below_the_grip()
    {
        var bottom = Bottom(HeldItemArt.PartsFor(HeldItem.Of(ToolId.WateringCan)));

        Assert.True(bottom < 0f, $"the can's lowest part bottoms out at {bottom:F3} m, not below the grip");
    }

    [Theory]
    [MemberData(nameof(SeedColours))]
    public void AC5_seed_pouch_hangs_below_the_grip(CropTypeId crop, string colour)
    {
        _ = colour;
        var bottom = Bottom(HeldItemArt.PartsFor(HeldItem.SeedOf(crop)));

        Assert.True(bottom < 0f, $"the {crop} pouch's lowest part bottoms out at {bottom:F3} m, not below the grip");
    }

    // ---- helpers ----

    private static float Top(IReadOnlyList<CropPart> parts)
    {
        Assert.NotEmpty(parts);
        return PartBoxes.Extents(parts.Select(PartBoxes.Corners)).Max.Y;
    }

    private static float Bottom(IReadOnlyList<CropPart> parts)
    {
        Assert.NotEmpty(parts);
        return PartBoxes.Extents(parts.Select(PartBoxes.Corners)).Min.Y;
    }
}
