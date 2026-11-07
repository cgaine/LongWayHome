#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using PlayerState = LongWayHome.Domain.Players.Player;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The heads-up display, top-left: the energy bar with "Energy &lt;n&gt; / 150", the Watering can's
/// "Can &lt;charges&gt; / &lt;capacity&gt;", the gold as <see cref="GoldText"/> ("Gold 100"), the carried crops as
/// <see cref="CropsText"/> ("Crops:" then each carried crop's icon and "Emberroot × 9", or "Crops: none") and the
/// held item's label (with the seed count when a seed is held). Along the bottom centre, the hotbar: one slot per
/// entry of <see cref="GameSession.Hotbar"/>, a seed slot showing its <see cref="ItemIcons.SeedIcon"/> left of its
/// label, the selected one bordered in ember orange. Redraws when the session raises <see cref="GameSession.PlayerChanged"/> or
/// <see cref="GameSession.InventoryChanged"/> and when the Player's <see cref="HeldItemSelection"/> changes. Just
/// above the hotbar, a <see cref="InputGlyphs.ChangeItemHint"/> line ("[Q] [E] Change item" / "(LB) (RB) Change item")
/// names the buttons that cycle the held item. Bottom centre, above the <see cref="UsePrompt"/>, a
/// "&lt;interact glyph&gt; Refill can" hint shows while the Player is in range of the pond's
/// <see cref="InteractionPoint"/>, and a "&lt;interact glyph&gt; Sleep" hint in the same place
/// while in range of the house door's, and a "&lt;interact glyph&gt; Ship crops" hint in the same place while in range
/// of the shipping bin's, and a "&lt;interact glyph&gt; Shop" hint in the same place while in range of the shop's;
/// their glyph, and the change-item hint's, follows <see cref="LastInputDevice"/>. Top right, the clock as
/// <see cref="ClockText"/> ("6:00 am"), redrawn on <see cref="GameSession.ClockChanged"/>, with the Day's weather as
/// <see cref="WeatherText"/> ("Clear", "Rain") under it and the date as <see cref="DateText"/> ("Spring 1, Year 1")
/// under that, and, only when the game is started with <c>--show-fps</c>, the <see cref="FpsCounter"/> under the date.
/// Everything is redrawn on
/// <see cref="GameSession.NightCompleted"/>. Holds no rules.
/// </summary>
public partial class Hud : CanvasLayer
{
    private const string PondPointId = "pond";

    private const string DoorPointId = "door";

    private const string BinPointId = "bin";

    private const string ShopPointId = "shop";

    private const int SlotFontSize = 20;

    /// <summary>The crops row's font size, as the other HUD rows in Hud.tscn.</summary>
    private const int CropsFontSize = 22;

    private const int HotbarIconSize = 32;

    private const int CropIconSize = 24;

    private const int SelectedBorderWidth = 3;

    /// <summary>Horizontal slot padding: 4 px. Text only, the widest hotbar (both tools and five "Moonmelon seeds × 99"
    /// and the like) is 1274 px at font 20; each seed slot's <see cref="HotbarIconSize"/> icon and its 4 px gap add
    /// 36 px to that (#95), measured in the game.</summary>
    private const int SlotPaddingX = 4;

    private const int SlotPaddingY = 4;

    private static readonly Color SlotBackground = new(0, 0, 0, 0.55f);

    /// <summary>The HUD accent, ember orange (docs/ART_DIRECTION.md).</summary>
    private static readonly Color EmberOrange = new("#E9773A");

    private GameSession? _session;

    private HeldItemSelection? _selection;

    private InteractionPoint? _pond;

    private InteractionPoint? _door;

    private InteractionPoint? _bin;

    private InteractionPoint? _shop;

    private LastInputDevice? _device;

    private ProgressBar _energyBar = null!;

    private Label _energyLabel = null!;

    private Label _canLabel = null!;

    private Label _goldLabel = null!;

    private HBoxContainer _cropsRow = null!;

    private Label _cropsLabel = null!;

    private IReadOnlyList<(CropTypeId Crop, string Text)> _cropEntries = [];

    private Label _heldLabel = null!;

    private HBoxContainer _hotbar = null!;

    private StyleBoxFlat _slotStyle = null!;

    private StyleBoxFlat _selectedSlotStyle = null!;

    private IReadOnlyList<HotbarSlot> _hotbarSlots = [];

    private Label _refillHint = null!;

    private Label _sleepHint = null!;

    private Label _shipHint = null!;

    private Label _shopHint = null!;

    private Label _changeItemHint = null!;

    private Label _clockLabel = null!;

    private Label _weatherLabel = null!;

    private Label _dateLabel = null!;

