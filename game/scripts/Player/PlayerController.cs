#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Farm;

namespace LongWayHome.Game.Player;

/// <summary>
/// Moves the Player on the ground plane from the <c>move_*</c> and <c>run</c> Input Map actions, at the walk and
/// run speeds from the tuning data, and tracks which way the Player faces. Walking follows the camera's facing:
/// forward walks away from the <see cref="FollowCamera"/>, whichever way it has turned (REQ-52). On
/// <see cref="GameSession.NightCompleted"/> it returns to where the scene placed it, in front of the house door, at
/// rest. While the <see cref="FarmerAnimator"/> locks movement (falling asleep or passing out, #103) the Player
/// stands still and keeps the way he faces. It also tracks the farmer's <see cref="Feet"/> tile (#130) and picks up the
/// DroppedItems there through <see cref="GameSession.PickUp"/> whenever that tile changes, and again after
/// <see cref="GameSession.InventoryChanged"/> while DroppedItems lie there (room may have been freed); never while the
/// tree is paused. Holds no rules: what is picked up is decided by the session (BR-K4).
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
    private FarmerAnimator? _animator;

    /// <summary>The feet tile the last pick-up ran on, or null before the first.</summary>
    private GridPosition? _pickedUpAt;

    /// <summary>Set by <see cref="GameSession.InventoryChanged"/> while DroppedItems lie at the feet: try again.</summary>
    private bool _retryPickUp;

    /// <summary>
    /// The last non-zero world move direction snapped to north (0,-1), south (0,1), west (-1,0) or east (1,0), where
    /// X is east and Y is south. Starts facing south.
    /// </summary>
    public Vector2I Facing { get; private set; } = new(0, 1);

    /// <summary>
    /// The farmer's feet tile: the tile of the current Area's grid nearest the farmer (<see cref="FeetTile.Nearest"/>
    /// on <see cref="FarmGrid.CellAt(AreaId, Vector3)"/>; the forest path has its own 14 x 40 grid, #154), so dropping
    /// and picking up beside the grid use the same tile (#130).
    /// </summary>
    public GridPosition Feet
    {
        get
        {
            var area = _session?.CurrentArea ?? AreaId.Farm;
            var cell = FarmGrid.CellAt(area, GlobalPosition);
            return FeetTile.Nearest(area, cell.X, cell.Y);
        }
    }

    public override void _Ready()
    {
        AddToGroup(Group);
        _model = GetNode<Node3D>("Model");
        _camera = GetNode<FollowCamera>("FollowCamera");
        _animator = GetNodeOrNull<FarmerAnimator>(nameof(FarmerAnimator));
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
            _session.InventoryChanged += OnInventoryChanged;
        }
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NightCompleted -= ReturnToStart;
            _session.InventoryChanged -= OnInventoryChanged;
        }
    }

    /// <summary>Puts the Player back where the scene placed it, in front of the house door, with no velocity.</summary>
    public void ReturnToStart()
    {
        GlobalPosition = _startPosition;
        Velocity = Vector3.Zero;
    }

    /// <summary>
    /// Puts the Player at <paramref name="position"/> on the ground, at rest, facing <paramref name="forward"/>
    /// snapped to the four-way facing, with the camera already caught up: where an <c>AreaArrival</c> walks him in
    /// (#136). Where the scene placed him stays his start for <see cref="ReturnToStart"/>.
    /// </summary>
    public void PlaceAt(Vector3 position, Vector3 forward)
    {
        GlobalPosition = position with { Y = 0f };
        Velocity = Vector3.Zero;
        if (forward.X != 0f || forward.Z != 0f)
        {
            var facing = FacingDirection.Snap(forward.X, forward.Z, (Facing.X, Facing.Y));
            Facing = new Vector2I(facing.X, facing.Y);
            _model.Rotation = _model.Rotation with { Y = FacingDirection.Yaw(Facing.X, Facing.Y) };
        }

        _camera.SnapToTarget();
    }

    public override void _PhysicsProcess(double delta)
    {
        PickUpAtFeet();

        if (_animator?.LocksMovement == true)
        {
            // He is falling asleep or passing out: no move, and no turn away from where the clip faces him.
            Velocity = Vector3.Zero;
            return;
        }

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
        else
        {
            // Standing still, the farmer turns straight to face the targeted tile (issue #71).
            _model.Rotation = _model.Rotation with { Y = FacingDirection.Yaw(Facing.X, Facing.Y) };
        }
    }

    // Room may have been freed (shipping, planting, a pick-up that left some behind): retry on the next unpaused
    // physics frame rather than inside the event, so a pick-up never runs in the middle of another action.
    private void OnInventoryChanged()
    {
        if (_session!.HasDroppedItemsAt(Feet))
        {
            _retryPickUp = true;
        }
    }

    // BR-K4: picks up at the feet tile when it changes, or when a retry is due; never while the tree is paused.
    private void PickUpAtFeet()
    {
        if (_session is null || GetTree().Paused)
        {
            return;
        }

        var feet = Feet;
        if (feet == _pickedUpAt && !_retryPickUp)
        {
            return;
        }

        _pickedUpAt = feet;
        _retryPickUp = false;
        _session.PickUp(feet);
    }
}
