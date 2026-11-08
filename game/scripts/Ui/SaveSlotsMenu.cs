#nullable enable

using System;
using System.IO;
using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Application.Saves;
using LongWayHome.Game.Core;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The title screen, the script of <c>Main.tscn</c>. Under the "Long Way Home" heading it shows one of three views
/// in its button column, with a message line below:
/// <list type="bullet">
/// <item>The title view: Continue (only when <see cref="SaveSlotChoice.ContinueSlot"/> finds a slot), New game,
/// Load game (only when a slot is Saved) and Quit.</item>
/// <item>The slot list, in new or load mode: one button per slot labelled <see cref="SaveSlotText.Row"/>, then Back.
/// In load mode the slots <see cref="SaveSlotChoice.CanLoad"/> refuses are disabled. In new mode a slot that
/// <see cref="SaveSlotChoice.NeedsOverwriteConfirmation"/> opens the confirmation; an Empty one starts at once.</item>
/// <item>The confirmation: <see cref="SaveSlotText.OverwriteQuestion"/> with Overwrite and Cancel.</item>
/// </list>
/// Starting or loading goes through <see cref="GameRoot.StartNewGame"/> or <see cref="GameRoot.LoadGame"/>, then
/// changes to <c>Farm.tscn</c>. When either throws a save store, IO or access error the screen stays, rebuilds the
/// slot list and shows <see cref="SaveSlotText.LoadFailed"/> or <see cref="SaveSlotText.SaveFailed"/>. Opening a view
/// gives focus to its first enabled button, so <c>ui_up</c>/<c>ui_down</c> move and <c>ui_accept</c> presses; when
/// nothing has focus (e.g. a mouse click on empty space cleared it), a navigation or accept press gives that button
/// focus back through <see cref="MenuFocusRecovery"/> and is consumed. <c>ui_cancel</c> is Back in the slot list and Cancel in the confirmation. Every text is 22 px or larger with a
/// black outline of 4, as the shop menu. The first time it opens in a process started with <c>--art-sandbox</c>
/// (<see cref="DisplayProfile.OpensArtSandbox"/>) it changes to the art sandbox instead of showing a view; later
/// openings, such as the sandbox's way back, show the title view as usual. Holds no rules.
/// </summary>
public partial class SaveSlotsMenu : Control
{
    private const string FarmScene = AreaScenes.FarmScene;

    /// <summary>The art sandbox (#75), opened instead of the title view when the game starts with <c>--art-sandbox</c>.</summary>
    private const string ArtSandboxScene = "res://art/sandbox/ArtSandbox.tscn";

    /// <summary>
    /// Set once the title screen has gone to the art sandbox, so returning from it (whose pause opens this scene
    /// again, with the flag still on the command line) shows the title view instead of bouncing back.
    /// </summary>
    private static bool _artSandboxOpened;

    private enum View
    {
        Title,
        Slots,
        Confirm,
    }

    private enum SlotMode
    {
        New,
        Load,
    }

    private GameRoot _root = null!;

    private VBoxContainer _buttons = null!;

    private Label _messageLabel = null!;

    private View _view = View.Title;

    private SlotMode _mode = SlotMode.New;

    /// <summary>Set once a Game has started or loaded and the scene is changing, so no further press acts.</summary>
    private bool _leaving;

    public override void _Ready()
    {
        _buttons = GetNode<VBoxContainer>("Center/Rows/Buttons");
        _messageLabel = GetNode<Label>("Center/Rows/MessageLabel");
        _root = GetNode<GameRoot>("/root/GameRoot");
        if (_root.Session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
            return;
        }

        if (!_artSandboxOpened && DisplayProfile.OpensArtSandbox(OS.GetCmdlineUserArgs()))
        {
            _artSandboxOpened = true;
            _leaving = true;
            // Deferred: the scene tree cannot change scene while this one is still being added.
            Callable.From(OpenArtSandbox).CallDeferred();
            return;
        }

        ShowTitle();
    }