    public override void _Ready()
    {
        _energyBar = GetNode<ProgressBar>("Panel/Rows/EnergyBar");
        _energyLabel = GetNode<Label>("Panel/Rows/EnergyLabel");
        _canLabel = GetNode<Label>("Panel/Rows/CanLabel");
        _goldLabel = GetNode<Label>("Panel/Rows/GoldLabel");
        _cropsRow = GetNode<HBoxContainer>("Panel/Rows/CropsRow");
        _cropsLabel = GetNode<Label>("Panel/Rows/CropsRow/CropsLabel");
        _heldLabel = GetNode<Label>("Panel/Rows/HeldLabel");
        _hotbar = GetNode<HBoxContainer>("Hotbar");
        _slotStyle = SlotStyle(borderWidth: 0);
        _selectedSlotStyle = SlotStyle(borderWidth: SelectedBorderWidth);
        _refillHint = GetNode<Label>("RefillHint");
        _sleepHint = GetNode<Label>("SleepHint");
        _shipHint = GetNode<Label>("ShipHint");
        _shopHint = GetNode<Label>("ShopHint");
        _changeItemHint = GetNode<Label>("ChangeItemHint");
        _clockLabel = GetNode<Label>("ClockLabel");
        _weatherLabel = GetNode<Label>("WeatherLabel");
        _dateLabel = GetNode<Label>("DateLabel");
        _energyBar.MaxValue = PlayerState.MaxEnergy;

        if (DisplayProfile.ShowFps(OS.GetCmdlineUserArgs()))
        {
            var fpsCounter = new FpsCounter();
            AddChild(fpsCounter);
            fpsCounter.PlaceUnder(_dateLabel);
        }

        _session = GetNode<GameRoot>("/root/GameRoot").Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            return;
        }

        _selection = GetTree().GetFirstNodeInGroup(PlayerController.Group)?
            .GetChildren()
            .OfType<HeldItemSelection>()
            .FirstOrDefault();

        _session.PlayerChanged += Refresh;
        _session.InventoryChanged += Refresh;
        _session.ClockChanged += RefreshClock;
        _session.NightCompleted += RefreshAll;
        if (_selection is not null)
        {
            _selection.SelectionChanged += Refresh;
        }

        _device = GetNodeOrNull<LastInputDevice>(LastInputDevice.Path);
        if (_device is not null)
        {
            _device.DeviceChanged += RefreshHints;
        }

        RefreshHints();

        _pond = InteractionPoint.Find(GetTree().Root, PondPointId);
        if (_pond is not null)
        {
            _pond.PlayerEntered += ShowRefillHint;
            _pond.PlayerExited += HideRefillHint;
            _refillHint.Visible = _pond.IsPlayerInRange;
        }

        _door = InteractionPoint.Find(GetTree().Root, DoorPointId);
        if (_door is not null)
        {
            _door.PlayerEntered += ShowSleepHint;
            _door.PlayerExited += HideSleepHint;
            _sleepHint.Visible = _door.IsPlayerInRange;
        }

        _bin = InteractionPoint.Find(GetTree().Root, BinPointId);
        if (_bin is not null)
        {
            _bin.PlayerEntered += ShowShipHint;
            _bin.PlayerExited += HideShipHint;
            _shipHint.Visible = _bin.IsPlayerInRange;
        }

        _shop = InteractionPoint.Find(GetTree().Root, ShopPointId);
        if (_shop is not null)
        {
            _shop.PlayerEntered += ShowShopHint;
            _shop.PlayerExited += HideShopHint;
            _shopHint.Visible = _shop.IsPlayerInRange;
        }

