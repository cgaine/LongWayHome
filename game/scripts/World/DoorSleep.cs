#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Opening;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.World;

/// <summary>
/// The house door's action: a child of the door's <see cref="InteractionPoint"/>. On each <c>interact</c> press (not
/// repeated while held) while the Player is in the point's range, asks the session to end the Day by sleeping.
/// Holds no rules: whether it is too late to sleep, and the Night itself, are decided by
/// <see cref="GameSession.Sleep"/>.
/// <para>
/// #103: when the <c>sleep</c> clip would end before 02:00 even if the true clock is a minute ahead of the one shown
/// (<see cref="FarmerAnimationState.SleepPlaysClip"/>) and the farmer isn't already falling asleep or passing out, it
/// turns him to face the door (north, −Z), plays the clip and sleeps when it finishes. Otherwise it sleeps at once
/// with no clip, as before, so a press before 02:00 is never turned into a pass out (BR-G3). The clip pauses with the
/// tree, and so does the clock, so the Day still ends by Sleep when it finishes.
/// </para>
/// <para>
/// #139: while the farm's <see cref="OpeningSequence"/> is playing the Opening (BR-I1) a press does nothing (and the
/// Hud hides the Sleep hint).
/// </para>
/// </summary>
public partial class DoorSleep : Node
{
    private GameSession? _session;

    private InteractionPoint _point = null!;

    public override void _Ready()
    {
        _point = GetParent<InteractionPoint>();

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcessUnhandledInput(false);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("interact", allowEcho: false) && _point.IsPlayerInRange
            && !OpeningSequence.IsActiveIn(this))
        {
            GetViewport().SetInputAsHandled();
            var player = GetTree().GetFirstNodeInGroup(PlayerController.Group) as Node3D;
            var animator = player?.GetNodeOrNull<FarmerAnimator>(nameof(FarmerAnimator));
            if (animator is null || animator.LocksMovement ||
                !FarmerAnimationState.SleepPlaysClip(_session!.Game.ClockMinutes, _session.Data.Tuning))
            {
                _session!.Sleep();
                return;
            }

            if (player!.GetNodeOrNull<Node3D>("Model") is { } model)
            {
                // The door is north of where he stands (−Z): he faces it to fall asleep.
                model.Rotation = model.Rotation with { Y = Mathf.Pi };
            }

            animator.PlayOneShot(FarmerClip.Sleep, () => _session!.Sleep());
        }
    }
}
