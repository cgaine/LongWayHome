using LongWayHome.Application.Games;

namespace LongWayHome.Tests.Games;

/// <summary>The camera's turn, zoom and tilt around the farmer: <see cref="CameraView"/> (issues #68 and #69, REQ-52).</summary>
public class CameraViewTests
{
    private const float Tolerance = 0.01f;

    private static float H(CameraView view) => view.Distance * MathF.Cos(view.PitchDegrees * MathF.PI / 180f);

    [Fact]
    public void AC4_default_view_is_yaw_0_zoom_half_and_sits_where_the_camera_does_today()
    {
        var view = CameraView.Default;

        Assert.Equal(0f, view.YawDegrees);
        Assert.Equal(0.5f, view.Zoom);
        Assert.Equal(12.75f, view.Distance, Tolerance);
        var (x, y, z) = view.Offset;
        Assert.Equal(0f, x, 0.1f);
        Assert.Equal(9f, y, 0.1f);
        Assert.Equal(9f, z, 0.1f);
    }

    [Fact]
    public void AC4_constants_match_the_specification()
    {
        Assert.Equal(120f, CameraView.RotateDegreesPerSecond);
        Assert.Equal(0.6f, CameraView.ZoomPerSecond);
        Assert.Equal(7f, CameraView.NearestDistance);
        Assert.Equal(18.5f, CameraView.FarthestDistance);
        Assert.Equal(30f, CameraView.NearestPitchDegrees);
        Assert.Equal(60f, CameraView.FarthestPitchDegrees);
    }

    [Theory]
    [InlineData(1f, 0.75f, 90f)]
    [InlineData(-1f, 0.75f, 270f)]
    [InlineData(0.5f, 1.5f, 90f)]
    public void AC5_turning_moves_yaw_by_120_degrees_per_second(float input, float seconds, float expected)
    {
        var view = CameraView.Default.Turned(input, seconds);

        Assert.Equal(expected, view.YawDegrees, Tolerance);
    }

    [Fact]
    public void AC5_turning_right_for_3_seconds_comes_full_circle()
    {
        var view = CameraView.Default.Turned(1f, 3f);

        var difference = MathF.Min(view.YawDegrees, 360f - view.YawDegrees);
        Assert.True(difference < Tolerance, $"yaw {view.YawDegrees} is not back at 0");
        Assert.InRange(view.YawDegrees, 0f, 359.9999f);
    }

    [Fact]
    public void AC5_yaw_stays_in_0_to_360_frame_by_frame_in_both_directions()
    {
        foreach (var input in new[] { 1f, -1f })
        {
            var view = CameraView.Default;
            for (var frame = 0; frame < 60 * 20; frame++)
            {
                view = view.Turned(input, 1f / 60f);
                Assert.True(view.YawDegrees >= 0f && view.YawDegrees < 360f, $"yaw {view.YawDegrees} out of range");
            }
        }
    }

    [Fact]
    public void AC5_yaw_wraps_even_for_a_very_long_turn()
    {
        var view = CameraView.Default.Turned(1f, 1000f).Turned(-1f, 5000f);

        Assert.True(view.YawDegrees >= 0f && view.YawDegrees < 360f, $"yaw {view.YawDegrees} out of range");
    }

    [Fact]
    public void AC5_turning_does_not_change_zoom()
    {
        Assert.Equal(0.5f, CameraView.Default.Turned(1f, 0.4f).Zoom);
    }

    [Fact]
    public void AC6_at_yaw_90_the_camera_is_west_of_the_farmer()
    {
        var view = CameraView.Default.Turned(1f, 0.75f);

        var (x, y, z) = view.Offset;
        Assert.Equal(-H(view), x, Tolerance);
        Assert.Equal(0f, z, Tolerance);
        Assert.Equal(9.02f, y, 0.01f);
    }

    [Fact]
    public void AC6_at_yaw_180_the_camera_is_north_of_the_farmer()
    {
        var view = CameraView.Default.Turned(1f, 1.5f);

        var (x, _, z) = view.Offset;
        Assert.Equal(0f, x, Tolerance);
        Assert.Equal(-H(view), z, Tolerance);
    }

    [Fact]
    public void AC6_offset_length_is_the_distance_at_any_yaw_and_zoom()
    {
        var view = new CameraView(37f, 0.2f);

        var (x, y, z) = view.Offset;
        Assert.Equal(view.Distance, MathF.Sqrt(x * x + y * y + z * z), Tolerance);
    }

    [Fact]
    public void AC7_zooming_in_for_5_seconds_stops_at_the_nearest_distance()
    {
        var view = CameraView.Default.Zoomed(-1f, 5f);

        Assert.Equal(0f, view.Zoom);
        Assert.Equal(7f, view.Distance, Tolerance);
    }

    [Fact]
    public void AC7_zooming_out_for_5_seconds_stops_at_the_farthest_distance()
    {
        var view = CameraView.Default.Zoomed(1f, 5f);

        Assert.Equal(1f, view.Zoom);
        Assert.Equal(18.5f, view.Distance, Tolerance);
    }

