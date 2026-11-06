#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The fade between two Days, over everything else. On <see cref="GameSession.DayEnded"/> it shows a full-screen
/// black panel with a centred "You slept." or "You passed out…", then fades it out over about a second. Purely
/// cosmetic: the Night has already run by the time the fade starts. Holds no rules.
/// </summary>
public partial class NightTransition : CanvasLayer
{
    /// <summary>How long the black screen holds before fading, in seconds.</summary>
    private const double HoldSeconds = 0.3;

    /// <summary>How long the fade out takes, in seconds.</summary>
    private const double FadeSeconds = 1.0;

    private GameSession? _session;

    private Control _panel = null!;

    private Label _message = null!;

    private Tween? _fade;

    public override void _Ready()
    {
        _panel = GetNode<Control>("Panel");
        _message = GetNode<Label>("Panel/Message");
        _panel.Visible = false;

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is not null)
        {
            _session.DayEnded += OnDayEnded;
        }
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.DayEnded -= OnDayEnded;
        }
    }

    /// <summary>The line shown for a day end reason.</summary>
    public static string MessageFor(DayEndReason reason) =>
        reason == DayEndReason.PassedOut ? "You passed out…" : "You slept.";

    private void OnDayEnded(DayEndReason reason)
    {
        _fade?.Kill();
        _message.Text = MessageFor(reason);
        _panel.Modulate = Colors.White;
        _panel.Visible = true;

        _fade = CreateTween();
        _fade.TweenInterval(HoldSeconds);
        _fade.TweenProperty(_panel, "modulate:a", 0.0, FadeSeconds);
        _fade.TweenCallback(Callable.From(() => _panel.Visible = false));
    }
}
