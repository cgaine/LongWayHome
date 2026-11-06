#nullable enable

using Godot;

namespace LongWayHome.Game.Player;

/// <summary>
/// Follows its parent (the Player) at a fixed offset with smoothing. The camera's angle is set once in the scene
/// and never changes: it neither rotates nor zooms. Holds no rules.
/// </summary>
public partial class FollowCamera : Camera3D
{
    /// <summary>Where the camera sits relative to the Player.</summary>
    [Export] public Vector3 Offset { get; set; } = new(0f, 9f, 9f);

    /// <summary>How quickly the camera catches up; 15 settles within about 0.3 s.</summary>
    [Export] public float Sharpness { get; set; } = 15f;

    private Node3D _target = null!;

    public override void _Ready()
    {
        _target = GetParent<Node3D>();
        // Top-level so the camera keeps its own transform instead of inheriting the Player's every frame.
        TopLevel = true;
        GlobalPosition = _target.GlobalPosition + Offset;
    }

    public override void _Process(double delta)
    {
        var weight = 1f - Mathf.Exp(-Sharpness * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(_target.GlobalPosition + Offset, weight);
    }
}
