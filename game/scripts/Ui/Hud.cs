#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Domain.Residents;
using PlayerState = LongWayHome.Domain.Players.Player;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The heads-up display, top-left: the energy bar with "Energy &lt;n&gt; / 150", the Watering can's
/// "Can &lt;charges&gt; / &lt;capacity&gt;", the gold as <see cref="GoldText"/> ("Gold 100"), the carried crops as
/// <see cref="CropsText"/> ("Crops:" then each carried crop's icon and "Emberroot × 9", or "Crops: none") and the
/// held slot's label (with that slot's seed count when a seed is held; empty for an empty slot). Along the bottom
/// centre, the hotbar: the six slot frames of <see cref="GameSession.Hotbar"/>, the current Backpack row (#129), a seed
/// slot showing its <see cref="ItemIcons.SeedIcon"/> and a crop slot its <see cref="ItemIcons.CropIcon"/> left of its
/// label, an empty slot an empty frame of the same minimum size, the held position bordered in ember orange, and
/// beside them the <see cref="HotbarText.Row"/> label ("Row 1/2"). When those full slots would not fit the visible
/// width (less a <see cref="HotbarMargin"/> each side), the seed slots switch to <see cref="HotbarText.Compact"/>
/// labels ("Emberroot × 99", the packet icon saying it is a seed; the held label keeps the full name), re-checked when
/// the viewport resizes. Redraws when the session raises <see cref="GameSession.PlayerChanged"/> or
/// <see cref="GameSession.InventoryChanged"/> and when the Player's <see cref="HeldItemSelection"/> changes. Just
/// above the hotbar, a <see cref="InputGlyphs.ChangeItemHint"/> line ("[Q] [E] Change item" / "(LB) (RB) Change item")
/// names the buttons that move the held position, and beside it a <see cref="InputGlyphs.CycleRowHint"/> line
/// ("[X] Next row" / "(View) Next row") the button that cycles the hotbar row. Bottom centre, above the <see cref="UsePrompt"/>, a
/// "&lt;interact glyph&gt; Refill can" hint shows while the Player is in range of at least one water
/// <see cref="InteractionPoint"/> (the pond, a river or a lake point; it counts enters and exits, so walking along a
/// bank from one point's range into the next keeps it on), and a "&lt;interact glyph&gt; Sleep" hint in the same place
/// while in range of the house door's, and a "&lt;interact glyph&gt; Ship crops" hint in the same place while in range
/// of the shipping bin's, and a "&lt;interact glyph&gt; Shop" hint in the same place while in range of the shop's, and
/// a <see cref="DialogueText.TalkHint"/> ("[F] Talk") in the same place while in range of a present Resident's point
/// (#138), standing in for the Refill hint there since the press talks;
/// their glyph, and the change-item and next-row hints', follows <see cref="LastInputDevice"/>. Top right, the clock as
/// <see cref="ClockText"/> ("6:00 am"), redrawn on <see cref="GameSession.ClockChanged"/>, with the Day's weather as
/// <see cref="WeatherText"/> ("Clear", "Rain") under it and the date as <see cref="DateText"/> ("Spring 1, Year 1")
/// under that, and, only when the game is started with <c>--show-fps</c>, the <see cref="FpsCounter"/> under the date.
/// Everything is redrawn on
/// <see cref="GameSession.NightCompleted"/>. Holds no rules.
/// </summary>
public partial class Hud : CanvasLayer
{
    /// <summary>The ids of the water points the Refill hint shows over (WaterSource codes).</summary>
    private static readonly string[] WaterPointIds = ["pond", "river", "lake"];

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
    /// 36 px to that (#95), too wide for 1280 px, so in that case the seed slots fall back to compact labels
    /// (<see cref="HotbarText.Compact"/>) rather than smaller padding or text.</summary>
    private const int SlotPaddingX = 4;

    private const int SlotPaddingY = 4;

    /// <summary>The space the hotbar keeps clear at each side of the screen before it falls back to compact labels.</summary>
    private const int HotbarMargin = 16;

    private static readonly Color SlotBackground = new(0, 0, 0, 0.55f);

    /// <summary>The HUD accent, ember orange (docs/ART_DIRECTION.md).</summary>
    private static readonly Color EmberOrange = new("#E9773A");

    private GameSession? _session;

