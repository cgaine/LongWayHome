#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.World;

/// <summary>
/// The shipping bin's action: a child of the bin's <see cref="InteractionPoint"/>. On each <c>interact</c> press (not
/// repeated while held) while the Player is in the point's range, asks the session to ship every Crop carried.
/// Holds no rules: what ships and when it is paid are decided by <see cref="GameSession.ShipAll"/> (BR-S1, BR-S2).
/// On success (<c>IsOk</c>) the farmer plays his <c>ship</c> clip (#103), after the action has happened.
/// </summary>
public partial class BinShip : Node
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
        if (@event.IsActionPressed("interact", allowEcho: false) && _point.IsPlayerInRange)
        {
            var result = _session!.ShipAll();
            GetViewport().SetInputAsHandled();
            if (result.IsOk && FindAnimator() is { } animator)
            {
                animator.PlayOneShot(FarmerClip.Ship);
            }
        }
    }

    /// <summary>The Player's <see cref="FarmerAnimator"/>, found through the Player group.</summary>
    private FarmerAnimator? FindAnimator() =>
        GetTree().GetFirstNodeInGroup(PlayerController.Group)?.GetNodeOrNull<FarmerAnimator>(nameof(FarmerAnimator));
}
