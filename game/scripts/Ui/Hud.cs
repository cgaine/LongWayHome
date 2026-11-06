#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Players;
using PlayerState = LongWayHome.Domain.Players.Player;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The heads-up display, top-left: the energy bar with "Energy &lt;n&gt; / 150", the Watering can's
/// "Can &lt;charges&gt; / &lt;capacity&gt;" and the held item's label (with the seed count when a seed is held).
/// Redraws when the session raises <see cref="GameSession.PlayerChanged"/> or
/// <see cref="GameSession.InventoryChanged"/> and when the Player's <see cref="HeldItemSelection"/> changes. Bottom
/// centre, above the <see cref="UsePrompt"/>, a "&lt;interact glyph&gt; Refill can" hint shows while the Player is in
/// range of the pond's <see cref="InteractionPoint"/>, and a "&lt;interact glyph&gt; Sleep" hint in the same place
/// while in range of the house door's, and a "&lt;interact glyph&gt; Ship crops" hint in the same place while in range
/// of the shipping bin's; their glyph follows <see cref="LastInputDevice"/>. Top right, the clock as
/// <see cref="ClockText"/> ("6:00 am"), redrawn on <see cref="GameSession.ClockChanged"/>, with the Day's weather as
/// <see cref="WeatherText"/> ("Clear", "Rain") under it and the date as <see cref="DateText"/> ("Spring 1, Year 1")
/// under that. Everything is redrawn on
/// <see cref="GameSession.NightCompleted"/>. Holds no rules.
/// </summary>
public partial class Hud : CanvasLayer
{
    private const string PondPointId = "pond";

    private const string DoorPointId = "door";

    private const string BinPointId = "bin";

    private GameSession? _session;

    private HeldItemSelection? _selection;

    private InteractionPoint? _pond;

    private InteractionPoint? _door;

    private InteractionPoint? _bin;

    private LastInputDevice? _device;

    private ProgressBar _energyBar = null!;

    private Label _energyLabel = null!;

    private Label _canLabel = null!;

    private Label _heldLabel = null!;

    private Label _refillHint = null!;

    private Label _sleepHint = null!;

    private Label _shipHint = null!;

    private Label _clockLabel = null!;

    private Label _weatherLabel = null!;

    private Label _dateLabel = null!;

    public override void _Ready()
    {
        _energyBar = GetNode<ProgressBar>("Panel/Rows/EnergyBar");
        _energyLabel = GetNode<Label>("Panel/Rows/EnergyLabel");
        _canLabel = GetNode<Label>("Panel/Rows/CanLabel");
        _heldLabel = GetNode<Label>("Panel/Rows/HeldLabel");
        _refillHint = GetNode<Label>("RefillHint");
        _sleepHint = GetNode<Label>("SleepHint");
        _shipHint = GetNode<Label>("ShipHint");
        _clockLabel = GetNode<Label>("ClockLabel");
        _weatherLabel = GetNode<Label>("WeatherLabel");
        _dateLabel = GetNode<Label>("DateLabel");
        _energyBar.MaxValue = PlayerState.MaxEnergy;

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

        _pond = FindPoint(GetTree().Root, PondPointId);
        if (_pond is not null)
        {
            _pond.PlayerEntered += ShowRefillHint;
            _pond.PlayerExited += HideRefillHint;
            _refillHint.Visible = _pond.IsPlayerInRange;
        }

        _door = FindPoint(GetTree().Root, DoorPointId);
        if (_door is not null)
        {
            _door.PlayerEntered += ShowSleepHint;
            _door.PlayerExited += HideSleepHint;
            _sleepHint.Visible = _door.IsPlayerInRange;
        }

        _bin = FindPoint(GetTree().Root, BinPointId);
        if (_bin is not null)
        {
            _bin.PlayerEntered += ShowShipHint;
            _bin.PlayerExited += HideShipHint;
            _shipHint.Visible = _bin.IsPlayerInRange;
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
    }

    private static InteractionPoint? FindPoint(Node node, string pointId)
    {
        if (node is InteractionPoint point && point.PointId == pointId)
        {
            return point;
        }

        foreach (var child in node.GetChildren())
        {
            if (FindPoint(child, pointId) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private void ShowRefillHint() => _refillHint.Visible = true;

    private void HideRefillHint() => _refillHint.Visible = false;

    private void ShowSleepHint() => _sleepHint.Visible = true;

    private void HideSleepHint() => _sleepHint.Visible = false;

    private void ShowShipHint() => _shipHint.Visible = true;

    private void HideShipHint() => _shipHint.Visible = false;

    private void RefreshHints()
    {
        var glyph = LastInputDevice.GlyphFor("interact", _device);
        _refillHint.Text = $"{glyph} Refill can";
        _sleepHint.Text = $"{glyph} Sleep";
        _shipHint.Text = $"{glyph} Ship crops";
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
        _heldLabel.Text = _session.HeldLabel(_selection?.Held ?? HeldItem.Of(ToolId.Hoe));
    }
}
