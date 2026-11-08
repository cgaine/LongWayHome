#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The dialogue box (#138): a bottom-of-screen panel with the speaker's name, one line of text, and a hint line
/// "&lt;interact glyph&gt; Next" ("Close" on the last line) and "&lt;open_menu glyph&gt; Skip" from
/// <see cref="DialogueText"/>, its glyphs following <see cref="LastInputDevice"/>. <see cref="Show"/> pauses the scene
/// tree, so the day clock and the Player stop (BR-G2); <c>interact</c> or <c>use_tool</c> advances to the next line or
/// closes after the last, <c>open_menu</c> closes at once, skipping the rest; closing resumes the tree and emits
/// <see cref="SignalName.Finished"/> once. While shown, those presses and <c>pause</c>/<c>ui_cancel</c> are handled in
/// <c>_Input</c>, before any gameplay or menu sees them, so the press that closes the box can't also refill the can,
/// sleep, use a tool or toggle the pause menu. Presses while it is not shown are ignored. Always processes. Glue over
/// <see cref="DialogueFlow"/>; holds no rules. The Opening (#121) reuses it, so keep <see cref="Show"/> and
/// <see cref="SignalName.Finished"/> as they are.
/// </summary>
public partial class DialogueBox : CanvasLayer
{
    /// <summary>The group the box joins, so a speaker (e.g. <c>ResidentTalk</c>) can find it in the area.</summary>
    public const string Group = "dialogue_box";

    /// <summary>Emitted once when the box closes, by advancing past the last line or by skipping.</summary>
    [Signal] public delegate void FinishedEventHandler();

    private readonly DialogueFlow _flow = new();

    private LastInputDevice? _device;

    private Control _panel = null!;

    private Label _speakerLabel = null!;

    private Label _lineLabel = null!;

    private Label _advanceHint = null!;

    private Label _skipHint = null!;

    /// <summary>Whether a conversation is showing.</summary>
    public bool IsOpen => _flow.IsOpen;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        AddToGroup(Group);
        _panel = GetNode<Control>("Panel");
        _speakerLabel = GetNode<Label>("Panel/Rows/SpeakerLabel");
        _lineLabel = GetNode<Label>("Panel/Rows/LineLabel");
        _advanceHint = GetNode<Label>("Panel/Rows/Hints/AdvanceHint");
        _skipHint = GetNode<Label>("Panel/Rows/Hints/SkipHint");
        _panel.Visible = false;
        _flow.Finished += OnFlowFinished;

        _device = GetNodeOrNull<LastInputDevice>(LastInputDevice.Path);
        if (_device is not null)
        {
            _device.DeviceChanged += Refresh;
        }
    }

    public override void _ExitTree()
    {
        _flow.Finished -= OnFlowFinished;
        if (_device is not null && IsInstanceValid(_device))
        {
            _device.DeviceChanged -= Refresh;
        }

        // Leaving the scene must not leave the tree paused with nothing to resume it.
        if (_flow.IsOpen)
        {
            GetTree().Paused = false;
        }
    }

    /// <summary>
    /// Opens the box on the first of <paramref name="lines"/>, spoken by <paramref name="speaker"/>, and pauses the
    /// tree (BR-G2). Showing while already open replaces the conversation without emitting
    /// <see cref="SignalName.Finished"/> for the replaced one.
    /// </summary>
    public void Show(string speaker, IReadOnlyList<string> lines)
    {
        _flow.Start(speaker, lines);
        GetTree().Paused = true;
        _panel.Visible = true;
        Refresh();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_flow.IsOpen)
        {
            return;
        }

        if (@event.IsActionPressed("interact", allowEcho: false) || @event.IsActionPressed("use_tool", allowEcho: false))
        {
            GetViewport().SetInputAsHandled();
            _flow.Advance();
            Refresh();
            return;
        }

        if (@event.IsActionPressed("open_menu", allowEcho: false))
        {
            GetViewport().SetInputAsHandled();
            _flow.Skip();
            return;
        }

        // Esc is pause and ui_cancel: kept from the pause menu, whose toggle would resume the tree under the box.
        if (@event.IsActionPressed("pause") || @event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnFlowFinished()
    {
        _panel.Visible = false;
        GetTree().Paused = false;
        EmitSignal(SignalName.Finished);
    }

    private void Refresh()
    {
        if (!_flow.IsOpen)
        {
            return;
        }

        var device = LastInputDevice.DeviceOf(_device);
        _speakerLabel.Text = _flow.Speaker;
        _lineLabel.Text = _flow.CurrentLine;
        _advanceHint.Text = DialogueText.AdvanceHint(device, _flow.IsLastLine);
        _skipHint.Text = DialogueText.SkipHint(device);
    }
}
