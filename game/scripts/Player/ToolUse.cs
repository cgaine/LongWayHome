#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// On each <c>use_tool</c> press (not repeated while held), asks the session to use the held item on the target
/// tile. A child of the Player, beside <see cref="ToolTargeting"/> and <see cref="HeldItemSelection"/>. Holds no
/// rules: what Use does, and whether it is allowed, is decided by <see cref="GameSession.Use"/>; a rejection
/// changes nothing on screen.
/// </summary>
public partial class ToolUse : Node
{
    private GameSession? _session;

    private ToolTargeting _targeting = null!;

    private HeldItemSelection _selection = null!;

    public override void _Ready()
    {
        _targeting = GetNode<ToolTargeting>("../ToolTargeting");
        _selection = GetNode<HeldItemSelection>("../HeldItemSelection");

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcessUnhandledInput(false);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("use_tool", allowEcho: false))
        {
            _session!.Use(_targeting.Target, _selection.Held);
            GetViewport().SetInputAsHandled();
        }
    }
}
