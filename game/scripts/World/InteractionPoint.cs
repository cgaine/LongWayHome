#nullable enable

using Godot;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.World;

/// <summary>
/// A place the Player will interact with (the door, the shipping bin, the shop, the pond). Tracks whether the
/// Player is within <see cref="Range"/> tiles, measured horizontally, and signals when that changes. It performs
/// no action itself; later features listen to it. Holds no rules.
/// </summary>
public partial class InteractionPoint : Node3D
{
    [Signal] public delegate void PlayerEnteredEventHandler();

    [Signal] public delegate void PlayerExitedEventHandler();

    /// <summary>Which point this is: <c>door</c>, <c>bin</c>, <c>shop</c> or <c>pond</c>.</summary>
    [Export] public string PointId { get; set; } = "";

    /// <summary>How close, in tiles, the Player must be. Height is ignored.</summary>
    [Export] public float Range { get; set; } = 1f;

    public bool IsPlayerInRange { get; private set; }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().GetFirstNodeInGroup(PlayerController.Group) is not Node3D player)
        {
            return;
        }

        var offset = player.GlobalPosition - GlobalPosition;
        var inRange = new Vector2(offset.X, offset.Z).Length() <= Range;
        if (inRange == IsPlayerInRange)
        {
            return;
        }

        IsPlayerInRange = inRange;
        EmitSignal(inRange ? SignalName.PlayerEntered : SignalName.PlayerExited);
    }
}
