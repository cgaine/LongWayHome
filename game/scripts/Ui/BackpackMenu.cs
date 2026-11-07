#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The Backpack screen (#131). On an <c>open_menu</c> press (not repeated while held) while the tree is not already
/// paused, it pauses the tree (which stops the day clock, BR-G2) and shows a centred "Backpack" panel: one button per
/// Slot of <see cref="GameSession.BackpackView"/> in a six-column grid, slot 0 top left, so 12, 24 or 36 Slots give 2,
/// 4 or 6 rows. Each filled button shows the Slot's icon and "× &lt;n&gt;", or the tool's name; empty ones show
/// empty. The hotbar row's buttons have an ember frame and a <see cref="BackpackText.HotbarLabel"/> label beside
/// the row (BR-K5). Slot 0 takes focus, so <c>ui_up/down/left/right</c> move between Slots, and <c>ui_accept</c> or a
/// click picks a Slot up (it shows dimmed with a light frame) or puts it down onto another Slot through
/// <see cref="GameSession.MoveSlot"/>, which decides the move, merge or swap; focus stays on the target. Pressing the
/// held Slot again, or <c>ui_cancel</c> (or <c>pause</c>) while holding, puts it back. With nothing held,
/// <c>ui_cancel</c>, <c>pause</c>, <c>open_menu</c> or the Close button (labelled <see cref="InputGlyphs.CloseLabel"/>)
/// closes the panel and resumes the tree; <c>open_menu</c> while holding also cancels the hold and closes. Those
/// presses are handled in <c>_Input</c> while open, so the <see cref="PauseMenu"/> never also toggles. The line under
/// the grid is <see cref="BackpackText.Hint"/>, and the glyphs follow <see cref="LastInputDevice"/>. When nothing has
/// focus, a navigation or accept press gives slot 0 focus back through <see cref="MenuFocusRecovery"/>. Always
/// processes. Holds no rules.
/// </summary>
public partial class BackpackMenu : CanvasLayer
{
    private const int SlotIconSize = 32;

    private const int SlotWidth = 156;

    private const int SlotHeight = 64;

    private const int RowLabelWidth = 96;

    private const int FrameWidth = 3;

    private static readonly Color SlotBackground = new(0, 0, 0, 0.55f);

    private static readonly Color SlotHoverBackground = new(0.2f, 0.2f, 0.2f, 0.7f);

    private static readonly Color EmberOrange = new("#E9773A");

    private static readonly Color HeldFrame = new(1, 1, 1);

    private static readonly Color HeldModulate = new(1, 1, 1, 0.5f);

    private readonly List<Button> _slotButtons = [];

    private GameSession? _session;

    private LastInputDevice? _device;

    private Control _panel = null!;

    private VBoxContainer _rowLabels = null!;

    private GridContainer _slots = null!;

    private Label _hintLabel = null!;

    private Button _closeButton = null!;

    private (StyleBoxFlat Normal, StyleBoxFlat Hover) _plainStyles;

    private (StyleBoxFlat Normal, StyleBoxFlat Hover) _hotbarStyles;

    private (StyleBoxFlat Normal, StyleBoxFlat Hover) _heldStyles;

    /// <summary>The index of the picked-up Slot, or null when nothing is held.</summary>
    private int? _held;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _plainStyles = SlotStyles(frame: null);
        _hotbarStyles = SlotStyles(EmberOrange);
        _heldStyles = SlotStyles(HeldFrame);
        _panel = GetNode<Control>("Panel");
        _rowLabels = GetNode<VBoxContainer>("Panel/Rows/Grid/RowLabels");
        _slots = GetNode<GridContainer>("Panel/Rows/Grid/Slots");
        _hintLabel = GetNode<Label>("Panel/Rows/HintLabel");
        _closeButton = GetNode<Button>("Panel/Rows/CloseButton");
        _panel.Visible = false;
        _closeButton.Pressed += Close;

        _device = GetNodeOrNull<LastInputDevice>(LastInputDevice.Path);
        if (_device is not null)
        {
            _device.DeviceChanged += RefreshGlyphs;
        }