    private void OpenArtSandbox()
    {
        var error = GetTree().ChangeSceneToFile(ArtSandboxScene);
        if (error != Error.Ok)
        {
            // The sandbox could not be opened: show the title screen so the Player can still play or quit.
            _leaving = false;
            GD.PushError($"Could not open {ArtSandboxScene}: {error}");
            ShowTitle();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_leaving)
        {
            MenuFocusRecovery.TryRecover(GetViewport(), @event, FocusFirstEnabledButton);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_leaving || !@event.IsActionPressed("ui_cancel", allowEcho: false))
        {
            return;
        }

        switch (_view)
        {
            case View.Slots:
                ShowTitle();
                GetViewport().SetInputAsHandled();
                break;
            case View.Confirm:
                ShowSlots(_mode);
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void ShowTitle(string message = "")
    {
        _view = View.Title;
        var slots = _root.SaveSlots();
        ClearButtons();

        if (SaveSlotChoice.ContinueSlot(slots) is { } continueSlot)
        {
            AddButton("Continue", () => Load(continueSlot, failedView: View.Title));
        }

        AddButton("New game", () => ShowSlots(SlotMode.New));
        if (slots.Any(SaveSlotChoice.CanLoad))
        {
            AddButton("Load game", () => ShowSlots(SlotMode.Load));
        }

        AddButton("Quit", () => GetTree().Quit());
        Open(message);
    }

    private void ShowSlots(SlotMode mode, string message = "")
    {
        _view = View.Slots;
        _mode = mode;
        var slots = _root.SaveSlots();
        ClearButtons();

        foreach (var slot in slots)
        {
            var row = AddButton(SaveSlotText.Row(slot), () => ChooseSlot(slot));
            row.Disabled = mode == SlotMode.Load && !SaveSlotChoice.CanLoad(slot);
        }

        AddButton("Back", () => ShowTitle());
        Open(message);
    }

    private void ShowConfirm(int slot)
    {
        _view = View.Confirm;
        ClearButtons();
        _buttons.AddChild(StyledLabel(SaveSlotText.OverwriteQuestion(slot)));
        AddButton("Overwrite", () => StartNew(slot));
        AddButton("Cancel", () => ShowSlots(SlotMode.New));
        Open("");
    }

    private void ChooseSlot(SaveSlotInfo slot)
    {
        if (_mode == SlotMode.Load)
        {
            Load(slot.Slot, failedView: View.Slots);
        }
        else if (SaveSlotChoice.NeedsOverwriteConfirmation(slot))
        {
            ShowConfirm(slot.Slot);
        }
        else
        {
            StartNew(slot.Slot);
        }
    }

    private void StartNew(int slot)
    {
        if (TryRun(() => _root.StartNewGame(slot)))
        {
            return;
        }

        ShowSlots(SlotMode.New, SaveSlotText.SaveFailed);
    }

    private void Load(int slot, View failedView)
    {
        if (TryRun(() => _root.LoadGame(slot)))
        {
            return;
        }

        if (failedView == View.Title)
        {
            ShowTitle(SaveSlotText.LoadFailed);
        }
        else
        {
            ShowSlots(SlotMode.Load, SaveSlotText.LoadFailed);
        }
    }

    /// <summary>Runs the start or load and changes to the farm; false when the store failed.</summary>
    private bool TryRun(Action startOrLoad)
    {
        if (_leaving)
        {
            return true;
        }

        try
        {
            startOrLoad();
        }
        catch (Exception exception) when (exception is SaveStoreException or IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Save slot failed: {exception.Message}");
            return false;
        }

        _leaving = true;
        var error = GetTree().ChangeSceneToFile(FarmScene);
        if (error != Error.Ok)
        {
            // The farm could not be opened: stay on the title screen so the Player can still act or quit.
            _leaving = false;
            GD.PushError($"Could not open {FarmScene}: {error}");
        }

        return true;
    }

    private void Open(string message)
    {
        _messageLabel.Text = message;
        FocusFirstEnabledButton();
    }

    private void FocusFirstEnabledButton()
    {
        var first = _buttons.GetChildren().OfType<Button>().FirstOrDefault(button => !button.Disabled);
        first?.GrabFocus();
    }

    private void ClearButtons()
    {
        foreach (var child in _buttons.GetChildren())
        {
            _buttons.RemoveChild(child);
            child.QueueFree();
        }
    }

    private Button AddButton(string text, Action pressed)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.All };
        Style(button);
        button.Pressed += pressed;
        _buttons.AddChild(button);
        return button;
    }

    private static Label StyledLabel(string text)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        Style(label);
        return label;
    }

    /// <summary>The shop menu's text style: font size 22 with a black outline of 4.</summary>
    private static void Style(Control control)
    {
        control.AddThemeColorOverride("font_outline_color", Colors.Black);
        control.AddThemeConstantOverride("outline_size", 4);
        control.AddThemeFontSizeOverride("font_size", 22);
    }
}
