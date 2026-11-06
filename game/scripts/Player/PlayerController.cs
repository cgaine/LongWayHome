#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// Moves the Player on the ground plane from the <c>move_*</c> and <c>run</c> Input Map actions, at the walk and
/// run speeds from the tuning data, and tracks which way the Player faces. Walking follows the camera's facing:
/// forward walks away from the <see cref="FollowCamera"/>, whichever way it has turned (REQ-52). On
/// <see cref="GameSession.NightCompleted"/> it returns to where the scene placed it, in front of the house door, at
/// rest. Holds no rules.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    /// <summary>The group the Player is in, so other nodes can find it without a node path.</summary>
    public const string Group = "player";

    private Node3D _model = null!;
    private FollowCamera _camera = null!;
    private float _walkSpeed;
    private float _runSpeed;
    private GameSession? _session;
    private Vector3 _startPosition;

    /// <summary>
    /// The last non-zero world move direction snapped to north (0,-1), south (0,1), west (-1,0) or east (1,0), where
    /// X is east and Y is south. Starts facing south.
    /// </summary>
    public Vector2I Facing { get; private set; } = new(0, 1);

    public override void _Ready()
    {
        AddToGroup(Group);
        _model = GetNode<Node3D>("Model");
        _camera = GetNode<FollowCamera>("FollowCamera");
        _startPosition = GlobalPosition;

        var root = GetNode<GameRoot>("/root/GameRoot");
        var data = root.Data;
        if (data is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetPhysicsProcess(false);
            return;
        }

        var tuning = data.Tuning;
        _walkSpeed = (float)tuning.WalkSpeedTilesPerSecond;
        _runSpeed = (float)tuning.RunSpeedTilesPerSecond;

        _session = root.Session;
        if (_session is not null)
        {
            _session.NightCompleted += ReturnToStart;
        }
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NightCompleted -= ReturnToStart;
        }
    }

    /// <summary>Puts the Player back where the scene placed it, in front of the house door, with no velocity.</summary>
    public void ReturnToStart()
    {
        GlobalPosition = _startPosition;
        Velocity = Vector3.Zero;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Capped at length 1 with the actions' deadzone applied: analog tilt scales speed, and a diagonal is no
        // faster than a straight line. Opposite directions cancel out.
        var move = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var speed = Input.IsActionPressed("run") ? _runSpeed : _walkSpeed;

        // Forward walks away from the camera. Godot processes this parent before its child camera, so this reads the
        // yaw from the previous physics frame: one frame behind, which is not noticeable.
        var world = _camera.View.ToWorldMove(move.X, move.Y);

        Velocity = new Vector3(world.X, 0f, world.Z) * speed;
        MoveAndSlide();
        Position = Position with { Y = 0f };

        // Only a move changes the facing, so turning the camera while standing still leaves it (and the targeted
        // tile) alone.
        if (move != Vector2.Zero)
        {
            var facing = FacingDirection.Snap(world.X, world.Z, (Facing.X, Facing.Y));
            Facing = new Vector2I(facing.X, facing.Y);
            _model.Rotation = _model.Rotation with { Y = Mathf.Atan2(world.X, world.Z) };
        }
    }
}
