#nullable enable

using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Art;

/// <summary>
/// The art sandbox's camera (#75): orbits <see cref="Centre"/> on the same <c>camera_rotate_*</c> and
/// <c>camera_zoom_*</c> actions and the same <see cref="CameraView"/> maths as the farm's follow camera, so the art is
/// judged from the view the game ships with (REQ-52). It follows nothing, but the <c>move_*</c> actions (the left
/// stick, the D-pad, WASD and the arrow keys) slide its centre over the ground through <see cref="SandboxPan"/>, so the
/// view can travel left and right (and forward and back) along the rows.
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
        var move = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var (x, z) = SandboxPan.Moved((Centre.X, Centre.Z), move.X, move.Y, View, seconds);
        Centre = new Vector3(x, Centre.Y, z);
        PlaceAroundCentre();
    }

    private void PlaceAroundCentre()
    {
        var (x, y, z) = View.Offset;
        GlobalPosition = Centre + new Vector3(x, y, z);
        LookAt(Centre, Vector3.Up);
    }
}
