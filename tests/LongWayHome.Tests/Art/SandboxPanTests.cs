using LongWayHome.Application.Games;
using LongWayHome.Game.Art;

namespace LongWayHome.Tests.Art;

/// <summary>The art sandbox camera's pan on the <c>move_*</c> actions (the D-pad, the left stick, WASD).</summary>
public class SandboxPanTests
{
    private const float Eps = 1e-4f;

    [Fact]
    public void Right_moves_the_centre_right_on_screen_at_the_default_view()
    {
        var (x, z) = SandboxPan.Moved((0f, 2.5f), 1f, 0f, CameraView.Default, 0.5f);

        Assert.Equal(SandboxPan.MetresPerSecond * 0.5f, x, Eps);
        Assert.Equal(2.5f, z, Eps);
    }

    [Fact]
    public void Left_moves_the_centre_left_on_screen_at_the_default_view()
    {
        var (x, z) = SandboxPan.Moved((0f, 0f), -1f, 0f, CameraView.Default, 1f);

        Assert.Equal(-SandboxPan.MetresPerSecond, x, Eps);
        Assert.Equal(0f, z, Eps);
    }

    [Fact]
    public void Up_moves_the_centre_away_from_the_camera()
    {
        // At yaw 0 the camera looks north (−Z), so away is −Z.
        var (x, z) = SandboxPan.Moved((0f, 0f), 0f, -1f, CameraView.Default, 1f);

        Assert.Equal(0f, x, Eps);
        Assert.Equal(-SandboxPan.MetresPerSecond, z, Eps);
    }

    [Fact]
    public void Right_follows_the_camera_turn()
    {
        // Turned to look east (+X), screen right is south (+Z).
        var view = CameraView.Default with { YawDegrees = 90f };

        var (x, z) = SandboxPan.Moved((0f, 0f), 1f, 0f, view, 1f);

        Assert.Equal(0f, x, Eps);
        Assert.Equal(SandboxPan.MetresPerSecond, z, Eps);
    }

    [Fact]
    public void No_input_leaves_the_centre_where_it_is()
    {
        Assert.Equal((1.5f, -2f), SandboxPan.Moved((1.5f, -2f), 0f, 0f, CameraView.Default, 1f));
    }

    [Fact]
    public void The_centre_stays_over_the_ground()
    {
        var (x, z) = SandboxPan.Moved((11f, -11f), 1f, -1f, CameraView.Default, 10f);
        var (_, front) = SandboxPan.Moved((0f, 57f), 0f, 1f, CameraView.Default, 10f);
        Assert.Equal(SandboxPan.MaxZ, front, Eps);

        Assert.Equal(SandboxPan.LimitX, x, Eps);
        Assert.Equal(SandboxPan.MinZ, z, Eps);
    }
}