    private HeldItemSelection? _selection;

    private IReadOnlyList<InteractionPoint> _waterPoints = [];

    /// <summary>How many of <see cref="_waterPoints"/> the Player is in range of.</summary>
    private int _waterPointsInRange;

    private InteractionPoint? _door;

    private InteractionPoint? _bin;

    private InteractionPoint? _shop;

    /// <summary>The points of the present Residents in this area (#138), found by their <see cref="ResidentCodes"/>.</summary>
    private IReadOnlyList<InteractionPoint> _residentPoints = [];

    /// <summary>How many of <see cref="_residentPoints"/> the Player is in range of.</summary>
    private int _residentPointsInRange;

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

    /// <summary>"Row 1/2", the first child of the hotbar so the fit check counts it; kept across rebuilds.</summary>
    private Label _rowLabel = null!;

    private StyleBoxFlat _slotStyle = null!;

    private StyleBoxFlat _selectedSlotStyle = null!;

    private IReadOnlyList<HotbarSlot> _hotbarSlots = [];

    private Label _refillHint = null!;

    private Label _sleepHint = null!;

    private Label _shipHint = null!;

    private Label _shopHint = null!;

    private Label _talkHint = null!;

    private Label _changeItemHint = null!;

    private Label _cycleRowHint = null!;

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
        _rowLabel = GetNode<Label>("Hotbar/RowLabel");
        _slotStyle = SlotStyle(borderWidth: 0);
        _selectedSlotStyle = SlotStyle(borderWidth: SelectedBorderWidth);
        _refillHint = GetNode<Label>("RefillHint");
        _sleepHint = GetNode<Label>("SleepHint");
        _shipHint = GetNode<Label>("ShipHint");
        _shopHint = GetNode<Label>("ShopHint");
        _talkHint = GetNode<Label>("TalkHint");
        _changeItemHint = GetNode<Label>("ChangeItemHint");
        _cycleRowHint = GetNode<Label>("CycleRowHint");
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
        GetViewport().SizeChanged += RefitHotbar;

        _waterPoints = InteractionPoint.FindAll(GetTree().Root, WaterPointIds);
        foreach (var point in _waterPoints)
        {
            point.PlayerEntered += OnWaterPointEntered;
            point.PlayerExited += OnWaterPointExited;
            if (point.IsPlayerInRange)
            {
                _waterPointsInRange++;
            }
        }

        _refillHint.Visible = _waterPointsInRange > 0;

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

