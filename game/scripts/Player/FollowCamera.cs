#nullable enable

using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Player;

/// <summary>
/// Follows its parent (the Player) with smoothing, and turns a full circle around it and zooms in and out on the
/// <c>camera_rotate_*</c> and <c>camera_zoom_*</c> actions (REQ-52). The turn and zoom maths is
/// <see cref="CameraView"/>; this node only reads the actions, follows the farmer and always looks at it. The view
/// starts at <see cref="CameraView.Default"/> whenever an area scene loads and is not saved. The node is pausable,
/// so it does not respond while a menu or the pause panel has paused the tree. Holds no rules.
/// </summary>
public partial class FollowCamera : Camera3D
{
    /// <summary>How quickly the camera catches up; 15 settles within about 0.3 s.</summary>
    [Export] public float Sharpness { get; set; } = 15f;

    /// <summary>The current turn and zoom, read by the Player for camera-relative walking.</summary>
    public CameraView View { get; private set; } = CameraView.Default;

    private Node3D _target = null!;
    private Vector3 _followPoint;

    public override void _Ready()
    {
        _target = GetParent<Node3D>();
        // Top-level so the camera keeps its own transform instead of inheriting the Player's every frame.
        TopLevel = true;
        _followPoint = _target.GlobalPosition;
        PlaceAroundFollowPoint();
    }

    // Physics rather than render frames: the Player moves on physics ticks, so following on the same tick keeps
    // the two in step at any refresh rate.
    public override void _PhysicsProcess(double delta)
    {
        var seconds = (float)delta;
        View = View
            .Turned(Input.GetAxis("camera_rotate_left", "camera_rotate_right"), seconds)
            .Zoomed(Input.GetAxis("camera_zoom_in", "camera_zoom_out"), seconds);

        // Only the follow point is smoothed; turn and zoom apply directly so the camera stays on its circle.
        var weight = 1f - Mathf.Exp(-Sharpness * seconds);
        _followPoint = _followPoint.Lerp(_target.GlobalPosition, weight);
        PlaceAroundFollowPoint();
    }

    /// <summary>Catches up with the Player at once, with no smoothing: after he is placed at an area's arrival (#136).</summary>
    public void SnapToTarget()
    {
        _followPoint = _target.GlobalPosition;
        PlaceAroundFollowPoint();
    }

    private void PlaceAroundFollowPoint()
    {
        var (x, y, z) = View.Offset;
        GlobalPosition = _followPoint + new Vector3(x, y, z);
        LookAt(_followPoint, Vector3.Up);
    }
}
