#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The fade between two Days, over everything else. On <see cref="GameSession.DayEnded"/> it shows a full-screen
/// black panel with a centred "You slept." or "You passed out…", then fades it out over about a second. When that
/// Night's save failed (<see cref="GameSession.SaveFailed"/>) a second line, "The game could not be saved.", shows
/// under the message. Purely
/// cosmetic: the Night has already run by the time the fade starts. Always processes, so pausing mid-fade cannot
/// leave the black screen stuck over the pause menu. While showing it swallows <c>interact</c> and <c>use_tool</c>
/// presses, so a press while waking in the door's range cannot sleep again (or refill, or use a tool). It is the last
/// child of the Farm scene, so its <c>_UnhandledInput</c> runs before those actions'. Holds no rules.
/// </summary>
public partial class NightTransition : CanvasLayer
{
    /// <summary>The second line, shown under the message when the Night's save failed.</summary>
    public const string SaveFailedText = "The game could not be saved.";

    /// <summary>How long the black screen holds before fading, in seconds.</summary>
    private const double HoldSeconds = 0.3;

    /// <summary>How long the fade out takes, in seconds.</summary>
    private const double FadeSeconds = 1.0;

    private GameSession? _session;

    private Control _panel = null!;

    private Label _message = null!;

    private Label _saveFailed = null!;

    private Tween? _fade;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNode<Control>("Panel");
        _message = GetNode<Label>("Panel/Message");
        _saveFailed = GetNode<Label>("Panel/SaveFailed");
        _saveFailed.Text = SaveFailedText;
        _saveFailed.Visible = false;
        _panel.Visible = false;

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is not null)
        {
            _session.DayEnded += OnDayEnded;
            _session.SaveFailed += OnSaveFailed;
        }
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.DayEnded -= OnDayEnded;
            _session.SaveFailed -= OnSaveFailed;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_panel.Visible && (@event.IsActionPressed("interact") || @event.IsActionPressed("use_tool")))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>The line shown for a day end reason.</summary>
    public static string MessageFor(DayEndReason reason) =>
        reason == DayEndReason.PassedOut ? "You passed out…" : "You slept.";

    private void OnDayEnded(DayEndReason reason)
    {
        _fade?.Kill();
        _message.Text = MessageFor(reason);

        // DayEnded comes before the Night, so its SaveFailed (if any) arrives after this and shows the line again.
        _saveFailed.Visible = false;
        _panel.Modulate = Colors.White;
        _panel.Visible = true;

        _fade = CreateTween();
        _fade.TweenInterval(HoldSeconds);
        _fade.TweenProperty(_panel, "modulate:a", 0.0, FadeSeconds);
        _fade.TweenCallback(Callable.From(() => _panel.Visible = false));
    }

    /// <summary>The Night this fade follows could not be saved (BR-G5): show the second line.</summary>
    private void OnSaveFailed(string reason) => _saveFailed.Visible = true;
}
