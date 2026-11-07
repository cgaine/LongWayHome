using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The farmer's part table (#102, ART_DIRECTION §6.1): a humanoid-profile skeleton, palette roles, about 3 heads tall
/// inside the collision capsule, the cap brim as the facing cue, and "left is +X" (facing +Z).
/// </summary>
public class FarmerArtTests
{
    private static readonly string[] HumanoidBones =
    [
        "Hips", "Spine", "Chest", "Neck", "Head",
        "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
        "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
        "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
        "RightUpperLeg", "RightLowerLeg", "RightFoot",
    ];

    /// <summary>The palette's length on main before #102 appended the farmer's swatches.</summary>
    private const int SwatchesBefore102 = 46;

    /// <summary>The swatches appended after #102: #104's <c>door-faded</c> and <c>glass-dull</c>, then the sandbox's seven tree and rock colours.</summary>
    private const int SwatchesAfter102 = 9;

    // ---- AC1: the skeleton ----

    [Fact]
    public void AC1_bones_are_exactly_the_19_humanoid_profile_names()
    {
        var names = FarmerArt.Bones.Select(bone => bone.Name).ToList();

        Assert.Equal(19, names.Count);
        Assert.Equal(HumanoidBones.OrderBy(n => n, StringComparer.Ordinal), names.OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void AC1_every_parent_is_listed_before_its_children()
    {
        var bones = FarmerArt.Bones;

        for (var i = 0; i < bones.Count; i++)
        {
            if (bones[i].Parent is not { } parent)
            {
                continue;
            }

            var parentIndex = bones.ToList().FindIndex(bone => bone.Name == parent);
            Assert.True(parentIndex >= 0, $"{bones[i].Name}'s parent {parent} is not a bone");
            Assert.True(parentIndex < i, $"{bones[i].Name} (index {i}) comes before its parent {parent} (index {parentIndex})");
        }
    }

    [Fact]
    public void AC1_hips_is_the_only_root()
    {
        var roots = FarmerArt.Bones.Where(bone => bone.Parent is null).Select(bone => bone.Name).ToList();

        Assert.Equal(["Hips"], roots);
    }

    [Fact]
    public void AC1_every_part_follows_a_bone_of_the_skeleton()
    {
        var bones = FarmerArt.Bones.Select(bone => bone.Name).ToHashSet(StringComparer.Ordinal);
        var parts = FarmerArt.Parts;

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.True(bones.Contains(part.Bone), $"{part.Name} follows unknown bone '{part.Bone}'"));
    }

    // ---- AC2: palette roles, and the appended swatches ----

    [Fact]
    public void AC2_every_farmer_part_role_is_a_palette_role()
    {
        var roles = ArtPalette.Swatches.Select(s => s.Role).ToHashSet(StringComparer.Ordinal);
        var parts = FarmerArt.Parts;

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.True(roles.Contains(part.Role), $"{part.Name} has unknown role '{part.Role}'"));
    }

    [Fact]
    public void AC2_skin_skin_shade_hair_and_seed_pouch_are_appended_after_the_existing_swatches()
    {
        ArtSwatch[] appended =
        [
            new("skin", "#D9A07A"),
            new("skin-shade", "#B57E5F"),
            new("hair", "#5A3E2E"),
            new("seed-pouch", "#D9C39A"),
        ];

        Assert.Equal(SwatchesBefore102 + appended.Length + SwatchesAfter102, ArtPalette.Swatches.Count);
        Assert.Equal(appended, ArtPalette.Swatches.Skip(SwatchesBefore102).Take(appended.Length).ToArray());
    }

    // ---- AC3: proportions ----

    [Fact]
    public void AC3_farmer_is_1_55_to_1_65_m_tall()
    {
        var (_, _, height) = FarmerArt.Bounds(FarmerArt.Parts);

        Assert.InRange(height, 1.55f, 1.65f);
    }

    [Fact]
    public void AC3_farmer_is_at_most_0_8_m_wide_and_0_7_m_deep()
    {
        var (width, depth, _) = FarmerArt.Bounds(FarmerArt.Parts);

        Assert.InRange(width, 0.01f, 0.8f);
        Assert.InRange(depth, 0.01f, 0.7f);
    }

    [Fact]
    public void AC3_bounds_are_the_extents_of_the_rotated_part_boxes()
    {
        var parts = FarmerArt.Parts;
        var (min, max) = PartBoxes.Extents(parts.Select(PartBoxes.Corners));

        var (width, depth, height) = FarmerArt.Bounds(parts);

        Assert.Equal(max.X - min.X, width, 1e-4f);
        Assert.Equal(max.Z - min.Z, depth, 1e-4f);
        Assert.Equal(max.Y - min.Y, height, 1e-4f);
    }

