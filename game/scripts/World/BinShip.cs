#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.World;

/// <summary>
/// The shipping bin's action: a child of the bin's <see cref="InteractionPoint"/>. On each <c>interact</c> press (not
/// repeated while held) while the Player is in the point's range, asks the session to ship every Crop carried.
/// Holds no rules: what ships and when it is paid are decided by <see cref="GameSession.ShipAll"/> (BR-S1, BR-S2).
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
            _session!.ShipAll();
            GetViewport().SetInputAsHandled();
        }
    }
}
