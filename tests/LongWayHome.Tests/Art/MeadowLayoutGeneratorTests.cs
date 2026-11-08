using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// The meadow generator's paths the spec tests don't reach (#77): a prop that finds no place in 200 attempts is
/// skipped, and a shade patch's whole outline, not just its centre, stays outside the plot plus margin.
/// </summary>
public class MeadowLayoutGeneratorTests
{
    [Fact]
    public void Edge_a_prop_with_no_allowed_place_is_skipped_after_200_attempts()
    {
        var attempts = new Dictionary<string, int>();
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed, (kind, _, _) =>
        {
            attempts[kind] = attempts.GetValueOrDefault(kind) + 1;
            return false;
        });

        Assert.Empty(props);
        Assert.Equal(10 * MeadowLayout.MaxAttempts, attempts["ShadePatch"]);
        Assert.Equal(120 * MeadowLayout.MaxAttempts, attempts["Tuft"]);
    }

    [Fact]
    public void Edge_only_the_kinds_that_find_a_place_are_placed()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed, (kind, _, _) => kind == "Rock");

        Assert.Equal(14, props.Count);
        Assert.All(props, p => Assert.Equal("Rock", p.Kind));
    }

    [Fact]
    public void Edge_a_prop_is_placed_on_its_first_allowed_attempt()
    {
        var calls = 0;
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed, (kind, _, _) => kind == "Stump" && ++calls % 199 == 0);

        Assert.Equal(6, props.Count);
        Assert.Equal(6 * 199, calls);
    }

    [Fact]
    public void AC3_shade_patch_outlines_have_6_to_9_points_and_stay_off_the_grid()
    {
        var patches = MeadowLayout.Generate(MeadowLayout.FarmSeed).Where(p => p.Kind == "ShadePatch").ToList();
        Assert.Equal(10, patches.Count);

        foreach (var patch in patches)
        {
            var outline = MeadowLayout.ShadePatchOutline(patch);
            Assert.InRange(outline.Count, 6, 9);
            Assert.Equal(outline, MeadowLayout.ShadePatchOutline(patch));
            foreach (var (x, z) in outline)
            {
                // #150: the grid, x -36..36, z -4.5..31.5, is farm ground.
                Assert.False(
                    x > -36f && x < 36f && z > -4.5f && z < 31.5f,
                    $"shade patch {patch} has an outline point ({x}, {z}) on the grid");
                var r = MathF.Sqrt((x - patch.X) * (x - patch.X) + (z - patch.Z) * (z - patch.Z));
                Assert.True(r <= MeadowLayout.ShadePatchRadius * patch.Scale + 1e-4f, $"{patch} point at {r} m");
            }

            // 2-4 m across: the farthest point is at least 1 m from the centre.
            Assert.True(
                outline.Max(p => MathF.Sqrt((p.X - patch.X) * (p.X - patch.X) + (p.Z - patch.Z) * (p.Z - patch.Z))) >= 1f - 1e-4f,
                $"shade patch {patch} is under 2 m across");
        }
    }
}