    [Fact]
    public void AC3_head_and_cap_are_at_least_30_percent_of_the_height()
    {
        var parts = FarmerArt.Parts;
        var (_, _, height) = FarmerArt.Bounds(parts);
        var head = parts.Where(part => part.Bone == "Head").ToList();
        Assert.NotEmpty(head);

        var (min, max) = PartBoxes.Extents(head.Select(PartBoxes.Corners));

        Assert.True(
            max.Y - min.Y >= 0.3f * height,
            $"head and cap span {max.Y - min.Y:F3} m of {height:F3} m (< 30%)");
    }

    // ---- AC4: the cap brim is the facing cue ----

    [Fact]
    public void AC4_cap_brim_is_the_frontmost_part_above_1_2_m()
    {
        var high = FarmerArt.Parts.Where(part => part.Y > 1.2f).ToList();
        var brim = high.Where(part => part.Name.Contains("Brim", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(brim);
        var brimFront = brim.Max(FrontEdge);

        foreach (var part in high.Except(brim))
        {
            Assert.True(
                FrontEdge(part) <= brimFront + 1e-4f,
                $"{part.Name}'s front edge z {FrontEdge(part):F3} is ahead of the brim's {brimFront:F3}");
        }
    }

    [Fact]
    public void AC4_cap_brim_reaches_at_least_0_12_m_in_front_of_the_head_centre()
    {
        var parts = FarmerArt.Parts;
        var brim = parts.Where(part => part.Name.Contains("Brim", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(brim);

        // The head's centre: the biggest part on the Head bone (the head itself, not the ears, eyes or brim).
        var head = parts.Where(part => part.Bone == "Head").MaxBy(Volume);
        Assert.NotNull(head);

        var reach = brim.Max(FrontEdge) - head!.Z;
        Assert.True(reach >= 0.12f, $"the brim's front edge is only {reach:F3} m in front of the head's centre");
    }

    // ---- "Left" is the farmer's own left: +X when facing +Z ----

    [Fact]
    public void Left_bones_are_on_plus_x_and_right_bones_on_minus_x()
    {
        foreach (var bone in FarmerArt.Bones.Where(bone => bone.Name is "LeftHand" or "LeftUpperArm" or "LeftFoot"))
        {
            Assert.True(bone.X > 0f, $"{bone.Name} is at x {bone.X}, not +X");
        }

        foreach (var bone in FarmerArt.Bones.Where(bone => bone.Name is "RightHand" or "RightUpperArm" or "RightFoot"))
        {
            Assert.True(bone.X < 0f, $"{bone.Name} is at x {bone.X}, not -X");
        }

        Assert.Equal(6, FarmerArt.Bones.Count(bone => bone.Name is "LeftHand" or "LeftUpperArm" or "LeftFoot"
            or "RightHand" or "RightUpperArm" or "RightFoot"));
    }

    [Fact]
    public void Wrist_unit_is_on_the_left_wrist()
    {
        var wrist = FarmerArt.Parts.Where(part => part.Name.Contains("Wrist", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(wrist);
        Assert.Contains(wrist, part => part.Role == "wrist-screen");
        Assert.All(wrist, part =>
        {
            Assert.True(part.Bone is "LeftHand" or "LeftLowerArm", $"{part.Name} follows {part.Bone}, not the left wrist");
            Assert.True(part.X > 0f, $"{part.Name} is at x {part.X}, not on the left (+X)");
        });
    }

    [Fact]
    public void Earth_patch_is_on_the_left_chest()
    {
        var patch = FarmerArt.Parts
            .Where(part => part.Role is "earth-blue" or "earth-green" && part.Bone != "Head")
            .ToList();

        Assert.Contains(patch, part => part.Role == "earth-blue");
        Assert.Contains(patch, part => part.Role == "earth-green");
        Assert.All(patch, part =>
        {
            Assert.True(part.X > 0f, $"{part.Name} is at x {part.X}, not on the left chest (+X)");
            Assert.True(part.Z > 0f, $"{part.Name} is at z {part.Z}, not on the front");
        });
    }

    // ---- helpers ----

    private static float FrontEdge(FarmerPart part) => PartBoxes.Corners(part).Max(corner => corner.Z);

    private static float Volume(FarmerPart part)
    {
        var (min, max) = PartBoxes.Extents([PartBoxes.Corners(part)]);
        var size = max - min;
        return size.X * size.Y * size.Z;
    }
}
