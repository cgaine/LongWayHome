#nullable enable

using System;
using System.Linq;
using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Ui;

/// <summary>
/// Gives an open menu its focus back, so a controller or keyboard player is never stranded after a mouse click clears
/// focus (REQ-13): when a press (not an echo) of an action arrives and <see cref="MenuFocus.ShouldRecover"/> accepts it
/// given whether anything in the viewport has focus, the menu focuses its first button and the press is consumed.
/// Holds no rules: which presses recover is decided by <see cref="MenuFocus"/>.
/// </summary>
public static class MenuFocusRecovery
{
    /// <summary>
    /// Calls <paramref name="focusFirst"/> and marks <paramref name="event"/> handled on <paramref name="viewport"/>
    /// when <see cref="MenuFocus.ShouldRecover"/> says so; true when it did.
    /// </summary>
    public static bool TryRecover(Viewport viewport, InputEvent @event, Action focusFirst)
    {
        // Checked first so the common case (a button has focus) costs no action lookups on every input event.
        var somethingFocused = viewport.GuiGetFocusOwner() is not null;
        if (somethingFocused)
        {
            return false;
        }

        var recovers = MenuFocus.RecoveryActions.Any(action =>
            @event.IsActionPressed(action, allowEcho: false) && MenuFocus.ShouldRecover(somethingFocused, action));
        if (!recovers)
        {
            return false;
        }

        focusFirst();
        viewport.SetInputAsHandled();
        return true;
    }
}
