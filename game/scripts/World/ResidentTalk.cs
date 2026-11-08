#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Player;
using LongWayHome.Game.Ui;

namespace LongWayHome.Game.World;

/// <summary>
/// Talking to a Resident (#138): a child of the Resident's <see cref="InteractionPoint"/> in <c>Resident.tscn</c>. On
/// each <c>interact</c> press (not repeated while held) while the Player is in the point's range and no menu has paused
/// the tree, opens the area's <see cref="DialogueBox"/> with <see cref="ResidentLines.DisplayName"/> and
/// <see cref="ResidentLines.Greeting"/>, and marks the press handled so nothing else in range also acts on it. The
/// point only has an id once <see cref="ResidentView"/> has found the Resident present (BR-R1), so an absent Resident
/// can't be talked to. Holds no rules.
/// </summary>
public partial class ResidentTalk : Node
{
    private InteractionPoint _point = null!;

    public override void _Ready()
    {
        _point = GetParent<InteractionPoint>();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("interact", allowEcho: false)
            || !_point.IsPlayerInRange
            || GetTree().Paused
            || !ResidentCodes.TryParse(_point.PointId, out var id))
        {
            return;
        }

        // He is falling asleep or passing out: nothing interrupts that, as with the door and his tools.
        if (GetTree().GetFirstNodeInGroup(PlayerController.Group)?.GetNodeOrNull<FarmerAnimator>(nameof(FarmerAnimator))
            is { LocksMovement: true })
        {
            return;
        }

        if (GetTree().GetFirstNodeInGroup(DialogueBox.Group) is not DialogueBox box)
        {
            GD.PushError($"{GetPath()}: no DialogueBox in the scene to talk in.");
            return;
        }

        GetViewport().SetInputAsHandled();
        box.Show(ResidentLines.DisplayName(id), [ResidentLines.Greeting(id)]);
    }
}