        RefreshGlyphs();

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
        }
    }

    public override void _ExitTree()
    {
        if (_device is not null && IsInstanceValid(_device))
        {
            _device.DeviceChanged -= RefreshGlyphs;
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

        if (@event.IsActionPressed("open_menu", allowEcho: false))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Esc is both ui_cancel and pause on the keyboard, so both put a held Slot back first, then close.
        if (@event.IsActionPressed("ui_cancel", allowEcho: false)
            || @event.IsActionPressed("pause", allowEcho: false))
        {
            if (_held is not null)
            {
                CancelHold();
            }
            else
            {
                Close();
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        MenuFocusRecovery.TryRecover(GetViewport(), @event, FocusFirstSlot);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_panel.Visible
            && !GetTree().Paused
            && @event.IsActionPressed("open_menu", allowEcho: false))
        {
            Open();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Open()
    {
        _held = null;

        // Rebuilt on every open, so a changed hotbar row or a larger Backpack shows.
        RebuildSlots();
        RefreshGlyphs();
        GetTree().Paused = true;
        _panel.Visible = true;
        FocusFirstSlot();
    }

    private void Close()
    {
        // Closing while holding puts the Slot back: nothing moved.
        _held = null;
        _panel.Visible = false;
        GetViewport().GuiReleaseFocus();
        GetTree().Paused = false;
    }

    private void RebuildSlots()
    {
        foreach (var child in _slots.GetChildren().Concat(_rowLabels.GetChildren()))
        {
            child.GetParent().RemoveChild(child);
            child.QueueFree();
        }

        _slotButtons.Clear();

        var view = _session!.BackpackView();
        foreach (var slot in view.Slots)
        {
            var index = slot.Index;
            var button = new Button
            {
                Name = $"Slot{index}",
                FocusMode = Control.FocusModeEnum.All,
                CustomMinimumSize = new Vector2(SlotWidth, SlotHeight),
                ExpandIcon = false,
            };
            button.AddThemeColorOverride("font_outline_color", Colors.Black);
            button.AddThemeConstantOverride("outline_size", 4);
            button.AddThemeFontSizeOverride("font_size", 22);
            button.AddThemeConstantOverride("icon_max_width", SlotIconSize);
            button.Pressed += () => OnSlotPressed(index);
            _slots.AddChild(button);
            _slotButtons.Add(button);
        }

        for (var row = 0; row < view.Capacity / _slots.Columns; row++)
        {
            var label = new Label
            {
                Text = row == view.HotbarRow ? BackpackText.HotbarLabel : "",
                CustomMinimumSize = new Vector2(RowLabelWidth, SlotHeight),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.AddThemeColorOverride("font_color", EmberOrange);
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 4);
            label.AddThemeFontSizeOverride("font_size", 22);
            _rowLabels.AddChild(label);
        }

        RefreshSlots(view);
    }

    /// <summary>Shows each Slot's contents, the hotbar frame and the lifted look of the held Slot.</summary>
    private void RefreshSlots(BackpackView view)
    {
        var hotbarStart = view.HotbarRow * _slots.Columns;
        foreach (var slot in view.Slots)
        {
            var button = _slotButtons[slot.Index];
            button.Icon = slot.IconPath is { } path ? IconTextures.Get(path) : null;
            button.Text = slot.Item is null ? "" : slot.IconPath is null ? slot.Label : $"× {slot.Quantity}";
            button.TooltipText = slot.Label;

            var held = slot.Index == _held;
            var inHotbar = slot.Index >= hotbarStart && slot.Index < hotbarStart + _slots.Columns;
            var (normal, hover) = held ? _heldStyles : inHotbar ? _hotbarStyles : _plainStyles;
            button.AddThemeStyleboxOverride("normal", normal);
            button.AddThemeStyleboxOverride("hover", hover);
            button.AddThemeStyleboxOverride("pressed", hover);
            button.Modulate = held ? HeldModulate : Colors.White;
        }

        RefreshGlyphs();
    }

    private void OnSlotPressed(int index)
    {
        if (_held is not { } from)
        {
            // Only a filled Slot can be picked up; an empty one has nothing to lift.
            if (_session!.BackpackView().Slots[index].Item is not null)
            {
                _held = index;
                RefreshSlots(_session.BackpackView());
            }

            return;
        }

        _held = null;
        if (from != index)
        {
            // The session decides the move, merge or swap (BR-K2, BR-K6) and raises InventoryChanged for the HUD.
            _session!.MoveSlot(from, index);
        }

        RefreshSlots(_session!.BackpackView());
        _slotButtons[index].GrabFocus();
    }

    private void CancelHold()
    {
        if (_held is not { } held)
        {
            return;
        }

        _held = null;
        RefreshSlots(_session!.BackpackView());
        _slotButtons[held].GrabFocus();
    }

    private void FocusFirstSlot()
    {
        // Slot 0 takes focus so a controller can move with the D-pad and pick up with ui_accept.
        Control first = _slotButtons.Count > 0 ? _slotButtons[0] : _closeButton;
        first.GrabFocus();
    }

    private void RefreshGlyphs()
    {
        // Refreshed on every device change, open or not, so the labels are right whenever the panel shows.
        var device = LastInputDevice.DeviceOf(_device);
        _closeButton.Text = InputGlyphs.CloseLabel(device);
        _hintLabel.Text = BackpackText.Hint(_held is not null, device);
    }

    private static (StyleBoxFlat Normal, StyleBoxFlat Hover) SlotStyles(Color? frame) =>
        (SlotStyle(SlotBackground, frame), SlotStyle(SlotHoverBackground, frame));

    private static StyleBoxFlat SlotStyle(Color background, Color? frame)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = frame ?? background,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
        style.SetBorderWidthAll(frame is null ? 0 : FrameWidth);
        style.SetCornerRadiusAll(4);
        return style;
    }
}
