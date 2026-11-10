#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Farming;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The Chest screen (#164), built like <see cref="BackpackMenu"/>. On an <c>interact</c> press (not repeated while
/// held) on the farm while the tree is not paused, the Player's <see cref="ToolTargeting"/> tile holds a chest
/// (<see cref="GameSession.ChestAt"/>) and no <see cref="InteractionPoint"/> is in range (a point's press wins), it
/// pauses the tree and shows a centred panel: the "Chest" grid of the chest's 36 slots (6 columns x 6 rows) above the
/// "Backpack" grid of <see cref="GameSession.BackpackView"/>, each filled button showing the slot's icon and
/// "× &lt;n&gt;", or the tool's name. <c>ui_accept</c> or a click on a filled slot moves its whole stack to the other
/// side through <see cref="GameSession.StoreInChest"/> or <see cref="GameSession.TakeFromChest"/>, which decide what
/// fits (a tool shows but never moves); the line under the grids shows <see cref="ChestText.Reason"/> for a refused
/// move, else <see cref="ChestText.Hint"/>. Chest slot 0 takes focus. <c>ui_cancel</c>, <c>pause</c>,
/// <c>open_menu</c> or the Close button (labelled <see cref="InputGlyphs.CloseLabel"/>) closes it and resumes the
/// tree; those presses are handled in <c>_Input</c> while open, so no other menu also toggles. Glyphs follow
/// <see cref="LastInputDevice"/>, and focus is recovered through <see cref="MenuFocusRecovery"/>. Always processes.
/// Holds no rules.
/// </summary>
public partial class ChestMenu : CanvasLayer
{
    private const int SlotIconSize = 28;

    private const int SlotWidth = 132;

    private const int SlotHeight = 40;

    private static readonly Color SlotBackground = new(0, 0, 0, 0.55f);

    private static readonly Color SlotHoverBackground = new(0.2f, 0.2f, 0.2f, 0.7f);

    private readonly List<Button> _chestButtons = [];

    private readonly List<Button> _backpackButtons = [];

    private GameSession? _session;

    private LastInputDevice? _device;

    private Control _panel = null!;

    private GridContainer _chestSlots = null!;

    private GridContainer _backpackSlots = null!;

    private Label _hintLabel = null!;

    private Button _closeButton = null!;

    private StyleBoxFlat _normal = null!;

    private StyleBoxFlat _hover = null!;

    /// <summary>The open chest's Tile, or null while closed.</summary>
    private GridPosition? _chest;

    /// <summary>Why the last move was refused, or null.</summary>
    private string? _reason;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _normal = SlotStyle(SlotBackground);
        _hover = SlotStyle(SlotHoverBackground);
        _panel = GetNode<Control>("Panel");
        _chestSlots = GetNode<GridContainer>("Panel/Rows/ChestSlots");
        _backpackSlots = GetNode<GridContainer>("Panel/Rows/BackpackSlots");
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