        Refresh();
        RefreshClock();
        RefreshWeather();
        RefreshDate();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.PlayerChanged -= Refresh;
            _session.InventoryChanged -= Refresh;
            _session.ClockChanged -= RefreshClock;
            _session.NightCompleted -= RefreshAll;
        }

        if (_selection is not null)
        {
            _selection.SelectionChanged -= Refresh;
        }

        if (_device is not null && IsInstanceValid(_device))
        {
            _device.DeviceChanged -= RefreshHints;
        }

        if (_pond is not null && IsInstanceValid(_pond))
        {
            _pond.PlayerEntered -= ShowRefillHint;
            _pond.PlayerExited -= HideRefillHint;
        }

        if (_door is not null && IsInstanceValid(_door))
        {
            _door.PlayerEntered -= ShowSleepHint;
            _door.PlayerExited -= HideSleepHint;
        }

        if (_bin is not null && IsInstanceValid(_bin))
        {
            _bin.PlayerEntered -= ShowShipHint;
            _bin.PlayerExited -= HideShipHint;
        }

        if (_shop is not null && IsInstanceValid(_shop))
        {
            _shop.PlayerEntered -= ShowShopHint;
            _shop.PlayerExited -= HideShopHint;
        }
    }

    private void ShowRefillHint() => _refillHint.Visible = true;

    private void HideRefillHint() => _refillHint.Visible = false;

    private void ShowSleepHint() => _sleepHint.Visible = true;

    private void HideSleepHint() => _sleepHint.Visible = false;

    private void ShowShipHint() => _shipHint.Visible = true;

    private void HideShipHint() => _shipHint.Visible = false;

    private void ShowShopHint() => _shopHint.Visible = true;

    private void HideShopHint() => _shopHint.Visible = false;

    private void RefreshHints()
    {
        var glyph = LastInputDevice.GlyphFor("interact", _device);
        _refillHint.Text = $"{glyph} Refill can";
        _sleepHint.Text = $"{glyph} Sleep";
        _shipHint.Text = $"{glyph} Ship crops";
        _shopHint.Text = $"{glyph} Shop";
        _changeItemHint.Text = InputGlyphs.ChangeItemHint(LastInputDevice.DeviceOf(_device));
    }

    private void RefreshAll()
    {
        Refresh();
        RefreshClock();
        RefreshWeather();
        RefreshDate();
    }

    private void RefreshDate() => _dateLabel.Text = DateText.For(_session!.Game.Date);

    private void RefreshWeather() => _weatherLabel.Text = WeatherText.For(_session!.Game.Weather);

    private void RefreshClock() => _clockLabel.Text = ClockText.For(_session!.Game.ClockMinutes);

    private void Refresh()
    {
        var player = _session!.Game.Player;
        _energyBar.Value = player.Energy;
        _energyLabel.Text = $"Energy {player.Energy} / {PlayerState.MaxEnergy}";
        _canLabel.Text = $"Can {player.WateringCan.Charges} / {player.WateringCan.Capacity}";
        _goldLabel.Text = GoldText.For(player.Gold.Value);
        RefreshCrops(player);
        var held = _selection?.Held ?? HeldItem.Of(ToolId.Hoe);
        _heldLabel.Text = _session.HeldLabel(held);
        RefreshHotbar(held);
    }

    private void RefreshCrops(PlayerState player)
    {
        // "Crops:" then, per carried crop, its icon and "Emberroot × 9" (", " after all but the last). Rebuild the
        // entry nodes only when the entries change, as the hotbar does with its slots.
        var entries = CropsText.Entries(player.Inventory.Stacks, _session!.Data.CropTypes);
        _cropsLabel.Text = CropsText.Heading(entries);
        if (entries.SequenceEqual(_cropEntries))
        {
            return;
        }

        _cropEntries = entries;
        foreach (var child in _cropsRow.GetChildren())
        {
            if (child != _cropsLabel)
            {
                _cropsRow.RemoveChild(child);
                child.QueueFree();
            }
        }

        for (var i = 0; i < entries.Count; i++)
        {
            var (crop, text) = entries[i];
            _cropsRow.AddChild(IconTextures.Rect(ItemIcons.CropIcon(crop), CropIconSize));
            var suffix = i < entries.Count - 1 ? CropsText.EntrySeparator : "";
            _cropsRow.AddChild(OutlinedLabel(text + suffix, CropsFontSize));
        }
    }

    private void RefreshHotbar(HeldItem held)
    {
        // Refresh runs on every energy and can change; rebuild the slot nodes only when the slots themselves change.
        var slots = _session!.Hotbar(held);
        if (slots.SequenceEqual(_hotbarSlots))
        {
            return;
        }

        _hotbarSlots = slots;
        foreach (var child in _hotbar.GetChildren())
        {
            _hotbar.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var slot in slots)
        {
            var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", slot.Selected ? _selectedSlotStyle : _slotStyle);
            var label = OutlinedLabel(slot.Label, SlotFontSize);
            if (ItemIcons.For(slot.Item) is { } icon)
            {
                // A seed slot: its seed icon left of the unchanged label. Tool slots stay text only.
                var content = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
                content.AddChild(IconTextures.Rect(icon, HotbarIconSize));
                content.AddChild(label);
                panel.AddChild(content);
            }
            else
            {
                panel.AddChild(label);
            }

            _hotbar.AddChild(panel);
        }
    }

    /// <summary>A HUD label: <paramref name="fontSize"/> with a black outline of 4, centred vertically on its row.</summary>
    private static Label OutlinedLabel(string text, int fontSize)
    {
        var label = new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeConstantOverride("outline_size", 4);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        return label;
    }

    private static StyleBoxFlat SlotStyle(int borderWidth)
    {
        var style = new StyleBoxFlat
        {
            BgColor = SlotBackground,
            BorderColor = EmberOrange,
            ContentMarginLeft = SlotPaddingX,
            ContentMarginRight = SlotPaddingX,
            ContentMarginTop = SlotPaddingY,
            ContentMarginBottom = SlotPaddingY,
        };
        style.SetBorderWidthAll(borderWidth);
        return style;
    }
}
