#nullable enable

using System;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// Which hotbar position the Player holds: 0–5 within the current Backpack row, starting at 0 (not saved).
/// <c>tool_next</c> / <c>tool_prev</c> step it by <see cref="HotbarPosition.Step"/>, wrapping within the row and
/// landing on empty slots too; <c>hotbar_cycle</c> moves the hotbar to the next row through
/// <see cref="GameSession.CycleHotbar"/> and keeps the position. A child of the Player. Holds no rules: what the
/// position holds comes from <see cref="GameSession.HeldItemAt"/>.
/// </summary>
public partial class HeldItemSelection : Node
{
    private GameSession? _session;

    /// <summary>Raised when the held position changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>The held hotbar position, 0–5.</summary>
    public int Position { get; private set; }

    /// <summary>What the held position holds, or null when it is empty, holds a crop, or there is no session.</summary>
    public HeldItem? Held => _session?.HeldItemAt(Position);

    public override void _Ready()
    {
        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcessUnhandledInput(false);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("tool_next", allowEcho: false))
        {
            Step(1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("tool_prev", allowEcho: false))
        {
            Step(-1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("hotbar_cycle", allowEcho: false))
        {
            // Raises InventoryChanged, which redraws the hotbar and the item in hand; the position stays.
            _session!.CycleHotbar();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Step(int delta)
    {
        Position = HotbarPosition.Step(Position, delta);
        SelectionChanged?.Invoke();
    }
}
