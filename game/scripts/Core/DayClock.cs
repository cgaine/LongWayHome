#nullable enable

using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Core;

/// <summary>
/// Runs the day clock: each frame of play passes the frame's real seconds to
/// <see cref="GameSession.AdvanceClock"/>. Pausable, so the clock stops while the scene tree is paused by a menu
/// (BR-G2); it is the only caller of AdvanceClock. Holds no rules: the rate and the day end are the session's.
/// </summary>
public partial class DayClock : Node
{
    private GameSession? _session;

    public override void _Ready()
    {
        // BR-G2: the clock never advances while a menu has paused the tree.
        ProcessMode = ProcessModeEnum.Pausable;

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcess(false);
        }
    }

    public override void _Process(double delta) => _session!.AdvanceClock(delta);
}
