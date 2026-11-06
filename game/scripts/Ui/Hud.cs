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
/// range of the pond's <see cref="InteractionPoint"/>; its glyph follows <see cref="LastInputDevice"/>. Holds no rules.
/// </summary>
public partial class Hud : CanvasLayer
{
    private const string PondPointId = "pond";

    private GameSession? _session;

    private HeldItemSelection? _selection;

    private InteractionPoint? _pond;

    private LastInputDevice? _device;

    private ProgressBar _energyBar = null!;

    private Label _energyLabel = null!;

    private Label _canLabel = null!;

    private Label _heldLabel = null!;

    private Label _refillHint = null!;

    public override void _Ready()
    {
        _energyBar = GetNode<ProgressBar>("Panel/Rows/EnergyBar");
        _energyLabel = GetNode<Label>("Panel/Rows/EnergyLabel");
        _canLabel = GetNode<Label>("Panel/Rows/CanLabel");
        _heldLabel = GetNode<Label>("Panel/Rows/HeldLabel");
        _refillHint = GetNode<Label>("RefillHint");
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
        if (_selection is not null)
        {
            _selection.SelectionChanged += Refresh;
        }

        _device = GetNodeOrNull<LastInputDevice>(LastInputDevice.Path);
        if (_device is not null)
        {
            _device.DeviceChanged += RefreshRefillHint;
        }

        RefreshRefillHint();

        _pond = FindPoint(GetTree().Root, PondPointId);
        if (_pond is not null)
        {
            _pond.PlayerEntered += ShowRefillHint;
            _pond.PlayerExited += HideRefillHint;
            _refillHint.Visible = _pond.IsPlayerInRange;
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.PlayerChanged -= Refresh;
            _session.InventoryChanged -= Refresh;
        }

        if (_selection is not null)
        {
            _selection.SelectionChanged -= Refresh;
        }

        if (_device is not null && IsInstanceValid(_device))
        {
            _device.DeviceChanged -= RefreshRefillHint;
        }

        if (_pond is not null && IsInstanceValid(_pond))
        {
            _pond.PlayerEntered -= ShowRefillHint;
            _pond.PlayerExited -= HideRefillHint;
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

    private void RefreshRefillHint()
    {
        var glyph = _device?.GlyphFor("interact") ?? LastInputDevice.GlyphFor("interact", InputDevice.KeyboardMouse);
        _refillHint.Text = $"{glyph} Refill can";
    }

    private void Refresh()
    {
        var player = _session!.Game.Player;
        _energyBar.Value = player.Energy;
        _energyLabel.Text = $"Energy {player.Energy} / {PlayerState.MaxEnergy}";
        _canLabel.Text = $"Can {player.WateringCan.Charges} / {player.WateringCan.Capacity}";
        _heldLabel.Text = _session.HeldLabel(_selection?.Held ?? HeldItem.Of(ToolId.Hoe));
    }
}
