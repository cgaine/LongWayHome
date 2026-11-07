using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The farmer's and the held items' tables beyond #102's acceptance criteria: <see cref="FarmerArt.Bounds"/> edge
/// cases, the left/right symmetry the mirrored parts and bones promise, each part sitting on its own bone, the back of
/// the head reading as hair, and <see cref="HeldItemArt.PartsFor"/> rejecting an item it has no art for.
/// </summary>
public class FarmerArtGeometryTests
{
    [Fact]
    public void Bounds_of_no_parts_is_zero()
    {
        Assert.Equal((0f, 0f, 0f), FarmerArt.Bounds([]));
    }

    [Fact]
    public void Bounds_of_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => FarmerArt.Bounds(null!));
    }

    [Fact]
    public void Bounds_of_one_rotated_box_is_its_rotated_extent()
    {
        // A 0.2 x 0.4 x 0.1 box rolled 90° lies on its side: 0.4 wide, 0.2 tall, 0.1 deep.
        FarmerPart part = new("Plank", CropPartShape.Box, 0.2f, 0.4f, 0.1f, 0f, 1f, 0f, 0f, 0f, 90f, "timber", "Hips");

        var (width, depth, height) = FarmerArt.Bounds([part]);

        Assert.Equal(0.4f, width, 1e-4f);
        Assert.Equal(0.1f, depth, 1e-4f);
        Assert.Equal(0.2f, height, 1e-4f);
    }

    [Fact]
    public void Every_left_bone_mirrors_a_right_bone()
    {
        var bones = FarmerArt.Bones.ToDictionary(bone => bone.Name);
        var left = FarmerArt.Bones.Where(bone => bone.Name.StartsWith("Left", StringComparison.Ordinal)).ToList();

        Assert.Equal(7, left.Count);
        foreach (var bone in left)
        {
            var right = bones["Right" + bone.Name["Left".Length..]];
            Assert.Equal(-bone.X, right.X, 1e-6f);
            Assert.Equal(bone.Y, right.Y, 1e-6f);
            Assert.Equal(bone.Z, right.Z, 1e-6f);
            Assert.Equal(bone.Parent?.Replace("Left", "Right", StringComparison.Ordinal), right.Parent);
        }
    }

    [Fact]
    public void Every_left_part_mirrors_a_right_part_except_the_one_sided_details()
    {
        var parts = FarmerArt.Parts.ToDictionary(part => part.Name);
        var left = FarmerArt.Parts.Where(part => part.Name.StartsWith("Left", StringComparison.Ordinal)).ToList();
        var right = FarmerArt.Parts.Where(part => part.Name.StartsWith("Right", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(left);
        Assert.Equal(left.Count, right.Count);
        foreach (var part in left)
        {
            var mirror = parts["Right" + part.Name["Left".Length..]];
            Assert.Equal(-part.X, mirror.X, 1e-6f);
            Assert.Equal((part.Y, part.Z, part.SizeX, part.SizeY, part.SizeZ), (mirror.Y, mirror.Z, mirror.SizeX, mirror.SizeY, mirror.SizeZ));
            Assert.Equal((part.Shape, part.Role, part.PitchDegrees), (mirror.Shape, mirror.Role, mirror.PitchDegrees));
            Assert.Equal(-part.YawDegrees, mirror.YawDegrees, 1e-6f);
            Assert.Equal(-part.RollDegrees, mirror.RollDegrees, 1e-6f);
            Assert.Equal(part.Bone.Replace("Left", "Right", StringComparison.Ordinal), mirror.Bone);
        }
    }

    [Fact]
    public void Every_part_sits_near_its_bone()
    {
        var bones = FarmerArt.Bones.ToDictionary(bone => bone.Name);

        Assert.All(FarmerArt.Parts, part =>
        {
            var bone = bones[part.Bone];
            var distance = MathF.Sqrt(
                MathF.Pow(part.X - bone.X, 2) + MathF.Pow(part.Y - bone.Y, 2) + MathF.Pow(part.Z - bone.Z, 2));
            Assert.True(distance <= 0.5f, $"{part.Name} is {distance:F2} m from its bone {part.Bone}");
        });
    }

    [Fact]
    public void Each_hand_part_is_centred_on_its_hand_bone()
    {
        // The held item's origin is the RightHand bone, so the fist must sit on it.
        var bones = FarmerArt.Bones.ToDictionary(bone => bone.Name);
        foreach (var side in new[] { "Left", "Right" })
        {
            var hand = FarmerArt.Parts.Single(part => part.Name == $"{side}Hand");
            var bone = bones[$"{side}Hand"];
            Assert.Equal($"{side}Hand", hand.Bone);
            Assert.Equal((bone.X, bone.Y, bone.Z), (hand.X, hand.Y, hand.Z));
        }
    }

    [Fact]
    public void The_back_of_the_head_is_hair()
    {
        // From behind, the rearmost thing below the cap is hair, so his back never reads as a face.
        var belowCap = FarmerArt.Parts
            .Where(part => part.Bone == "Head" && part.Y is > 1.15f and < 1.42f)
            .ToList();

        var rearmost = belowCap.MinBy(part => PartBoxes.Corners(part).Min(corner => corner.Z));

        Assert.Equal("hair", rearmost!.Role);
    }

    [Fact]
    public void Parts_for_an_item_with_neither_tool_nor_seed_throws()
    {
        Assert.Throws<ArgumentException>(() => HeldItemArt.PartsFor(new HeldItem(null, null)));
    }

    [Fact]
    public void Parts_for_an_undefined_tool_or_crop_throws()
    {
        Assert.Throws<ArgumentException>(() => HeldItemArt.PartsFor(HeldItem.Of((ToolId)99)));
        Assert.Throws<ArgumentException>(() => HeldItemArt.PartsFor(HeldItem.SeedOf((CropTypeId)99)));
    }

    [Fact]
    public void Parts_for_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => HeldItemArt.PartsFor(null!));
    }

    [Fact]
    public void Seed_pouches_differ_only_in_the_seed_colour()
    {
        var emberroot = HeldItemArt.PartsFor(HeldItem.SeedOf(CropTypeId.Emberroot));
        var starcorn = HeldItemArt.PartsFor(HeldItem.SeedOf(CropTypeId.Starcorn));

        Assert.Equal(emberroot.Count, starcorn.Count);
        Assert.Equal(
            emberroot.Select(part => part.Role == "ember" ? part with { Role = "starcorn" } : part),
            starcorn);
    }
}
