#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Core;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The Rebuild menu (#163), built like <see cref="ShopMenu"/>. On an <c>interact</c> press (not repeated while held)
/// while the Player is in range of a <c>"sign"</c> <see cref="InteractionPoint"/> whose <see cref="RebuildSignPoint"/>
/// is up (<see cref="GameSession.RebuildSign"/> is not null, BR-B4) and the tree is not already paused, it pauses the
/// tree (which stops the day clock, BR-G2) and shows a centred panel: the <see cref="RebuildText.Title"/> ("Rebuild the
/// general store — stage 2"), the stage's <see cref="RebuildSignView.Unlocks"/> line, then one row per cost line, its
/// <see cref="RebuildText.Row"/> ("Wood 12 / 50 (you have 8)") beside a <see cref="RebuildText.DeliverButton"/> button
/// (<see cref="RebuildText.DeliverGoldButton"/> on the gold row), disabled once the line is complete; a Close button
/// labelled <see cref="InputGlyphs.CloseLabel"/>, and a message line. Pressing Deliver asks the session to
/// <see cref="GameSession.Deliver"/> (every one held, up to the need) or <see cref="GameSession.DeliverGold"/> (100
/// gold at most); the rows are redrawn and the message shows <see cref="RebuildText.AfterDeliver"/> or
/// <see cref="RebuildText.Rejection"/>. Close, or a press of <c>ui_cancel</c>, <c>interact</c> or <c>pause</c>, closes
/// the panel and resumes the tree; those presses are handled in <c>_Input</c> while open, so the
/// <see cref="PauseMenu"/> never also toggles. Focus recovery follows <see cref="MenuFocusRecovery"/>. Always processes.
/// Holds no rules: what is needed, what a delivery takes and whether it happens are decided by the session (BR-B2).
/// </summary>
public partial class RebuildMenu : CanvasLayer
{
    private GameSession? _session;

    private LastInputDevice? _device;

    private List<InteractionPoint> _signs = [];

    private BuildingId _building;

    private Control _panel = null!;

    private Label _titleLabel = null!;

    private Label _unlocksLabel = null!;

    private VBoxContainer _items = null!;

    private Button _closeButton = null!;

    private Label _messageLabel = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNode<Control>("Panel");
        _titleLabel = GetNode<Label>("Panel/Rows/TitleLabel");
        _unlocksLabel = GetNode<Label>("Panel/Rows/UnlocksLabel");
        _items = GetNode<VBoxContainer>("Panel/Rows/Items");
        _closeButton = GetNode<Button>("Panel/Rows/CloseButton");
        _messageLabel = GetNode<Label>("Panel/Rows/MessageLabel");
        _panel.Visible = false;
        _closeButton.Pressed += Close;

        _device = GetNodeOrNull<LastInputDevice>(LastInputDevice.Path);
        if (_device is not null)
        {
            _device.DeviceChanged += RefreshCloseLabel;
        }

