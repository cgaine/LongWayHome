#nullable enable

using Godot;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The pause overlay. On each <c>pause</c> press (not repeated while held) it pauses or resumes the scene tree and
/// shows or hides a centred "Paused" panel with a "&lt;pause glyph&gt; Resume" line; the glyph follows
/// <see cref="LastInputDevice"/>. Always processes, so it hears the press that resumes. Pausing the tree stops the
/// day clock, the Player and everything else that is Pausable (BR-G2). Holds no rules.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    private LastInputDevice? _device;

    private Control _panel = null!;

    private Label _resumeLabel = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNode<Control>("Panel");
        _resumeLabel = GetNode<Label>("Panel/Rows/ResumeLabel");
        _panel.Visible = false;

        _device = GetNodeOrNull<LastInputDevice>(LastInputDevice.Path);
        if (_device is not null)
        {
            _device.DeviceChanged += RefreshResumeLabel;
        }

        RefreshResumeLabel();
    }

    public override void _ExitTree()
    {
        if (_device is not null && IsInstanceValid(_device))
        {
            _device.DeviceChanged -= RefreshResumeLabel;
        }

        // Leaving the scene must not leave the tree paused with nothing to resume it.
        if (_panel.Visible)
        {
            GetTree().Paused = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("pause", allowEcho: false))
        {
            return;
        }

        var paused = !GetTree().Paused;
        GetTree().Paused = paused;
        RefreshResumeLabel();
        _panel.Visible = paused;
        GetViewport().SetInputAsHandled();
    }

    private void RefreshResumeLabel() =>
        _resumeLabel.Text = $"{LastInputDevice.GlyphFor("pause", _device)} Resume";
}
