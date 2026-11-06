#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.World;

/// <summary>
/// The house door's action: a child of the door's <see cref="InteractionPoint"/>. On each <c>interact</c> press (not
/// repeated while held) while the Player is in the point's range, asks the session to end the Day by sleeping.
/// Holds no rules: whether it is too late to sleep, and the Night itself, are decided by
/// <see cref="GameSession.Sleep"/>.
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
        if (@event.IsActionPressed("interact", allowEcho: false) && _point.IsPlayerInRange)
        {
            _session!.Sleep();
            GetViewport().SetInputAsHandled();
        }
    }
}
