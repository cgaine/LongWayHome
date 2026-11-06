#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Core;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The shop menu. On an <c>interact</c> press (not repeated while held) while the Player is in range of the shop's
/// <see cref="InteractionPoint"/> and the tree is not already paused, it pauses the tree (which stops the day clock,
/// BR-G2) and shows a centred "Shop" panel: "Gold &lt;n&gt;", one button per listing in
/// <see cref="GameSession.ShopOffers"/> labelled <see cref="ShopText.Row"/> (the seed rows, then the Watering can
/// upgrade while it is offered; a <see cref="ShopText.NoSeeds"/> line stands in for the seed rows when there are none),
/// a Close button labelled <see cref="InputGlyphs.CloseLabel"/> ("[Esc] Close" / "(B) Close", following
/// <see cref="LastInputDevice"/> while open), and a message line. The first button takes focus, so
/// <c>ui_up</c>/<c>ui_down</c> move between rows and <c>ui_accept</c> presses one; when nothing has focus (e.g. a
/// mouse click on empty panel space cleared it), a navigation or accept press gives the first button focus back
/// through <see cref="MenuFocusRecovery"/> and is consumed, without touching the pause (BR-G2). Pressing a listing asks the session to buy one; a rejection shows
/// <see cref="ShopText.Rejection"/>, and buying the Watering can upgrade rebuilds the rows so its row goes. The Close
/// button, or a press of <c>ui_cancel</c>, <c>interact</c> or <c>pause</c>, closes the panel and resumes the tree;
/// those presses are handled in <c>_Input</c> while open, so the <see cref="PauseMenu"/> never also toggles. Opening is in <c>_UnhandledInput</c>, after <c>_Input</c>, so the
/// press that opens the menu cannot also close it, and it sits before <see cref="NightTransition"/> in the Farm
/// scene so the night fade still swallows <c>interact</c> first. Always processes. Holds no rules: what is offered,
/// the prices and whether a purchase happens are decided by <see cref="GameSession"/> (BR-L1, BR-L2).
/// </summary>
public partial class ShopMenu : CanvasLayer
{
    private const string ShopPointId = "shop";

    private GameSession? _session;

    private LastInputDevice? _device;

    private InteractionPoint? _shop;

    private Control _panel = null!;

    private Label _goldLabel = null!;

    private VBoxContainer _items = null!;

    private Button _closeButton = null!;

    private Label _messageLabel = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNode<Control>("Panel");
        _goldLabel = GetNode<Label>("Panel/Rows/GoldLabel");
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
        _shop = InteractionPoint.Find(GetTree().Root, ShopPointId);
        if (_session is null || _shop is null)
        {
            // GameRoot has already reported the data errors and is quitting, or this scene has no shop.
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
        if (!_panel.Visible
            && !GetTree().Paused
            && @event.IsActionPressed("interact", allowEcho: false)
            && _shop!.IsPlayerInRange)
        {
            Open();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Open()
    {
        RebuildItems();
        RefreshGold();
        _messageLabel.Text = "";
        RefreshCloseLabel();
        GetTree().Paused = true;
        _panel.Visible = true;
        FocusFirstButton();
    }

    private void Close()
    {
        _panel.Visible = false;
        GetViewport().GuiReleaseFocus();
        GetTree().Paused = false;
    }

    private void RebuildItems()
    {
        foreach (var child in _items.GetChildren())
        {
            _items.RemoveChild(child);
            child.QueueFree();
        }

        var offers = _session!.ShopOffers();
        if (!offers.Any(listing => listing.Kind == ShopItemKind.Seed))
        {
            // Offers list the seeds first, so this line sits above the Can upgrade row (BR-L1).
            _items.AddChild(StyledLabel(ShopText.NoSeeds));
        }

        foreach (var listing in offers)
        {
            var row = StyledButton(ShopText.Row(listing, _session.Data));
            row.Pressed += () => Buy(listing);
            _items.AddChild(row);
        }
    }

    private void Buy(ShopListing listing)
    {
        var result = _session!.Buy(listing.Id);
        if (result.Rejection is { } rejection)
        {
            _messageLabel.Text = ShopText.Rejection(rejection);
            return;
        }

        RefreshGold();
        _messageLabel.Text = "";

        // BR-L1: the Can upgrade is offered only until bought, so its row goes once it is.
        if (listing.Kind == ShopItemKind.CanUpgrade)
        {
            RebuildItems();
            FocusFirstButton();
        }
    }

    private void FocusFirstButton()
    {
        // The first button takes focus (skipping the "No seeds" line) so a controller can move with ui_up / ui_down
        // and buy with ui_accept.
        var first = _items.GetChildren().OfType<Button>().FirstOrDefault() ?? _closeButton;
        first.GrabFocus();
    }

    private void RefreshCloseLabel()
    {
        // Refreshed on every device change, open or not, so the label is right whenever the panel shows.
        _closeButton.Text = InputGlyphs.CloseLabel(LastInputDevice.DeviceOf(_device));
    }

    private void RefreshGold() => _goldLabel.Text = ShopText.Gold(_session!.Game.Player.Gold.Value);

    private static Label StyledLabel(string text)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        Style(label);
        return label;
    }

    private static Button StyledButton(string text)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.All };
        Style(button);
        return button;
    }

    /// <summary>The HUD's label style: font size 22 with a black outline of 4, as the Close button in the scene.</summary>
    private static void Style(Control control)
    {
        control.AddThemeColorOverride("font_outline_color", Colors.Black);
        control.AddThemeConstantOverride("outline_size", 4);
        control.AddThemeFontSizeOverride("font_size", 22);
    }
}
