#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.World;

/// <summary>
/// A water point's action: a child of the pond's, a river's or a lake's <see cref="InteractionPoint"/>. On each
/// <c>interact</c> press (not repeated while held) while the Player is in the point's range, asks the session to refill
/// the Watering can at <see cref="Source"/> and marks the press handled, so two points in range refill once.
/// Holds no rules: the refill amount and its cost are decided by <see cref="GameSession.RefillCan"/>.
/// On success (<c>IsOk</c>) the farmer plays his <c>refill</c> clip (#103), after the action has happened.
/// </summary>
public partial class WaterRefill : Node
{
    /// <summary>The <see cref="WaterSource"/> code this point refills from: <c>pond</c>, <c>river</c> or <c>lake</c>
    /// (<see cref="WaterSourceCodes"/>). An unknown code is reported once and the point does nothing.</summary>
    [Export] public string Source { get; set; } = "pond";

    private GameSession? _session;

    private InteractionPoint _point = null!;

    private WaterSource _source;

    public override void _Ready()
    {
        _point = GetParent<InteractionPoint>();

        if (!WaterSourceCodes.TryParse(Source, out _source))
        {
            GD.PushError($"{GetPath()}: Source \"{Source}\" is not a water source (pond, river, lake).");
            SetProcessUnhandledInput(false);
            return;
        }

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
            var result = _session!.RefillCan(_source);
            GetViewport().SetInputAsHandled();
            if (result.IsOk && FindAnimator() is { } animator)
            {
                animator.PlayOneShot(FarmerClip.Refill);
            }
        }
    }

    /// <summary>The Player's <see cref="FarmerAnimator"/>, found through the Player group.</summary>
    private FarmerAnimator? FindAnimator() =>
        GetTree().GetFirstNodeInGroup(PlayerController.Group)?.GetNodeOrNull<FarmerAnimator>(nameof(FarmerAnimator));
}