    [Fact]
    public void AC7_zooming_with_no_input_changes_nothing()
    {
        Assert.Equal(CameraView.Default, CameraView.Default.Zoomed(0f, 3f));
    }

    [Fact]
    public void AC7_zoom_moves_0_6_per_second_and_does_not_change_yaw()
    {
        var view = new CameraView(45f, 0.5f).Zoomed(1f, 0.5f);

        Assert.Equal(0.8f, view.Zoom, Tolerance);
        Assert.Equal(45f, view.YawDegrees);
    }

    [Fact]
    public void Both_rotate_actions_held_cancel_out()
    {
        // Input.GetAxis gives 0 when both are held; zero input must leave the view alone.
        Assert.Equal(CameraView.Default, CameraView.Default.Turned(0f, 2f));
    }

    // Issue #69: the tilt follows the zoom.

    [Theory]
    [InlineData(0f, 30f)]
    [InlineData(0.25f, 37.5f)]
    [InlineData(0.5f, 45f)]
    [InlineData(1f, 60f)]
    public void AC1_pitch_runs_from_30_at_nearest_to_60_at_farthest(float zoom, float expected)
    {
        Assert.Equal(expected, new CameraView(0f, zoom).PitchDegrees, Tolerance);
    }

    [Fact]
    public void AC1_pitch_rises_steadily_with_zoom_and_stays_between_30_and_60()
    {
        var previous = float.NegativeInfinity;
        for (var step = 0; step <= 100; step++)
        {
            var pitch = new CameraView(0f, step / 100f).PitchDegrees;
            Assert.InRange(pitch, 30f, 60f);
            Assert.True(pitch > previous, $"pitch {pitch} at zoom {step / 100f} did not rise");
            previous = pitch;
        }
    }

    [Fact]
    public void AC2_default_pitch_is_45_so_the_starting_view_is_unchanged()
    {
        Assert.Equal(45f, CameraView.Default.PitchDegrees, Tolerance);
    }

    [Theory]
    [InlineData(1f, 16.02f, 9.25f)]
    [InlineData(0f, 3.5f, 6.06f)]
    public void AC3_offset_tilts_with_zoom_at_yaw_0(float zoom, float expectedY, float expectedZ)
    {
        var (x, y, z) = new CameraView(0f, zoom).Offset;

        Assert.Equal(0f, x, 0.05f);
        Assert.Equal(expectedY, y, 0.05f);
        Assert.Equal(expectedZ, z, 0.05f);
    }

    [Fact]
    public void AC4_tilt_applies_at_yaw_90()
    {
        var (x, y, z) = new CameraView(90f, 1f).Offset;

        Assert.Equal(-9.25f, x, 0.05f);
        Assert.Equal(16.02f, y, 0.05f);
        Assert.Equal(0f, z, 0.05f);
    }

    // Issue #70: walking is relative to the camera.

    private const float MoveTolerance = 1e-5f;

    [Theory]
    [InlineData(0f, 0f, -1f)]
    [InlineData(90f, 1f, 0f)]
    [InlineData(180f, 0f, 1f)]
    [InlineData(270f, -1f, 0f)]
    public void AC1_forward_walks_away_from_the_camera(float yaw, float expectedX, float expectedZ)
    {
        var (x, z) = new CameraView(yaw, 0.5f).ToWorldMove(0f, -1f);

        Assert.Equal(expectedX, x, MoveTolerance);
        Assert.Equal(expectedZ, z, MoveTolerance);
    }

    [Fact]
    public void AC2_right_walks_to_the_cameras_right_at_yaw_90()
    {
        var (x, z) = new CameraView(90f, 0.5f).ToWorldMove(1f, 0f);

        Assert.Equal(0f, x, MoveTolerance);
        Assert.Equal(1f, z, MoveTolerance);
    }

    [Fact]
    public void AC3_to_world_move_preserves_a_full_tilts_length()
    {
        var (x, z) = new CameraView(37f, 0.5f).ToWorldMove(0.6f, -0.8f);

        Assert.Equal(1f, MathF.Sqrt(x * x + z * z), MoveTolerance);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(45f)]
    [InlineData(90f)]
    [InlineData(137.5f)]
    [InlineData(270f)]
    [InlineData(359f)]
    public void AC3_to_world_move_preserves_a_partial_tilts_length(float yaw)
    {
        var (x, z) = new CameraView(yaw, 0.5f).ToWorldMove(0.3f, 0f);

        Assert.Equal(0.3f, MathF.Sqrt(x * x + z * z), MoveTolerance);
    }

    [Theory]
    [InlineData(0.6f, -0.8f)]
    [InlineData(-1f, 0f)]
    [InlineData(0.25f, 0.5f)]
    [InlineData(0f, 0f)]
    public void AC4_to_world_move_at_yaw_0_returns_the_input(float inputX, float inputY)
    {
        var (x, z) = new CameraView(0f, 0.5f).ToWorldMove(inputX, inputY);

        Assert.Equal(inputX, x, MoveTolerance);
        Assert.Equal(inputY, z, MoveTolerance);
    }
}