        RefreshCloseLabel();

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        _signs = InteractionPoint.FindAll(GetTree().Root, "sign");
        if (_session is null || _signs.Count == 0)
        {
            // GameRoot has already reported the data errors and is quitting, or this scene has no Rebuild sign.
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
        }
    }

    public override void _ExitTree()
    {
        if (_device is not null && IsInstanceValid(_device))
        {
            _device.DeviceChanged -= RefreshCloseLabel;
        }

        // Leaving the scene must not leave the tree paused with nothing to resume it.
        if (_panel.Visible)
        {
            GetTree().Paused = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_panel.Visible)
        {
            return;
        }

        if (@event.IsActionPressed("ui_cancel", allowEcho: false)
            || @event.IsActionPressed("interact", allowEcho: false)
            || @event.IsActionPressed("pause", allowEcho: false))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        MenuFocusRecovery.TryRecover(GetViewport(), @event, FocusFirstButton);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_panel.Visible || GetTree().Paused || !@event.IsActionPressed("interact", allowEcho: false))
        {
            return;
        }

        // BR-B4: only a sign that is up opens, i.e. its Building has a next stage.
        var sign = _signs
            .Where(point => point.IsPlayerInRange)
            .Select(RebuildSignPoint.Of)
            .FirstOrDefault(point => point is not null && point.IsUp(_session));
        if (sign?.BuildingId is not { } building)
        {
            return;
        }

        _building = building;
        Open();
        GetViewport().SetInputAsHandled();
    }

    private void Open()
    {
        _messageLabel.Text = "";
        RefreshCloseLabel();
        GetTree().Paused = true;
        _panel.Visible = true;
        if (RebuildRows() is not null)
        {
            FocusFirstButton();
        }
    }

    private void Close()
    {
        _panel.Visible = false;
        GetViewport().GuiReleaseFocus();
        GetTree().Paused = false;
    }

    /// <summary>Redraws the title, the Unlocks line and the rows from the session; closes when the sign is gone.</summary>
    private RebuildSignView? RebuildRows()
    {
        foreach (var child in _items.GetChildren())
        {
            _items.RemoveChild(child);
            child.QueueFree();
        }

        var view = _session!.RebuildSign(_building);
        if (view is null)
        {
            Close();
            return null;
        }

        _titleLabel.Text = RebuildText.Title(view.Building, view.Stage);
        _unlocksLabel.Text = view.Unlocks;
        for (var index = 0; index < view.Lines.Count; index++)
        {
            var line = view.Lines[index];
            var row = new HBoxContainer { Name = $"Row{index}" };
            row.AddThemeConstantOverride("separation", 16);

            var label = new Label
            {
                Name = "Label",
                Text = RebuildText.Row(line),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Style(label);
            row.AddChild(label);

            var button = new Button
            {
                Name = "Deliver",
                Text = line.IsGold ? RebuildText.DeliverGoldButton : RebuildText.DeliverButton,
                FocusMode = Control.FocusModeEnum.All,
                Disabled = line.Complete,
            };
            Style(button);
            var rowIndex = index;
            button.Pressed += () => Deliver(line, rowIndex);
            row.AddChild(button);

            _items.AddChild(row);
        }

        return view;
    }

    private void Deliver(RebuildLine line, int rowIndex)
    {
        var result = line.Material is { } material
            ? _session!.Deliver(_building, material)
            : _session!.DeliverGold(_building);
        _messageLabel.Text = result.Rejection is { } rejection
            ? RebuildText.Rejection(rejection)
            : RebuildText.AfterDeliver(line, result.Delivered);

        if (RebuildRows() is null)
        {
            return;
        }

        // Keep focus on the row just pressed while it can still be pressed, else the first one that can.
        var buttons = DeliverButtons();
        if (rowIndex < buttons.Count && !buttons[rowIndex].Disabled)
        {
            buttons[rowIndex].GrabFocus();
        }
        else
        {
            FocusFirstButton();
        }
    }

    private List<Button> DeliverButtons() =>
        _items.GetChildren().OfType<HBoxContainer>().Select(row => row.GetNode<Button>("Deliver")).ToList();

    private void FocusFirstButton()
    {
        // The first Deliver button that can be pressed takes focus, so a controller can move with ui_up / ui_down and
        // deliver with ui_accept; with every line complete, the Close button.
        var first = DeliverButtons().FirstOrDefault(button => !button.Disabled) ?? _closeButton;
        first.GrabFocus();
    }

    private void RefreshCloseLabel()
    {
        // Refreshed on every device change, open or not, so the label is right whenever the panel shows.
        _closeButton.Text = InputGlyphs.CloseLabel(LastInputDevice.DeviceOf(_device));
    }

    /// <summary>The HUD's label style: font size 22 with a black outline of 4, as the Close button in the scene.</summary>
    private static void Style(Control control)
    {
        control.AddThemeColorOverride("font_outline_color", Colors.Black);
        control.AddThemeConstantOverride("outline_size", 4);
        control.AddThemeFontSizeOverride("font_size", 22);
    }
}
