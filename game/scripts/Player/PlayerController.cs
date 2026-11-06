#nullable enable

using System;
using Godot;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// Moves the Player on the ground plane from the <c>move_*</c> and <c>run</c> Input Map actions, at the walk and
/// run speeds from the tuning data, and tracks which way the Player faces. Holds no rules.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    /// <summary>The group the Player is in, so other nodes can find it without a node path.</summary>
    public const string Group = "player";

    private Node3D _model = null!;
    private float _walkSpeed;
    private float _runSpeed;

    /// <summary>
    /// The last non-zero move direction snapped to north (0,-1), south (0,1), west (-1,0) or east (1,0), where
    /// X is east and Y is south. Starts facing south.
    /// </summary>
    public Vector2I Facing { get; private set; } = new(0, 1);

    public override void _Ready()
    {
        AddToGroup(Group);
        _model = GetNode<Node3D>("Model");

        var data = GetNode<GameRoot>("/root/GameRoot").Data;
        if (data is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetPhysicsProcess(false);
            return;
        }

        var tuning = data.Tuning;
        _walkSpeed = (float)tuning.WalkSpeedTilesPerSecond;
        _runSpeed = (float)tuning.RunSpeedTilesPerSecond;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Capped at length 1 with the actions' deadzone applied: analog tilt scales speed, and a diagonal is no
        // faster than a straight line. Opposite directions cancel out.
        var move = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var speed = Input.IsActionPressed("run") ? _runSpeed : _walkSpeed;

        Velocity = new Vector3(move.X, 0f, move.Y) * speed;
        MoveAndSlide();
        Position = Position with { Y = 0f };

        if (move != Vector2.Zero)
        {
            Facing = SnapToCardinal(move, Facing);
            _model.Rotation = _model.Rotation with { Y = Mathf.Atan2(move.X, move.Y) };
        }
    }

    /// <summary>
    /// Snaps a move direction to the nearest cardinal direction. On an exact diagonal, keeps the current facing
    /// when it is one of the two directions pressed, so walking diagonally doesn't flip it.
    /// </summary>
    private static Vector2I SnapToCardinal(Vector2 move, Vector2I current)
    {
        var horizontal = new Vector2I(Math.Sign(move.X), 0);
        var vertical = new Vector2I(0, Math.Sign(move.Y));
        var x = Mathf.Abs(move.X);
        var y = Mathf.Abs(move.Y);

        if (Mathf.IsEqualApprox(x, y))
        {
            return current == horizontal ? horizontal : vertical;
        }

        return x > y ? horizontal : vertical;
    }
}
