#nullable enable

using Godot;

namespace LongWayHome.Game.Player;

/// <summary>
/// Follows its parent (the Player) with smoothing. The offset and angle are the camera's position and rotation as
/// authored in the Player scene, and never change: the camera neither rotates nor zooms. Holds no rules.
/// </summary>
public partial class FollowCamera : Camera3D
{

    /// <summary>How quickly the camera catches up; 15 settles within about 0.3 s.</summary>
    [Export] public float Sharpness { get; set; } = 15f;

    private Node3D _target = null!;
    private Vector3 _offset;

    public override void _Ready()
    {
        _target = GetParent<Node3D>();
        _offset = Position;
        // Top-level so the camera keeps its own transform instead of inheriting the Player's every frame.
        TopLevel = true;
        GlobalPosition = _target.GlobalPosition + _offset;
    }

    // Physics rather than render frames: the Player moves on physics ticks, so following on the same tick keeps
    // the two in step at any refresh rate.
    public override void _PhysicsProcess(double delta)
    {
        var weight = 1f - Mathf.Exp(-Sharpness * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(_target.GlobalPosition + _offset, weight);
    }
}