        // #138: the Talk hint over every present Resident's point (BR-R1); ResidentView names only those points.
        var residentCodes = Enum.GetValues<ResidentId>()
            .Where(_session.Game.IsPresent)
            .Select(ResidentCodes.Code)
            .ToArray();
        _residentPoints = InteractionPoint.FindAll(GetTree().Root, residentCodes);
        foreach (var point in _residentPoints)
        {
            point.PlayerEntered += OnResidentPointEntered;
            point.PlayerExited += OnResidentPointExited;
            if (point.IsPlayerInRange)
            {
                OnResidentPointEntered();
            }
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
            // Connected in _Ready only once the session exists.
            GetViewport().SizeChanged -= RefitHotbar;
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

        foreach (var point in _waterPoints)
        {
            if (IsInstanceValid(point))
            {
                point.PlayerEntered -= OnWaterPointEntered;
                point.PlayerExited -= OnWaterPointExited;
            }
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

        foreach (var point in _residentPoints)
        {
            if (IsInstanceValid(point))
            {
                point.PlayerEntered -= OnResidentPointEntered;
                point.PlayerExited -= OnResidentPointExited;
            }
        }
    }

    private void OnWaterPointEntered()
    {
        _waterPointsInRange++;
        ShowRefillHint();
    }

    private void OnWaterPointExited()
    {
        if (_waterPointsInRange > 0)
        {
            _waterPointsInRange--;
        }

        if (_waterPointsInRange == 0)
        {
            HideRefillHint();
        }
    }

    /// <summary>Near a Resident, interact talks rather than refills (their point is later in the tree, so it handles
    /// the press first), so the Talk hint stands in for the Refill hint.</summary>
    private void ShowRefillHint() => _refillHint.Visible = _residentPointsInRange == 0;

    private void HideRefillHint() => _refillHint.Visible = false;

    private void ShowSleepHint() => _sleepHint.Visible = true;

    private void HideSleepHint() => _sleepHint.Visible = false;

    private void ShowShipHint() => _shipHint.Visible = true;

    private void HideShipHint() => _shipHint.Visible = false;

    private void ShowShopHint() => _shopHint.Visible = true;

    private void OnResidentPointEntered()
    {
        _residentPointsInRange++;
        _talkHint.Visible = true;
        _refillHint.Visible = false;
    }

    private void OnResidentPointExited()
    {
        if (_residentPointsInRange > 0)
        {
            _residentPointsInRange--;
        }

        if (_residentPointsInRange == 0)
        {
            _talkHint.Visible = false;
            _refillHint.Visible = _waterPointsInRange > 0;
        }
    }

    private void HideShopHint() => _shopHint.Visible = false;

    private void RefreshHints()
    {
        var glyph = LastInputDevice.GlyphFor("interact", _device);
        _refillHint.Text = $"{glyph} Refill can";
        _sleepHint.Text = $"{glyph} Sleep";
        _shipHint.Text = $"{glyph} Ship crops";
        _shopHint.Text = $"{glyph} Shop";
        var device = LastInputDevice.DeviceOf(_device);
        _talkHint.Text = DialogueText.TalkHint(device);
        _changeItemHint.Text = InputGlyphs.ChangeItemHint(device);
        _cycleRowHint.Text = InputGlyphs.CycleRowHint(device);
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
        var position = _selection?.Position ?? 0;
        var slots = _session.Hotbar(position);
        _heldLabel.Text = slots[position].Label;
        _rowLabel.Text = HotbarText.Row(player.Backpack.HotbarRow, player.Backpack.Capacity);
        RefreshHotbar(slots);
    }

    private void RefreshCrops(PlayerState player)
    {
        // "Crops:" then, per carried crop, its icon and "Emberroot × 9" (", " after all but the last). Rebuild the
        // entry nodes only when the entries change, as the hotbar does with its slots.
        var entries = CropsText.Entries(player.Backpack, _session!.Data.CropTypes);
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

    private void RefreshHotbar(IReadOnlyList<HotbarSlot> slots)
    {
        // Refresh runs on every energy and can change; rebuild the slot nodes only when the slots themselves change.
        if (slots.SequenceEqual(_hotbarSlots))
        {
            return;
        }

        _hotbarSlots = slots;
        RefitHotbar();
    }

    /// <summary>Builds the slots with their full labels, then with compact seed labels if those don't fit (#95).</summary>
    private void RefitHotbar()
    {
        BuildHotbar(compact: false);
        if (_hotbar.GetCombinedMinimumSize().X > GetViewport().GetVisibleRect().Size.X - (2 * HotbarMargin))
        {
            BuildHotbar(compact: true);
        }
    }

    private void BuildHotbar(bool compact)
    {
        foreach (var child in _hotbar.GetChildren())
        {
            if (child != _rowLabel)
            {
                _hotbar.RemoveChild(child);
                child.QueueFree();
            }
        }

        foreach (var slot in _hotbarSlots)
        {
            // Every slot frame is at least an icon's size, so an empty slot is an empty frame of the same size.
            var panel = new PanelContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(HotbarIconSize + (2 * SlotPaddingX), HotbarIconSize + (2 * SlotPaddingY)),
            };
            panel.AddThemeStyleboxOverride("panel", slot.Selected ? _selectedSlotStyle : _slotStyle);
            var text = compact && slot.Item is { Seed: not null } seedItem
                ? HotbarText.Compact(seedItem, slot.Quantity)
                : slot.Label;
            if (text.Length == 0)
            {
                _hotbar.AddChild(panel);
                continue;
            }

            var label = OutlinedLabel(text, SlotFontSize);
            if (SlotIcon(slot) is { } icon)
            {
                // A seed or crop slot: its icon left of the unchanged label. Tool slots stay text only.
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

    /// <summary>A seed slot's seed icon, a crop slot's crop icon, or null (tools and empty slots).</summary>
    private static string? SlotIcon(HotbarSlot slot) =>
        slot.Item is { } item
            ? ItemIcons.For(item)
            : slot.SlotItem is { Kind: ItemKind.Crop, CropTypeId: { } crop } ? ItemIcons.CropIcon(crop) : null;

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