        if (@event.IsActionPressed("open_menu", allowEcho: false)
            || @event.IsActionPressed("ui_cancel", allowEcho: false)
            || @event.IsActionPressed("pause", allowEcho: false))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        MenuFocusRecovery.TryRecover(GetViewport(), @event, FocusFirstSlot);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_panel.Visible
            || GetTree().Paused
            || !@event.IsActionPressed("interact", allowEcho: false))
        {
            return;
        }

        // A water point, the door, the bin, the shop or a Resident in range takes the press instead.
        if (InteractionPoint.AnyPlayerInRange(GetTree().Root))
        {
            return;
        }

        if (ToolTargeting.OfPlayer(GetTree())?.Target is { } target && _session!.ChestAt(target) is not null)
        {
            Open(target);
            GetViewport().SetInputAsHandled();
        }
    }

    private void Open(GridPosition chest)
    {
        _chest = chest;
        _reason = null;

        // Rebuilt on every open, so a larger Backpack shows.
        RebuildSlots();
        GetTree().Paused = true;
        _panel.Visible = true;
        FocusFirstSlot();
    }

    private void Close()
    {
        _chest = null;
        _panel.Visible = false;
        GetViewport().GuiReleaseFocus();
        GetTree().Paused = false;
    }

    private void RebuildSlots()
    {
        foreach (var child in _chestSlots.GetChildren().Concat(_backpackSlots.GetChildren()))
        {
            child.GetParent().RemoveChild(child);
            child.QueueFree();
        }

        _chestButtons.Clear();
        _backpackButtons.Clear();

        foreach (var slot in _session!.ChestAt(_chest!.Value)!.Slots)
        {
            var index = slot.Index;
            _chestButtons.Add(AddSlotButton(_chestSlots, $"Chest{index}", () => OnChestSlotPressed(index)));
        }

        foreach (var slot in _session.BackpackView().Slots)
        {
            var index = slot.Index;
            _backpackButtons.Add(AddSlotButton(_backpackSlots, $"Slot{index}", () => OnBackpackSlotPressed(index)));
        }

        RefreshSlots();
    }

    private Button AddSlotButton(GridContainer grid, string name, System.Action pressed)
    {
        var button = new Button
        {
            Name = name,
            FocusMode = Control.FocusModeEnum.All,
            CustomMinimumSize = new Vector2(SlotWidth, SlotHeight),
            ExpandIcon = false,
        };
        button.AddThemeColorOverride("font_outline_color", Colors.Black);
        button.AddThemeConstantOverride("outline_size", 4);
        button.AddThemeFontSizeOverride("font_size", 20);
        button.AddThemeConstantOverride("icon_max_width", SlotIconSize);
        button.AddThemeStyleboxOverride("normal", _normal);
        button.AddThemeStyleboxOverride("hover", _hover);
        button.AddThemeStyleboxOverride("pressed", _hover);
        button.Pressed += pressed;
        grid.AddChild(button);
        return button;
    }

    /// <summary>Shows each slot's contents on both sides.</summary>
    private void RefreshSlots()
    {
        if (_chest is not { } chest || _session!.ChestAt(chest) is not { } view)
        {
            // The chest is gone (it can't be picked up while open, but never draw from nothing).
            Close();
            return;
        }

        ShowSlots(_chestButtons, view.Slots);
        ShowSlots(_backpackButtons, _session.BackpackView().Slots);
        RefreshGlyphs();
    }

    private static void ShowSlots(List<Button> buttons, IReadOnlyList<BackpackViewSlot> slots)
    {
        foreach (var slot in slots)
        {
            if (slot.Index >= buttons.Count)
            {
                continue;
            }

            var button = buttons[slot.Index];
            button.Icon = slot.IconPath is { } path ? IconTextures.Get(path) : null;
            button.Text = slot.Item is null ? "" : slot.IconPath is null ? slot.Label : $"× {slot.Quantity}";
            button.TooltipText = slot.Label;
        }
    }

    private void OnChestSlotPressed(int index)
    {
        // The session decides what fits (BR-K2, BR-K3) and raises InventoryChanged and PlacedObjectsChanged.
        Moved(_session!.TakeFromChest(_chest!.Value, index));
        _chestButtons[index].GrabFocus();
    }

    private void OnBackpackSlotPressed(int index)
    {
        // The session refuses a tool (BR-K6) and decides what fits (BR-K2).
        Moved(_session!.StoreInChest(_chest!.Value, index));
        _backpackButtons[index].GrabFocus();
    }

    private void Moved(ActionResult result)
    {
        _reason = ChestText.Reason(result);
        RefreshSlots();
    }

    private void FocusFirstSlot()
    {
        Control first = _chestButtons.Count > 0 ? _chestButtons[0] : _closeButton;
        first.GrabFocus();
    }

    private void RefreshGlyphs()
    {
        // Refreshed on every device change, open or not, so the labels are right whenever the panel shows.
        var device = LastInputDevice.DeviceOf(_device);
        _closeButton.Text = InputGlyphs.CloseLabel(device);
        _hintLabel.Text = _reason ?? ChestText.Hint(device);
    }

    private static StyleBoxFlat SlotStyle(Color background)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
        style.SetCornerRadiusAll(4);
        return style;
    }
}
