using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>Snapping a camera-relative move to the farmer's four-way facing: <see cref="FacingDirection"/> (issue #70, REQ-52).</summary>
public class FacingDirectionTests
{
    private static readonly (int X, int Y) North = (0, -1);
    private static readonly (int X, int Y) South = (0, 1);
    private static readonly (int X, int Y) East = (1, 0);

    [Fact]
    public void AC5_mostly_east_snaps_east()
    {
        Assert.Equal(East, FacingDirection.Snap(0.9f, 0.1f, South));
    }

    [Fact]
    public void AC5_mostly_north_snaps_north()
    {
        Assert.Equal(North, FacingDirection.Snap(0.1f, -0.9f, South));
    }

    [Fact]
    public void AC5_exact_diagonal_keeps_east_when_facing_east()
    {
        Assert.Equal(East, FacingDirection.Snap(0.7071f, -0.7071f, East));
    }

    [Fact]
    public void AC5_exact_diagonal_gives_north_when_facing_south()
    {
        Assert.Equal(North, FacingDirection.Snap(0.7071f, -0.7071f, South));
    }

    [Theory]
    [InlineData(-1f, 0f, -1, 0)]
    [InlineData(0f, 1f, 0, 1)]
    [InlineData(1f, 0f, 1, 0)]
    [InlineData(0f, -1f, 0, -1)]
    public void AC5_each_pure_direction_snaps_to_itself(float x, float y, int expectedX, int expectedY)
    {
        // Facing somewhere else first, so the result cannot just be the current facing kept.
        var current = expectedX == 0 ? East : North;

        Assert.Equal((expectedX, expectedY), FacingDirection.Snap(x, y, current));
    }

    [Fact]
    public void AC5_near_diagonal_within_tolerance_counts_as_diagonal()
    {
        // |y| - |x| is about 5e-6 in single precision: inside the 1e-5 tolerance, so facing east is kept.
        Assert.Equal(East, FacingDirection.Snap(0.707105f, -0.70711f, East));
    }

    [Fact]
    public void AC5_just_outside_tolerance_snaps_to_the_larger_axis()
    {
        // |y| - |x| is about 1e-4: outside the tolerance, so the larger (north) axis wins over the current east facing.
        Assert.Equal(North, FacingDirection.Snap(0.7f, -0.7001f, East));
    }
}
