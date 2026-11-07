#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// On each <c>use_tool</c> press (not repeated while held), asks the session to use the held hotbar position on the
/// target tile. A child of the Player, beside <see cref="ToolTargeting"/> and <see cref="HeldItemSelection"/>. Holds no
/// rules: what Use does, and whether it is allowed, is decided by <see cref="GameSession.Use"/>; a rejection
/// changes nothing on screen. The action happens on the press; when its result <c>IsOk</c>, the farmer then plays the
/// action's clip (<see cref="FarmerAnimationState.ForUse"/> of the previewed action, #103). While the
/// <see cref="FarmerAnimator"/> locks movement (the sleep or the pass out) the press is ignored.
/// </summary>
public partial class ToolUse : Node
{
    private GameSession? _session;

    private ToolTargeting _targeting = null!;

    private HeldItemSelection _selection = null!;

    private FarmerAnimator? _animator;

    private PlayerController _player = null!;

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
        _targeting = GetNode<ToolTargeting>("../ToolTargeting");
        _selection = GetNode<HeldItemSelection>("../HeldItemSelection");
        _animator = GetNodeOrNull<FarmerAnimator>("../FarmerAnimator");

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcessUnhandledInput(false);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("use_tool", allowEcho: false))
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        if (_animator?.LocksMovement == true)
        {
            // He is falling asleep or passing out.
            return;
        }

        var target = _targeting.Target;
        var held = _selection.Position;
        // What Use will do, read first: after a successful Use the preview has moved on (a tilled tile previews Plant).
        var action = _session!.PreviewUse(target, held).Action;
        // A harvested Crop that fits nowhere drops at the farmer's feet tile (BR-K3).
        var result = _session.Use(target, held, _player.Feet);
        if (result.IsOk && FarmerAnimationState.ForUse(action) is { } clip)
        {
            _animator?.PlayOneShot(clip);
        }
    }
}
