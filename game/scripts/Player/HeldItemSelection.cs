#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Players;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// Which item the Player holds: a position in <see cref="GameSession.HeldItems"/>. Starts on the hoe;
/// <c>tool_next</c> / <c>tool_prev</c> move forward / back and wrap around. When the held item is no longer in the
/// list, the hoe is held. A child of the Player. Holds no rules: the list comes from the session.
/// </summary>
public partial class HeldItemSelection : Node
{
    private static readonly HeldItem Hoe = HeldItem.Of(ToolId.Hoe);

    private GameSession? _session;

    private HeldItem _held = Hoe;

    /// <summary>Raised when the held item changes.</summary>
    public event Action? SelectionChanged;

    /// <summary>The held item; the hoe when the last selected item has left the list.</summary>
    public HeldItem Held => _session is not null && _session.HeldItems().Contains(_held) ? _held : Hoe;

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
            Cycle(1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("tool_prev", allowEcho: false))
        {
            Cycle(-1);
            GetViewport().SetInputAsHandled();
        }
    }

    private void Cycle(int step)
    {
        var items = _session!.HeldItems();
        var current = IndexOf(items, Held);
        _held = items[(current + step + items.Count) % items.Count];
        SelectionChanged?.Invoke();
    }

    private static int IndexOf(IReadOnlyList<HeldItem> items, HeldItem item)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] == item)
            {
                return i;
            }
        }

        return 0;
    }
}
