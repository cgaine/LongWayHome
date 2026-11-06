#nullable enable

using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Art;

/// <summary>
/// The art sandbox's camera (#75): orbits <see cref="Centre"/> on the same <c>camera_rotate_*</c> and
/// <c>camera_zoom_*</c> actions and the same <see cref="CameraView"/> maths as the farm's follow camera, so the art is
/// judged from the view the game ships with (REQ-52). It follows nothing: the centre stays put.
/// </summary>
public partial class SandboxCamera : Camera3D
{
    /// <summary>The point the camera orbits and looks at.</summary>
    [Export] public Vector3 Centre { get; set; } = Vector3.Zero;

    /// <summary>The current turn and zoom; public so a probe can read it.</summary>
    public CameraView View { get; private set; } = CameraView.Default;

    public override void _Ready() => PlaceAroundCentre();

    public override void _PhysicsProcess(double delta)
    {
        var seconds = (float)delta;
        View = View
            .Turned(Input.GetAxis("camera_rotate_left", "camera_rotate_right"), seconds)
            .Zoomed(Input.GetAxis("camera_zoom_in", "camera_zoom_out"), seconds);
        PlaceAroundCentre();
    }

    private void PlaceAroundCentre()
    {
        var (x, y, z) = View.Offset;
        GlobalPosition = Centre + new Vector3(x, y, z);
        LookAt(Centre, Vector3.Up);
    }
}
